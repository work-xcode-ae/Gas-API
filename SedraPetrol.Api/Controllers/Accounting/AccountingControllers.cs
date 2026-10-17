using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Chart of accounts.</summary>
[Route("api/accounts")]
[PermissionModule("accounting")]
public class AccountsController : CrudController<Account>
{
    /// <summary>Chart of accounts as a tree.</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> Tree()
    {
        await LoadLookups();
        var all = await Db.Accounts.AsNoTracking().OrderBy(a => a.AccountCode).ToListAsync();
        object Node(Account a) => new
        {
            a.Id, a.AccountCode, name = Loc(a.NameEn, a.NameAr), a.NameEn, a.NameAr, type = Code(a.AccountTypeId), a.Level, a.IsGroup, a.IsPostable, a.IsActive,
            children = all.Where(c => c.ParentAccountId == a.Id).Select(Node).ToList(),
        };
        return Ok(all.Where(a => a.ParentAccountId == null).Select(Node));
    }

    protected override async Task OnCreatingAsync(Account e)
    {
        e.CompanyId = e.CompanyId == 0 ? Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync() : e.CompanyId;
        if (await Db.Accounts.AnyAsync(a => a.CompanyId == e.CompanyId && a.AccountCode == e.AccountCode)) throw new BusinessRuleException($"Account code {e.AccountCode} already exists.");
        if (e.ParentAccountId != null)
        {
            var parent = await Db.Accounts.FirstOrDefaultAsync(a => a.Id == e.ParentAccountId) ?? throw new NotFoundException("Account", e.ParentAccountId);
            e.Level = (short)((parent.Level ?? 1) + 1);
            e.AccountTypeId ??= parent.AccountTypeId;
        }
        else e.Level ??= 1;
        if (e.IsGroup) e.IsPostable = false;
    }

    protected override async Task OnDeletingAsync(Account entity)
    {
        if (await Db.JournalLines.AnyAsync(l => l.AccountId == entity.Id)) throw new BusinessRuleException("The account has journal lines; deactivate it instead.");
        if (await Db.Accounts.AnyAsync(a => a.ParentAccountId == entity.Id)) throw new BusinessRuleException("The account has sub-accounts.");
    }
}

[Route("api/account-mappings")]
[PermissionModule("accounting")]
public class AccountMappingsController : CrudController<AccountMapping> { }

/// <summary>Fiscal periods (monthly). Posting into a closed period is blocked.</summary>
[Route("api/fiscal-periods")]
[PermissionModule("accounting")]
public class FiscalPeriodsController : CrudController<FiscalPeriod>
{
    /// <summary>Create the 12 monthly periods of a year.</summary>
    [HttpPost("generate/{year:int}")]
    public async Task<IActionResult> Generate(int year)
    {
        var companyId = Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync();
        var created = 0;
        for (short m = 1; m <= 12; m++)
        {
            if (await Db.FiscalPeriods.AnyAsync(p => p.CompanyId == companyId && p.Year == year && p.PeriodNo == m)) continue;
            var start = new DateOnly(year, m, 1);
            Db.FiscalPeriods.Add(new FiscalPeriod { CompanyId = companyId, Year = (short)year, PeriodNo = m, StartDate = start, EndDate = start.AddMonths(1).AddDays(-1) });
            created++;
        }
        await Db.SaveChangesAsync();
        return Ok(new { created });
    }

