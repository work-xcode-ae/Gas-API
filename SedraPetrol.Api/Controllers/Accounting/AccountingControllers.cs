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

    protected override async Task OnUpdatingAsync(Account existing, Account incoming)
    {
        if (incoming.CompanyId == 0) incoming.CompanyId = existing.CompanyId;
        incoming.AccountCode = (incoming.AccountCode ?? "").Trim();
        if (string.IsNullOrEmpty(incoming.AccountCode)) throw new AppException("Account code is required.");
        if (incoming.AccountCode != existing.AccountCode && await Db.Accounts.AnyAsync(a => a.CompanyId == incoming.CompanyId && a.AccountCode == incoming.AccountCode && a.Id != existing.Id))
            throw new BusinessRuleException($"Account code {incoming.AccountCode} already exists.");
        if (incoming.ParentAccountId == existing.Id) throw new AppException("An account cannot be its own parent.");
        if (incoming.ParentAccountId != null)
        {
            // walk up from the new parent: it must not be a descendant of this account
            var cursor = incoming.ParentAccountId;
            for (var i = 0; cursor != null && i < 20; i++)
            {
                if (cursor == existing.Id) throw new AppException("The parent account cannot be one of this account's sub-accounts.");
                cursor = await Db.Accounts.Where(a => a.Id == cursor).Select(a => a.ParentAccountId).FirstOrDefaultAsync();
            }
            var parent = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == incoming.ParentAccountId) ?? throw new NotFoundException("Account", incoming.ParentAccountId);
            incoming.Level = (short)((parent.Level ?? 1) + 1);
        }
        else incoming.Level = 1;
        if (incoming.IsGroup && !existing.IsGroup && await Db.JournalLines.AnyAsync(l => l.AccountId == existing.Id))
            throw new BusinessRuleException("The account has journal lines; it cannot become a group account.");
        if (incoming.IsGroup) incoming.IsPostable = false;
    }

    protected override async Task OnDeletingAsync(Account entity)
    {
        if (await Db.JournalLines.AnyAsync(l => l.AccountId == entity.Id)) throw new BusinessRuleException("The account has journal lines; deactivate it instead.");
        if (await Db.Accounts.AnyAsync(a => a.ParentAccountId == entity.Id)) throw new BusinessRuleException("The account has sub-accounts.");
    }
}

[Route("api/account-mappings")]
[PermissionModule("accounting")]
public class AccountMappingsController : CrudController<AccountMapping>
{
    protected override async Task OnCreatingAsync(AccountMapping e)
    {
        if (e.CompanyId == 0) e.CompanyId = Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync();
        if (string.IsNullOrWhiteSpace(e.SourceType)) throw new AppException("Source type is required.");
        if (e.DebitAccountId == null && e.CreditAccountId == null) throw new AppException("Choose a debit or a credit account.");
    }

    protected override Task OnUpdatingAsync(AccountMapping existing, AccountMapping incoming)
    {
        if (incoming.CompanyId == 0) incoming.CompanyId = existing.CompanyId;
        return Task.CompletedTask;
    }

