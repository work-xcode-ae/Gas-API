using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Vendors: gas suppliers, maintenance contractors, service vendors.</summary>
[Route("api/vendors")]
[PermissionModule("vendors")]
public class VendorsController : CrudController<Vendor>
{
    /// <summary>Vendors for dropdowns, optionally filtered by type code (gas_supplier, maintenance_vendor ...).</summary>
    [HttpGet("options")]
    public async Task<IActionResult> Options([FromQuery] string? type)
    {
        var typeId = await Lookups.IdOrNullAsync(LookupTypes.VendorType, type);
        var rows = await Db.Vendors.AsNoTracking().Where(v => v.IsActive && (type == null || v.VendorTypeId == typeId))
            .OrderBy(v => v.Party!.NameAr).Select(v => new { v.Id, Code = v.Party!.PartyCode, v.Party!.NameEn, v.Party!.NameAr, v.Party!.Phone, v.Party!.Email }).ToListAsync();
        return Ok(rows.Select(v => new { v.Id, v.Code, name = Loc(v.NameEn, v.NameAr), v.Phone, v.Email }));
    }

    /// <summary>Vendor with contacts, drivers, vehicles and A/P balance (v_vendor_balance).</summary>
    [HttpGet("{id:long}/details")]
    public async Task<IActionResult> Details(long id)
    {
        await LoadLookups();
        var v = await Db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Vendor", id);
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var bills = await Db.VendorBills.AsNoTracking().Where(b => b.VendorId == id && b.StatusId != cancelledBill).Select(b => new { b.Id, b.TotalAmount }).ToListAsync();
        var paid = await billing.PaidByBillAsync(bills.Select(b => b.Id));
        var dead = await billing.DeadPaymentStatusIdsAsync();
        var totalPaid = await Db.VendorPayments.Where(p => p.VendorId == id && (p.StatusId == null || !dead.Contains(p.StatusId.Value))).SumAsync(p => (decimal?)p.Amount) ?? 0;
        return Ok(new
        {
            vendor = v, type = Code(v.VendorTypeId), typeLabel = Label(v.VendorTypeId),
            contacts = await Db.VendorContacts.AsNoTracking().Where(c => c.VendorId == id).ToListAsync(),
            drivers = await Db.Drivers.AsNoTracking().Where(d => d.VendorId == id).ToListAsync(),
            vehicles = await Db.Vehicles.AsNoTracking().Where(x => x.VendorId == id).ToListAsync(),
            balance = new { billed = bills.Sum(b => b.TotalAmount ?? 0), paid = totalPaid, allocated = paid.Values.Sum(), outstanding = bills.Sum(b => b.TotalAmount ?? 0) - paid.Values.Sum(), advance = totalPaid - paid.Values.Sum() },
            amcContracts = await Db.AmcContracts.AsNoTracking().CountAsync(a => a.VendorId == id),
        });
    }

    protected override async Task OnCreatingAsync(Vendor e)
    {
        await Lookups.ResolveAsync(LookupTypes.VendorType, e.VendorTypeId, null, true, "vendorTypeId");
    }
}

[Route("api/vendor-contacts")]
[PermissionModule("vendors")]
public class VendorContactsController : CrudController<VendorContact> { }

[Route("api/drivers")]
[PermissionModule("vendors")]
public class DriversController : CrudController<Driver> { }

[Route("api/vehicles")]
[PermissionModule("vendors")]
public class VehiclesController : CrudController<Vehicle>
{
    protected override async Task OnCreatingAsync(Vehicle e)
    {
        e.PlateNo = e.PlateNo.Trim();
        if (await Db.Vehicles.AnyAsync(v => v.PlateNo == e.PlateNo)) throw new BusinessRuleException($"Plate {e.PlateNo} already exists.");
    }
}
