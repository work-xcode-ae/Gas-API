using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Reports page: consumption, collections, A/R aging, occupancy, refills, vendor balances and VAT.</summary>
[Route("api/reports")]
[PermissionModule("reports")]
public class ReportsController : ApiControllerBase
{
    private (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to) => (from ?? new DateOnly(Today.Year, 1, 1), to ?? Today);
    private List<long> Branches(long? branchId) => branchId != null ? new() { branchId.Value } : (Me.IsAdmin ? new() : Me.BranchIds.ToList());

    /// <summary>Gas consumption (m³) by building, by month or by unit.</summary>
    [HttpGet("consumption")]
    public async Task<IActionResult> Consumption([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? buildingId, [FromQuery] long? branchId, [FromQuery] string groupBy = "building")
    {
        var (f, t) = Range(from, to);
        var fd = new DateTimeOffset(f.ToDateTime(TimeOnly.MinValue)); var td = new DateTimeOffset(t.ToDateTime(TimeOnly.MaxValue));
        var br = Branches(branchId); var all = br.Count == 0;
        var rows = from r in Db.MeterReadings.AsNoTracking()
                   join a in Db.MeterAssignments on r.MeterId equals a.MeterId
                   where r.ReadingDate >= fd && r.ReadingDate <= td && a.ToDate == null
                         && (buildingId == null || a.Unit!.BuildingId == buildingId) && (all || br.Contains(r.Meter!.BranchId))
                   select new { r.ReadingDate, Consumption = r.Consumption ?? 0, a.UnitId, a.Unit!.UnitNo, a.Unit.BuildingId, a.Unit.Building!.Code, a.Unit.Building.NameEn, a.Unit.Building.NameAr };
        switch (groupBy)
        {
            case "month":
                var m = await rows.GroupBy(x => new { x.ReadingDate.Year, x.ReadingDate.Month }).Select(g => new { g.Key.Year, g.Key.Month, M3 = g.Sum(x => x.Consumption), Readings = g.Count() })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
                return Ok(new { from = f, to = t, groupBy, rows = m.Select(x => new { month = $"{x.Year}-{x.Month:00}", m3 = x.M3, readings = x.Readings }), total = m.Sum(x => x.M3) });
            case "unit":
                var u = await rows.GroupBy(x => new { x.UnitId, x.UnitNo, x.Code }).Select(g => new { g.Key.UnitId, g.Key.UnitNo, g.Key.Code, M3 = g.Sum(x => x.Consumption), Readings = g.Count() })
                    .OrderByDescending(x => x.M3).ToListAsync();
                return Ok(new { from = f, to = t, groupBy, rows = u, total = u.Sum(x => x.M3) });
            default:
                var b = await rows.GroupBy(x => new { x.BuildingId, x.Code, x.NameEn, x.NameAr }).Select(g => new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, M3 = g.Sum(x => x.Consumption), Readings = g.Count() })
                    .OrderByDescending(x => x.M3).ToListAsync();
                return Ok(new { from = f, to = t, groupBy, rows = b.Select(x => new { x.BuildingId, x.Code, building = Loc(x.NameEn, x.NameAr), m3 = x.M3, readings = x.Readings }), total = b.Sum(x => x.M3) });
        }
    }