    /// <summary>Mappings with account and key labels, for the mapping editor.</summary>
    [HttpGet("view")]
    public async Task<IActionResult> View()
    {
        await LoadLookups();
        var rows = await Db.AccountMappings.AsNoTracking().OrderBy(m => m.SourceType).ThenBy(m => m.SourceKeyId)
            .Select(m => new { M = m, Dr = m.DebitAccount!.AccountCode, DrEn = m.DebitAccount.NameEn, DrAr = m.DebitAccount.NameAr,
                               Cr = m.CreditAccount!.AccountCode, CrEn = m.CreditAccount.NameEn, CrAr = m.CreditAccount.NameAr, Br = m.Branch!.NameAr ?? m.Branch.NameEn })
            .ToListAsync();
        return Ok(rows.Select(x => new
        {
            x.M.Id, x.M.CompanyId, x.M.BranchId, branch = x.Br, x.M.SourceType, x.M.SourceKeyId, sourceKey = Code(x.M.SourceKeyId), sourceKeyLabel = Label(x.M.SourceKeyId),
            x.M.DebitAccountId, debitAccount = x.M.DebitAccountId == null ? null : $"{x.Dr} {Loc(x.DrEn, x.DrAr)}",
            x.M.CreditAccountId, creditAccount = x.M.CreditAccountId == null ? null : $"{x.Cr} {Loc(x.CrEn, x.CrAr)}", x.M.IsActive,
        }));
    }
}

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

    /// <summary>Periods of a year with journal counts (drafts block closing).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] int? year)
    {
        var y = (short)(year ?? Today.Year);
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var periods = await Db.FiscalPeriods.AsNoTracking().Where(p => p.Year == y).OrderBy(p => p.PeriodNo).ToListAsync();
        var ids = periods.Select(p => p.Id).ToList();
        var counts = await Db.JournalEntries.AsNoTracking().Where(j => ids.Contains(j.FiscalPeriodId))
            .GroupBy(j => j.FiscalPeriodId).Select(g => new { g.Key, Total = g.Count(), Drafts = g.Count(j => j.StatusId == draft) }).ToDictionaryAsync(x => x.Key);
        var years = await Db.FiscalPeriods.AsNoTracking().Select(p => p.Year).Distinct().OrderBy(x => x).ToListAsync();
        return Ok(new
        {
            year = y, years,
            periods = periods.Select(p => new { p.Id, p.Year, p.PeriodNo, p.StartDate, p.EndDate, p.IsClosed,
                journals = counts.GetValueOrDefault(p.Id)?.Total ?? 0, drafts = counts.GetValueOrDefault(p.Id)?.Drafts ?? 0 }),
        });
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
    public record DecisionInput(string? Reason);

    private async Task<AccountingSetting> SettingAsync()
    {
        var companyId = Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync();
        return await Db.AccountingSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId) ?? new AccountingSetting { CompanyId = companyId };
    }

    private async Task<decimal> TotalAsync(long journalId) =>
        await Db.JournalLines.Where(l => l.JournalEntryId == journalId).SumAsync(l => (decimal?)(l.Debit ?? 0)) ?? 0;

    /// <summary>Manual journals above the threshold need an approved approval request before posting.</summary>
    private async Task<bool> NeedsApprovalAsync(JournalEntry j)
    {
        if (j.SourceType != "manual") return false;
        var threshold = (await SettingAsync()).JournalApprovalThreshold;
        return threshold > 0 && await TotalAsync(j.Id) > threshold;
    }

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
            .Select(j => new { J = j, Total = Db.JournalLines.Where(l => l.JournalEntryId == j.Id).Sum(l => l.Debit ?? 0),
                               Approval = Db.JournalApprovals.Where(a => a.JournalEntryId == j.Id).OrderByDescending(a => a.Id).Select(a => a.Status).FirstOrDefault(),
                               Creator = Db.AppUsers.Where(u => u.Id == j.CreatedBy).Select(u => u.NameAr ?? u.NameEn).FirstOrDefault() }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.J.Id, x.J.JournalNo, x.J.EntryDate, x.J.Description, x.J.SourceType, x.J.SourceId, status = Code(x.J.StatusId), total = x.Total, x.J.PostedAt, x.J.ReversedEntryId,
            x.J.BranchId, approval = x.Approval, createdBy = x.Creator, createdById = x.J.CreatedBy,
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
        var approvals = await Db.JournalApprovals.AsNoTracking().Where(a => a.JournalEntryId == id).OrderBy(a => a.Id).ToListAsync();
        var userIds = approvals.SelectMany(a => new[] { a.RequestedBy, a.DecidedBy }).Concat(new[] { j.CreatedBy, j.PostedBy }).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var users = await Db.AppUsers.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NameAr ?? u.NameEn ?? u.Email);
        string? U(long? uid) => uid == null ? null : users.GetValueOrDefault(uid.Value);
        var needs = Code(j.StatusId) == "draft" && await NeedsApprovalAsync(j);
        return Ok(new
        {
            entry = j, status = Code(j.StatusId), createdBy = U(j.CreatedBy), postedBy = U(j.PostedBy), needsApproval = needs,
            approvalThreshold = (await SettingAsync()).JournalApprovalThreshold,
            approvals = approvals.Select(a => new { a.Id, a.Status, a.Amount, a.Threshold, requestedBy = U(a.RequestedBy), a.RequestedAt, decidedBy = U(a.DecidedBy), a.DecidedAt, a.Reason }),
            lines = lines.Select(l => new { l.Id, l.AccountId, l.AccountCode, account = Loc(l.NameEn, l.NameAr), l.Description, l.Debit, l.Credit, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId }),
        });
    }

    /// <summary>Manual journal entry (must balance). post=true posts it immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EntryInput input)
    {
        if (input.Post && !Me.HasPermission("accounting.post")) throw new AppException("Missing permission: accounting.post", 403);
        var branchId = RequireBranch(input.BranchId);
        var total = input.Lines.Sum(l => l.Debit);
        var setting = await SettingAsync();
        var needsApproval = setting.JournalApprovalThreshold > 0 && total > setting.JournalApprovalThreshold;
        await using var tx = await Db.Database.BeginTransactionAsync();
        // above the threshold the entry is saved as a draft and an approval request is opened instead of posting
        var je = await Posting.CreateEntryAsync(branchId, input.EntryDate, input.Description ?? "Manual entry", "manual", null,
            input.Lines.Select(l => new PostingService.Line(l.AccountId, l.Debit, l.Credit, l.Description, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId)), input.Post && !needsApproval);
        if (needsApproval && input.Post)
        {
            Db.JournalApprovals.Add(new JournalApproval { JournalEntryId = je.Id, Status = "pending", Amount = total, Threshold = setting.JournalApprovalThreshold, RequestedBy = Me.UserId, RequestedAt = DateTimeOffset.UtcNow });
            Activity.Log("journal_entry", je.Id, "approval_request", $"Journal {je.JournalNo} ({total:0.##}) sent for approval");
            await Db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = je.Id }, new { je.Id, je.JournalNo, pendingApproval = needsApproval && input.Post });
    }

    [HttpPost("{id:long}/post")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Post(long id)
    {
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        if (await Lookups.CodeAsync(j.StatusId) != "draft") throw new BusinessRuleException("Only draft entries can be posted.");
        if (await NeedsApprovalAsync(j) && !await Db.JournalApprovals.AnyAsync(a => a.JournalEntryId == id && a.Status == "approved"))
            throw new BusinessRuleException("This journal is above the approval limit. Send it for approval; another user with posting permission must approve it.");
        var period = await Db.FiscalPeriods.FirstAsync(p => p.Id == j.FiscalPeriodId);
        if (period.IsClosed) throw new BusinessRuleException("The fiscal period is closed.");
        var sums = await Db.JournalLines.Where(l => l.JournalEntryId == id).GroupBy(l => 1).Select(g => new { Dr = g.Sum(l => l.Debit ?? 0), Cr = g.Sum(l => l.Credit ?? 0) }).FirstOrDefaultAsync();
        if (sums == null || sums.Dr != sums.Cr || sums.Dr == 0) throw new BusinessRuleException("The entry is not balanced.");
        j.StatusId = await LkId(LookupTypes.JournalStatus, "posted");
        j.PostedAt = DateTimeOffset.UtcNow; j.PostedBy = Me.UserId;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Send a draft manual journal for approval.</summary>
    [HttpPost("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id)
    {
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        EnsureBranchAccess(j.BranchId);
        if (await Lookups.CodeAsync(j.StatusId) != "draft") throw new BusinessRuleException("Only draft entries can be sent for approval.");
        if (await Db.JournalApprovals.AnyAsync(a => a.JournalEntryId == id && a.Status == "pending")) throw new BusinessRuleException("An approval request is already pending.");
        var setting = await SettingAsync();
        Db.JournalApprovals.Add(new JournalApproval { JournalEntryId = id, Status = "pending", Amount = await TotalAsync(id), Threshold = setting.JournalApprovalThreshold, RequestedBy = Me.UserId, RequestedAt = DateTimeOffset.UtcNow });
        Activity.Log("journal_entry", id, "approval_request", $"Journal {j.JournalNo} sent for approval", null, j.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Approve (and post) a pending journal. The approver must not be the creator of the journal.</summary>
    [HttpPost("{id:long}/approve")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Approve(long id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DecisionInput? input)
    {
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        EnsureBranchAccess(j.BranchId);
        var req = await Db.JournalApprovals.Where(a => a.JournalEntryId == id && a.Status == "pending").OrderByDescending(a => a.Id).FirstOrDefaultAsync()
                  ?? throw new BusinessRuleException("There is no pending approval request for this journal.");
        if (j.CreatedBy != null && j.CreatedBy == Me.UserId) throw new BusinessRuleException("You created this journal; another user must approve it.");
        if (await Lookups.CodeAsync(j.StatusId) != "draft") throw new BusinessRuleException("Only draft entries can be approved.");
        var period = await Db.FiscalPeriods.FirstAsync(p => p.Id == j.FiscalPeriodId);
        if (period.IsClosed) throw new BusinessRuleException("The fiscal period is closed.");
        var sums = await Db.JournalLines.Where(l => l.JournalEntryId == id).GroupBy(l => 1).Select(g => new { Dr = g.Sum(l => l.Debit ?? 0), Cr = g.Sum(l => l.Credit ?? 0) }).FirstOrDefaultAsync();
        if (sums == null || sums.Dr != sums.Cr || sums.Dr == 0) throw new BusinessRuleException("The entry is not balanced.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        req.Status = "approved"; req.DecidedBy = Me.UserId; req.DecidedAt = DateTimeOffset.UtcNow; req.Reason = input?.Reason;
        j.StatusId = await LkId(LookupTypes.JournalStatus, "posted");
        j.PostedAt = DateTimeOffset.UtcNow; j.PostedBy = Me.UserId;
        Activity.Log("journal_entry", id, "approve", $"Journal {j.JournalNo} approved and posted", null, j.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    /// <summary>Reject a pending journal (stays as draft with the reason; can be corrected and re-submitted).</summary>
    [HttpPost("{id:long}/reject")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Reject(long id, [FromBody] DecisionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the rejection reason.");
        var j = await Db.JournalEntries.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("JournalEntry", id);
        EnsureBranchAccess(j.BranchId);
        var req = await Db.JournalApprovals.Where(a => a.JournalEntryId == id && a.Status == "pending").OrderByDescending(a => a.Id).FirstOrDefaultAsync()
                  ?? throw new BusinessRuleException("There is no pending approval request for this journal.");
        req.Status = "rejected"; req.DecidedBy = Me.UserId; req.DecidedAt = DateTimeOffset.UtcNow; req.Reason = input.Reason.Trim();
        Activity.Log("journal_entry", id, "reject", $"Journal {j.JournalNo} rejected: {input.Reason}", null, j.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Approval requests waiting for a decision.</summary>
    [HttpGet("approvals")]
    public async Task<IActionResult> PendingApprovals()
    {
        var rows = await Db.JournalApprovals.AsNoTracking().Where(a => a.Status == "pending").OrderBy(a => a.RequestedAt)
            .Select(a => new { a.Id, a.JournalEntryId, a.JournalEntry!.JournalNo, a.JournalEntry.EntryDate, a.JournalEntry.Description, a.Amount, a.Threshold, a.RequestedAt,
                               requestedBy = Db.AppUsers.Where(u => u.Id == a.RequestedBy).Select(u => u.NameAr ?? u.NameEn).FirstOrDefault(), a.JournalEntry.CreatedBy })
            .ToListAsync();
        return Ok(rows.Select(r => new { r.Id, r.JournalEntryId, r.JournalNo, r.EntryDate, r.Description, r.Amount, r.Threshold, r.RequestedAt, r.requestedBy, canDecide = r.CreatedBy != Me.UserId }));
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
