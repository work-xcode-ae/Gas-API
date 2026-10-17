using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Buildings list, KPIs, profile and the "add building" wizard (building + owners + tanks).</summary>
[Route("api/buildings")]
[PermissionModule("buildings")]
public class BuildingsController : ApiControllerBase
{
    // ------------------------------------------------------------------ DTOs
    public record BuildingListItem(long Id, string Code, string? Name, string? NameEn, string? NameAr, long BranchId, string? Branch,
        long? CityId, string? City, string? Type, string? TypeLabel, string? Status, bool IsActive, string? Zone,
        int Units, int Occupied, int Vacant, int Meters, int Tanks, decimal? TankLevelPct, int OpenRequests, string? PrimaryOwner);

    public record OwnerInput(long? PartyId, NewPartyInput? Party, decimal? OwnershipPercentage, bool IsPrimary, DateOnly? FromDate, DateOnly? ToDate);

    public record NewPartyInput(string? PartyType, string? NameEn, string? NameAr, string? EmiratesId, string? Nationality, string? TradeLicenseNo,
        string? TaxNumber, long? CityId, string? PoBox, string? Address, string? Phone, string? Mobile, string? Email, string? BankName, string? Iban);

    public record TankInput(string? TankNo, long? GasTypeId, string? GasType, decimal CapacityLitres, decimal? CurrentLevelLitres, string? SerialNo,
        string? Manufacturer, DateOnly? InstallDate, DateOnly? LastInspectionDate, DateOnly? NextInspectionDate, string? Status);

    public record BuildingInput(long? BranchId, string? NameEn, string? NameAr, long? BuildingTypeId, string? BuildingType, long? StatusId, string? Status,
        long? CityId, string? Zone, string? Street, string? PoBox, string? Address, decimal? Latitude, decimal? Longitude, int? TotalFloors, string? Notes,
        List<OwnerInput>? Owners, List<TankInput>? Tanks, List<UnitsController.UnitInput>? Units);

    // ------------------------------------------------------------------ list & KPIs

