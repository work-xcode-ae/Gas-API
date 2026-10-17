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
            .Select(i => new { i.Id, no = i.InvoiceNo, i.Tenant!.NameEn, i.Tenant.NameAr, BEn = i.Building!.NameEn, BAr = i.Building.NameAr, amount = i.TotalAmount, i.StatusId, i.DueDate }).ToListAsync(ct);
        var recentReadings = await Db.MeterReadings.Where(r => all || branches.Contains(r.Meter!.BranchId)).OrderByDescending(r => r.ReadingDate).Take(6)
            .Select(r => new { r.Id, r.ReadingDate, r.Meter!.MeterNo, r.ReadingValue, r.Consumption,
                               B = Db.MeterAssignments.Where(a => a.MeterId == r.MeterId && a.ToDate == null).Select(a => new { a.Unit!.Building!.NameEn, a.Unit.Building.NameAr, a.Unit.UnitNo }).FirstOrDefault() })
            .ToListAsync(ct);
        var srOpen = new[] { await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "pending"), await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "in_progress") };
        var recentRequests = await Db.ServiceRequests.Where(r => (all || branches.Contains(r.BranchId)) && srOpen.Contains(r.StatusId)).OrderByDescending(r => r.RequestedAt).Take(6)
            .Select(r => new { r.Id, no = r.RequestNo, r.Description, r.Building!.NameEn, r.Building.NameAr, UnitNo = r.Unit!.UnitNo, r.StatusId, r.PriorityId, r.RequestedAt }).ToListAsync(ct);

        var sixMonths = DateTimeOffset.UtcNow.AddMonths(-6);
        var refillByBuilding = await Db.TankRefills.Where(t => t.RefillDatetime >= sixMonths && (all || branches.Contains(t.Tank!.BranchId)))
            .GroupBy(t => new { t.Tank!.BuildingId, t.Tank.Building!.Code, t.Tank.Building.NameEn, t.Tank.Building.NameAr })
            .Select(g => new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Litres = g.Sum(t => t.DeliveredQtyLitres ?? 0), Count = g.Count() })
            .OrderByDescending(x => x.Litres).Take(10).ToListAsync(ct);
        var recentRefills = await Db.RefillRequests.Where(r => all || branches.Contains(r.BranchId)).OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id).Take(6)
            .Select(r => new { r.Id, r.RequestNo, r.RequestDate, r.RequestedQtyLitres, r.PriorityId, r.StatusId, r.CurrentStageId, r.Tank!.TankNo, r.Tank.CapacityLitres, r.Tank.CurrentLevelLitres,
                               r.Tank.BuildingId, r.Tank.Building!.NameEn, r.Tank.Building.NameAr }).ToListAsync(ct);

        var alerts = await HttpContext.RequestServices.GetRequiredService<AlertsQuery>().GetAsync(30, branches);

        static decimal Growth(decimal now, decimal before) => before == 0 ? (now > 0 ? 100 : 0) : Math.Round((now - before) / before * 100, 1);

        return Ok(new
        {
            kpis = new
            {
                buildings = await buildings.CountAsync(ct),
                apartments = await Db.Units.CountAsync(u => buildings.Any(b => b.Id == u.BuildingId), ct),
                activeCustomers = await Db.SupplyContracts.Where(c => c.IsActive && (all || branches.Contains(c.BranchId))).Select(c => c.TenantId).Distinct().CountAsync(ct),
                activeContracts = await Db.SupplyContracts.CountAsync(c => c.IsActive && (all || branches.Contains(c.BranchId)), ct),
                meters = await Db.Meters.CountAsync(m => all || branches.Contains(m.BranchId), ct),
                totalInvoices = invoicedYear, invoicesGrowth = Growth(invThisMonth, invLastMonth),
                collected = collectedYear, collectedGrowth = Growth(colThisMonth, colLastMonth),
                collectionRate = invoicedYear == 0 ? 0 : Math.Round(collectedYear / invoicedYear * 100, 1),
                overdue = overdueRows.Sum(x => x.Total - x.Paid), overdueClients = overdueRows.Select(x => x.TenantId).Distinct().Count(),
                lowTanks = await Db.Tanks.CountAsync(t => (all || branches.Contains(t.BranchId)) && t.CapacityLitres > 0 && (t.CurrentLevelLitres ?? 0) * 100 / t.CapacityLitres < TankRules.LowLevelPercent, ct),
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
            notifications = alerts.Take(10),
            alertsCount = alerts.Count,
        });
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
