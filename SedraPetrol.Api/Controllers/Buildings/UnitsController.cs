using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Units (apartments) with their current tenant, contract and meter.</summary>
[Route("api/units")]
[PermissionModule("buildings")]
public class UnitsController : ApiControllerBase
{
    public record UnitInput(long? BuildingId, string? No, int? Floor, long? UnitTypeId, string? Type, decimal? Area, short? Bedrooms, short? Bathrooms,
        decimal? Rent, long? StatusId, string? Status, DateOnly? LastInspectionDate, string? Notes);

    public record BulkUnitsInput(long BuildingId, List<UnitInput> Units);

    public record UnitDto(long Id, string Key, long BuildingId, string? BuildingCode, string? BuildingName, string No, int? Floor,
        string? Type, string? TypeLabel, decimal? Area, short? Bedrooms, short? Bathrooms, decimal? Rent, string? Status, string? StatusLabel,
        long? TenantId, string? Tenant, long? ContractId, string? ContractNo, long? MeterId, string? Meter, DateOnly? LastInspectionDate, string? Notes);

    /// <summary>
    /// Units list. Filters: buildingId, status (vacant|occupied|reserved|maintenance), type, floor, vacantOnly, withoutMeter.
    /// Use pageSize=0 to get all units of a building for dropdowns (UnitOption in the portal).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<UnitDto>>> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] string? type, [FromQuery] int? floor, [FromQuery] bool vacantOnly = false, [FromQuery] bool withoutMeter = false,
        CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.Units.AsNoTracking().Where(u => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(u.Building!.BranchId));
        if (buildingId != null) query = query.Where(u => u.BuildingId == buildingId);
        if (branchId != null) query = query.Where(u => u.Building!.BranchId == branchId);
        if (floor != null) query = query.Where(u => u.FloorNo == floor);
        if (!string.IsNullOrEmpty(type)) { var t = await Lookups.IdOrNullAsync(LookupTypes.UnitType, type); query = query.Where(u => u.UnitTypeId == t); }
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.UnitStatus, status); query = query.Where(u => u.StatusId == s); }
        if (vacantOnly) query = query.Where(u => !Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive));
        if (withoutMeter) query = query.Where(u => !Db.MeterAssignments.Any(a => a.UnitId == u.Id && a.ToDate == null));
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(u => u.UnitNo.Contains(s) || Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive &&
                                     ((c.Tenant!.NameAr != null && c.Tenant.NameAr.Contains(s)) || (c.Tenant.NameEn != null && c.Tenant.NameEn.Contains(s)))));
        }
        query = q.Sort?.ToLowerInvariant() switch
        {
            "no" or "unitno" => q.Desc ? query.OrderByDescending(u => u.UnitNo) : query.OrderBy(u => u.UnitNo),
            "id" => q.Desc ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id),
            _ => query.OrderBy(u => u.BuildingId).ThenBy(u => u.FloorNo).ThenBy(u => u.UnitNo),
        };

        var page = await Project(query).ToPagedAsync(q, ct);
        return Ok(new PagedResult<UnitDto>(page.Items.Select(Map).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UnitDto>> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Project(Db.Units.AsNoTracking().Where(u => u.Id == id)).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Unit", id);
        return Ok(Map(row));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UnitInput input)
    {
        if (input.BuildingId == null) throw new AppException("buildingId is required.");
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        var unit = await AddUnit(Db, Lookups, b.Id, input);
        await Db.SaveChangesAsync();
        Activity.Log("unit", unit.Id, "create", $"Unit {unit.UnitNo} added to {b.Code}", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = unit.Id }, new { unit.Id });
    }

    /// <summary>Add several units at once (AddUnitModal rows).</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulk([FromBody] BulkUnitsInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        if (input.Units.Count == 0) throw new AppException("No units to add.");
        var dup = input.Units.GroupBy(u => u.No?.Trim()).FirstOrDefault(g => g.Count() > 1);
        if (dup != null) throw new AppException($"Unit number '{dup.Key}' is repeated.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var created = new List<Unit>();
        foreach (var u in input.Units) created.Add(await AddUnit(Db, Lookups, b.Id, u));
        await Db.SaveChangesAsync();
        Activity.Log("unit", null, "create", $"{created.Count} units added to {b.Code}", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(created.Select(c => new { c.Id, c.UnitNo }));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UnitInput input)
    {
        var u = await Db.Units.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Unit", id);
        var no = input.No?.Trim();
        if (string.IsNullOrEmpty(no)) throw new AppException("Unit number is required.");
        if (no != u.UnitNo && await Db.Units.AnyAsync(x => x.BuildingId == u.BuildingId && x.UnitNo == no && x.Id != id))
            throw new BusinessRuleException($"Unit {no} already exists in this building.");
        u.UnitNo = no;
        u.FloorNo = input.Floor;
        u.UnitTypeId = await Lk(LookupTypes.UnitType, input.UnitTypeId, input.Type, field: "type");
        if (input.StatusId != null || input.Status != null) u.StatusId = await Lk(LookupTypes.UnitStatus, input.StatusId, input.Status, field: "status");
        u.AreaSqm = input.Area; u.Bedrooms = input.Bedrooms; u.Bathrooms = input.Bathrooms; u.RentAmount = input.Rent;
        u.LastInspectionDate = input.LastInspectionDate; u.Notes = input.Notes;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var u = await Db.Units.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Unit", id);
        if (await Db.SupplyContracts.AnyAsync(c => c.UnitId == id && c.IsActive)) throw new BusinessRuleException("The unit has an active supply contract.");
        if (await Db.MeterAssignments.AnyAsync(a => a.UnitId == id && a.ToDate == null)) throw new BusinessRuleException("A meter is still assigned to this unit. Unassign it first.");
        Db.Units.Remove(u);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ helpers (also used by the building wizard)

    internal static async Task<Unit> AddUnit(AppDbContext db, LookupService lookups, long buildingId, UnitInput i)
    {
        var no = i.No?.Trim();
        if (string.IsNullOrEmpty(no)) throw new AppException("Unit number is required.");
        if (await db.Units.AnyAsync(x => x.BuildingId == buildingId && x.UnitNo == no) || db.Units.Local.Any(x => x.BuildingId == buildingId && x.UnitNo == no && x.Id == 0))
            throw new BusinessRuleException($"Unit {no} already exists in this building.");
        var unit = new Unit
        {
            BuildingId = buildingId, UnitNo = no, FloorNo = i.Floor,
            UnitTypeId = await lookups.ResolveAsync(LookupTypes.UnitType, i.UnitTypeId, i.Type ?? "apartment", field: "type"),
            StatusId = await lookups.ResolveAsync(LookupTypes.UnitStatus, i.StatusId, i.Status ?? "vacant", field: "status"),
            AreaSqm = i.Area, Bedrooms = i.Bedrooms, Bathrooms = i.Bathrooms, RentAmount = i.Rent, LastInspectionDate = i.LastInspectionDate,
            Notes = i.Notes, IsActive = true,
        };
        db.Units.Add(unit);
        return unit;
    }

    private record Row(Unit U, string? BCode, string? BNameEn, string? BNameAr, long? ContractId, string? ContractNo, long? TenantId,
        string? TenantEn, string? TenantAr, long? MeterId, string? MeterNo);

    private IQueryable<Row> Project(IQueryable<Unit> q) =>
        from u in q
        let c = Db.SupplyContracts.Where(c => c.UnitId == u.Id && c.IsActive).OrderByDescending(c => c.StartDate).FirstOrDefault()
        let m = Db.MeterAssignments.Where(a => a.UnitId == u.Id && a.ToDate == null).Select(a => a.Meter).FirstOrDefault()
        select new Row(u, u.Building!.Code, u.Building.NameEn, u.Building.NameAr, c == null ? null : c.Id, c == null ? null : c.ContractNo,
            c == null ? null : c.TenantId, c == null ? null : c.Tenant!.NameEn, c == null ? null : c.Tenant!.NameAr,
            m == null ? null : m.Id, m == null ? null : m.MeterNo);

    private UnitDto Map(Row r)
    {
        var occupied = r.ContractId != null;
        var status = Code(r.U.StatusId) ?? (occupied ? "occupied" : "vacant");
        if (occupied && status == "vacant") status = "occupied";
        return new UnitDto(r.U.Id, r.U.Id.ToString(), r.U.BuildingId, r.BCode, Loc(r.BNameEn, r.BNameAr), r.U.UnitNo, r.U.FloorNo,
            Code(r.U.UnitTypeId), Label(r.U.UnitTypeId), r.U.AreaSqm, r.U.Bedrooms, r.U.Bathrooms, r.U.RentAmount, status, Label(r.U.StatusId),
            r.TenantId, Loc(r.TenantEn, r.TenantAr), r.ContractId, r.ContractNo, r.MeterId, r.MeterNo, r.U.LastInspectionDate, r.U.Notes);
    }
}
