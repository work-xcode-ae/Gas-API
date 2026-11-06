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
        // money actually received: a deposit applied to invoices at move-out (receipt without a payment method) is an offset, not a collection
        var q = Db.Payments.AsNoTracking().Where(p => p.PaymentDate >= f && p.PaymentDate <= t && !deadPay.Contains(p.StatusId) && p.PaymentMethodId != null && (all || br.Contains(p.BranchId)));
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

        // unallocated customer credit (receipts / credit notes not applied, net of refunds): net = invoices due − credit, which agrees with the ledger
        var balances = HttpContext.RequestServices.GetRequiredService<BalanceQueries>();
        var (_, deadPay) = await balances.DeadStatusesAsync();
        var creditTenants = await Db.Payments.AsNoTracking().Where(p => !deadPay.Contains(p.StatusId) && (all || br.Contains(p.BranchId))).Select(p => p.TenantId)
            .Union(Db.CreditNotes.AsNoTracking().Where(c => c.Status == "posted" && (all || br.Contains(c.BranchId))).Select(c => c.TenantId)).Distinct().ToListAsync();
        if (buildingId != null) creditTenants = creditTenants.Where(t => rows.Any(r => r.tenantId == t)).ToList();
        var credit = new Dictionary<long, decimal>();
        foreach (var t in creditTenants) { var c = await balances.TenantCreditAsync(t); if (c > 0.004m) credit[t] = c; }
        var names = await Db.Tenants.AsNoTracking().Where(t => credit.Keys.Contains(t.Id)).Select(t => new { t.Id, t.Party!.NameEn, t.Party.NameAr, t.Party.Phone }).ToListAsync();
        var all2 = rows.Select(r => new { r.tenantId, r.tenant, r.phone, r.current, r.d30, r.d60, r.d90, r.over90, r.total, credit = credit.GetValueOrDefault(r.tenantId), net = r.total - credit.GetValueOrDefault(r.tenantId) })
            .Concat(names.Where(n => rows.All(r => r.tenantId != n.Id)).Select(n => new { tenantId = n.Id, tenant = Loc(n.NameEn, n.NameAr), phone = n.Phone, current = 0m, d30 = 0m, d60 = 0m, d90 = 0m, over90 = 0m, total = 0m,
                credit = credit[n.Id], net = -credit[n.Id] }))
            .OrderByDescending(x => x.net).ToList();
        return Ok(new
        {
            asOf = Today, rows = all2,
            totals = new { current = rows.Sum(r => r.current), d30 = rows.Sum(r => r.d30), d60 = rows.Sum(r => r.d60), d90 = rows.Sum(r => r.d90), over90 = rows.Sum(r => r.over90), total = rows.Sum(r => r.total),
                           credit = all2.Sum(r => r.credit), net = all2.Sum(r => r.net) },
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

    /// <summary>
    /// Meter stock and value: meters by where their purchase cost sits (in stock / installed / written off), by model and by
    /// purchase bill (bought vs registered), with the ledger balance of «meters in stock» (FEE_TYPE meter_purchase account, 1402)
    /// for reconciliation. Meters registered without a purchase bill have no cost in the ledger («untracked»).
    /// </summary>
    [HttpGet("meter-stock")]
    public async Task<IActionResult> MeterStock([FromQuery] long? branchId, [FromQuery] DateOnly? asOf)
    {
        await LoadLookups();
        var br = Branches(branchId); var all = br.Count == 0;
        var meters = await Db.Meters.AsNoTracking().Where(m => all || br.Contains(m.BranchId ?? 0))
            .Select(m => new { m.Id, m.MeterNo, m.SerialNo, m.Brand, m.Model, m.StatusId, m.PurchaseCost, m.PurchaseBillId, m.CostLocation, BillNo = m.PurchaseBill!.BillNo,
                               Installed = Db.MeterAssignments.Any(a => a.MeterId == m.Id && a.ToDate == null) }).ToListAsync();
        string Loc2(string? l) => l ?? "untracked";
        var byLocation = new[] { "stock", "installed", "written_off", "untracked" }.Select(l => new
        {
            location = l, count = meters.Count(m => Loc2(m.CostLocation) == l), value = meters.Where(m => Loc2(m.CostLocation) == l).Sum(m => m.PurchaseCost ?? 0),
        }).ToList();
        var byModel = meters.GroupBy(m => new { m.Brand, m.Model }).Select(g => new
        {
            g.Key.Brand, g.Key.Model, total = g.Count(), inStock = g.Count(m => !m.Installed && Code(m.StatusId) == "in_stock"), installed = g.Count(m => m.Installed),
            stockValue = g.Where(m => m.CostLocation == "stock").Sum(m => m.PurchaseCost ?? 0), installedValue = g.Where(m => m.CostLocation == "installed").Sum(m => m.PurchaseCost ?? 0),
        }).OrderBy(x => x.Brand).ThenBy(x => x.Model).ToList();
        var meterFee = await Lookups.IdOrNullAsync(LookupTypes.FeeType, "meter_purchase");
        var billIds = meters.Where(m => m.PurchaseBillId != null).Select(m => m.PurchaseBillId!.Value).Distinct().ToList();
        var billQty = await Db.VendorBillItems.AsNoTracking().Where(i => billIds.Contains(i.BillId) && i.ItemTypeId == meterFee)
            .GroupBy(i => i.BillId).Select(g => new { g.Key, Qty = g.Sum(i => i.Quantity ?? 0), Amount = g.Sum(i => i.Amount ?? 0) }).ToDictionaryAsync(x => x.Key);
        var byBill = meters.Where(m => m.PurchaseBillId != null).GroupBy(m => new { m.PurchaseBillId, m.BillNo }).Select(g => new
        {
            billId = g.Key.PurchaseBillId, billNo = g.Key.BillNo, bought = billQty.GetValueOrDefault(g.Key.PurchaseBillId!.Value)?.Qty ?? 0,
            billAmount = billQty.GetValueOrDefault(g.Key.PurchaseBillId!.Value)?.Amount ?? 0, registered = g.Count(), registeredValue = g.Sum(m => m.PurchaseCost ?? 0),
        }).ToList();
        // ledger: «meters in stock» account
        var companyId = await Db.Companies.Select(c => c.Id).FirstAsync();
        var stockAcc = await Db.AccountMappings.Where(m => m.CompanyId == companyId && m.SourceType == MappingSources.VendorBillItem && m.SourceKeyId == meterFee && m.IsActive)
            .Select(m => m.DebitAccountId).FirstOrDefaultAsync();
        var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
        var d = asOf ?? Today;
        var ledger = stockAcc == null ? (decimal?)null : await Db.JournalLines.Where(l => l.AccountId == stockAcc && l.JournalEntry!.StatusId != draft && l.JournalEntry.EntryDate <= d
                                                                                          && (all || br.Contains(l.JournalEntry.BranchId)))
            .SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        // bought but not yet registered as meters (still in the stock account without a serial)
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var boughtTotal = await Db.VendorBillItems.AsNoTracking().Where(i => i.ItemTypeId == meterFee && i.Bill!.StatusId != cancelledBill && i.Bill.BillDate <= d
                                                                               && (all || br.Contains(i.Bill.BranchId)))
            .SumAsync(i => (decimal?)(i.Amount ?? 0)) ?? 0;
        var notRegistered = boughtTotal - meters.Where(m => m.PurchaseBillId != null && m.CostLocation != null).Sum(m => m.PurchaseCost ?? 0);
        var stockValue = byLocation.First(x => x.location == "stock").value;
        return Ok(new
        {
            asOf = d, byLocation, byModel, byBill,
            ledger = new { stockAccountBalance = ledger, metersInStockValue = stockValue, boughtNotRegistered = notRegistered,
                           difference = ledger == null ? (decimal?)null : ledger - stockValue - notRegistered },
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
        var vendors = await Db.Vendors.AsNoTracking().Include(v => v.Party).ToDictionaryAsync(v => v.Id);
        // payments not allocated to any bill (advances to the supplier) reduce what we owe, as in the ledger
        var advances = await billing.VendorAdvancesAsync();
        return Ok(bills.Select(b => b.VendorId).Concat(advances.Keys).Distinct().Select(vid =>
        {
            var g = bills.Where(b => b.VendorId == vid).ToList();
            var adv = advances.GetValueOrDefault(vid);
            return new
            {
                vendorId = vid, vendor = Loc(vendors.GetValueOrDefault(vid)?.Party?.NameEn, vendors.GetValueOrDefault(vid)?.Party?.NameAr),
                billed = g.Sum(b => b.Total), paid = g.Sum(b => paid.GetValueOrDefault(b.Id)), advance = adv,
                outstanding = g.Sum(b => b.Total - paid.GetValueOrDefault(b.Id)) - adv,
            };
        }).OrderByDescending(x => x.outstanding));
    }

    /// <summary>VAT summary for a period: output VAT (tenant invoices) vs input VAT (vendor bills).</summary>
    [HttpGet("vat")]
    public async Task<IActionResult> Vat([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (f, t) = Range(from, to);
        var (deadInv, _) = await HttpContext.RequestServices.GetRequiredService<BalanceQueries>().DeadStatusesAsync();
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        // supplies = invoice lines except refundable deposits (a liability, not a supply), less posted credit notes of the period
        var depositType = await Lookups.IdOrNullAsync(LookupTypes.FeeType, "deposit");
        var invNet = await Db.InvoiceItems.Where(x => x.Invoice!.IssueDate >= f && x.Invoice.IssueDate <= t && !deadInv.Contains(x.Invoice.StatusId) && x.ItemTypeId != depositType)
            .SumAsync(x => (decimal?)(x.Amount ?? 0)) ?? 0;
        var invVat = await Db.Invoices.Where(i => i.IssueDate >= f && i.IssueDate <= t && !deadInv.Contains(i.StatusId)).SumAsync(i => (decimal?)(i.VatAmount ?? 0)) ?? 0;
        var cnNet = await Db.CreditNoteLines.Where(l => l.CreditNote!.Status == "posted" && l.CreditNote.IssueDate >= f && l.CreditNote.IssueDate <= t && l.ItemTypeId != depositType)
            .SumAsync(l => (decimal?)l.Amount) ?? 0;
        var cnVat = await Db.CreditNotes.Where(c => c.Status == "posted" && c.IssueDate >= f && c.IssueDate <= t).SumAsync(c => (decimal?)c.VatAmount) ?? 0;
        var output = new { Net = invNet - cnNet, Vat = invVat - cnVat };
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
