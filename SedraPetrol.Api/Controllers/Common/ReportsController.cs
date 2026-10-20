using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Reports page: consumption, collections, A/R aging, occupancy, meters, refills, vendor balances and VAT.</summary>
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
                         && (buildingId == null || a.Unit!.BuildingId == buildingId) && (all || br.Contains(r.Meter!.BranchId ?? 0))
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
            .Select(i => new { i.Id, i.TenantId, i.Tenant!.Party!.NameEn, i.Tenant.Party!.NameAr, i.Tenant.Party!.Phone, i.DueDate, Total = i.TotalAmount ?? 0 }).ToListAsync();
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

    /// <summary>
    /// Meters by status and calibration validity: totals per status, per building (incl. meters in stock) and the meter list.
    /// Calibration is valid for Meters:CalibrationValidMonths (default 12) months; due_soon = expires within 30 days.
    /// </summary>
    [HttpGet("meters")]
    public async Task<IActionResult> Meters([FromQuery] long? branchId, [FromQuery] long? buildingId, [FromQuery] string? status, [FromQuery] string? calibration)
    {
        await LoadLookups();
        var months = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<int?>("Meters:CalibrationValidMonths") ?? 12;
        var br = Branches(branchId); var all = br.Count == 0;
        var q = from m in Db.Meters.AsNoTracking()
                where all || br.Contains(m.BranchId ?? 0)
                let a = Db.MeterAssignments.Where(x => x.MeterId == m.Id && x.ToDate == null).FirstOrDefault()
                select new
                {
                    m.Id, m.MeterNo, m.SerialNo, m.StatusId, m.MeterTypeId, m.CalibrationDate, m.InstallDate, m.Brand,
                    UnitId = a == null ? (long?)null : a.UnitId, UnitNo = a == null ? null : a.Unit!.UnitNo,
                    BuildingId = a == null ? (long?)null : a.Unit!.BuildingId, BCode = a == null ? null : a.Unit!.Building!.Code,
                    BEn = a == null ? null : a.Unit!.Building!.NameEn, BAr = a == null ? null : a.Unit!.Building!.NameAr,
                    LastRead = Db.MeterReadings.Where(r => r.MeterId == m.Id).Max(r => (DateTimeOffset?)r.ReadingDate),
                };
        if (buildingId != null) q = q.Where(x => x.BuildingId == buildingId);
        var list = await q.OrderBy(x => x.BCode).ThenBy(x => x.UnitNo).ThenBy(x => x.MeterNo).ToListAsync();

        string CalState(DateOnly? d)
        {
            if (d == null) return "unknown";
            var exp = d.Value.AddMonths(months).AddDays(-1);
            return exp < Today ? "expired" : exp <= Today.AddDays(30) ? "due_soon" : "valid";
        }
        var rows = list.Select(x => new
        {
            x.Id, meterNo = x.MeterNo, serial = x.SerialNo, type = Code(x.MeterTypeId), typeLabel = Label(x.MeterTypeId),
            status = Code(x.StatusId) ?? "unknown", statusLabel = Label(x.StatusId) ?? "—", x.Brand,
            x.BuildingId, buildingCode = x.BCode, building = x.BuildingId == null ? null : Loc(x.BEn, x.BAr), unitId = x.UnitId, unit = x.UnitNo,
            installed = x.UnitId != null, x.InstallDate, calibrationDate = x.CalibrationDate,
            calibrationExpiry = x.CalibrationDate?.AddMonths(months).AddDays(-1), calibrationState = CalState(x.CalibrationDate), lastRead = x.LastRead,
        }).ToList();
        if (!string.IsNullOrWhiteSpace(status)) rows = rows.Where(r => r.status == status).ToList();
        if (!string.IsNullOrWhiteSpace(calibration)) rows = rows.Where(r => r.calibrationState == calibration).ToList();

        var statuses = (await Lookups.ByTypeAsync(LookupTypes.MeterStatus)).Where(v => v.IsActive).Select(v => new { v.Code, label = Loc(v.NameEn, v.NameAr) }).ToList();
        var byStatus = statuses.Select(s => new { status = s.Code, s.label, count = rows.Count(r => r.status == s.Code) })
            .Concat(rows.Where(r => !statuses.Any(s => s.Code == r.status)).GroupBy(r => r.status).Select(g => new { status = g.Key, label = g.First().statusLabel, count = g.Count() }))
            .ToList();
        var byBuilding = rows.GroupBy(r => new { r.BuildingId, r.buildingCode, r.building })
            .Select(g => new
            {
                g.Key.BuildingId, code = g.Key.buildingCode ?? "—", building = g.Key.building ?? (IsEnglish ? "In stock (not installed)" : "في المخزون (غير مركّب)"),
                total = g.Count(), counts = g.GroupBy(r => r.status).ToDictionary(x => x.Key, x => x.Count()),
                calibrationExpired = g.Count(r => r.calibrationState == "expired"), calibrationDueSoon = g.Count(r => r.calibrationState == "due_soon"),
            }).OrderBy(b => b.BuildingId == null ? 1 : 0).ThenBy(b => b.code).ToList();
        return Ok(new
        {
            calibrationValidMonths = months, statuses = byStatus, byBuilding, rows,
            totals = new
            {
                total = rows.Count, installed = rows.Count(r => r.installed), inStock = rows.Count(r => !r.installed),
                calibrationValid = rows.Count(r => r.calibrationState == "valid"), calibrationDueSoon = rows.Count(r => r.calibrationState == "due_soon"),
                calibrationExpired = rows.Count(r => r.calibrationState == "expired"), calibrationUnknown = rows.Count(r => r.calibrationState == "unknown"),
            },
        });
    }

    /// <summary>Refills delivered per building with purchase cost.</summary>
    [HttpGet("refills")]
    public async Task<IActionResult> Refills([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? branchId)
    {
        var (f, t) = Range(from, to);
        var fd = new DateTimeOffset(f.ToDateTime(TimeOnly.MinValue)); var td = new DateTimeOffset(t.ToDateTime(TimeOnly.MaxValue));
        var br = Branches(branchId); var all = br.Count == 0;
        // SQL Server cannot SUM a sub-query: load the refills with their bill amount, then group in memory
        var refills = await (from tr in Db.TankRefills.AsNoTracking()
                             where tr.RefillDatetime >= fd && tr.RefillDatetime <= td && (all || br.Contains(tr.Tank!.BranchId ?? 0))
                             select new
                             {
                                 tr.Tank!.BuildingId, tr.Tank.Building!.Code, tr.Tank.Building.NameEn, tr.Tank.Building.NameAr, Litres = tr.DeliveredQtyLitres ?? 0,
                                 Cost = Db.RefillPurchaseInvoices.Where(p => p.RefillRequestId == tr.RefillRequestId).Select(p => p.VendorBill!.TotalAmount).FirstOrDefault(),
                             }).ToListAsync();
        var rows = refills.GroupBy(x => new { x.BuildingId, x.Code, x.NameEn, x.NameAr })
            .Select(g => new { g.Key.BuildingId, g.Key.Code, g.Key.NameEn, g.Key.NameAr, Refills = g.Count(), Litres = g.Sum(x => x.Litres), Cost = g.Sum(x => x.Cost ?? 0) })
            .OrderByDescending(x => x.Litres).ToList();
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
            vendorId = g.Key, vendor = Loc(vendors.GetValueOrDefault(g.Key)?.Party!.NameEn, vendors.GetValueOrDefault(g.Key)?.Party!.NameAr),
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
