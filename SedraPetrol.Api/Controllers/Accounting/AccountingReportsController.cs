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

    // ------------------------------------------------------------------ profit per building

    /// <summary>
    /// Profit per building for [from, to]: revenue and expense journal lines tagged with the building (tenant / owner invoices,
    /// credit notes, supplier bills with a building, vouchers with a building, AMC recognition, meter write-offs).
    /// Lines without a building are shown as «not assigned», so the total equals the P&amp;L. buildingId = one building with
    /// its accounts and months.
    /// </summary>
    [HttpGet("building-profit")]
    public async Task<IActionResult> BuildingProfit([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId, [FromQuery] long? buildingId)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        if (t < f) throw new AppException("The end date must be after the start date.");
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var revenueType = await Lookups.IdOrNullAsync(LookupTypes.AccountType, "revenue");
        var expenseType = await Lookups.IdOrNullAsync(LookupTypes.AccountType, "expense");
        var q = Db.JournalLines.AsNoTracking().Where(l => l.JournalEntry!.StatusId != draft && l.JournalEntry.EntryDate >= f && l.JournalEntry.EntryDate <= t
                                                        && (l.Account!.AccountTypeId == revenueType || l.Account.AccountTypeId == expenseType));
        if (branchId != null) q = q.Where(l => l.JournalEntry!.BranchId == branchId);
        if (buildingId != null) q = q.Where(l => l.BuildingId == buildingId);
        var rows = await q.Select(l => new
        {
            l.BuildingId, l.AccountId, l.Account!.AccountCode, l.Account.NameEn, l.Account.NameAr, IsRevenue = l.Account.AccountTypeId == revenueType,
            l.JournalEntry!.EntryDate.Year, l.JournalEntry.EntryDate.Month, Net = (l.Credit ?? 0) - (l.Debit ?? 0),
        }).ToListAsync();
        var ids = rows.Where(r => r.BuildingId != null).Select(r => r.BuildingId!.Value).Distinct().ToList();
        var buildings = await Db.Buildings.AsNoTracking().Where(b => ids.Contains(b.Id)).Select(b => new { b.Id, b.Code, b.NameEn, b.NameAr }).ToDictionaryAsync(b => b.Id);
        object Row(long? bid, IEnumerable<dynamic> g)
        {
            var list = g.ToList();
            decimal rev = list.Where(x => x.IsRevenue).Sum(x => (decimal)x.Net), exp = -list.Where(x => !x.IsRevenue).Sum(x => (decimal)x.Net);
            var b = bid == null ? null : buildings.GetValueOrDefault(bid.Value);
            return new
            {
                buildingId = bid, code = b?.Code, building = b == null ? (IsEnglish ? "Not assigned to a building" : "غير مخصص لمبنى") : Loc(b.NameEn, b.NameAr),
                revenue = rev, expenses = exp, profit = rev - exp, margin = rev == 0 ? (decimal?)null : Math.Round((rev - exp) * 100 / rev, 1),
            };
        }
        var perBuilding = rows.GroupBy(r => r.BuildingId).Select(g => Row(g.Key, g)).OrderBy(x => ((dynamic)x).buildingId == null ? 1 : 0)
            .ThenByDescending(x => (decimal)((dynamic)x).profit).ToList();
        decimal Rev(IEnumerable<dynamic> g) => g.Where(x => x.IsRevenue).Sum(x => (decimal)x.Net);
        decimal Exp(IEnumerable<dynamic> g) => -g.Where(x => !x.IsRevenue).Sum(x => (decimal)x.Net);
        var all = rows.Cast<dynamic>().ToList();
        return Ok(new
        {
            from = f, to = t, branchId, buildingId, buildings = perBuilding,
            accounts = buildingId == null ? null : rows.GroupBy(r => new { r.AccountId, r.AccountCode, r.NameEn, r.NameAr, r.IsRevenue })
                .Select(g => new { g.Key.AccountId, accountCode = g.Key.AccountCode, account = Loc(g.Key.NameEn, g.Key.NameAr), type = g.Key.IsRevenue ? "revenue" : "expense",
                                   amount = g.Key.IsRevenue ? g.Sum(x => x.Net) : -g.Sum(x => x.Net) }).OrderBy(x => x.accountCode).ToList(),
            months = buildingId == null ? null : rows.GroupBy(r => new { r.Year, r.Month }).OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new { month = $"{g.Key.Year}-{g.Key.Month:00}", revenue = Rev(g), expenses = Exp(g), profit = Rev(g) - Exp(g) }).ToList(),
            totals = new { revenue = Rev(all), expenses = Exp(all), profit = Rev(all) - Exp(all) },
        });
    }

    // ------------------------------------------------------------------ cash-flow statement (direct method)

    private static (string Section, string Key, string En, string Ar) CashClass(string code, string? type)
    {
        if (code.StartsWith("1201") || code.StartsWith("1202") || type == "revenue") return ("operating", "customers", "Receipts from customers", "المقبوض من العملاء");
        if (code.StartsWith("2301")) return ("operating", "deposits", "Customer deposits (received − refunded)", "تأمينات العملاء (مقبوض − مسترد)");
        if (code.StartsWith("2101") || code.StartsWith("2102")) return ("operating", "suppliers", "Payments to suppliers", "المدفوع للموردين");
        if (type == "expense") return ("operating", "expenses", "Expenses paid", "مصروفات مدفوعة");
        if (code.StartsWith("2201") || code.StartsWith("1301")) return ("operating", "vat", "VAT", "ضريبة القيمة المضافة");
        if (code.StartsWith("14")) return ("operating", "inventory", "Inventory purchases", "مشتريات المخزون");
        if (code.StartsWith("15")) return ("investing", "fixed_assets", "Fixed assets", "الأصول الثابتة");
        if (type == "equity") return ("financing", "equity", "Capital / owners", "رأس المال / الملاك");
        if (type == "liability") return ("financing", "liabilities", "Other liabilities (loans …)", "التزامات أخرى (قروض …)");
        return ("operating", "other", "Other", "أخرى");
    }

    /// <summary>
    /// Cash-flow statement (direct method) for [from, to]. Every posted / reversed journal entry that moves the cash &amp; bank
    /// accounts (codes 11…) is classified by the other accounts in the entry (customers, deposits, suppliers, expenses, VAT,
    /// fixed assets, equity …); moves only between cash / bank accounts (transfers) are left out. Opening + net change = closing,
    /// and closing equals the cash &amp; bank balance in the ledger (check field).
    /// </summary>
    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        if (t < f) throw new AppException("The end date must be after the start date.");
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var lines = Db.JournalLines.AsNoTracking().Where(l => l.JournalEntry!.StatusId != draft);
        if (branchId != null) lines = lines.Where(l => l.JournalEntry!.BranchId == branchId);
        var opening = await lines.Where(l => l.Account!.AccountCode.StartsWith("11") && l.JournalEntry!.EntryDate < f).SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        var entryIds = lines.Where(l => l.Account!.AccountCode.StartsWith("11") && l.JournalEntry!.EntryDate >= f && l.JournalEntry.EntryDate <= t).Select(l => l.JournalEntryId).Distinct();
        var rows = await lines.Where(l => entryIds.Contains(l.JournalEntryId))
            .Select(l => new { l.JournalEntryId, l.Account!.AccountCode, l.Account.AccountTypeId, Debit = l.Debit ?? 0, Credit = l.Credit ?? 0 }).ToListAsync();

        var amounts = new Dictionary<(string Section, string Key, string En, string Ar), decimal>();
        decimal netChange = 0, transfers = 0;
        foreach (var je in rows.GroupBy(r => r.JournalEntryId))
        {
            var cash = je.Where(r => r.AccountCode.StartsWith("11")).Sum(r => r.Debit - r.Credit);
            netChange += cash;
            var other = je.Where(r => !r.AccountCode.StartsWith("11")).ToList();
            if (other.Count == 0) { transfers += je.Where(r => r.AccountCode.StartsWith("11")).Sum(r => r.Debit); continue; }   // bank ↔ cash
            // the cash moved is shared between the other accounts in proportion to their movement in the opposite direction
            var opposite = other.Select(r => (r.AccountCode, Type: Code(r.AccountTypeId), Amt: cash >= 0 ? r.Credit - r.Debit : r.Debit - r.Credit)).ToList();
            var basis = opposite.Where(o => o.Amt > 0).ToList();
            if (basis.Count == 0) basis = opposite.Select(o => (o.AccountCode, o.Type, Amt: Math.Abs(o.Amt))).ToList();
            var total = basis.Sum(b => b.Amt);
            var left = cash;
            for (var i = 0; i < basis.Count; i++)
            {
                var share = i == basis.Count - 1 ? left : Math.Round(cash * basis[i].Amt / total, 2);
                left -= share;
                var k = CashClass(basis[i].AccountCode, basis[i].Type);
                amounts[k] = amounts.GetValueOrDefault(k) + share;
            }
        }
        object Section(string name) => new
        {
            lines = amounts.Where(a => a.Key.Section == name && a.Value != 0).OrderBy(a => a.Key.Key)
                .Select(a => new { key = a.Key.Key, label = Loc(a.Key.En, a.Key.Ar), amount = a.Value }).ToList(),
            total = amounts.Where(a => a.Key.Section == name).Sum(a => a.Value),
        };
        var closing = opening + netChange;
        var ledgerClosing = await lines.Where(l => l.Account!.AccountCode.StartsWith("11") && l.JournalEntry!.EntryDate <= t).SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        return Ok(new
        {
            from = f, to = t, branchId, openingCash = opening,
            operating = Section("operating"), investing = Section("investing"), financing = Section("financing"),
            netChange, closingCash = closing, internalTransfers = transfers,
            check = new { ledgerClosing, difference = closing - ledgerClosing, sectionsTotal = amounts.Values.Sum(),
                          balanced = Math.Abs(closing - ledgerClosing) < 0.01m && Math.Abs(amounts.Values.Sum() - netChange) < 0.01m },
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
        // advances (vendor payments not allocated to a bill) are shown per supplier; net = what we really owe (agrees with the ledger)
        var advances = await HttpContext.RequestServices.GetRequiredService<BillingService>().VendorAdvancesAsync(vendorId);
        var vendors = open.GroupBy(b => new { b.VendorId, b.vendor, b.vendorCode }).Select(g => new
        {
            g.Key.VendorId, g.Key.vendor, g.Key.vendorCode, total = g.Sum(b => b.balance), count = g.Count(),
            current = g.Where(b => b.bucket == "current").Sum(b => b.balance), d1_30 = g.Where(b => b.bucket == "d1_30").Sum(b => b.balance),
            d31_60 = g.Where(b => b.bucket == "d31_60").Sum(b => b.balance), d61_90 = g.Where(b => b.bucket == "d61_90").Sum(b => b.balance),
            d90_plus = g.Where(b => b.bucket == "d90_plus").Sum(b => b.balance),
            advance = advances.GetValueOrDefault(g.Key.VendorId), net = g.Sum(b => b.balance) - advances.GetValueOrDefault(g.Key.VendorId),
        }).OrderByDescending(v => v.total).ToList();
        var advanceOnly = advances.Where(a => vendors.All(v => v.VendorId != a.Key)).Select(a => a.Key).ToList();
        var extra = await Db.Vendors.AsNoTracking().Where(v => advanceOnly.Contains(v.Id))
            .Select(v => new { v.Id, v.Party!.NameEn, v.Party.NameAr, v.Party.PartyCode }).ToListAsync();
        var vendorRows = vendors.Cast<object>().Concat(extra.Select(v => (object)new
        {
            VendorId = v.Id, vendor = Loc(v.NameEn, v.NameAr), vendorCode = v.PartyCode, total = 0m, count = 0,
            current = 0m, d1_30 = 0m, d31_60 = 0m, d61_90 = 0m, d90_plus = 0m, advance = advances[v.Id], net = -advances[v.Id],
        })).ToList();
        var totals = buckets.ToDictionary(k => k, k => open.Where(b => b.bucket == k).Sum(b => b.balance));
        totals["advance"] = advances.Values.Sum();
        totals["net"] = open.Sum(b => b.balance) - advances.Values.Sum();
        return Ok(new
        {
            asOf = d, vendors = vendorRows, bills = open, totals,
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
