using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Main dashboard (Dashboard.tsx): KPI cards, charts, recent tables and notifications in one call.</summary>
[Route("api/dashboard")]
[PermissionModule("dashboard")]
public class DashboardController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long? branchId, CancellationToken ct)
    {
        await LoadLookups();
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var balances = HttpContext.RequestServices.GetRequiredService<BalanceQueries>();
        var (deadInv, deadPay) = await balances.DeadStatusesAsync();
        var branches = branchId != null ? new List<long> { branchId.Value } : (Me.IsAdmin ? new List<long>() : Me.BranchIds.ToList());
        var all = branches.Count == 0;

        var yearStart = new DateOnly(Today.Year, 1, 1);
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var from12 = monthStart.AddMonths(-11);

        var buildings = Db.Buildings.Where(b => all || branches.Contains(b.BranchId));
        var invoices = Db.Invoices.Where(i => (all || branches.Contains(i.BranchId)) && !deadInv.Contains(i.StatusId));
        var payments = Db.Payments.Where(p => (all || branches.Contains(p.BranchId)) && !deadPay.Contains(p.StatusId));

        var invoicedYear = await invoices.Where(i => i.IssueDate >= yearStart).SumAsync(i => i.TotalAmount ?? 0, ct);
        var collectedYear = await payments.Where(p => p.PaymentDate >= yearStart).SumAsync(p => p.Amount, ct);
        var invThisMonth = await invoices.Where(i => i.IssueDate >= monthStart).SumAsync(i => i.TotalAmount ?? 0, ct);
        var invLastMonth = await invoices.Where(i => i.IssueDate >= lastMonthStart && i.IssueDate < monthStart).SumAsync(i => i.TotalAmount ?? 0, ct);
        var colThisMonth = await payments.Where(p => p.PaymentDate >= monthStart).SumAsync(p => p.Amount, ct);
        var colLastMonth = await payments.Where(p => p.PaymentDate >= lastMonthStart && p.PaymentDate < monthStart).SumAsync(p => p.Amount, ct);

        var overdueRows = await invoices.Where(i => i.DueDate < Today)
            .Select(i => new { i.TenantId, Total = i.TotalAmount ?? 0, Paid = Db.PaymentAllocations.Where(a => a.InvoiceId == i.Id && !deadPay.Contains(a.Payment!.StatusId)).Sum(a => (decimal?)a.Amount) ?? 0 })
            .Where(x => x.Total > x.Paid).ToListAsync(ct);

        var revenueInv = await invoices.Where(i => i.IssueDate >= from12).GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.TotalAmount ?? 0) }).ToListAsync(ct);
        var revenuePay = await payments.Where(p => p.PaymentDate >= from12).GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.Amount) }).ToListAsync(ct);
        var months = Enumerable.Range(0, 12).Select(n => from12.AddMonths(n)).ToList();

        var invStatus = await Db.Invoices.Where(i => (all || branches.Contains(i.BranchId)) && i.IssueDate >= yearStart)
            .GroupBy(i => i.StatusId).Select(g => new { g.Key, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount ?? 0) }).ToListAsync(ct);
        var contractStatus = await Db.SupplyContracts.Where(c => all || branches.Contains(c.BranchId))
            .GroupBy(c => c.StatusId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);

        var recentInvoices = await invoices.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id).Take(6)
            .Select(i => new { i.Id, no = i.InvoiceNo, i.Tenant!.Party!.NameEn, i.Tenant.Party!.NameAr, BEn = i.Building!.NameEn, BAr = i.Building.NameAr, amount = i.TotalAmount, i.StatusId, i.DueDate }).ToListAsync(ct);
        var recentReadings = await Db.MeterReadings.Where(r => all || branches.Contains(r.Meter!.BranchId ?? 0)).OrderByDescending(r => r.ReadingDate).Take(6)
            .Select(r => new { r.Id, r.ReadingDate, r.Meter!.MeterNo, r.ReadingValue, r.Consumption,
                               B = Db.MeterAssignments.Where(a => a.MeterId == r.MeterId && a.ToDate == null).Select(a => new { a.Unit!.Building!.NameEn, a.Unit.Building.NameAr, a.Unit.UnitNo }).FirstOrDefault() })
            .ToListAsync(ct);
        var srOpen = new[] { await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "pending"), await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "in_progress") };
        var recentRequests = await Db.ServiceRequests.Where(r => (all || branches.Contains(r.BranchId)) && srOpen.Contains(r.StatusId)).OrderByDescending(r => r.RequestedAt).Take(6)
            .Select(r => new { r.Id, no = r.RequestNo, r.Description, r.Building!.NameEn, r.Building.NameAr, UnitNo = r.Unit!.UnitNo, r.StatusId, r.PriorityId, r.RequestedAt }).ToListAsync(ct);

        var sixMonths = DateTimeOffset.UtcNow.AddMonths(-6);
        var refillByBuilding = await Db.TankRefills.Where(t => t.RefillDatetime >= sixMonths && (all || branches.Contains(t.Tank!.BranchId ?? 0)))
            .GroupBy(t => new { t.Tank!.BuildingId, t.Tank.Building!.Code, t.Tank.Building.NameEn, t.Tank.Building.NameAr })
            .Select(g => new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Litres = g.Sum(t => t.DeliveredQtyLitres ?? 0), Count = g.Count() })
            .OrderByDescending(x => x.Litres).Take(10).ToListAsync(ct);
        var recentRefills = await Db.RefillRequests.Where(r => all || branches.Contains(r.BranchId)).OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id).Take(6)
            .Select(r => new { r.Id, r.RequestNo, r.RequestDate, r.RequestedQtyLitres, r.PriorityId, r.StatusId, r.CurrentStageId, r.Tank!.TankNo, r.Tank.CapacityLitres, r.Tank.CurrentLevelLitres,
                               r.Tank.BuildingId, r.Tank.Building!.NameEn, r.Tank.Building.NameAr }).ToListAsync(ct);

        var alerts = await HttpContext.RequestServices.GetRequiredService<AlertsQuery>().GetAsync(30, branches);

        // refill workflow counters (dashboard refill cards)
        var refillDone = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed");
        var refillCancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var approvalStage = await Lookups.IdOrNullAsync(LookupTypes.RefillStage, "7");
        var refillScope = Db.RefillRequests.Where(r => all || branches.Contains(r.BranchId));
        var monthStartTs = new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var refillStats = new
        {
            open = await refillScope.CountAsync(r => r.StatusId != refillDone && r.StatusId != refillCancelled, ct),
            pendingApproval = await refillScope.CountAsync(r => r.StatusId != refillDone && r.StatusId != refillCancelled && r.CurrentStageId == approvalStage, ct),
            completedThisMonth = await refillScope.CountAsync(r => r.StatusId == refillDone && r.CompletedAt != null && r.CompletedAt >= monthStartTs, ct),
        };

        static decimal Growth(decimal now, decimal before) => before == 0 ? (now > 0 ? 100 : 0) : Math.Round((now - before) / before * 100, 1);

        return Ok(new
        {
            kpis = new
            {
                buildings = await buildings.CountAsync(ct),
                apartments = await Db.Units.CountAsync(u => buildings.Any(b => b.Id == u.BuildingId), ct),
                activeCustomers = await Db.SupplyContracts.Where(c => c.IsActive && (all || branches.Contains(c.BranchId))).Select(c => c.TenantId).Distinct().CountAsync(ct),
                activeContracts = await Db.SupplyContracts.CountAsync(c => c.IsActive && (all || branches.Contains(c.BranchId)), ct),
                meters = await Db.Meters.CountAsync(m => all || branches.Contains(m.BranchId ?? 0), ct),
                totalInvoices = invoicedYear, invoicesGrowth = Growth(invThisMonth, invLastMonth),
                collected = collectedYear, collectedGrowth = Growth(colThisMonth, colLastMonth),
                collectionRate = invoicedYear == 0 ? 0 : Math.Round(collectedYear / invoicedYear * 100, 1),
                overdue = overdueRows.Sum(x => x.Total - x.Paid), overdueClients = overdueRows.Select(x => x.TenantId).Distinct().Count(),
                lowTanks = await Db.Tanks.CountAsync(t => (all || branches.Contains(t.BranchId ?? 0)) && t.CapacityLitres > 0 && (t.CurrentLevelLitres ?? 0) * 100 / t.CapacityLitres < TankRules.LowLevelPercent, ct),
                openServiceRequests = await Db.ServiceRequests.CountAsync(r => (all || branches.Contains(r.BranchId)) && srOpen.Contains(r.StatusId), ct),
            },
            revenue = new
            {
                categories = months.Select(m => $"{m:yyyy-MM}"),
                invoiced = months.Select(m => revenueInv.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Amount ?? 0),
                collected = months.Select(m => revenuePay.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Amount ?? 0),
            },
            invoiceStatus = invStatus.Select(s => new { status = Code(s.Key), label = Label(s.Key), s.Count, s.Amount }),
            contractStatus = contractStatus.Select(s => new { status = Code(s.Key), label = Label(s.Key), s.Count }),
            recentInvoices = recentInvoices.Select(i => new { i.Id, i.no, customer = Loc(i.NameEn, i.NameAr), building = Loc(i.BEn, i.BAr), i.amount,
                                                             status = Code(i.StatusId) is "pending" or "partial" && i.DueDate < Today ? "overdue" : Code(i.StatusId) }),
            recentReadings = recentReadings.Select(r => new { r.Id, date = r.ReadingDate, meterNo = r.MeterNo, building = r.B == null ? null : Loc(r.B.NameEn, r.B.NameAr), unit = r.B?.UnitNo,
                                                             current = r.ReadingValue, previous = r.ReadingValue - (r.Consumption ?? 0), consumption = r.Consumption }),
            serviceRequests = recentRequests.Select(r => new { r.Id, r.no, type = r.Description, location = $"{Loc(r.NameEn, r.NameAr)}{(r.UnitNo == null ? "" : " – " + r.UnitNo)}",
                                                              status = Code(r.StatusId), priority = Code(r.PriorityId), r.RequestedAt }),
            refillByBuilding = refillByBuilding.Select(x => new { x.BuildingId, x.Code, building = Loc(x.NameEn, x.NameAr), litres = x.Litres, refills = x.Count }),
            recentRefills = recentRefills.Select(r => new { r.Id, r.RequestNo, r.RequestDate, qty = r.RequestedQtyLitres, r.TankNo, r.BuildingId, building = Loc(r.NameEn, r.NameAr),
                                                           levelPct = r.CapacityLitres > 0 ? Math.Round((r.CurrentLevelLitres ?? 0) / r.CapacityLitres * 100, 1) : 0,
                                                           priority = Code(r.PriorityId), status = Code(r.StatusId), stage = Code(r.CurrentStageId) }),
            refillStats,
            notifications = alerts.Take(10),
            alertsCount = alerts.Count,
        });
    }
}