    /// <summary>Buildings table with occupancy, meters, tank level and open requests.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BuildingListItem>>> List([FromQuery] PageQuery q, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] string? type, [FromQuery] long? cityId, CancellationToken ct)
    {
        await LoadLookups();
        var query = Scoped(Db.Buildings.AsNoTracking());
        if (branchId != null) query = query.Where(b => b.BranchId == branchId);
        if (cityId != null) query = query.Where(b => b.CityId == cityId);
        if (!string.IsNullOrEmpty(status)) { var sid = await Lookups.IdOrNullAsync(LookupTypes.BuildingStatus, status); query = query.Where(b => b.StatusId == sid); }
        if (!string.IsNullOrEmpty(type)) { var tid = await Lookups.IdOrNullAsync(LookupTypes.BuildingType, type); query = query.Where(b => b.BuildingTypeId == tid); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(b => b.Code.Contains(s) || (b.NameEn != null && b.NameEn.Contains(s)) || (b.NameAr != null && b.NameAr.Contains(s))
                                     || (b.Zone != null && b.Zone.Contains(s)) || (b.Street != null && b.Street.Contains(s)));
        }
        query = q.Sort?.ToLowerInvariant() switch
        {
            "name" => q.Desc ? query.OrderByDescending(b => b.NameAr) : query.OrderBy(b => b.NameAr),
            "code" or "no" => q.Desc ? query.OrderByDescending(b => b.Code) : query.OrderBy(b => b.Code),
            _ => q.Desc ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
        };

        var srOpen = await OpenSrStatusIds();
        var projected = query.Select(b => new
        {
            b,
            Branch = b.Branch,
            City = b.City,
            Units = Db.Units.Count(u => u.BuildingId == b.Id),
            Occupied = Db.Units.Count(u => u.BuildingId == b.Id && Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive)),
            Meters = Db.MeterAssignments.Count(a => a.ToDate == null && Db.Units.Any(u => u.Id == a.UnitId && u.BuildingId == b.Id)),
            Tanks = Db.Tanks.Count(t => t.BuildingId == b.Id),
            Cap = Db.Tanks.Where(t => t.BuildingId == b.Id).Sum(t => (decimal?)t.CapacityLitres),
            Level = Db.Tanks.Where(t => t.BuildingId == b.Id).Sum(t => t.CurrentLevelLitres),
            OpenRequests = Db.ServiceRequests.Count(r => r.BuildingId == b.Id && r.StatusId != null && srOpen.Contains(r.StatusId.Value)),
            Owner = Db.BuildingOwners.Where(o => o.BuildingId == b.Id && o.IsPrimary).Select(o => o.Party!.NameAr ?? o.Party.NameEn).FirstOrDefault(),
        });
        var page = await projected.ToPagedAsync(q, ct);
        var items = page.Items.Select(x => new BuildingListItem(x.b.Id, x.b.Code, Loc(x.b.NameEn, x.b.NameAr), x.b.NameEn, x.b.NameAr, x.b.BranchId,
            Loc(x.Branch?.NameEn, x.Branch?.NameAr), x.b.CityId, Loc(x.City?.NameEn, x.City?.NameAr), Code(x.b.BuildingTypeId), Label(x.b.BuildingTypeId),
            Code(x.b.StatusId) ?? (x.b.IsActive ? "active" : "inactive"), x.b.IsActive, x.b.Zone, x.Units, x.Occupied, x.Units - x.Occupied, x.Meters, x.Tanks,
            x.Cap > 0 ? Math.Round((x.Level ?? 0) / x.Cap.Value * 100, 1) : null, x.OpenRequests, x.Owner)).ToList();
        return Ok(new PagedResult<BuildingListItem>(items, page.Total, page.Page, page.PageSize));
    }

    /// <summary>KPI cards and charts for the Buildings page.</summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis([FromQuery] long? branchId, CancellationToken ct)
    {
        var buildings = Scoped(Db.Buildings.AsNoTracking());
        if (branchId != null) buildings = buildings.Where(b => b.BranchId == branchId);
        var inactive = await Lookups.IdOrNullAsync(LookupTypes.BuildingStatus, "inactive");
        var units = Db.Units.Where(u => buildings.Any(b => b.Id == u.BuildingId));
        var total = await buildings.CountAsync(ct);
        var active = await buildings.CountAsync(b => b.IsActive && b.StatusId != inactive, ct);
        var totalUnits = await units.CountAsync(ct);
        var occupied = await units.CountAsync(u => Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive), ct);
        var buildingsByBranch = await buildings.GroupBy(b => b.BranchId).Select(g => new { BranchId = g.Key, Count = g.Count() }).ToListAsync(ct);
        var unitsByBranch = await (from u in units join b in Db.Buildings on u.BuildingId equals b.Id group u by b.BranchId into g
                                   select new { BranchId = g.Key, Count = g.Count() }).ToListAsync(ct);
        var branchIds = buildingsByBranch.Select(x => x.BranchId).ToList();
        var branches = await Db.Branches.AsNoTracking().Where(b => branchIds.Contains(b.Id)).ToListAsync(ct);
        var byBranch = branches.Select(br => new
        {
            BranchId = br.Id, br.NameEn, br.NameAr,
            Buildings = buildingsByBranch.First(x => x.BranchId == br.Id).Count,
            Units = unitsByBranch.FirstOrDefault(x => x.BranchId == br.Id)?.Count ?? 0,
        }).OrderByDescending(x => x.Buildings).ToList();
        var monthStart = new DateTimeOffset(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return Ok(new
        {
            total,
            active,
            inactive = total - active,
            totalUnits,
            occupiedUnits = occupied,
            vacantUnits = totalUnits - occupied,
            occupancyRate = totalUnits == 0 ? 0 : Math.Round(occupied * 100m / totalUnits, 1),
            newThisMonth = await buildings.CountAsync(b => b.CreatedAt >= monthStart, ct),
            byBranch = byBranch.Select(b => new { b.BranchId, branch = Loc(b.NameEn, b.NameAr), b.Buildings, b.Units }),
        });
    }

    // ------------------------------------------------------------------ profile

    /// <summary>Building profile header: details, owners, tanks and summary counters.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var b = await Scoped(Db.Buildings.AsNoTracking()).Include(x => x.Branch).Include(x => x.City)
                    .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Building", id);

        var phoneTypes = await ContactTypeIds("phone", "mobile");
        var emailTypes = await ContactTypeIds("email");
        var owners = await (from o in Db.BuildingOwners.AsNoTracking()
                            join p in Db.Parties on o.PartyId equals p.Id
                            where o.BuildingId == id
                            orderby o.IsPrimary descending
                            select new
                            {
                                o.Id, o.PartyId, p.NameEn, p.NameAr, p.PartyTypeId, p.EmiratesId, p.TradeLicenseNo, o.OwnershipPercentage, o.IsPrimary, o.FromDate, o.ToDate,
                                Phone = Db.PartyContacts.Where(c => c.PartyId == p.Id && phoneTypes.Contains(c.ContactTypeId)).OrderByDescending(c => c.IsDefault).Select(c => c.Value).FirstOrDefault(),
                                Email = Db.PartyContacts.Where(c => c.PartyId == p.Id && emailTypes.Contains(c.ContactTypeId)).OrderByDescending(c => c.IsDefault).Select(c => c.Value).FirstOrDefault(),
                            }).ToListAsync(ct);

        var tanks = await Db.Tanks.AsNoTracking().Where(t => t.BuildingId == id).Include(t => t.GasType).OrderBy(t => t.TankNo).ToListAsync(ct);
        var srOpen = await OpenSrStatusIds();
        var completed = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed");
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var unitCount = await Db.Units.CountAsync(u => u.BuildingId == id, ct);
        var occupied = await Db.Units.CountAsync(u => u.BuildingId == id && Db.SupplyContracts.Any(c => c.UnitId == u.Id && c.IsActive), ct);
        var amcActive = await Lookups.IdOrNullAsync(LookupTypes.AmcStatus, "active");
        var cd = await Db.CdCertificates.AsNoTracking().Where(c => c.BuildingId == id).OrderByDescending(c => c.ExpiryDate).FirstOrDefaultAsync(ct);

        return Ok(new
        {
            b.Id, b.Code, name = Loc(b.NameEn, b.NameAr), b.NameEn, b.NameAr, b.BranchId, branch = Loc(b.Branch?.NameEn, b.Branch?.NameAr),
            b.CityId, city = Loc(b.City?.NameEn, b.City?.NameAr), b.BuildingTypeId, type = Code(b.BuildingTypeId), typeLabel = Label(b.BuildingTypeId),
            b.StatusId, status = Code(b.StatusId) ?? (b.IsActive ? "active" : "inactive"), statusLabel = Label(b.StatusId),
            b.Zone, b.Street, b.PoBox, b.Address, b.Latitude, b.Longitude, b.TotalFloors, b.Notes, b.IsActive, b.CreatedAt,
            owners = owners.Select(o => new { o.Id, o.PartyId, name = Loc(o.NameEn, o.NameAr), o.NameEn, o.NameAr, partyType = Code(o.PartyTypeId),
                                              o.EmiratesId, o.TradeLicenseNo, o.Phone, o.Email, o.OwnershipPercentage, o.IsPrimary, o.FromDate, o.ToDate }),
            tanks = tanks.Select(t => TanksController.ToDto(t, Code, Label, Loc)),
            stats = new
            {
                units = unitCount,
                occupied,
                vacant = unitCount - occupied,
                meters = await Db.MeterAssignments.CountAsync(a => a.ToDate == null && Db.Units.Any(u => u.Id == a.UnitId && u.BuildingId == id), ct),
                activeContracts = await Db.SupplyContracts.CountAsync(c => c.IsActive && Db.Units.Any(u => u.Id == c.UnitId && u.BuildingId == id), ct),
                tenants = await Db.SupplyContracts.Where(c => c.IsActive && Db.Units.Any(u => u.Id == c.UnitId && u.BuildingId == id)).Select(c => c.TenantId).Distinct().CountAsync(ct),
                openServiceRequests = await Db.ServiceRequests.CountAsync(r => r.BuildingId == id && r.StatusId != null && srOpen.Contains(r.StatusId.Value), ct),
                activeRefills = await Db.RefillRequests.CountAsync(r => r.StatusId != completed && r.StatusId != cancelled && Db.Tanks.Any(t => t.Id == r.TankId && t.BuildingId == id), ct),
                activeAmc = await Db.AmcContracts.CountAsync(a => a.BuildingId == id && a.StatusId == amcActive, ct),
                tankCapacity = tanks.Sum(t => t.CapacityLitres),
                tankLevel = tanks.Sum(t => t.CurrentLevelLitres ?? 0),
                tankLevelPct = tanks.Sum(t => t.CapacityLitres) > 0 ? Math.Round(tanks.Sum(t => t.CurrentLevelLitres ?? 0) / tanks.Sum(t => t.CapacityLitres) * 100, 1) : (decimal?)null,
                civilDefense = cd == null ? null : new { cd.Id, cd.CertificateNo, cd.ExpiryDate, status = Code(cd.StatusId) },
            }
        });
    }

    /// <summary>Activity timeline of the building.</summary>
    [HttpGet("{id:long}/activity")]
    public async Task<IActionResult> ActivityLog(long id, [FromQuery] PageQuery q, CancellationToken ct)
    {
        await LoadLookups();
        var page = await Db.ActivityLogs.AsNoTracking().Where(a => a.BuildingId == id).OrderByDescending(a => a.OccurredAt)
            .Select(a => new { a.Id, a.EntityType, a.EntityId, a.Action, a.Description, a.SeverityId, a.OccurredAt, a.UserId, user = a.User!.NameAr ?? a.User.NameEn })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(a => (object)new { a.Id, a.EntityType, a.EntityId, a.Action, a.Description, severity = Code(a.SeverityId), a.OccurredAt, a.UserId, a.user }).ToList(), page.Total, page.Page, page.PageSize));
    }

    // ------------------------------------------------------------------ create / update / delete

    /// <summary>
    /// "Add building" wizard in one call: building info, owners (existing partyId or a new party), tanks and optional units.
    /// The building code (B-001) is generated.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BuildingInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) && string.IsNullOrWhiteSpace(input.NameEn)) throw new AppException("Building name is required.");
        var branchId = RequireBranch(input.BranchId);
        await using var tx = await Db.Database.BeginTransactionAsync();

        var b = new Building
        {
            BranchId = branchId,
            Code = await Numbers.NextAsync(NumberPrefixes.Building, yearly: false, padding: 3),
            IsActive = true,
        };
        await Apply(b, input);
        Db.Buildings.Add(b);
        await Db.SaveChangesAsync();

        foreach (var o in input.Owners ?? new()) await AddOwner(b.Id, branchId, o);
        await EnsureSinglePrimary(b.Id);

        foreach (var t in input.Tanks ?? new())
            await TanksController.CreateTank(Db, Lookups, b, t.TankNo, t.GasTypeId, t.GasType, t.CapacityLitres, t.CurrentLevelLitres, t.SerialNo,
                t.Manufacturer, t.InstallDate, t.LastInspectionDate, t.NextInspectionDate, t.Status);

        foreach (var u in input.Units ?? new()) await UnitsController.AddUnit(Db, Lookups, b.Id, u);

        await Db.SaveChangesAsync();
        Activity.Log("building", b.Id, "create", $"Building {b.Code} created", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = b.Id }, new { b.Id, b.Code });
    }

    /// <summary>Update building details (owners, tanks and units have their own endpoints).</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] BuildingInput input)
    {
        var b = await Scoped(Db.Buildings).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Building", id);
        if (input.BranchId != null && input.BranchId != b.BranchId) { EnsureBranchAccess(input.BranchId); b.BranchId = input.BranchId.Value; }
        await Apply(b, input);
        Activity.Log("building", b.Id, "update", $"Building {b.Code} updated", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { b.Id, b.Code });
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] StatusInput input)
    {
        var b = await Scoped(Db.Buildings).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Building", id);
        b.StatusId = await Lk(LookupTypes.BuildingStatus, null, input.Status, true, "status");
        b.IsActive = input.Status == "active";
        Activity.Log("building", b.Id, "status_change", $"Building {b.Code} → {input.Status}", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    public record StatusInput(string Status);

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var b = await Scoped(Db.Buildings).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Building", id);
        if (await Db.SupplyContracts.AnyAsync(c => c.IsActive && Db.Units.Any(u => u.Id == c.UnitId && u.BuildingId == id)))
            throw new BusinessRuleException("The building has active supply contracts. Terminate them first.");
        Db.Buildings.Remove(b);
        Activity.Log("building", b.Id, "delete", $"Building {b.Code} deleted", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ owners

    [HttpGet("{id:long}/owners")]
    public async Task<IActionResult> Owners(long id) =>
        Ok(await (from o in Db.BuildingOwners.AsNoTracking()
                  join p in Db.Parties on o.PartyId equals p.Id
                  where o.BuildingId == id
                  select new { o.Id, o.PartyId, name = IsEnglish ? p.NameEn ?? p.NameAr : p.NameAr ?? p.NameEn, o.OwnershipPercentage, o.IsPrimary, o.FromDate, o.ToDate })
                 .ToListAsync());

    [HttpPost("{id:long}/owners")]
    public async Task<IActionResult> AddOwnerEndpoint(long id, [FromBody] OwnerInput input)
    {
        var b = await Scoped(Db.Buildings).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Building", id);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var owner = await AddOwner(id, b.BranchId, input);
        await Db.SaveChangesAsync();
        await EnsureSinglePrimary(id, input.IsPrimary ? owner.Id : null);
        await CheckOwnershipTotal(id);
        await tx.CommitAsync();
        return Ok(new { owner.Id, owner.PartyId });
    }

    [HttpPut("{id:long}/owners/{ownerId:long}")]
    public async Task<IActionResult> UpdateOwner(long id, long ownerId, [FromBody] OwnerInput input)
    {
        var o = await Db.BuildingOwners.FirstOrDefaultAsync(x => x.Id == ownerId && x.BuildingId == id) ?? throw new NotFoundException("BuildingOwner", ownerId);
        o.OwnershipPercentage = input.OwnershipPercentage;
        o.IsPrimary = input.IsPrimary;
        o.FromDate = input.FromDate;
        o.ToDate = input.ToDate;
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (input.IsPrimary) await Db.BuildingOwners.Where(x => x.BuildingId == id && x.Id != ownerId && x.IsPrimary).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsPrimary, false));
        await Db.SaveChangesAsync();
        await CheckOwnershipTotal(id);
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}/owners/{ownerId:long}")]
    public async Task<IActionResult> RemoveOwner(long id, long ownerId)
    {
        var o = await Db.BuildingOwners.FirstOrDefaultAsync(x => x.Id == ownerId && x.BuildingId == id) ?? throw new NotFoundException("BuildingOwner", ownerId);
        Db.BuildingOwners.Remove(o);
        await Db.SaveChangesAsync();
        await EnsureSinglePrimary(id);
        return NoContent();
    }

    // ------------------------------------------------------------------ helpers

    private IQueryable<Building> Scoped(IQueryable<Building> q) =>
        Me.IsAdmin || Me.BranchIds.Count == 0 ? q : q.Where(b => Me.BranchIds.Contains(b.BranchId));

    private async Task<List<long>> OpenSrStatusIds()
    {
        var list = new List<long>();
        foreach (var c in new[] { "pending", "in_progress" })
            if (await Lookups.IdOrNullAsync(LookupTypes.SrStatus, c) is long v) list.Add(v);
        return list;
    }

    private async Task<List<long>> ContactTypeIds(params string[] codes)
    {
        var list = new List<long>();
        foreach (var c in codes)
            if (await Lookups.IdOrNullAsync(LookupTypes.ContactType, c) is long v) list.Add(v);
        return list;
    }

    private async Task Apply(Building b, BuildingInput i)
    {
        b.NameEn = i.NameEn?.Trim();
        b.NameAr = i.NameAr?.Trim();
        b.BuildingTypeId = await Lk(LookupTypes.BuildingType, i.BuildingTypeId, i.BuildingType, field: "buildingType");
        b.StatusId = await Lk(LookupTypes.BuildingStatus, i.StatusId, i.Status ?? "active", field: "status");
        b.IsActive = await Lookups.CodeAsync(b.StatusId) != "inactive";
        b.CityId = i.CityId;
        b.Zone = i.Zone; b.Street = i.Street; b.PoBox = i.PoBox; b.Address = i.Address;
        b.Latitude = i.Latitude; b.Longitude = i.Longitude; b.TotalFloors = i.TotalFloors; b.Notes = i.Notes;
    }

    private async Task<BuildingOwner> AddOwner(long buildingId, long branchId, OwnerInput o)
    {
        long partyId;
        if (o.PartyId != null)
        {
            if (!await Db.Parties.AnyAsync(p => p.Id == o.PartyId)) throw new NotFoundException("Party", o.PartyId);
            partyId = o.PartyId.Value;
        }
        else if (o.Party != null)
        {
            partyId = (await PartiesController.CreateParty(Db, Lookups, branchId, o.Party)).Id;
        }
        else throw new AppException("Each owner needs a partyId or a new party.");

        var owner = new BuildingOwner { BuildingId = buildingId, PartyId = partyId, OwnershipPercentage = o.OwnershipPercentage, IsPrimary = o.IsPrimary, FromDate = o.FromDate, ToDate = o.ToDate };
        Db.BuildingOwners.Add(owner);
        await Db.SaveChangesAsync();
        return owner;
    }

    /// <summary>Exactly one primary owner (filtered unique index ux_building_owner_primary).</summary>
    private async Task EnsureSinglePrimary(long buildingId, long? preferId = null)
    {
        var owners = await Db.BuildingOwners.Where(o => o.BuildingId == buildingId).OrderBy(o => o.Id).ToListAsync();
        if (owners.Count == 0) return;
        var primary = owners.FirstOrDefault(o => o.Id == preferId) ?? owners.FirstOrDefault(o => o.IsPrimary) ?? owners[0];
        foreach (var o in owners.Where(o => o.Id != primary.Id && o.IsPrimary)) o.IsPrimary = false;
        await Db.SaveChangesAsync();
        if (!primary.IsPrimary) { primary.IsPrimary = true; await Db.SaveChangesAsync(); }
    }

    private async Task CheckOwnershipTotal(long buildingId)
    {
        var total = await Db.BuildingOwners.Where(o => o.BuildingId == buildingId).SumAsync(o => o.OwnershipPercentage ?? 0);
        if (total > 100) throw new AppException($"Total ownership would be {total}%, which is more than 100%.");
    }
}