    [HttpPost("{id:long}/close")]
    [RequirePermission("accounting.close_period")]
    public async Task<IActionResult> Close(long id)
    {
        var p = await Db.FiscalPeriods.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("FiscalPeriod", id);
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        if (await Db.JournalEntries.AnyAsync(j => j.FiscalPeriodId == id && j.StatusId == draft)) throw new BusinessRuleException("The period has draft journal entries.");
        p.IsClosed = true;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/reopen")]
    [RequirePermission("accounting.close_period")]
    public async Task<IActionResult> Reopen(long id)
    {
        var p = await Db.FiscalPeriods.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("FiscalPeriod", id);
        p.IsClosed = false;
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>Journal entries: list, detail, manual entries, post, reverse; trial balance and account ledger.</summary>
[Route("api/journal-entries")]
[PermissionModule("accounting")]
public class JournalEntriesController : ApiControllerBase
{
    public record LineInput(long AccountId, decimal Debit, decimal Credit, string? Description, long? BuildingId, long? UnitId, long? TenantId, long? VendorId, long? ContractId);
    public record EntryInput(long? BranchId, DateOnly EntryDate, string? Description, List<LineInput> Lines, bool Post = false);
    public record ReverseInput(DateOnly? Date, string? Reason);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? sourceType,
        [FromQuery] string? status, [FromQuery] long? accountId, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.JournalEntries.AsNoTracking().Where(j => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(j.BranchId));
        if (from != null) query = query.Where(j => j.EntryDate >= from);
        if (to != null) query = query.Where(j => j.EntryDate <= to);
        if (sourceType != null) query = query.Where(j => j.SourceType == sourceType);
        if (accountId != null) query = query.Where(j => Db.JournalLines.Any(l => l.JournalEntryId == j.Id && l.AccountId == accountId));
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, status); query = query.Where(j => j.StatusId == s); }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(j => j.JournalNo.Contains(q.Search) || (j.Description ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id)
            .Select(j => new { J = j, Total = Db.JournalLines.Where(l => l.JournalEntryId == j.Id).Sum(l => l.Debit ?? 0) }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.J.Id, x.J.JournalNo, x.J.EntryDate, x.J.Description, x.J.SourceType, x.J.SourceId, status = Code(x.J.StatusId), total = x.Total, x.J.PostedAt, x.J.ReversedEntryId,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var j = await Db.JournalEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        var lines = await Db.JournalLines.AsNoTracking().Where(l => l.JournalEntryId == id)
            .Select(l => new { l.Id, l.AccountId, l.Account!.AccountCode, l.Account.NameEn, l.Account.NameAr, l.Description, l.Debit, l.Credit, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId })
            .ToListAsync();
        return Ok(new { entry = j, status = Code(j.StatusId), lines = lines.Select(l => new { l.Id, l.AccountId, l.AccountCode, account = Loc(l.NameEn, l.NameAr), l.Description, l.Debit, l.Credit, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId }) });
    }

    /// <summary>Manual journal entry (must balance). post=true posts it immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EntryInput input)
    {
        if (input.Post && !Me.HasPermission("accounting.post")) throw new AppException("Missing permission: accounting.post", 403);
        var branchId = RequireBranch(input.BranchId);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var je = await Posting.CreateEntryAsync(branchId, input.EntryDate, input.Description ?? "Manual entry", "manual", null,
            input.Lines.Select(l => new PostingService.Line(l.AccountId, l.Debit, l.Credit, l.Description, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId)), input.Post);
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = je.Id }, new { je.Id, je.JournalNo });
    }

