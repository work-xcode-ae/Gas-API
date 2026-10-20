using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Payment calendar and cash-flow forecast (BRD §16): one schedule of expected receipts and payments built from the source
/// documents (customer invoices / instalments, AMC instalments not yet invoiced, pending cheques, supply-contract monthly fees,
/// supplier bills, refill purchase orders not yet billed) plus manually entered expected items (<see cref="CashflowItem"/>).
/// Nothing here changes the ledger.
/// </summary>
[Route("api/cashflow")]
[PermissionModule("payments")]
public class CashflowController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record ScheduleItem(DateOnly Date, string Direction, string Source, string SourceLabel, string Reference, string? Counterparty,
        long? BuildingId, string? Building, decimal Amount, decimal Paid, decimal Remaining, string Status, string Confidence,
        string LinkType, long LinkId, int DaysLate, long BranchId, string? Note);

    public record ItemInput(long? BranchId, string Direction, string? Category, string? Counterparty, long? BuildingId, decimal Amount,
        DateOnly StartDate, DateOnly? EndDate, string? Recurrence, string? Confidence, string? Description, bool IsActive = true);

    private static readonly Dictionary<string, string> SourceLabels = new()
    {
        ["invoice"] = "فاتورة عميل", ["installment"] = "قسط فاتورة", ["amc"] = "قسط عقد صيانة", ["contract_fee"] = "رسوم عقد توريد (تقديري)",
        ["cheque"] = "شيك قيد التحصيل", ["vendor_bill"] = "فاتورة مورد", ["refill_po"] = "أمر شراء غاز", ["manual"] = "بند متوقع",
    };

    // ================================================================== schedule

    /// <summary>
    /// All expected receipts (direction=in) and payments (direction=out) between from and to (default: this month … +3 months).
    /// Overdue unpaid items before «from» are always included (status overdue). source = comma list of source codes.
    /// </summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> Schedule([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? direction,
        [FromQuery] string? source, [FromQuery] long? branchId, [FromQuery] long? buildingId, [FromQuery] string? confidence,
        [FromQuery] bool includePaid = false, CancellationToken ct = default)
    {
        var f = from ?? new DateOnly(Today.Year, Today.Month, 1);
        var t = to ?? f.AddMonths(3).AddDays(-1);
        if (t < f) throw new AppException("The end date must be after the start date.");
        var items = await BuildAsync(f, t, branchId, includePaid, ct);
        items = Filter(items, direction, source, buildingId, confidence);
        var open = items.Where(i => i.Remaining > 0).ToList();
        return Ok(new
        {
            from = f, to = t,
            items = items.OrderBy(i => i.Date).ThenBy(i => i.Direction).ThenBy(i => i.Reference),
            totals = new
            {
                expectedIn = open.Where(i => i.Direction == "in").Sum(i => i.Remaining),
                expectedOut = open.Where(i => i.Direction == "out").Sum(i => i.Remaining),
                overdueIn = open.Where(i => i.Direction == "in" && i.Status == "overdue").Sum(i => i.Remaining),
                overdueOut = open.Where(i => i.Direction == "out" && i.Status == "overdue").Sum(i => i.Remaining),
                overdueInCount = open.Count(i => i.Direction == "in" && i.Status == "overdue"),
                overdueOutCount = open.Count(i => i.Direction == "out" && i.Status == "overdue"),
            },
            bySource = open.GroupBy(i => new { i.Source, i.Direction })
                .Select(g => new { g.Key.Source, label = SourceLabels.GetValueOrDefault(g.Key.Source, g.Key.Source), g.Key.Direction, count = g.Count(), amount = g.Sum(i => i.Remaining) }),
        });
    }

    // ================================================================== forecast

    /// <summary>
    /// Cash-flow forecast: opening cash (posted GL balance of the cash / bank accounts) + expected receipts − expected payments per period.
    /// scenario=confirmed uses confirmed + committed items only; scenario=all also adds estimates.
    /// Overdue items are reported separately and only enter the first period when includeOverdue=true.
    /// </summary>
    [HttpGet("forecast")]
    public async Task<IActionResult> Forecast([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string granularity = "week",
        [FromQuery] string scenario = "confirmed", [FromQuery] long? branchId = null, [FromQuery] bool includeOverdue = false, CancellationToken ct = default)
    {
        var f = from ?? Today;
        var t = to ?? f.AddMonths(3).AddDays(-1);
        if (t < f) throw new AppException("The end date must be after the start date.");
        if ((t.DayNumber - f.DayNumber) > 800) throw new AppException("The forecast period is limited to about two years.");
        granularity = granularity is "day" or "week" or "month" ? granularity : "week";
        bool InScenario(ScheduleItem i) => scenario == "all" || i.Confidence != "estimate";

        // actual cash at the start
        var cashAccounts = await CashAccountIdsAsync();
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var cutoff = f <= Today ? f : Today.AddDays(1);
        var glQ = Db.JournalLines.AsNoTracking().Where(l => cashAccounts.Contains(l.AccountId) && l.JournalEntry!.StatusId != draft && l.JournalEntry.EntryDate < cutoff);
        if (branchId != null) glQ = glQ.Where(l => l.JournalEntry!.BranchId == branchId);
        else if (!(Me.IsAdmin || Me.BranchIds.Count == 0)) { var ids = Me.BranchIds.ToList(); glQ = glQ.Where(l => ids.Contains(l.JournalEntry!.BranchId)); }
        var actualCash = await glQ.SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0)), ct) ?? 0;

        var items = (await BuildAsync(f <= Today ? f : Today, t, branchId, false, ct)).Where(i => i.Remaining > 0 && InScenario(i)).ToList();
        decimal Net(IEnumerable<ScheduleItem> list) => list.Sum(i => i.Direction == "in" ? i.Remaining : -i.Remaining);

        // a forecast that starts in the future: carry the expected movement between today and «from»
        var bridge = f > Today ? Net(items.Where(i => i.Status != "overdue" && i.Date >= Today && i.Date < f)) : 0;
        var opening = actualCash + bridge;
        var overdue = items.Where(i => i.Status == "overdue").ToList();
        var future = items.Where(i => i.Status != "overdue" && i.Date >= f && i.Date <= t).ToList();

        var periods = new List<object>();
        var running = opening;
        var start = PeriodStart(f, granularity);
        var first = true;
        var lowest = opening;
        while (start <= t)
        {
            var end = PeriodEnd(start, granularity);
            var pf = start < f ? f : start;
            var pt = end > t ? t : end;
            var inPeriod = future.Where(i => i.Date >= pf && i.Date <= pt).ToList();
            if (first && includeOverdue) inPeriod.AddRange(overdue);
            var inAmt = inPeriod.Where(i => i.Direction == "in").Sum(i => i.Remaining);
            var outAmt = inPeriod.Where(i => i.Direction == "out").Sum(i => i.Remaining);
            var open = running;
            running += inAmt - outAmt;
            if (running < lowest) lowest = running;
            periods.Add(new
            {
                from = pf, to = pt, label = PeriodLabel(pf, granularity), opening = open, expectedIn = inAmt, expectedOut = outAmt, net = inAmt - outAmt, closing = running,
                confirmedIn = inPeriod.Where(i => i.Direction == "in" && i.Confidence != "estimate").Sum(i => i.Remaining),
                estimateIn = inPeriod.Where(i => i.Direction == "in" && i.Confidence == "estimate").Sum(i => i.Remaining),
                confirmedOut = inPeriod.Where(i => i.Direction == "out" && i.Confidence != "estimate").Sum(i => i.Remaining),
                estimateOut = inPeriod.Where(i => i.Direction == "out" && i.Confidence == "estimate").Sum(i => i.Remaining),
                count = inPeriod.Count,
            });
            first = false;
            start = end.AddDays(1);
        }
        return Ok(new
        {
            from = f, to = t, granularity, scenario, includeOverdue,
            actualCash, actualCashDate = cutoff.AddDays(-1), bridge, opening, closing = running, lowestClosing = lowest,
            cashAccounts = await Db.Accounts.AsNoTracking().Where(a => cashAccounts.Contains(a.Id)).Select(a => new { a.Id, a.AccountCode, a.NameAr, a.NameEn }).ToListAsync(ct),
            overdue = new
            {
                receivables = overdue.Where(i => i.Direction == "in").Sum(i => i.Remaining), receivablesCount = overdue.Count(i => i.Direction == "in"),
                payables = overdue.Where(i => i.Direction == "out").Sum(i => i.Remaining), payablesCount = overdue.Count(i => i.Direction == "out"),
            },
            totals = new { expectedIn = future.Where(i => i.Direction == "in").Sum(i => i.Remaining), expectedOut = future.Where(i => i.Direction == "out").Sum(i => i.Remaining) },
            periods,
        });
    }

    // ================================================================== manual expected items

    [HttpGet("items")]
    public async Task<IActionResult> Items([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var q = Db.CashflowItems.AsNoTracking().Where(i => Me.IsAdmin || Me.BranchIds.Count == 0 || i.BranchId == null || Me.BranchIds.Contains(i.BranchId.Value));
        if (!includeInactive) q = q.Where(i => i.IsActive);
        var rows = await q.OrderBy(i => i.Direction).ThenBy(i => i.StartDate)
            .Select(i => new { I = i, Bld = i.Building!.NameAr ?? i.Building.NameEn }).ToListAsync(ct);
        return Ok(rows.Select(x => new
        {
            x.I.Id, x.I.BranchId, x.I.Direction, x.I.Category, x.I.Counterparty, x.I.BuildingId, building = x.Bld, x.I.Amount, x.I.StartDate, x.I.EndDate,
            x.I.Recurrence, x.I.Confidence, x.I.Description, x.I.IsActive,
            nextDate = Occurrences(x.I, Today, Today.AddYears(2)).Cast<DateOnly?>().FirstOrDefault(),
        }));
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] ItemInput input)
    {
        var e = new CashflowItem();
        Apply(e, input);
        if (e.BranchId == null) e.BranchId = Me.BranchId ?? (Me.BranchIds.Count > 0 ? Me.BranchIds.First() : null);
        Db.CashflowItems.Add(e);
        await Db.SaveChangesAsync();
        return Ok(new { e.Id });
    }

    [HttpPut("items/{id:long}")]
    public async Task<IActionResult> UpdateItem(long id, [FromBody] ItemInput input)
    {
        var e = await Db.CashflowItems.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CashflowItem", id);
        if (e.BranchId != null) EnsureBranchAccess(e.BranchId);
        Apply(e, input);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("items/{id:long}")]
    public async Task<IActionResult> DeleteItem(long id)
    {
        var e = await Db.CashflowItems.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CashflowItem", id);
        if (e.BranchId != null) EnsureBranchAccess(e.BranchId);
        Db.CashflowItems.Remove(e);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private void Apply(CashflowItem e, ItemInput i)
    {
        if (i.Direction is not ("in" or "out")) throw new AppException("Direction must be in or out.");
        if (i.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        var rec = i.Recurrence ?? "none";
        if (rec is not ("none" or "monthly" or "quarterly" or "yearly")) throw new AppException("Recurrence must be none, monthly, quarterly or yearly.");
        if (i.EndDate != null && i.EndDate < i.StartDate) throw new AppException("The end date must be after the start date.");
        if (i.BranchId != null) EnsureBranchAccess(i.BranchId);
        e.BranchId = i.BranchId ?? e.BranchId; e.Direction = i.Direction; e.Category = string.IsNullOrWhiteSpace(i.Category) ? "other" : i.Category.Trim();
        e.Counterparty = i.Counterparty?.Trim(); e.BuildingId = i.BuildingId; e.Amount = i.Amount; e.StartDate = i.StartDate;
        e.EndDate = rec == "none" ? null : i.EndDate; e.Recurrence = rec; e.Confidence = i.Confidence == "confirmed" ? "confirmed" : "estimate";
        e.Description = i.Description?.Trim(); e.IsActive = i.IsActive;
    }

    // ================================================================== building the schedule

    private static List<ScheduleItem> Filter(List<ScheduleItem> items, string? direction, string? source, long? buildingId, string? confidence)
    {
        IEnumerable<ScheduleItem> q = items;
        if (direction is "in" or "out") q = q.Where(i => i.Direction == direction);
        if (!string.IsNullOrWhiteSpace(source))
        {
            var set = source.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            q = q.Where(i => set.Contains(i.Source));
        }
        if (buildingId != null) q = q.Where(i => i.BuildingId == buildingId);
        if (!string.IsNullOrWhiteSpace(confidence))
        {
            var set = confidence.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
            q = q.Where(i => set.Contains(i.Confidence));
        }
        return q.ToList();
    }

    private static string StatusOf(DateOnly date, decimal paid, decimal remaining)
    {
        if (remaining <= 0) return "paid";
        if (date < Today) return "overdue";
        return paid > 0 ? "partial" : "due";
    }
    private static int Late(DateOnly date, decimal remaining) => remaining > 0 && date < Today ? Today.DayNumber - date.DayNumber : 0;

    /// <summary>
    /// Collects the schedule. Open documents dated before «f» are included (they are overdue); paid ones only when includePaid
    /// and dated inside the period.
    /// </summary>
    private async Task<List<ScheduleItem>> BuildAsync(DateOnly f, DateOnly t, long? branchId, bool includePaid, CancellationToken ct)
    {
        await LoadLookups();
        var list = new List<ScheduleItem>();
        var scopeAll = Me.IsAdmin || Me.BranchIds.Count == 0;
        var myBranches = Me.BranchIds.ToList();
        bool BranchOk(long b) => (branchId == null || b == branchId) && (scopeAll || myBranches.Contains(b));
        bool Keep(DateOnly date, decimal remaining) => remaining > 0 ? date <= t : includePaid && date >= f && date <= t;
        var vat = Billing.DefaultVatRate;

        // ---------- customer invoices (or their instalments) ----------
        var deadInv = await Billing.InvoiceStatusIdsAsync("draft", "cancelled");
        var invQ = Db.Invoices.AsNoTracking().Where(i => i.StatusId == null || !deadInv.Contains(i.StatusId.Value));
        if (!includePaid) invQ = invQ.Where(i => i.DueDate <= t || Db.InvoiceInstallments.Any(s => s.InvoiceId == i.Id && s.DueDate <= t));
        else invQ = invQ.Where(i => i.IssueDate <= t);
        var invs = await invQ.Select(i => new { i.Id, i.BranchId, i.InvoiceNo, i.DueDate, i.IssueDate, Total = i.TotalAmount ?? 0, i.BuildingId,
                                                Bld = i.Building!.NameAr ?? i.Building.NameEn, i.Tenant!.Party!.NameAr, i.Tenant.Party!.NameEn }).ToListAsync(ct);
        invs = invs.Where(i => BranchOk(i.BranchId)).ToList();
        var paidInv = await Billing.PaidByInvoiceAsync(invs.Select(i => i.Id));
        var invIds = invs.Select(i => i.Id).ToList();
        var inst = await Db.InvoiceInstallments.AsNoTracking().Where(s => invIds.Contains(s.InvoiceId) && s.Status != "cancelled")
            .OrderBy(s => s.InvoiceId).ThenBy(s => s.SeqNo).ToListAsync(ct);
        foreach (var i in invs)
        {
            var paid = paidInv.GetValueOrDefault(i.Id);
            var who = Loc(i.NameEn, i.NameAr);
            var mine = inst.Where(s => s.InvoiceId == i.Id).ToList();
            if (mine.Count == 0)
            {
                var rem = Math.Max(0, i.Total - paid);
                if (!Keep(i.DueDate, rem)) continue;
                list.Add(new ScheduleItem(i.DueDate, "in", "invoice", SourceLabels["invoice"], i.InvoiceNo, who, i.BuildingId, i.Bld, i.Total, Math.Min(paid, i.Total), rem,
                    StatusOf(i.DueDate, paid, rem), "confirmed", "invoice", i.Id, Late(i.DueDate, rem), i.BranchId, null));
                continue;
            }
            // payments settle the instalments oldest first
            var left = paid;
            foreach (var s in mine)
            {
                var p = Math.Min(left, s.Amount);
                left -= p;
                var rem = s.Amount - p;
                if (!Keep(s.DueDate, rem)) continue;
                var st = s.Status == "on_hold" && rem > 0 ? "on_hold" : StatusOf(s.DueDate, p, rem);
                list.Add(new ScheduleItem(s.DueDate, "in", "installment", SourceLabels["installment"], $"{i.InvoiceNo} / {s.SeqNo}", who, i.BuildingId, i.Bld,
                    s.Amount, p, rem, st, "confirmed", "invoice", i.Id, Late(s.DueDate, rem), i.BranchId, s.Notes));
            }
        }

        // ---------- AMC instalments not yet invoiced ----------
        var deadAmc = new List<long>();
        foreach (var c in new[] { "terminated", "expired" }) if (await Lookups.IdOrNullAsync(LookupTypes.AmcStatus, c) is long x) deadAmc.Add(x);
        var amc = await Db.AmcPaymentSchedules.AsNoTracking()
            .Where(s => s.InvoiceId == null && s.VendorBillId == null && s.DueDate <= t && s.Contract!.IsActive && (s.Contract.StatusId == null || !deadAmc.Contains(s.Contract.StatusId.Value)))
            .Select(s => new { s.Id, s.ContractId, s.DueDate, s.Amount, s.Label, s.Contract!.ContractNo, s.Contract.BranchId, s.Contract.BuildingId,
                               Bld = s.Contract.Building!.NameAr ?? s.Contract.Building.NameEn, Own = s.Contract.OwnerParty!.NameAr ?? s.Contract.OwnerParty.NameEn })
            .ToListAsync(ct);
        foreach (var s in amc.Where(s => BranchOk(s.BranchId)))
        {
            var exp = BillingService.R2(s.Amount * (1 + vat / 100m));
            list.Add(new ScheduleItem(s.DueDate, "in", "amc", SourceLabels["amc"], $"{s.ContractNo} — {s.Label}", s.Own ?? s.Bld, s.BuildingId, s.Bld, exp, 0, exp,
                StatusOf(s.DueDate, 0, exp), "confirmed", "amc", s.ContractId, Late(s.DueDate, exp), s.BranchId, "لم تصدر فاتورته بعد"));
        }

        // ---------- pending cheques (received, not yet collected) ----------
        var pendingPay = await Lookups.IdOrNullAsync(LookupTypes.PaymentStatus, "pending");
        if (pendingPay != null)
        {
            var cheques = await Db.Payments.AsNoTracking().Where(p => p.StatusId == pendingPay && p.PaymentDate <= t)
                .Select(p => new { p.Id, p.BranchId, p.ReceiptNo, p.PaymentDate, p.Amount, p.ReferenceNo, p.Tenant!.Party!.NameAr, p.Tenant.Party!.NameEn }).ToListAsync(ct);
            foreach (var p in cheques.Where(p => BranchOk(p.BranchId)))
            {
                var date = p.PaymentDate < Today ? Today : p.PaymentDate; // expected to clear; never shown as overdue
                list.Add(new ScheduleItem(date, "in", "cheque", SourceLabels["cheque"], $"{p.ReceiptNo}{(p.ReferenceNo != null ? $" ({p.ReferenceNo})" : "")}",
                    Loc(p.NameEn, p.NameAr), null, null, p.Amount, 0, p.Amount, "due", "committed", "receipt", p.Id, 0, p.BranchId, "شيك مستلم بانتظار التحصيل"));
            }
        }

        // ---------- supply contracts: future monthly fees not yet invoiced (estimate) ----------
        var monthly = await Lookups.IdOrNullAsync(LookupTypes.FeeCycle, "monthly");
        if (monthly != null)
        {
            var firstMonth = new DateOnly(Today.Year, Today.Month, 1);
            var fees = await Db.ContractFees.AsNoTracking()
                .Where(fe => fe.IsActive && fe.CycleId == monthly && fe.Contract!.IsActive && (fe.EffectiveTo == null || fe.EffectiveTo >= firstMonth))
                .Select(fe => new { fe.ContractId, fe.Amount, fe.EffectiveFrom, fe.EffectiveTo, fe.Contract!.ContractNo, fe.Contract.BranchId, fe.Contract.EndDate,
                                    BuildingId = (long?)fe.Contract.Unit!.BuildingId, Bld = fe.Contract.Unit.Building!.NameAr ?? fe.Contract.Unit.Building.NameEn,
                                    Unit = fe.Contract.Unit.UnitNo, ((SupplyContract)fe.Contract).Tenant!.Party!.NameAr, ((SupplyContract)fe.Contract).Tenant!.Party!.NameEn })
                .ToListAsync(ct);
            var contractIds = fees.Select(x => x.ContractId).Distinct().ToList();
            var billed = await Db.Invoices.AsNoTracking()
                .Where(i => i.ContractId != null && contractIds.Contains(i.ContractId.Value) && i.BillingPeriodStart != null && (i.StatusId == null || !deadInv.Contains(i.StatusId.Value)))
                .Select(i => new { ContractId = i.ContractId!.Value, Start = i.BillingPeriodStart!.Value }).ToListAsync(ct);
            var billedSet = billed.Select(b => (b.ContractId, b.Start.Year, b.Start.Month)).ToHashSet();
            foreach (var g in fees.Where(x => BranchOk(x.BranchId)).GroupBy(x => x.ContractId))
            {
                var c = g.First();
                for (var m = firstMonth; m <= t; m = m.AddMonths(1))
                {
                    var monthEnd = m.AddMonths(1).AddDays(-1);
                    if (c.EndDate != null && m > c.EndDate) break;
                    if (billedSet.Contains((g.Key, m.Year, m.Month))) continue;
                    var amount = g.Where(x => x.EffectiveFrom <= monthEnd && (x.EffectiveTo == null || x.EffectiveTo >= m)).Sum(x => x.Amount);
                    if (amount <= 0) continue;
                    var due = monthEnd.AddDays(Billing.DueDays); // billed at month end, due after the normal term
                    if (due < f || due > t) continue;
                    var exp = BillingService.R2(amount * (1 + vat / 100m));
                    list.Add(new ScheduleItem(due, "in", "contract_fee", SourceLabels["contract_fee"], $"{c.ContractNo} — {m:yyyy-MM}", Loc(c.NameEn, c.NameAr),
                        c.BuildingId, c.Bld, exp, 0, exp, "due", "estimate", "supply_contract", g.Key, 0, c.BranchId, $"الرسوم الشهرية لوحدة {c.Unit} (بدون الاستهلاك)"));
                }
            }
        }

        // ---------- supplier bills ----------
        var deadBill = new List<long>();
        foreach (var c in new[] { "draft", "cancelled" }) if (await Lookups.IdOrNullAsync(LookupTypes.BillStatus, c) is long x) deadBill.Add(x);
        var billQ = Db.VendorBills.AsNoTracking().Where(b => b.StatusId == null || !deadBill.Contains(b.StatusId.Value));
        billQ = includePaid ? billQ.Where(b => b.BillDate <= t) : billQ.Where(b => (b.DueDate ?? b.BillDate) <= t);
        var bills = await billQ.Select(b => new { b.Id, b.BranchId, b.BillNo, b.BillDate, b.DueDate, Total = b.TotalAmount ?? 0, b.SourceType, b.SourceId,
                                                  b.Vendor!.Party!.NameAr, b.Vendor.Party!.NameEn }).ToListAsync(ct);
        bills = bills.Where(b => BranchOk(b.BranchId)).ToList();
        var paidBill = await Billing.PaidByBillAsync(bills.Select(b => b.Id));
        // building of refill bills (through the refill request's tank)
        var refillBillIds = bills.Where(b => b.SourceType == "refill_request" && b.SourceId != null).Select(b => b.SourceId!.Value).ToList();
        var refillBld = await Db.RefillRequests.AsNoTracking().Where(r => refillBillIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Tank!.BuildingId, Bld = r.Tank.Building!.NameAr ?? r.Tank.Building.NameEn }).ToDictionaryAsync(r => r.Id, ct);
        foreach (var b in bills)
        {
            var paid = paidBill.GetValueOrDefault(b.Id);
            var rem = Math.Max(0, b.Total - paid);
            var date = b.DueDate ?? b.BillDate;
            if (!Keep(date, rem)) continue;
            var rb = b.SourceType == "refill_request" && b.SourceId != null ? refillBld.GetValueOrDefault(b.SourceId.Value) : null;
            var note = b.SourceType switch { "refill_request" => "تعبئة غاز", "cd_renewal" => "رسوم الدفاع المدني", "amc" => "عقد صيانة", _ => null };
            list.Add(new ScheduleItem(date, "out", "vendor_bill", SourceLabels["vendor_bill"], b.BillNo, Loc(b.NameEn, b.NameAr), rb?.BuildingId, rb?.Bld,
                b.Total, Math.Min(paid, b.Total), rem, StatusOf(date, paid, rem), "confirmed", "vendor_bill", b.Id, Late(date, rem), b.BranchId, note));
        }

        // ---------- refill purchase orders not yet billed (committed) ----------
        var deadRefill = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var pos = await Db.RefillPos.AsNoTracking()
            .Where(p => p.RefillRequest!.StatusId != deadRefill && !Db.RefillPurchaseInvoices.Any(x => x.RefillRequestId == p.RefillRequestId))
            .Select(p => new { p.Id, p.RefillRequestId, p.PoNumber, p.PoDate, Total = p.TotalAmount ?? ((p.QtyLitres ?? 0) * (p.UnitPrice ?? 0)),
                               p.RefillRequest!.BranchId, p.RefillRequest.RequestNo, BuildingId = (long?)p.RefillRequest.Tank!.BuildingId,
                               Bld = p.RefillRequest.Tank.Building!.NameAr ?? p.RefillRequest.Tank.Building.NameEn, p.Vendor!.Party!.NameAr, p.Vendor.Party!.NameEn })
            .ToListAsync(ct);
        var reqIds = pos.Select(p => p.RefillRequestId).ToList();
        var deadPay = await Billing.DeadPaymentStatusIdsAsync();
        var prepaid = await (from sp in Db.RefillSupplierPayments.AsNoTracking()
                             join vp in Db.VendorPayments on sp.VendorPaymentId equals vp.Id
                             where reqIds.Contains(sp.RefillRequestId) && (vp.StatusId == null || !deadPay.Contains(vp.StatusId.Value))
                             group vp by sp.RefillRequestId into g
                             select new { g.Key, Paid = g.Sum(v => v.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Paid, ct);
        foreach (var p in pos.Where(p => BranchOk(p.BranchId)))
        {
            var paid = prepaid.GetValueOrDefault(p.RefillRequestId);
            var rem = Math.Max(0, p.Total - paid);
            var date = p.PoDate < Today && rem > 0 ? Today : p.PoDate; // payment due on the PO stage — show it from today
            if (!Keep(date, rem)) continue;
            list.Add(new ScheduleItem(date, "out", "refill_po", SourceLabels["refill_po"], $"{p.PoNumber} ({p.RequestNo})", Loc(p.NameEn, p.NameAr), p.BuildingId, p.Bld,
                p.Total, Math.Min(paid, p.Total), rem, rem > 0 ? (paid > 0 ? "partial" : "due") : "paid", "committed", "refill", p.RefillRequestId, 0, p.BranchId,
                "أمر شراء بدون فاتورة مورد بعد"));
        }

        // ---------- manual expected items ----------
        var manual = await Db.CashflowItems.AsNoTracking().Where(m => m.IsActive)
            .Select(m => new { M = m, Bld = m.Building!.NameAr ?? m.Building.NameEn }).ToListAsync(ct);
        foreach (var x in manual.Where(x => x.M.BranchId == null ? (branchId == null) : BranchOk(x.M.BranchId.Value)))
        {
            var startFrom = f < Today ? Today : f; // expectations are not tracked as paid, so only future occurrences are shown
            foreach (var d in Occurrences(x.M, startFrom, t))
                list.Add(new ScheduleItem(d, x.M.Direction, "manual", SourceLabels["manual"], x.M.Category, x.M.Counterparty, x.M.BuildingId, x.Bld,
                    x.M.Amount, 0, x.M.Amount, "due", x.M.Confidence, "manual", x.M.Id, 0, x.M.BranchId ?? 0, x.M.Description));
        }
        return list;
    }

    private static IEnumerable<DateOnly> Occurrences(CashflowItem m, DateOnly from, DateOnly to)
    {
        var step = m.Recurrence switch { "monthly" => 1, "quarterly" => 3, "yearly" => 12, _ => 0 };
        var last = m.EndDate != null && m.EndDate < to ? m.EndDate.Value : to;
        if (step == 0)
        {
            if (m.StartDate >= from && m.StartDate <= to) yield return m.StartDate;
            yield break;
        }
        for (var n = 0; n < 1000; n++)
        {
            var d = m.StartDate.AddMonths(n * step);
            if (d > last) yield break;
            if (d >= from) yield return d;
        }
    }

    /// <summary>Cash / bank accounts: debit accounts mapped to customer receipt methods (except cheques under collection) and credit accounts mapped to supplier payment methods.</summary>
    private async Task<List<long>> CashAccountIdsAsync()
    {
        var cheque = await Lookups.IdOrNullAsync(LookupTypes.PaymentMethod, "cheque");
        var rows = await Db.AccountMappings.AsNoTracking().Where(m => m.IsActive && (m.SourceType == MappingSources.Payment || m.SourceType == MappingSources.VendorPayment))
            .Select(m => new { m.SourceType, m.SourceKeyId, m.DebitAccountId, m.CreditAccountId }).ToListAsync();
        var ids = new HashSet<long>();
        foreach (var r in rows)
        {
            if (r.SourceType == MappingSources.Payment && r.DebitAccountId != null && (cheque == null || r.SourceKeyId != cheque)) ids.Add(r.DebitAccountId.Value);
            if (r.SourceType == MappingSources.VendorPayment && r.CreditAccountId != null) ids.Add(r.CreditAccountId.Value);
        }
        // registered bank / cash accounts (Banking) — cheque-clearing accounts are not registered there
        foreach (var gl in await Db.BankAccounts.AsNoTracking().Where(b => b.IsActive).Select(b => b.GlAccountId).ToListAsync()) ids.Add(gl);
        return ids.ToList();
    }

    private static DateOnly PeriodStart(DateOnly d, string g) => g switch
    {
        "month" => new DateOnly(d.Year, d.Month, 1),
        "week" => d.AddDays(-(((int)d.DayOfWeek + 1) % 7)), // weeks start on Saturday
        _ => d,
    };
    private static DateOnly PeriodEnd(DateOnly start, string g) => g switch
    {
        "month" => start.AddMonths(1).AddDays(-1),
        "week" => start.AddDays(6),
        _ => start,
    };
    private static string PeriodLabel(DateOnly d, string g) => g switch { "month" => d.ToString("yyyy-MM"), "week" => $"أسبوع {d:MM-dd}", _ => d.ToString("MM-dd") };
}