/// <summary>
/// Dashboard report for a period (default: this month) with the previous period of the same length for comparison:
/// sales / collections / purchases / consumption / refills, A/R aging and top debtors, top buildings, 12-month trend,
/// meters by status and calibration, occupancy, cash &amp; bank balance, alerts and data-quality checks.
/// The portal writes the executive summary from these figures.
/// </summary>
[Route("api/dashboard/summary")]
[PermissionModule("dashboard")]
public class DashboardSummaryController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId, CancellationToken ct)
    {
        await LoadLookups();
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var balances = HttpContext.RequestServices.GetRequiredService<BalanceQueries>();
        var (deadInv, deadPay) = await balances.DeadStatusesAsync();
        var br = branchId != null ? new List<long> { branchId.Value } : (Me.IsAdmin ? new List<long>() : Me.BranchIds.ToList());
        var all = br.Count == 0;

        var f = from ?? new DateOnly(Today.Year, Today.Month, 1);
        var t = to ?? Today;
        if (t < f) (f, t) = (t, f);
        var days = t.DayNumber - f.DayNumber + 1;
        var pt = f.AddDays(-1);
        var pf = pt.AddDays(-(days - 1));
        static DateTimeOffset Ts(DateOnly d) => new(d.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(4));

        var invoices = Db.Invoices.AsNoTracking().Where(i => (all || br.Contains(i.BranchId)) && !deadInv.Contains(i.StatusId));
        var payments = Db.Payments.AsNoTracking().Where(p => (all || br.Contains(p.BranchId)) && !deadPay.Contains(p.StatusId));
        var deadBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var draftBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "draft");
        var bills = Db.VendorBills.AsNoTracking().Where(b => (all || br.Contains(b.BranchId)) && b.StatusId != deadBill && b.StatusId != draftBill);
        var readings = Db.MeterReadings.AsNoTracking().Where(r => all || br.Contains(r.Meter!.BranchId ?? 0));
        var refills = Db.TankRefills.AsNoTracking().Where(r => all || br.Contains(r.Tank!.BranchId ?? 0));

        async Task<object> Period(DateOnly a, DateOnly b)
        {
            var ta = Ts(a); var tb = Ts(b.AddDays(1));
            var inv = await invoices.Where(i => i.IssueDate >= a && i.IssueDate <= b)
                .GroupBy(_ => 1).Select(g => new { Total = g.Sum(i => i.TotalAmount ?? 0), Vat = g.Sum(i => i.VatAmount ?? 0), Count = g.Count() }).FirstOrDefaultAsync(ct);
            var pay = await payments.Where(p => p.PaymentDate >= a && p.PaymentDate <= b)
                .GroupBy(_ => 1).Select(g => new { Total = g.Sum(p => p.Amount), Count = g.Count() }).FirstOrDefaultAsync(ct);
            decimal credits = 0;
            try
            {
                credits = await Db.CreditNotes.AsNoTracking().Where(c => (all || br.Contains(c.BranchId)) && c.Status != "cancelled" && c.IssueDate >= a && c.IssueDate <= b)
                    .SumAsync(c => (decimal?)c.TotalAmount, ct) ?? 0;
            }
            catch { /* table not migrated yet */ }
            var bill = await bills.Where(x => x.BillDate >= a && x.BillDate <= b)
                .GroupBy(_ => 1).Select(g => new { Total = g.Sum(x => x.TotalAmount ?? 0), Vat = g.Sum(x => x.VatAmount ?? 0), Count = g.Count() }).FirstOrDefaultAsync(ct);
            var cons = await readings.Where(r => r.ReadingDate >= ta && r.ReadingDate < tb).SumAsync(r => (decimal?)r.Consumption, ct) ?? 0;
            var readCount = await readings.CountAsync(r => r.ReadingDate >= ta && r.ReadingDate < tb, ct);
            var ref1 = await refills.Where(r => r.RefillDatetime >= ta && r.RefillDatetime < tb)
                .GroupBy(_ => 1).Select(g => new { Litres = g.Sum(r => r.DeliveredQtyLitres ?? 0), Count = g.Count() }).FirstOrDefaultAsync(ct);
            var newContracts = await Db.SupplyContracts.CountAsync(c => (all || br.Contains(c.BranchId)) && c.StartDate >= a && c.StartDate <= b, ct);
            return new
            {
                from = a, to = b,
                invoiced = inv?.Total ?? 0, invoiceCount = inv?.Count ?? 0, outputVat = inv?.Vat ?? 0, creditNotes = credits,
                collected = pay?.Total ?? 0, receiptCount = pay?.Count ?? 0,
                purchases = bill?.Total ?? 0, billCount = bill?.Count ?? 0, inputVat = bill?.Vat ?? 0,
                consumptionM3 = cons, readings = readCount, refillLitres = ref1?.Litres ?? 0, refills = ref1?.Count ?? 0, newContracts,
            };
        }
        var current = await Period(f, t);
        var previous = await Period(pf, pt);

        // ---- receivables aging (today) ----
        var issuedOpen = await invoices.Where(i => (i.TotalAmount ?? 0) > 0)
            .Select(i => new { i.Id, i.TenantId, i.BuildingId, i.DueDate, Total = i.TotalAmount ?? 0, i.Tenant!.Party!.NameEn, i.Tenant.Party!.NameAr }).ToListAsync(ct);
        var paidMap = await billing.PaidByInvoiceAsync(issuedOpen.Select(i => i.Id));
        var open = issuedOpen.Select(i => new { i.TenantId, i.BuildingId, i.DueDate, i.NameEn, i.NameAr, Due = Math.Max(0, i.Total - paidMap.GetValueOrDefault(i.Id)) })
            .Where(i => i.Due > 0.009m).ToList();
        int Late(DateOnly d) => Today.DayNumber - d.DayNumber;
        var aging = new
        {
            notDue = open.Where(i => Late(i.DueDate) <= 0).Sum(i => i.Due),
            d1_30 = open.Where(i => Late(i.DueDate) is > 0 and <= 30).Sum(i => i.Due),
            d31_60 = open.Where(i => Late(i.DueDate) is > 30 and <= 60).Sum(i => i.Due),
            d61_90 = open.Where(i => Late(i.DueDate) is > 60 and <= 90).Sum(i => i.Due),
            d90p = open.Where(i => Late(i.DueDate) > 90).Sum(i => i.Due),
            total = open.Sum(i => i.Due),
            overdue = open.Where(i => Late(i.DueDate) > 0).Sum(i => i.Due),
            customers = open.Select(i => i.TenantId).Distinct().Count(),
            overdueCustomers = open.Where(i => Late(i.DueDate) > 0).Select(i => i.TenantId).Distinct().Count(),
        };
        var topDebtors = open.GroupBy(i => new { i.TenantId, i.NameEn, i.NameAr })
            .Select(g => new { g.Key.TenantId, customer = Loc(g.Key.NameEn, g.Key.NameAr), amount = g.Sum(x => x.Due),
                               overdue = g.Where(x => Late(x.DueDate) > 0).Sum(x => x.Due), oldestDays = g.Max(x => Math.Max(0, Late(x.DueDate))) })
            .OrderByDescending(x => x.amount).Take(5).ToList();

        // ---- buildings: invoiced / collected in the period ----
        var bInv = await invoices.Where(i => i.IssueDate >= f && i.IssueDate <= t && i.BuildingId != null)
            .GroupBy(i => new { i.BuildingId, i.Building!.Code, i.Building.NameEn, i.Building.NameAr })
            .Select(g => new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Invoiced = g.Sum(i => i.TotalAmount ?? 0) })
            .OrderByDescending(x => x.Invoiced).Take(5).ToListAsync(ct);
        var topBuildings = bInv.Select(b => new
        {
            b.BuildingId, code = b.Code, building = Loc(b.NameEn, b.NameAr), invoiced = b.Invoiced,
            outstanding = open.Where(o => o.BuildingId == b.BuildingId).Sum(o => o.Due),
        }).ToList();

        // ---- 12-month trend ending with the period's month ----
        var m0 = new DateOnly(t.Year, t.Month, 1).AddMonths(-11);
        var mEnd = new DateOnly(t.Year, t.Month, 1).AddMonths(1);
        var tInv = await invoices.Where(i => i.IssueDate >= m0 && i.IssueDate < mEnd).GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, A = g.Sum(i => i.TotalAmount ?? 0) }).ToListAsync(ct);
        var tPay = await payments.Where(p => p.PaymentDate >= m0 && p.PaymentDate < mEnd).GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, A = g.Sum(p => p.Amount) }).ToListAsync(ct);
        var tBill = await bills.Where(b => b.BillDate >= m0 && b.BillDate < mEnd).GroupBy(b => new { b.BillDate.Year, b.BillDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, A = g.Sum(b => b.TotalAmount ?? 0) }).ToListAsync(ct);
        var tm0 = Ts(m0); var tmEnd = Ts(mEnd);
        var tCons = (await readings.Where(r => r.ReadingDate >= tm0 && r.ReadingDate < tmEnd).Select(r => new { r.ReadingDate, r.Consumption }).ToListAsync(ct))
            .GroupBy(r => new { r.ReadingDate.ToOffset(TimeSpan.FromHours(4)).Year, r.ReadingDate.ToOffset(TimeSpan.FromHours(4)).Month })
            .Select(g => new { g.Key.Year, g.Key.Month, A = g.Sum(r => r.Consumption ?? 0) }).ToList();
        var monthsList = Enumerable.Range(0, 12).Select(n => m0.AddMonths(n)).ToList();
        decimal Pick<T>(IEnumerable<T> src, DateOnly m, Func<T, int> y, Func<T, int> mo, Func<T, decimal> v) => src.Where(x => y(x) == m.Year && mo(x) == m.Month).Select(v).FirstOrDefault();
        var trend = new
        {
            months = monthsList.Select(m => $"{m:yyyy-MM}"),
            invoiced = monthsList.Select(m => Pick(tInv, m, x => x.Year, x => x.Month, x => x.A)),
            collected = monthsList.Select(m => Pick(tPay, m, x => x.Year, x => x.Month, x => x.A)),
            purchases = monthsList.Select(m => Pick(tBill, m, x => x.Year, x => x.Month, x => x.A)),
            consumptionM3 = monthsList.Select(m => Pick(tCons, m, x => x.Year, x => x.Month, x => x.A)),
        };

        // ---- assets & operations ----
        var meterRows = await Db.Meters.AsNoTracking().Where(m => all || br.Contains(m.BranchId ?? 0)).Select(m => new { m.StatusId, m.CalibrationDate, m.IsActive,
            Installed = Db.MeterAssignments.Any(a => a.MeterId == m.Id && a.ToDate == null) }).ToListAsync(ct);
        var calMonths = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<int?>("Meters:CalibrationValidMonths") ?? 12;
        var meters = new
        {
            total = meterRows.Count, installed = meterRows.Count(m => m.Installed),
            byStatus = meterRows.GroupBy(m => m.StatusId).Select(g => new { status = Code(g.Key) ?? "unknown", label = Label(g.Key) ?? "—", count = g.Count() }).OrderByDescending(x => x.count).ToList(),
            calibrationExpired = meterRows.Count(m => m.IsActive && m.CalibrationDate != null && m.CalibrationDate.Value.AddMonths(calMonths) <= Today),
            noCalibration = meterRows.Count(m => m.IsActive && m.CalibrationDate == null),
        };
        var units = await Db.Units.AsNoTracking().Where(u => all || br.Contains(u.Building!.BranchId)).Select(u => new
        {
            u.Id, Occupied = Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive), HasMeter = Db.MeterAssignments.Any(a => a.UnitId == u.Id && a.ToDate == null),
        }).ToListAsync(ct);
        var occupancy = new { units = units.Count, occupied = units.Count(u => u.Occupied), vacant = units.Count(u => !u.Occupied), withoutMeter = units.Count(u => !u.HasMeter),
                              occupiedWithoutMeter = units.Count(u => u.Occupied && !u.HasMeter) };

        var srOpenIds = new[] { await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "pending"), await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "in_progress") };
        var weekAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var refillDone = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed");
        var refillCancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var pipeline = await Db.RefillRequests.AsNoTracking().Where(r => (all || br.Contains(r.BranchId)) && r.StatusId != refillDone && r.StatusId != refillCancelled)
            .GroupBy(r => r.CurrentStageId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var tanks = await Db.Tanks.AsNoTracking().Where(x => (all || br.Contains(x.BranchId ?? 0)) && x.CapacityLitres > 0)
            .Select(x => new { x.Id, x.TankNo, x.BuildingId, x.Building!.Code, x.Building.NameEn, x.Building.NameAr, Pct = (x.CurrentLevelLitres ?? 0) * 100 / x.CapacityLitres })
            .OrderBy(x => x.Pct).Take(5).ToListAsync(ct);
        var operations = new
        {
            openServiceRequests = await Db.ServiceRequests.CountAsync(r => (all || br.Contains(r.BranchId)) && srOpenIds.Contains(r.StatusId), ct),
            staleServiceRequests = await Db.ServiceRequests.CountAsync(r => (all || br.Contains(r.BranchId)) && srOpenIds.Contains(r.StatusId) && r.RequestedAt < weekAgo, ct),
            refillPipeline = pipeline.Select(p => new { stage = Code(p.Key), label = Label(p.Key), count = p.Count }).OrderBy(p => p.stage).ToList(),
            lowestTanks = tanks.Select(x => new { x.Id, tankNo = x.TankNo, x.BuildingId, code = x.Code, building = Loc(x.NameEn, x.NameAr), levelPct = Math.Round(x.Pct, 1),
                                                  low = x.Pct < TankRules.LowLevelPercent }),
            lowTanks = await Db.Tanks.CountAsync(x => (all || br.Contains(x.BranchId ?? 0)) && x.CapacityLitres > 0 && (x.CurrentLevelLitres ?? 0) * 100 / x.CapacityLitres < TankRules.LowLevelPercent, ct),
        };

        // ---- cash & bank balance (posted journals on accounts 11xx) ----
        decimal? cash = null;
        try
        {
            var posted = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "posted");
            cash = await Db.JournalLines.AsNoTracking()
                .Where(l => l.Account!.AccountCode.StartsWith("11") && l.JournalEntry!.StatusId == posted && (all || br.Contains(l.JournalEntry.BranchId)))
                .SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0)), ct) ?? 0;
        }
        catch { /* accounting not set up */ }

        // ---- alerts & data quality ----
        var alertList = await HttpContext.RequestServices.GetRequiredService<AlertsQuery>().GetAsync(30, br);
        var alerts = new
        {
            total = alertList.Count,
            bySeverity = alertList.GroupBy(a => a.Severity).ToDictionary(g => g.Key, g => g.Count()),
            byType = alertList.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count()),
            top = alertList.Take(8),
        };
        var dq = new
        {
            invoicesWithoutJournal = await invoices.CountAsync(i => i.JournalEntryId == null, ct),
            billsWithoutJournal = await bills.CountAsync(b => b.JournalEntryId == null, ct),
            occupiedUnitsWithoutMeter = occupancy.occupiedWithoutMeter,
            activeContractsNoReading45 = await Db.SupplyContracts.CountAsync(c => (all || br.Contains(c.BranchId)) && c.IsActive
                && !Db.MeterReadings.Any(r => r.UnitId == c.UnitId && r.ReadingDate >= DateTimeOffset.UtcNow.AddDays(-45)), ct),
        };

        return Ok(new { period = new { from = f, to = t, days, prevFrom = pf, prevTo = pt }, current, previous, aging, topDebtors, topBuildings, trend, meters,
                        occupancy, operations, cash, alerts, dataQuality = dq, generatedAt = DateTimeOffset.UtcNow });
    }
}

/// <summary>Expiring / attention items (the v_expiring_items view from the schema doc).</summary>
[Route("api/alerts")]
[PermissionModule("dashboard")]
public class AlertsController : ApiControllerBase
{
    /// <summary>Everything that expires or needs attention within {days} days (default 30), most urgent first.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int days = 30, [FromQuery] long? branchId = null)
    {
        var branches = branchId != null ? new List<long> { branchId.Value } : (Me.IsAdmin ? new List<long>() : Me.BranchIds.ToList());
        var items = await HttpContext.RequestServices.GetRequiredService<AlertsQuery>().GetAsync(days, branches);
        return Ok(new { days, count = items.Count, byType = items.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count()), items });
    }
}