    [HttpPost("{id:long}/post")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Post(long id)
    {
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        if (await Lookups.CodeAsync(j.StatusId) != "draft") throw new BusinessRuleException("Only draft entries can be posted.");
        var period = await Db.FiscalPeriods.FirstAsync(p => p.Id == j.FiscalPeriodId);
        if (period.IsClosed) throw new BusinessRuleException("The fiscal period is closed.");
        var sums = await Db.JournalLines.Where(l => l.JournalEntryId == id).GroupBy(l => 1).Select(g => new { Dr = g.Sum(l => l.Debit ?? 0), Cr = g.Sum(l => l.Credit ?? 0) }).FirstOrDefaultAsync();
        if (sums == null || sums.Dr != sums.Cr || sums.Dr == 0) throw new BusinessRuleException("The entry is not balanced.");
        j.StatusId = await LkId(LookupTypes.JournalStatus, "posted");
        j.PostedAt = DateTimeOffset.UtcNow; j.PostedBy = Me.UserId;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/reverse")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Reverse(long id, [FromBody] ReverseInput input)
    {
        await using var tx = await Db.Database.BeginTransactionAsync();
        var rev = await Posting.ReverseAsync(id, input.Date ?? Today, input.Reason ?? "Reversal");
        await tx.CommitAsync();
        return Ok(new { rev.Id, rev.JournalNo });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteDraft(long id)
    {
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        if (await Lookups.CodeAsync(j.StatusId) != "draft") throw new BusinessRuleException("Only draft entries can be deleted; reverse posted entries.");
        Db.JournalLines.RemoveRange(await Db.JournalLines.Where(l => l.JournalEntryId == id).ToListAsync());
        Db.JournalEntries.Remove(j);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Trial balance of posted entries (reversed entries and their reversals both count, so they net to zero).</summary>
    [HttpGet("~/api/accounting/trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        var lines = Db.JournalLines.AsNoTracking().Where(l => l.JournalEntry!.StatusId != draft && (branchId == null || l.JournalEntry.BranchId == branchId));
        var opening = await lines.Where(l => l.JournalEntry!.EntryDate < f).GroupBy(l => l.AccountId)
            .Select(g => new { g.Key, Bal = g.Sum(l => (l.Debit ?? 0) - (l.Credit ?? 0)) }).ToDictionaryAsync(x => x.Key, x => x.Bal);
        var period = await lines.Where(l => l.JournalEntry!.EntryDate >= f && l.JournalEntry.EntryDate <= t).GroupBy(l => l.AccountId)
            .Select(g => new { g.Key, Dr = g.Sum(l => l.Debit ?? 0), Cr = g.Sum(l => l.Credit ?? 0) }).ToDictionaryAsync(x => x.Key);
        var accounts = await Db.AccountsWithActivity(opening.Keys.Concat(period.Keys).Distinct().ToList());
        var rows = accounts.Select(a =>
        {
            var o = opening.GetValueOrDefault(a.Id);
            var p = period.GetValueOrDefault(a.Id);
            var closing = o + (p?.Dr ?? 0) - (p?.Cr ?? 0);
            return new { a.Id, a.AccountCode, account = Loc(a.NameEn, a.NameAr), type = Code(a.AccountTypeId), opening = o, debit = p?.Dr ?? 0, credit = p?.Cr ?? 0,
                         closing, closingDebit = closing > 0 ? closing : 0, closingCredit = closing < 0 ? -closing : 0 };
        }).OrderBy(r => r.AccountCode).ToList();
        return Ok(new { from = f, to = t, rows, totals = new { debit = rows.Sum(r => r.debit), credit = rows.Sum(r => r.credit), closingDebit = rows.Sum(r => r.closingDebit), closingCredit = rows.Sum(r => r.closingCredit) } });
    }

    /// <summary>General ledger of one account with running balance.</summary>
    [HttpGet("~/api/accounting/ledger/{accountId:long}")]
    public async Task<IActionResult> Ledger(long accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        var acc = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId) ?? throw new NotFoundException("Account", accountId);
        var baseQ = Db.JournalLines.AsNoTracking().Where(l => l.AccountId == accountId && l.JournalEntry!.StatusId != draft);
        var opening = await baseQ.Where(l => l.JournalEntry!.EntryDate < f).SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        var lines = await baseQ.Where(l => l.JournalEntry!.EntryDate >= f && l.JournalEntry.EntryDate <= t).OrderBy(l => l.JournalEntry!.EntryDate).ThenBy(l => l.Id)
            .Select(l => new { l.Id, l.JournalEntryId, l.JournalEntry!.JournalNo, l.JournalEntry.EntryDate, l.JournalEntry.SourceType, l.Description, Debit = l.Debit ?? 0, Credit = l.Credit ?? 0 })
            .ToListAsync();
        var running = opening;
        return Ok(new
        {
            account = new { acc.Id, acc.AccountCode, name = Loc(acc.NameEn, acc.NameAr) }, from = f, to = t, opening,
            lines = lines.Select(l => { running += l.Debit - l.Credit; return new { l.Id, l.JournalEntryId, l.JournalNo, l.EntryDate, l.SourceType, l.Description, l.Debit, l.Credit, balance = running }; }).ToList(),
            closing = running,
        });
    }
}

internal static class AccountingQueryExtensions
{
    public static Task<List<Account>> AccountsWithActivity(this Data.AppDbContext db, List<long> ids) =>
        db.Accounts.AsNoTracking().Where(a => ids.Contains(a.Id)).ToListAsync();
}
