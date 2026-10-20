using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Employees (CRUD). The employee number (EMP-0001) is generated when empty.</summary>
[Route("api/employees")]
[PermissionModule("employees")]
public class EmployeesController : CrudController<Employee>
{
    protected override async Task OnCreatingAsync(Employee e)
    {
        if (string.IsNullOrWhiteSpace(e.NameAr) && string.IsNullOrWhiteSpace(e.NameEn)) throw new AppException("The employee name is required.");
        if (string.IsNullOrWhiteSpace(e.EmployeeNo)) e.EmployeeNo = await Numbers.NextAsync("EMP", yearly: false);
        else if (await Db.Employees.AnyAsync(x => x.EmployeeNo == e.EmployeeNo)) throw new BusinessRuleException($"Employee number {e.EmployeeNo} already exists.");
        await CheckUser(e);
    }

    protected override async Task OnUpdatingAsync(Employee existing, Employee incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming.EmployeeNo)) incoming.EmployeeNo = existing.EmployeeNo;
        if (incoming.EmployeeNo != existing.EmployeeNo && await Db.Employees.AnyAsync(x => x.EmployeeNo == incoming.EmployeeNo && x.Id != existing.Id))
            throw new BusinessRuleException($"Employee number {incoming.EmployeeNo} already exists.");
        incoming.Id = existing.Id;
        await CheckUser(incoming);
    }

    protected override async Task OnDeletingAsync(Employee e)
    {
        if (await Db.Assets.AnyAsync(a => a.AssignedEmployeeId == e.Id))
            throw new BusinessRuleException("The employee holds assets. Re-assign them first, or deactivate the employee instead.");
        if (await Db.MaintenanceVisits.AnyAsync(v => v.TechnicianEmployeeId == e.Id))
            throw new BusinessRuleException("The employee is the technician of maintenance visits. Deactivate the employee instead.");
    }

    private async Task CheckUser(Employee e)
    {
        if (e.UserId != null && await Db.Employees.AnyAsync(x => x.UserId == e.UserId && x.Id != e.Id))
            throw new BusinessRuleException("This system user is already linked to another employee.");
    }

    /// <summary>Active employees for dropdowns (custodian, «read by» …).</summary>
    [HttpGet("options")]
    [RequirePermission("dashboard.view")]
    public async Task<IActionResult> Options() =>
        Ok((await Db.Employees.AsNoTracking().Where(e => e.IsActive).OrderBy(e => e.Party!.NameAr ?? e.Party!.NameEn)
            .Select(e => new { e.Id, e.EmployeeNo, e.Party!.NameEn, e.Party!.NameAr, e.JobTitle, e.Party!.Phone }).ToListAsync())
            .Select(e => new { e.Id, e.EmployeeNo, name = Loc(e.NameEn, e.NameAr), e.JobTitle, e.Phone }));
}

/// <summary>Company assets (CRUD). The asset code (AST-0001) is generated when empty.</summary>
[Route("api/assets")]
[PermissionModule("assets")]
public class AssetsController : CrudController<Asset>
{
    protected override async Task OnCreatingAsync(Asset a)
    {
        if (string.IsNullOrWhiteSpace(a.NameAr) && string.IsNullOrWhiteSpace(a.NameEn)) throw new AppException("The asset name is required.");
        if (string.IsNullOrWhiteSpace(a.AssetCode)) a.AssetCode = await Numbers.NextAsync("AST", yearly: false);
        else if (await Db.Assets.AnyAsync(x => x.AssetCode == a.AssetCode)) throw new BusinessRuleException($"Asset code {a.AssetCode} already exists.");
        if (a.PurchaseCost < 0) throw new AppException("The purchase cost cannot be negative.");
        a.StatusId ??= await Lookups.IdOrNullAsync("ASSET_STATUS", a.AssignedEmployeeId != null ? "in_use" : "in_stock");
        Activity.Log("asset", null, "create", $"Asset {a.AssetCode} {a.NameAr ?? a.NameEn} added", a.BuildingId, a.BranchId);
    }

