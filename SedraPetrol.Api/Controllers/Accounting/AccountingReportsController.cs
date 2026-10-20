using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Financial statements (BRD §19.2): profit &amp; loss with comparison, balance sheet, AP aging; plus accounting settings
/// (journal approval threshold, opening balance equity account).
/// All amounts come from posted / reversed journal lines (drafts excluded); reversals net to zero.
/// </summary>
[Route("api/accounting")]
[PermissionModule("accounting")]
public class AccountingReportsController : ApiControllerBase
{
    public record SettingsInput(decimal JournalApprovalThreshold, long? OpeningEquityAccountId);

    private record AccRow(long Id, string Code, string? NameEn, string? NameAr, long? ParentId, long? TypeId);

    private async Task<long> CompanyIdAsync() => Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync();

    /// <summary>Sum of (debit − credit) per account for posted lines in [from, to] (null bounds = open).</summary>
    private async Task<Dictionary<long, decimal>> NetByAccountAsync(DateOnly? from, DateOnly? to, long? branchId)
    {
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var q = Db.JournalLines.AsNoTracking().Where(l => l.JournalEntry!.StatusId != draft);
        if (branchId != null) q = q.Where(l => l.JournalEntry!.BranchId == branchId);
        if (from != null) q = q.Where(l => l.JournalEntry!.EntryDate >= from);
        if (to != null) q = q.Where(l => l.JournalEntry!.EntryDate <= to);
        return await q.GroupBy(l => l.AccountId).Select(g => new { g.Key, Net = g.Sum(l => (l.Debit ?? 0) - (l.Credit ?? 0)) }).ToDictionaryAsync(x => x.Key, x => x.Net);
    }

    private async Task<List<AccRow>> AccountsAsync() =>
        await Db.Accounts.AsNoTracking().Select(a => new AccRow(a.Id, a.AccountCode, a.NameEn, a.NameAr, a.ParentAccountId, a.AccountTypeId)).ToListAsync();

    // ------------------------------------------------------------------ profit & loss