    /// <summary>Collections by month, payment method or building.</summary>
    [HttpGet("collections")]
    public async Task<IActionResult> Collections([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId, [FromQuery] string groupBy = "month")
    {
        await LoadLookups();
        var (f, t) = Range(from, to);
        var br = Branches(branchId); var all = br.Count == 0;
        var (_, deadPay) = await HttpContext.RequestServices.GetRequiredService<BalanceQueries>().DeadStatusesAsync();
        var q = Db.Payments.AsNoTracking().Where(p => p.PaymentDate >= f && p.PaymentDate <= t && !deadPay.Contains(p.StatusId) && (all || br.Contains(p.BranchId)));
        if (groupBy == "method")
        {
            var rows = await q.GroupBy(p => p.PaymentMethodId).Select(g => new { g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() }).ToListAsync();
            return Ok(new { from = f, to = t, groupBy, rows = rows.Select(r => new { method = Code(r.Key), label = Label(r.Key), r.Amount, r.Count }), total = rows.Sum(r => r.Amount) });
        }
        if (groupBy == "building")
        {
            var rows = await (from a in Db.PaymentAllocations join p in q on a.PaymentId equals p.Id
                              group a by new { a.Invoice!.BuildingId, a.Invoice.Building!.Code, a.Invoice.Building.NameEn, a.Invoice.Building.NameAr } into g
                              select new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Amount = g.Sum(x => x.Amount) }).OrderByDescending(x => x.Amount).ToListAsync();
            return Ok(new { from = f, to = t, groupBy, rows = rows.Select(r => new { r.BuildingId, r.Code, building = Loc(r.NameEn, r.NameAr), r.Amount }), total = rows.Sum(r => r.Amount) });
        }
        var byMonth = await q.GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month }).Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.Amount), Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
        return Ok(new { from = f, to = t, groupBy = "month", rows = byMonth.Select(x => new { month = $"{x.Year}-{x.Month:00}", x.Amount, x.Count }), total = byMonth.Sum(x => x.Amount) });
    }

    /// <summary>A/R aging per tenant: current, 1-30, 31-60, 61-90, 90+ days past due.</summary>
    [HttpGet("aging")]
    public async Task<IActionResult> Aging([FromQuery] long? buildingId, [FromQuery] long? branchId)
    {
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var open = await billing.InvoiceStatusIdsAsync("pending", "partial", "overdue");
        var br = Branches(branchId); var all = br.Count == 0;
        var inv = await Db.Invoices.AsNoTracking().Where(i => i.StatusId != null && open.Contains(i.StatusId.Value) && (buildingId == null || i.BuildingId == buildingId) && (all || br.Contains(i.BranchId)))
            .Select(i => new { i.Id, i.TenantId, i.Tenant!.NameEn, i.Tenant.NameAr, i.Tenant.Phone, i.DueDate, Total = i.TotalAmount ?? 0 }).ToListAsync();
        var paid = await billing.PaidByInvoiceAsync(inv.Select(i => i.Id));
        var rows = inv.Select(i => new { i, bal = i.Total - paid.GetValueOrDefault(i.Id), days = Today.DayNumber - i.DueDate.DayNumber }).Where(x => x.bal > 0)
            .GroupBy(x => x.i.TenantId).Select(g => new
            {
                tenantId = g.Key, tenant = Loc(g.First().i.NameEn, g.First().i.NameAr), phone = g.First().i.Phone,
                current = g.Where(x => x.days <= 0).Sum(x => x.bal), d30 = g.Where(x => x.days is > 0 and <= 30).Sum(x => x.bal),
                d60 = g.Where(x => x.days is > 30 and <= 60).Sum(x => x.bal), d90 = g.Where(x => x.days is > 60 and <= 90).Sum(x => x.bal),
                over90 = g.Where(x => x.days > 90).Sum(x => x.bal), total = g.Sum(x => x.bal),
            }).OrderByDescending(x => x.total).ToList();
        return Ok(new
        {
            asOf = Today, rows,
            totals = new { current = rows.Sum(r => r.current), d30 = rows.Sum(r => r.d30), d60 = rows.Sum(r => r.d60), d90 = rows.Sum(r => r.d90), over90 = rows.Sum(r => r.over90), total = rows.Sum(r => r.total) },
        });
    }

    /// <summary>Occupancy per building.</summary>
    [HttpGet("occupancy")]
    public async Task<IActionResult> Occupancy([FromQuery] long? branchId)
    {
        var br = Branches(branchId); var all = br.Count == 0;
        var rows = await Db.Buildings.AsNoTracking().Where(b => all || br.Contains(b.BranchId)).Select(b => new
        {
            b.Id, b.Code, b.NameEn, b.NameAr, Branch = b.Branch!.NameEn,
            Units = Db.Units.Count(u => u.BuildingId == b.Id),
            Occupied = Db.Units.Count(u => u.BuildingId == b.Id && Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive)),
        }).OrderBy(b => b.Code).ToListAsync();
        return Ok(rows.Select(r => new { r.Id, r.Code, building = Loc(r.NameEn, r.NameAr), r.Branch, r.Units, r.Occupied, vacant = r.Units - r.Occupied,
                                         rate = r.Units == 0 ? 0 : Math.Round(r.Occupied * 100m / r.Units, 1) }));
    }

    /// <summary>Refills delivered per building with purchase cost.</summary>
    [HttpGet("refills")]
    public async Task<IActionResult> Refills([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId)
    {
        var (f, t) = Range(from, to);
        var fd = new DateTimeOffset(f.ToDateTime(TimeOnly.MinValue)); var td = new DateTimeOffset(t.ToDateTime(TimeOnly.MaxValue));
        var br = Branches(branchId); var all = br.Count == 0;
        var rows = await (from tr in Db.TankRefills.AsNoTracking()
                          where tr.RefillDatetime >= fd && tr.RefillDatetime <= td && (all || br.Contains(tr.Tank!.BranchId))
                          let cost = Db.RefillPurchaseInvoices.Where(p => p.RefillRequestId == tr.RefillRequestId).Select(p => p.VendorBill!.TotalAmount).FirstOrDefault()
                          group new { tr, cost } by new { tr.Tank!.BuildingId, tr.Tank.Building!.Code, tr.Tank.Building.NameEn, tr.Tank.Building.NameAr } into g
                          select new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Refills = g.Count(), Litres = g.Sum(x => x.tr.DeliveredQtyLitres ?? 0), Cost = g.Sum(x => x.cost ?? 0) })
                         .OrderByDescending(x => x.Litres).ToListAsync();
        return Ok(new { from = f, to = t, rows = rows.Select(r => new { r.BuildingId, r.Code, building = Loc(r.NameEn, r.NameAr), r.Refills, r.Litres, r.Cost }),
                        totals = new { refills = rows.Sum(r => r.Refills), litres = rows.Sum(r => r.Litres), cost = rows.Sum(r => r.Cost) } });
    }

    /// <summary>Amounts billed, paid and outstanding per vendor (v_vendor_balance).</summary>
    [HttpGet("vendor-balances")]
    public async Task<IActionResult> VendorBalances()
    {
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var bills = await Db.VendorBills.AsNoTracking().Where(b => b.StatusId != cancelled).Select(b => new { b.Id, b.VendorId, Total = b.TotalAmount ?? 0 }).ToListAsync();
        var paid = await billing.PaidByBillAsync(bills.Select(b => b.Id));
        var vendors = await Db.Vendors.AsNoTracking().ToDictionaryAsync(v => v.Id);
        return Ok(bills.GroupBy(b => b.VendorId).Select(g => new
        {
            vendorId = g.Key, vendor = Loc(vendors.GetValueOrDefault(g.Key)?.NameEn, vendors.GetValueOrDefault(g.Key)?.NameAr),
            billed = g.Sum(b => b.Total), paid = g.Sum(b => paid.GetValueOrDefault(b.Id)), outstanding = g.Sum(b => b.Total - paid.GetValueOrDefault(b.Id)),
        }).OrderByDescending(x => x.outstanding));
    }

    /// <summary>VAT summary for a period: output VAT (tenant invoices) vs input VAT (vendor bills).</summary>
    [HttpGet("vat")]
    public async Task<IActionResult> Vat([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (f, t) = Range(from, to);
        var (deadInv, _) = await HttpContext.RequestServices.GetRequiredService<BalanceQueries>().DeadStatusesAsync();
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var output = await Db.Invoices.Where(i => i.IssueDate >= f && i.IssueDate <= t && !deadInv.Contains(i.StatusId))
            .GroupBy(i => 1).Select(g => new { Net = g.Sum(i => i.Subtotal ?? 0), Vat = g.Sum(i => i.VatAmount ?? 0) }).FirstOrDefaultAsync();
        var input = await Db.VendorBills.Where(b => b.BillDate >= f && b.BillDate <= t && b.StatusId != cancelledBill)
            .GroupBy(b => 1).Select(g => new { Net = g.Sum(b => b.Subtotal ?? 0), Vat = g.Sum(b => b.VatAmount ?? 0) }).FirstOrDefaultAsync();
        var trn = await Db.Companies.Select(c => c.Trn).FirstOrDefaultAsync();
        return Ok(new
        {
            from = f, to = t, trn,
            sales = new { net = output?.Net ?? 0, vat = output?.Vat ?? 0 },
            purchases = new { net = input?.Net ?? 0, vat = input?.Vat ?? 0 },
            payable = (output?.Vat ?? 0) - (input?.Vat ?? 0),
        });
    }
}