    protected override async Task OnDeletingAsync(Asset a)
    {
        if (a.AssetType != AssetTypes.General)
            throw new BusinessRuleException($"This asset is a {a.AssetType}. Remove it from the {a.AssetType}s screen.");
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        if (await Db.VendorBills.AnyAsync(b => b.SourceType == "asset" && b.SourceId == a.Id && b.StatusId != cancelled))
            throw new BusinessRuleException($"Asset {a.AssetCode} has a purchase bill. Cancel the bill first.");
    }

    public record PurchaseInput(long VendorId, string BillNo, DateOnly? BillDate, DateOnly? DueDate, long? BranchId, long? TaxRateId, decimal? VatRate, string? Notes);

    /// <summary>The supplier bill this asset was bought with (null when it was not bought through the system).</summary>
    [HttpGet("{id:long}/purchase")]
    public async Task<IActionResult> Purchase(long id)
    {
        await LoadLookups();
        var b = await Db.VendorBills.AsNoTracking().Where(x => x.SourceType == "asset" && x.SourceId == id).OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.BillNo, x.BillDate, x.VendorId, vendor = x.Vendor!.Party!.NameAr ?? x.Vendor.Party!.NameEn, x.Subtotal, x.VatAmount, x.TotalAmount, x.StatusId, x.JournalEntryId })
            .FirstOrDefaultAsync();
        return Ok(b == null ? null : new { b.Id, b.BillNo, b.BillDate, b.VendorId, b.vendor, b.Subtotal, b.VatAmount, b.TotalAmount, status = Code(b.StatusId), b.JournalEntryId });
    }

    /// <summary>
    /// Records the purchase of the asset from a supplier: a vendor bill with one «fixed asset» line for the purchase cost.
    /// Posted Dr Fixed assets (1501) + Dr VAT input / Cr the supplier's account. Pay it later from the supplier payments screen.
    /// </summary>
    [HttpPost("{id:long}/purchase")]
    [RequirePermission("payables.manage")]
    public async Task<IActionResult> RecordPurchase(long id, [FromBody] PurchaseInput input)
    {
        var a = await Db.Assets.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Asset", id);
        if ((a.PurchaseCost ?? 0) <= 0) throw new AppException("Set the purchase cost of the asset first.");
        if (string.IsNullOrWhiteSpace(input.BillNo)) throw new AppException("The supplier invoice number is required.");
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        if (await Db.VendorBills.AnyAsync(b => b.SourceType == "asset" && b.SourceId == id && b.StatusId != cancelled))
            throw new BusinessRuleException($"The purchase of asset {a.AssetCode} is already recorded.");
        if (await Db.VendorBills.AnyAsync(b => b.VendorId == input.VendorId && b.BillNo == input.BillNo.Trim() && b.StatusId != cancelled))
            throw new BusinessRuleException($"Invoice {input.BillNo} of this vendor is already recorded.");
        var branchId = RequireBranch(input.BranchId ?? a.BranchId);
        var billing = HttpContext.RequestServices.GetRequiredService<Services.BillingService>();
        var date = input.BillDate ?? a.PurchaseDate ?? Today;
        var bill = await billing.CreateVendorBillAsync(branchId, input.VendorId, input.BillNo.Trim(), date, input.DueDate, "asset", a.Id,
            new[] { new Services.BillingService.BillLineInput("fixed_asset", null, $"{a.AssetCode} — {a.NameAr ?? a.NameEn}", 1, "each", a.PurchaseCost!.Value, input.VatRate, null, input.TaxRateId) },
            input.Notes ?? $"Purchase of asset {a.AssetCode}");
        a.PurchaseDate ??= date;
        await Db.SaveChangesAsync();
        Activity.Log("asset", a.Id, "purchase", $"Asset {a.AssetCode} purchase bill {bill.BillNo} recorded", a.BuildingId, branchId);
        return Ok(new { bill.Id, bill.BillNo, bill.TotalAmount, bill.JournalEntryId });
    }

    protected override async Task OnUpdatingAsync(Asset existing, Asset incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming.AssetCode)) incoming.AssetCode = existing.AssetCode;
        if (incoming.AssetCode != existing.AssetCode && await Db.Assets.AnyAsync(x => x.AssetCode == incoming.AssetCode && x.Id != existing.Id))
            throw new BusinessRuleException($"Asset code {incoming.AssetCode} already exists.");
        if (incoming.PurchaseCost < 0) throw new AppException("The purchase cost cannot be negative.");
        if (incoming.AssignedEmployeeId != existing.AssignedEmployeeId)
        {
            var who = incoming.AssignedEmployeeId == null ? "stock" : await Db.Employees.Where(e => e.Id == incoming.AssignedEmployeeId).Select(e => e.Party!.NameAr ?? e.Party!.NameEn).FirstOrDefaultAsync();
            Activity.Log("asset", existing.Id, "assign", $"Asset {existing.AssetCode} assigned to {who}", incoming.BuildingId, incoming.BranchId);
        }
    }
}