    /// <summary>Profit and loss for [from, to] with the previous period of the same length for comparison.</summary>
    [HttpGet("profit-loss")]
    public async Task<IActionResult> ProfitLoss([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        if (t < f) throw new AppException("The end date must be after the start date.");
        var days = t.DayNumber - f.DayNumber;
        var pt = f.AddDays(-1);
        var pf = pt.AddDays(-days);
        var cur = await NetByAccountAsync(f, t, branchId);
        var prev = await NetByAccountAsync(pf, pt, branchId);
        var accounts = await AccountsAsync();
        var byId = accounts.ToDictionary(a => a.Id);

        object Section(string type)
        {
            var sign = type == "revenue" ? -1m : 1m; // revenue is credit-natured
            var rows = accounts.Where(a => Code(a.TypeId) == type && (cur.ContainsKey(a.Id) || prev.ContainsKey(a.Id)))
                .Select(a =>
                {
                    var parent = a.ParentId != null && byId.TryGetValue(a.ParentId.Value, out var p) ? p : null;
                    var c = sign * cur.GetValueOrDefault(a.Id);
                    var pv = sign * prev.GetValueOrDefault(a.Id);
                    return new { a.Id, accountCode = a.Code, account = Loc(a.NameEn, a.NameAr), groupCode = parent?.Code, group = parent == null ? null : Loc(parent.NameEn, parent.NameAr),
                                 current = c, previous = pv, change = c - pv, changePct = pv == 0 ? (decimal?)null : Math.Round((c - pv) * 100 / Math.Abs(pv), 1) };
                })
                .Where(r => r.current != 0 || r.previous != 0).OrderBy(r => r.accountCode).ToList();
            return new { rows, current = rows.Sum(r => r.current), previous = rows.Sum(r => r.previous) };
        }

        decimal Tot(Dictionary<long, decimal> d, string type) => accounts.Where(a => Code(a.TypeId) == type).Sum(a => d.GetValueOrDefault(a.Id));
        var revCur = -Tot(cur, "revenue"); var expCur = Tot(cur, "expense");
        var revPrev = -Tot(prev, "revenue"); var expPrev = Tot(prev, "expense");
        return Ok(new
        {
            from = f, to = t, previousFrom = pf, previousTo = pt, branchId,
            revenue = Section("revenue"), expenses = Section("expense"),
            netProfit = new { current = revCur - expCur, previous = revPrev - expPrev },
            margin = revCur == 0 ? (decimal?)null : Math.Round((revCur - expCur) * 100 / revCur, 1),
        });
    }

    // ------------------------------------------------------------------ balance sheet

    /// <summary>Balance sheet as of a date: assets = liabilities + equity + profit of the current year + unclosed profit of earlier years.</summary>
    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet([FromQuery] DateOnly? asOf, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var d = asOf ?? Today;
        var yearStart = new DateOnly(d.Year, 1, 1);
        var all = await NetByAccountAsync(null, d, branchId);
        var thisYear = await NetByAccountAsync(yearStart, d, branchId);
        var accounts = await AccountsAsync();
        var byId = accounts.ToDictionary(a => a.Id);

        object Section(string type, decimal sign)
        {
            var rows = accounts.Where(a => Code(a.TypeId) == type && all.GetValueOrDefault(a.Id) != 0)
                .Select(a =>
                {
                    var parent = a.ParentId != null && byId.TryGetValue(a.ParentId.Value, out var p) ? p : null;
                    return new { a.Id, accountCode = a.Code, account = Loc(a.NameEn, a.NameAr), groupCode = parent?.Code, group = parent == null ? null : Loc(parent.NameEn, parent.NameAr),
                                 balance = sign * all.GetValueOrDefault(a.Id) };
                }).OrderBy(r => r.accountCode).ToList();
            return new { rows, total = rows.Sum(r => r.balance) };
        }

        decimal Sum(Dictionary<long, decimal> m, string type) => accounts.Where(a => Code(a.TypeId) == type).Sum(a => m.GetValueOrDefault(a.Id));
        var assets = Sum(all, "asset");
        var liabilities = -Sum(all, "liability");
        var equity = -Sum(all, "equity");
        var profitAll = -Sum(all, "revenue") - Sum(all, "expense");
        var profitYear = -Sum(thisYear, "revenue") - Sum(thisYear, "expense");
        var profitPrior = profitAll - profitYear;
        var totalLe = liabilities + equity + profitAll;
        return Ok(new
        {
            asOf = d, branchId,
            assets = Section("asset", 1), liabilities = Section("liability", -1), equity = Section("equity", -1),
            currentYearProfit = profitYear, priorYearsProfit = profitPrior,
            totals = new { assets, liabilities, equity = equity + profitAll, liabilitiesAndEquity = totalLe, difference = assets - totalLe, balanced = Math.Abs(assets - totalLe) < 0.01m },
        });
    }

    // ------------------------------------------------------------------ AP aging

    /// <summary>Accounts payable aging: open vendor bills by days past due (not due yet / 1-30 / 31-60 / 61-90 / 90+), per supplier.</summary>
    [HttpGet("ap-aging")]
    public async Task<IActionResult> ApAging([FromQuery] DateOnly? asOf, [FromQuery] long? branchId, [FromQuery] long? vendorId)
    {
        var d = asOf ?? Today;
        var dead = new List<long>();
        foreach (var c in new[] { "draft", "cancelled" }) if (await Lookups.IdOrNullAsync(LookupTypes.BillStatus, c) is long x) dead.Add(x);
        var q = Db.VendorBills.AsNoTracking().Where(b => (b.StatusId == null || !dead.Contains(b.StatusId.Value)) && b.BillDate <= d
                                                       && (Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(b.BranchId)));
        if (branchId != null) q = q.Where(b => b.BranchId == branchId);
        if (vendorId != null) q = q.Where(b => b.VendorId == vendorId);
        var bills = await q.Select(b => new { b.Id, b.BillNo, b.VendorId, b.BillDate, b.DueDate, Total = b.TotalAmount ?? 0, b.SourceType, VEn = b.Vendor!.Party!.NameEn, VAr = b.Vendor.Party!.NameAr, VCode = b.Vendor.Party!.PartyCode }).ToListAsync();
        var paid = await HttpContext.RequestServices.GetRequiredService<BillingService>().PaidByBillAsync(bills.Select(b => b.Id));
        static string Bucket(int days) => days <= 0 ? "current" : days <= 30 ? "d1_30" : days <= 60 ? "d31_60" : days <= 90 ? "d61_90" : "d90_plus";
        var open = bills.Select(b =>
        {
            var bal = b.Total - paid.GetValueOrDefault(b.Id);
            var days = d.DayNumber - (b.DueDate ?? b.BillDate).DayNumber;
            return new { b.Id, b.BillNo, b.VendorId, vendor = Loc(b.VEn, b.VAr), vendorCode = b.VCode, b.BillDate, b.DueDate, total = b.Total, paid = paid.GetValueOrDefault(b.Id), balance = bal, daysPastDue = Math.Max(0, days), bucket = Bucket(days), b.SourceType };
        }).Where(b => b.balance > 0.004m).OrderBy(b => b.vendor).ThenBy(b => b.DueDate ?? b.BillDate).ToList();
        var buckets = new[] { "current", "d1_30", "d31_60", "d61_90", "d90_plus" };
        var vendors = open.GroupBy(b => new { b.VendorId, b.vendor, b.vendorCode }).Select(g => new
        {
            g.Key.VendorId, g.Key.vendor, g.Key.vendorCode, total = g.Sum(b => b.balance), count = g.Count(),
            current = g.Where(b => b.bucket == "current").Sum(b => b.balance), d1_30 = g.Where(b => b.bucket == "d1_30").Sum(b => b.balance),
            d31_60 = g.Where(b => b.bucket == "d31_60").Sum(b => b.balance), d61_90 = g.Where(b => b.bucket == "d61_90").Sum(b => b.balance),
            d90_plus = g.Where(b => b.bucket == "d90_plus").Sum(b => b.balance),
        }).OrderByDescending(v => v.total).ToList();
        return Ok(new
        {
            asOf = d, vendors, bills = open,
            totals = buckets.ToDictionary(k => k, k => open.Where(b => b.bucket == k).Sum(b => b.balance)),
            total = open.Sum(b => b.balance),
        });
    }

    // ------------------------------------------------------------------ settings

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var companyId = await CompanyIdAsync();
        var s = await Db.AccountingSettings.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId);
        var acc = s?.OpeningEquityAccountId == null ? null : await Db.Accounts.AsNoTracking().Where(a => a.Id == s.OpeningEquityAccountId)
            .Select(a => new { a.Id, a.AccountCode, a.NameAr, a.NameEn }).FirstOrDefaultAsync();
        return Ok(new { journalApprovalThreshold = s?.JournalApprovalThreshold ?? 0, openingEquityAccountId = s?.OpeningEquityAccountId,
                        openingEquityAccount = acc == null ? null : $"{acc.AccountCode} {Loc(acc.NameEn, acc.NameAr)}" });
    }

    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsInput input)
    {
        if (input.JournalApprovalThreshold < 0) throw new AppException("The threshold cannot be negative.");
        var companyId = await CompanyIdAsync();
        if (input.OpeningEquityAccountId != null)
        {
            var a = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.OpeningEquityAccountId) ?? throw new NotFoundException("Account", input.OpeningEquityAccountId);
            if (a.IsGroup || !a.IsPostable) throw new AppException($"Account {a.AccountCode} is a group account.");
        }
        var s = await Db.AccountingSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId);
        if (s == null) { s = new AccountingSetting { CompanyId = companyId }; Db.AccountingSettings.Add(s); }
        s.JournalApprovalThreshold = input.JournalApprovalThreshold;
        s.OpeningEquityAccountId = input.OpeningEquityAccountId;
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