/// <summary>Fleet usage: refill collections done by each vehicle / driver (read only).</summary>
[Route("api/fleet")]
[PermissionModule("vendors")]
public class FleetController : ApiControllerBase
{
    /// <summary>Per vehicle and per driver: number of collections, litres collected and the last collection date.</summary>
    [HttpGet("usage")]
    public async Task<IActionResult> Usage(CancellationToken ct)
    {
        var vehicles = await Db.RefillCollections.AsNoTracking().Where(c => c.VehicleId != null).GroupBy(c => c.VehicleId)
            .Select(g => new { vehicleId = g.Key, count = g.Count(), litres = g.Sum(c => c.CollectedQtyLitres ?? 0), last = g.Max(c => c.CollectionDate) }).ToListAsync(ct);
        var drivers = await Db.RefillCollections.AsNoTracking().Where(c => c.DriverId != null).GroupBy(c => c.DriverId)
            .Select(g => new { driverId = g.Key, count = g.Count(), litres = g.Sum(c => c.CollectedQtyLitres ?? 0), last = g.Max(c => c.CollectionDate) }).ToListAsync(ct);
        return Ok(new { vehicles, drivers });
    }

    /// <summary>Collections of one vehicle (or driver with ?driverId), newest first.</summary>
    [HttpGet("collections")]
    public async Task<IActionResult> Collections([FromQuery] long? vehicleId, [FromQuery] long? driverId, CancellationToken ct)
    {
        if (vehicleId == null && driverId == null) throw new AppException("vehicleId or driverId is required.");
        var q = Db.RefillCollections.AsNoTracking().AsQueryable();
        if (vehicleId != null) q = q.Where(c => c.VehicleId == vehicleId);
        if (driverId != null) q = q.Where(c => c.DriverId == driverId);
        var rows = await q.OrderByDescending(c => c.CollectionDate).Take(200)
            .Select(c => new
            {
                c.Id, c.CollectionDate, c.MeterBefore, c.MeterAfter, qty = c.CollectedQtyLitres,
                requestId = c.RefillRequestId, requestNo = c.RefillRequest!.RequestNo,
                tank = c.RefillRequest.Tank!.TankNo, buildingCode = c.RefillRequest.Tank.Building!.Code,
                buildingEn = c.RefillRequest.Tank.Building.NameEn, buildingAr = c.RefillRequest.Tank.Building.NameAr,
                plate = c.Vehicle!.PlateNo, driverEn = c.Driver!.Party!.NameEn, driverAr = c.Driver.Party!.NameAr,
            }).ToListAsync(ct);
        return Ok(rows.Select(r => new
        {
            r.Id, r.CollectionDate, r.MeterBefore, r.MeterAfter, r.qty, r.requestId, r.requestNo, r.tank, r.buildingCode,
            building = Loc(r.buildingEn, r.buildingAr), r.plate, driver = Loc(r.driverEn, r.driverAr),
        }));
    }
}
