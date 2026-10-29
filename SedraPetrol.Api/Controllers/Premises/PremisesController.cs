using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Restaurants &amp; Villas (additive module, no existing endpoint changed).
/// <list type="bullet">
/// <item><b>Villa</b> = a standalone site: its host building row (type villa, code VL-…) is created with it; the owner is the customer and signs.</item>
/// <item><b>Restaurant</b> = a site inside an existing building (optionally one of its units), with its owner and its own authorised signatories.</item>
/// <item>Each site has its own tank(s). A restaurant tank is NOT a tank of the building (asset.building_id = NULL), so the building's
/// tank totals / insights are unchanged. Refills use the existing refill workflow (POST /api/refill-requests with the tank id).</item>
/// <item>Gas is sold per refill: <c>POST /api/premises/refills/{refillRequestId}/invoice</c> (user button) = litres delivered × the
/// price per litre of the site's delivery contract.</item>
/// </list>
/// Permissions: the "buildings" module (view / manage); issuing a refill invoice needs "invoices.manage".
/// </summary>
[Route("api/premises")]
[PermissionModule("buildings")]
public class PremisesController : ApiControllerBase
{
    public const string VillaCodePrefix = "VL", RestaurantCodePrefix = "RS", DeliveryContractPrefix = "GDC";

    // ------------------------------------------------------------------ inputs

    /// <summary>An existing party (PartyId) or a new person / company (Party).</summary>
    public record OwnerRef(long? PartyId, BuildingsController.NewPartyInput? Party);

    public record SignatoryInput(string? NameEn, string? NameAr, string? EmiratesId, string? Position, string? Phone, string? Email,
        bool CanSignContracts, string? Notes, bool IsActive = true);

    /// <summary>
    /// Delivery contract: price per litre delivered (before VAT). EndDate or DurationMonths (default 12).
    /// Restaurant: SignatoryId (existing) or SignatoryIndex (position in the Signatories list of the same create request) — the person must be allowed to sign.
    /// Villa: the owner signs (SignedByName defaults to the owner's name).
    /// </summary>
    public record ContractInput(DateOnly StartDate, DateOnly? EndDate, short? DurationMonths, decimal PricePerLitre, long? TaxRateId,
        short? PaymentTermsDays, long? SignatoryId, int? SignatoryIndex, string? SignedByName, DateOnly? SignedDate,
        bool AutoRenew, short? RenewalPeriodMonths, string? Notes,
        /// <summary>Terms &amp; conditions (Settings → «supply» terms, possibly reworded). NULL on update = keep the current terms.</summary>
        List<ContractService.TermItemInput>? TermItems = null);

    public record VillaInput(long? BranchId, string? NameEn, string? NameAr, OwnerRef? Owner,
        long? CityId, string? Zone, string? Street, string? PoBox, string? Address, decimal? Latitude, decimal? Longitude,
        string? Community, string? PlotNo, string? VillaNo, string? MakaniNo, short? Bedrooms, string? Notes,
        List<BuildingsController.TankInput>? Tanks, ContractInput? Contract);

    public record RestaurantInput(long BuildingId, long? UnitId, string? NameEn, string? NameAr, OwnerRef? Owner,
        string? TradeName, string? TradeLicenseNo, DateOnly? TradeLicenseExpiry, int? SeatingCapacity, int? BurnersCount,
        long? KitchenTypeId, string? KitchenType, string? Cuisine, string? OperatingHours, string? Notes,
        List<SignatoryInput>? Signatories, List<BuildingsController.TankInput>? Tanks, ContractInput? Contract);

    public record ActiveInput(bool IsActive);
    public record TerminateInput(DateOnly? Date, string? Reason);
    public record RefillInvoiceInput(DateOnly? IssueDate, string? Notes);

    private PremisesBillingService Billing => HttpContext.RequestServices.GetRequiredService<PremisesBillingService>();
    private PartyService Parties => HttpContext.RequestServices.GetRequiredService<PartyService>();

    // ------------------------------------------------------------------ list / KPIs

    /// <summary>Villas and / or restaurants. Filters: type (villa | restaurant), buildingId, branchId, active, search.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] string? type, [FromQuery] long? buildingId, [FromQuery] long? branchId,
        [FromQuery] bool? active, CancellationToken ct)
    {
        await Billing.LinkVillaHostTanksAsync();
        var activeContract = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        var query = Scoped(Db.Premises.AsNoTracking());
        if (!string.IsNullOrEmpty(type)) query = query.Where(p => p.PremisesType == type);
        if (buildingId != null) query = query.Where(p => p.BuildingId == buildingId);
        if (branchId != null) query = query.Where(p => p.BranchId == branchId);
        if (active != null) query = query.Where(p => p.IsActive == active);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(p => p.Code.Contains(s) || (p.NameEn ?? "").Contains(s) || (p.NameAr ?? "").Contains(s)
                                     || p.Building!.Code.Contains(s) || (p.Building.NameAr ?? "").Contains(s) || (p.Building.NameEn ?? "").Contains(s)
                                     || (p.OwnerParty!.NameAr ?? "").Contains(s) || (p.OwnerParty.NameEn ?? "").Contains(s) || (p.OwnerParty.Phone ?? "").Contains(s));
        }
        query = q.Sort?.ToLowerInvariant() switch
        {
            "name" => q.Desc ? query.OrderByDescending(p => p.NameAr) : query.OrderBy(p => p.NameAr),
            "code" => q.Desc ? query.OrderByDescending(p => p.Code) : query.OrderBy(p => p.Code),
            _ => q.Desc ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
        };
        var projected = query.Select(p => new
        {
            p.Id, p.PremisesType, p.Code, p.NameEn, p.NameAr, p.BranchId, p.BuildingId, p.UnitId, p.IsActive, p.CreatedAt,
            BranchEn = p.Branch!.NameEn, BranchAr = p.Branch.NameAr,
            BuildingCode = p.Building!.Code, BuildingEn = p.Building.NameEn, BuildingAr = p.Building.NameAr, p.Building.Zone,
            UnitNo = p.Unit != null ? p.Unit.UnitNo : null,
            p.OwnerPartyId, OwnerEn = p.OwnerParty!.NameEn, OwnerAr = p.OwnerParty.NameAr, OwnerPhone = p.OwnerParty.Phone,
            Tanks = Db.PremisesTanks.Count(l => l.PremisesId == p.Id),
            Cap = Db.PremisesTanks.Where(l => l.PremisesId == p.Id).Sum(l => (decimal?)l.Tank!.CapacityLitres),
            Level = Db.PremisesTanks.Where(l => l.PremisesId == p.Id).Sum(l => l.Tank!.CurrentLevelLitres),
            Signatories = Db.PremisesSignatories.Count(s => s.PremisesId == p.Id && s.IsActive && s.CanSignContracts),
            Contract = Db.DeliveryContracts.Where(c => c.PremisesId == p.Id && c.StatusId == activeContract).OrderByDescending(c => c.StartDate)
                .Select(c => new { c.Id, c.ContractNo, c.PricePerLitre, c.StartDate, c.EndDate }).FirstOrDefault(),
            Invoiced = Db.PremisesRefillInvoices.Where(x => x.PremisesId == p.Id).Sum(x => (decimal?)x.Invoice!.TotalAmount) ?? 0,
        });
        var page = await projected.ToPagedAsync(q, ct);
        var ids = page.Items.Select(x => x.Id).ToList();
        var ready = (await Billing.RefillsAsync(onlyReady: true, branchIds: BranchScope())).Where(r => ids.Contains(r.PremisesId))
            .GroupBy(r => r.PremisesId).ToDictionary(g => g.Key, g => g.Count());
        var items = page.Items.Select(x => (object)new
        {
            x.Id, type = x.PremisesType, x.Code, name = Loc(x.NameEn, x.NameAr), x.NameEn, x.NameAr,
            x.BranchId, branch = Loc(x.BranchEn, x.BranchAr), x.BuildingId, x.BuildingCode, building = Loc(x.BuildingEn, x.BuildingAr), x.Zone,
            x.UnitId, x.UnitNo, x.OwnerPartyId, owner = Loc(x.OwnerEn, x.OwnerAr), x.OwnerPhone,
            x.Tanks, capacityLitres = x.Cap ?? 0, levelLitres = x.Level ?? 0,
            levelPct = x.Cap > 0 ? Math.Round((x.Level ?? 0) / x.Cap.Value * 100, 1) : (decimal?)null,
            signatories = x.Signatories,
            contractId = x.Contract?.Id, contractNo = x.Contract?.ContractNo, pricePerLitre = x.Contract?.PricePerLitre, contractEnd = x.Contract?.EndDate,
            invoicedTotal = x.Invoiced, readyToInvoice = ready.GetValueOrDefault(x.Id), x.IsActive, x.CreatedAt,
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize));
    }

    /// <summary>KPI cards for the Villas / Restaurants pages.</summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis([FromQuery] string? type, [FromQuery] long? branchId, CancellationToken ct)
    {
        await Billing.LinkVillaHostTanksAsync();
        var query = Scoped(Db.Premises.AsNoTracking());
        if (!string.IsNullOrEmpty(type)) query = query.Where(p => p.PremisesType == type);
        if (branchId != null) query = query.Where(p => p.BranchId == branchId);
        var activeContract = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        var ids = query.Select(p => p.Id);
        var tanks = Db.PremisesTanks.Where(l => ids.Contains(l.PremisesId)).Select(l => l.Tank!);
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);
        var idList = await ids.ToListAsync(ct);
        var ready = (await Billing.RefillsAsync(premisesType: string.IsNullOrEmpty(type) ? null : type, onlyReady: true, branchIds: BranchScope()))
            .Where(r => idList.Contains(r.PremisesId)).ToList();
        return Ok(new
        {
            total = await query.CountAsync(ct),
            active = await query.CountAsync(p => p.IsActive, ct),
            withContract = await query.CountAsync(p => Db.DeliveryContracts.Any(c => c.PremisesId == p.Id && c.StatusId == activeContract), ct),
            tanks = await tanks.CountAsync(ct),
            lowTanks = await tanks.CountAsync(t => t.CapacityLitres > 0 && (t.CurrentLevelLitres ?? 0) * 100 / t.CapacityLitres < TankRules.LowLevelPercent, ct),
            readyToInvoice = ready.Count,
            readyLitres = ready.Sum(r => r.DeliveredLitres ?? 0),
            invoicedThisMonth = await Db.PremisesRefillInvoices.Where(x => ids.Contains(x.PremisesId) && x.Invoice!.IssueDate >= monthStart)
                .SumAsync(x => (decimal?)x.Invoice!.TotalAmount, ct) ?? 0,
            litresThisMonth = await Db.PremisesRefillInvoices.Where(x => ids.Contains(x.PremisesId) && x.Invoice!.IssueDate >= monthStart)
                .SumAsync(x => (decimal?)x.Litres, ct) ?? 0,
        });
    }

    /// <summary>For the Buildings page: which buildings are villas and how many restaurants each building has.</summary>
    [HttpGet("by-building")]
    public async Task<IActionResult> ByBuilding([FromQuery] long? branchId, CancellationToken ct)
    {
        var query = Scoped(Db.Premises.AsNoTracking());
        if (branchId != null) query = query.Where(p => p.BranchId == branchId);
        var list = await query.Select(p => new { p.Id, p.BuildingId, p.PremisesType }).ToListAsync(ct);
        return Ok(list.GroupBy(p => p.BuildingId).Select(g => new
        {
            buildingId = g.Key,
            villaId = g.Where(p => p.PremisesType == PremisesTypes.Villa).Select(p => (long?)p.Id).FirstOrDefault(),
            restaurants = g.Count(p => p.PremisesType == PremisesTypes.Restaurant),
        }));
    }

    // ------------------------------------------------------------------ profile

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await Billing.LinkVillaHostTanksAsync();
        await LoadLookups();
        var p = await Scoped(Db.Premises.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Premises", id);
        var b = await Db.Buildings.AsNoTracking().Include(x => x.City).FirstOrDefaultAsync(x => x.Id == p.BuildingId, ct);
        var unit = p.UnitId == null ? null : await Db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == p.UnitId, ct);
        var owner = await Db.Parties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == p.OwnerPartyId, ct);
        var branch = await Db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == p.BranchId, ct);
        var signatories = await Db.PremisesSignatories.AsNoTracking().Where(s => s.PremisesId == id).OrderByDescending(s => s.CanSignContracts).ThenBy(s => s.Id).ToListAsync(ct);
        var tankIds = await Db.PremisesTanks.AsNoTracking().Where(l => l.PremisesId == id).Select(l => l.TankId).ToListAsync(ct);
        var tanks = await Db.Tanks.AsNoTracking().Include(t => t.GasType).Where(t => tankIds.Contains(t.Id)).OrderBy(t => t.TankNo).ToListAsync(ct);
        var openRefill = await OpenRefillsByTank(tankIds);
        var contracts = await ContractDtos(Db.DeliveryContracts.AsNoTracking().Where(c => c.PremisesId == id));
        var refills = await Billing.RefillsAsync(premisesId: id);
        var isCustomer = owner != null && await Db.Tenants.AnyAsync(t => t.Id == owner.Id, ct);

        return Ok(new
        {
            p.Id, type = p.PremisesType, p.Code, name = Loc(p.NameEn, p.NameAr), p.NameEn, p.NameAr, p.Notes, p.IsActive, p.CreatedAt,
            p.BranchId, branch = Loc(branch?.NameEn, branch?.NameAr),
            building = b == null ? null : new
            {
                b.Id, b.Code, name = Loc(b.NameEn, b.NameAr), b.NameEn, b.NameAr, b.CityId, city = Loc(b.City?.NameEn, b.City?.NameAr),
                b.Zone, b.Street, b.PoBox, b.Address, b.Latitude, b.Longitude, buildingType = Code(b.BuildingTypeId), buildingTypeLabel = Label(b.BuildingTypeId),
            },
            unit = unit == null ? null : new { unit.Id, unit.UnitNo, unit.FloorNo },
            owner = owner == null ? null : new
            {
                owner.Id, owner.PartyCode, name = Loc(owner.NameEn, owner.NameAr), owner.NameEn, owner.NameAr, owner.Phone, owner.Email,
                owner.EmiratesId, owner.TradeLicenseNo, trn = owner.TaxNumber, isCustomer,
            },
            villa = p is Villa v ? new { v.Community, v.PlotNo, v.VillaNo, v.MakaniNo, v.Bedrooms } : null,
            restaurant = p is Restaurant r ? new
            {
                r.TradeName, r.TradeLicenseNo, r.TradeLicenseExpiry, r.SeatingCapacity, r.BurnersCount,
                r.KitchenTypeId, kitchenType = Code(r.KitchenTypeId), kitchenTypeLabel = Label(r.KitchenTypeId), r.Cuisine, r.OperatingHours,
            } : null,
            signatories = signatories.Select(SignatoryDto),
            tanks = tanks.Select(t => new { tank = TanksController.ToDto(t, Code, Label, Loc), openRequest = openRefill.GetValueOrDefault(t.Id) }),
            contracts,
            refills,
        });
    }

    // ------------------------------------------------------------------ villa

    /// <summary>Create a villa: host building (type villa, code VL-…), primary owner (100 %), tanks, optional delivery contract.</summary>
    [HttpPost("villas")]
    public async Task<IActionResult> CreateVilla([FromBody] VillaInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) && string.IsNullOrWhiteSpace(input.NameEn)) throw new AppException("The villa name is required.");
        var branchId = RequireBranch(input.BranchId);
        await using var tx = await Db.Database.BeginTransactionAsync();

        var code = await Numbers.NextAsync(VillaCodePrefix, yearly: false, padding: 3);
        if (await Db.Buildings.IgnoreQueryFilters().AnyAsync(x => x.Code == code)) throw new BusinessRuleException($"Code {code} is already used by a building.");
        var b = new Building
        {
            BranchId = branchId, Code = code, IsActive = true,
            BuildingTypeId = await Lookups.IdOrNullAsync(LookupTypes.BuildingType, PremisesTypes.Villa),
            StatusId = await Lookups.IdOrNullAsync(LookupTypes.BuildingStatus, "active"),
        };
        ApplyVillaBuilding(b, input);
        Db.Buildings.Add(b);
        await Db.SaveChangesAsync();

        var ownerId = await ResolveOwner(input.Owner, branchId, "villa owner");
        Db.BuildingOwners.Add(new BuildingOwner { BuildingId = b.Id, PartyId = ownerId, OwnershipPercentage = 100, IsPrimary = true });

        var v = new Villa { Code = code, BranchId = branchId, BuildingId = b.Id, OwnerPartyId = ownerId, IsActive = true };
        ApplyVilla(v, input);
        Db.Villas.Add(v);
        await Db.SaveChangesAsync();

        foreach (var t in input.Tanks ?? new())
        {
            var tank = await TanksController.CreateTank(Db, Lookups, b, t.TankNo, t.GasTypeId, t.GasType, t.CapacityLitres, t.CurrentLevelLitres, t.SerialNo,
                t.Manufacturer, t.InstallDate, t.LastInspectionDate, t.NextInspectionDate, t.Status);
            Db.PremisesTanks.Add(new PremisesTank { PremisesId = v.Id, TankId = tank.Id });
        }
        await Db.SaveChangesAsync();

        long? contractId = null;
        if (input.Contract != null) contractId = (await CreateContract(v, input.Contract, null)).Id;

        Activity.Log(PremisesTypes.Villa, v.Id, "create", $"Villa {v.Code} created", b.Id, branchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        await Parties.EnsureAccountAsync(ownerId);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, new { v.Id, v.Code, buildingId = b.Id, contractId });
    }

    /// <summary>Update a villa (its details and the address on its host building). Owner and tanks have their own endpoints.</summary>
    [HttpPut("villas/{id:long}")]
    public async Task<IActionResult> UpdateVilla(long id, [FromBody] VillaInput input)
    {
        var v = await Db.Villas.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Villa", id);
        EnsureBranchAccess(v.BranchId);
        if (string.IsNullOrWhiteSpace(input.NameAr) && string.IsNullOrWhiteSpace(input.NameEn)) throw new AppException("The villa name is required.");
        var b = await Db.Buildings.FirstAsync(x => x.Id == v.BuildingId);
        ApplyVillaBuilding(b, input);
        ApplyVilla(v, input);
        if (input.Owner != null)
        {
            var ownerId = await ResolveOwner(input.Owner, v.BranchId, "villa owner");
            if (ownerId != v.OwnerPartyId)
            {
                v.OwnerPartyId = ownerId;
                foreach (var o in await Db.BuildingOwners.Where(o => o.BuildingId == b.Id).ToListAsync()) Db.BuildingOwners.Remove(o);
                await Db.SaveChangesAsync();
                Db.BuildingOwners.Add(new BuildingOwner { BuildingId = b.Id, PartyId = ownerId, OwnershipPercentage = 100, IsPrimary = true });
            }
        }
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private static void ApplyVillaBuilding(Building b, VillaInput i)
    {
        b.NameEn = i.NameEn?.Trim(); b.NameAr = i.NameAr?.Trim();
        b.CityId = i.CityId; b.Zone = i.Zone ?? i.Community; b.Street = i.Street; b.PoBox = i.PoBox; b.Address = i.Address;
        b.Latitude = i.Latitude; b.Longitude = i.Longitude; b.Notes = i.Notes;
    }

    private static void ApplyVilla(Villa v, VillaInput i)
    {
        v.NameEn = i.NameEn?.Trim(); v.NameAr = i.NameAr?.Trim(); v.Notes = i.Notes;
        v.Community = i.Community; v.PlotNo = i.PlotNo; v.VillaNo = i.VillaNo; v.MakaniNo = i.MakaniNo; v.Bedrooms = i.Bedrooms;
    }

    // ------------------------------------------------------------------ restaurant

    /// <summary>Add a restaurant to an existing building (optionally to one of its units), with owner, signatories, tanks and an optional contract.</summary>
    [HttpPost("restaurants")]
    public async Task<IActionResult> CreateRestaurant([FromBody] RestaurantInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) && string.IsNullOrWhiteSpace(input.NameEn) && string.IsNullOrWhiteSpace(input.TradeName))
            throw new AppException("The restaurant name is required.");
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        if (await Db.Premises.AnyAsync(p => p.BuildingId == b.Id && p.PremisesType == PremisesTypes.Villa))
            throw new BusinessRuleException("This building is a villa. Restaurants are added to buildings.");
        await CheckUnit(b.Id, input.UnitId, null);
        await using var tx = await Db.Database.BeginTransactionAsync();

        var ownerId = await ResolveOwner(input.Owner, b.BranchId, "restaurant owner");
        var r = new Restaurant
        {
            Code = await Numbers.NextAsync(RestaurantCodePrefix, yearly: false, padding: 3),
            BranchId = b.BranchId, BuildingId = b.Id, OwnerPartyId = ownerId, IsActive = true,
        };
        await ApplyRestaurant(r, input);
        Db.Restaurants.Add(r);
        await Db.SaveChangesAsync();

        var signatories = new List<PremisesSignatory>();
        foreach (var s in input.Signatories ?? new())
        {
            var row = new PremisesSignatory { PremisesId = r.Id };
            ApplySignatory(row, s);
            Db.PremisesSignatories.Add(row);
            signatories.Add(row);
        }
        await Db.SaveChangesAsync();

        foreach (var t in input.Tanks ?? new()) await CreateSiteTank(r, t);
        await Db.SaveChangesAsync();

        long? contractId = null;
        if (input.Contract != null)
        {
            var sigId = input.Contract.SignatoryId
                        ?? (input.Contract.SignatoryIndex is int ix && ix >= 0 && ix < signatories.Count ? signatories[ix].Id : null);
            contractId = (await CreateContract(r, input.Contract, sigId)).Id;
        }

        Activity.Log(PremisesTypes.Restaurant, r.Id, "create", $"Restaurant {r.Code} added to building {b.Code}", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        await Parties.EnsureAccountAsync(ownerId);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, new { r.Id, r.Code, contractId });
    }

    [HttpPut("restaurants/{id:long}")]
    public async Task<IActionResult> UpdateRestaurant(long id, [FromBody] RestaurantInput input)
    {
        var r = await Db.Restaurants.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Restaurant", id);
        EnsureBranchAccess(r.BranchId);
        if (input.BuildingId != 0 && input.BuildingId != r.BuildingId) throw new BusinessRuleException("A restaurant cannot be moved to another building. Add it to the other building instead.");
        await CheckUnit(r.BuildingId, input.UnitId, r.Id);
        await ApplyRestaurant(r, input);
        if (input.Owner != null) r.OwnerPartyId = await ResolveOwner(input.Owner, r.BranchId, "restaurant owner");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task ApplyRestaurant(Restaurant r, RestaurantInput i)
    {
        r.NameEn = (i.NameEn ?? i.TradeName)?.Trim(); r.NameAr = i.NameAr?.Trim(); r.Notes = i.Notes; r.UnitId = i.UnitId;
        r.TradeName = i.TradeName?.Trim(); r.TradeLicenseNo = i.TradeLicenseNo?.Trim(); r.TradeLicenseExpiry = i.TradeLicenseExpiry;
        if (i.SeatingCapacity < 0 || i.BurnersCount < 0) throw new AppException("Seating and burners cannot be negative.");
        r.SeatingCapacity = i.SeatingCapacity; r.BurnersCount = i.BurnersCount; r.Cuisine = i.Cuisine; r.OperatingHours = i.OperatingHours;
        r.KitchenTypeId = await Lk("KITCHEN_TYPE", i.KitchenTypeId, i.KitchenType, field: "kitchenType");
    }

    private async Task CheckUnit(long buildingId, long? unitId, long? selfId)
    {
        if (unitId == null) return;
        if (!await Db.Units.AnyAsync(u => u.Id == unitId && u.BuildingId == buildingId)) throw new AppException("The unit does not belong to this building.");
        if (await Db.Premises.AnyAsync(p => p.UnitId == unitId && p.Id != selfId)) throw new BusinessRuleException("Another restaurant is already linked to this unit.");
    }

    // ------------------------------------------------------------------ common: active / delete

    [HttpPatch("{id:long}/active")]
    public async Task<IActionResult> SetActive(long id, [FromBody] ActiveInput input)
    {
        var p = await Db.Premises.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Premises", id);
        EnsureBranchAccess(p.BranchId);
        if (!input.IsActive && await HasActiveContract(p.Id)) throw new BusinessRuleException("Terminate the active delivery contract first.");
        p.IsActive = input.IsActive;
        Activity.Log(p.PremisesType, p.Id, "status_change", $"{p.Code} {(p.IsActive ? "activated" : "deactivated")}", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Delete a villa / restaurant that was never used (no contract, no refill, no invoice). Its tanks and signatories go with it;
    /// a villa's host building only when nothing else uses that building. Used sites are deactivated instead.
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var p = await Db.Premises.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Premises", id);
        EnsureBranchAccess(p.BranchId);
        if (await Db.DeliveryContracts.AnyAsync(c => c.PremisesId == id)) throw new BusinessRuleException("It has delivery contracts. Deactivate it instead.");
        if (await Db.PremisesRefillInvoices.AnyAsync(x => x.PremisesId == id)) throw new BusinessRuleException("It has refill invoices. Deactivate it instead.");
        if (await Db.AmcContracts.AnyAsync(x => x.PremisesId == id)) throw new BusinessRuleException("It has maintenance contracts. Deactivate it instead.");
        var links = await Db.PremisesTanks.Where(l => l.PremisesId == id).ToListAsync();
        var tankIds = links.Select(l => l.TankId).ToList();
        if (await Db.RefillRequests.AnyAsync(r => tankIds.Contains(r.TankId))) throw new BusinessRuleException("Its tanks have refills. Deactivate it instead.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        if (p is Villa)
        {
            var bid = p.BuildingId;
            var used = await Db.Contracts.AnyAsync(c => c.BuildingId == bid) || await Db.Invoices.AnyAsync(i => i.BuildingId == bid)
                       || await Db.ServiceRequests.AnyAsync(s => s.BuildingId == bid) || await Db.CdCertificates.AnyAsync(c => c.BuildingId == bid)
                       || await Db.MaintenanceVisits.AnyAsync(m => m.BuildingId == bid) || await Db.Units.AnyAsync(u => u.BuildingId == bid)
                       || await Db.Tanks.AnyAsync(t => t.BuildingId == bid && !tankIds.Contains(t.Id));
            if (used) throw new BusinessRuleException("The villa's building has contracts, invoices, certificates, requests or units. Deactivate the villa instead.");
        }
        foreach (var l in links) Db.PremisesTanks.Remove(l);
        foreach (var t in await Db.Tanks.Where(t => tankIds.Contains(t.Id)).ToListAsync()) Db.Tanks.Remove(t);
        foreach (var s in await Db.PremisesSignatories.Where(s => s.PremisesId == id).ToListAsync()) Db.PremisesSignatories.Remove(s);
        Db.Premises.Remove(p);
        if (p is Villa)
        {
            foreach (var o in await Db.BuildingOwners.Where(o => o.BuildingId == p.BuildingId).ToListAsync()) Db.BuildingOwners.Remove(o);
            var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == p.BuildingId);
            if (b != null) Db.Buildings.Remove(b);
        }
        Activity.Log(p.PremisesType, p.Id, "delete", $"{p.Code} deleted", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ signatories (restaurant staff who may sign)

    [HttpGet("{id:long}/signatories")]
    public async Task<IActionResult> Signatories(long id)
    {
        await RequireSite(id);
        return Ok((await Db.PremisesSignatories.AsNoTracking().Where(s => s.PremisesId == id).OrderBy(s => s.Id).ToListAsync()).Select(SignatoryDto));
    }

    [HttpPost("{id:long}/signatories")]
    public async Task<IActionResult> AddSignatory(long id, [FromBody] SignatoryInput input)
    {
        var p = await RequireSite(id);
        var s = new PremisesSignatory { PremisesId = p.Id };
        ApplySignatory(s, input);
        Db.PremisesSignatories.Add(s);
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: signatory {s.NameAr ?? s.NameEn} added", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return Ok(SignatoryDto(s));
    }

    [HttpPut("{id:long}/signatories/{signatoryId:long}")]
    public async Task<IActionResult> UpdateSignatory(long id, long signatoryId, [FromBody] SignatoryInput input)
    {
        await RequireSite(id);
        var s = await Db.PremisesSignatories.FirstOrDefaultAsync(x => x.Id == signatoryId && x.PremisesId == id) ?? throw new NotFoundException("PremisesSignatory", signatoryId);
        ApplySignatory(s, input);
        await Db.SaveChangesAsync();
        return Ok(SignatoryDto(s));
    }

    [HttpDelete("{id:long}/signatories/{signatoryId:long}")]
    public async Task<IActionResult> DeleteSignatory(long id, long signatoryId)
    {
        await RequireSite(id);
        var s = await Db.PremisesSignatories.FirstOrDefaultAsync(x => x.Id == signatoryId && x.PremisesId == id) ?? throw new NotFoundException("PremisesSignatory", signatoryId);
        if (await Db.DeliveryContracts.AnyAsync(c => c.SignatoryId == signatoryId))
        {
            // signed a contract: keep the record, only withdraw the authority
            s.IsActive = false; s.CanSignContracts = false;
        }
        else Db.PremisesSignatories.Remove(s);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private static void ApplySignatory(PremisesSignatory s, SignatoryInput i)
    {
        if (string.IsNullOrWhiteSpace(i.NameAr) && string.IsNullOrWhiteSpace(i.NameEn)) throw new AppException("The signatory name is required.");
        s.NameEn = i.NameEn?.Trim(); s.NameAr = i.NameAr?.Trim(); s.EmiratesId = i.EmiratesId?.Trim(); s.Position = i.Position?.Trim();
        s.Phone = i.Phone?.Trim(); s.Email = i.Email?.Trim(); s.CanSignContracts = i.CanSignContracts; s.Notes = i.Notes; s.IsActive = i.IsActive;
    }

    private object SignatoryDto(PremisesSignatory s) => new
    {
        s.Id, s.PremisesId, name = Loc(s.NameEn, s.NameAr), s.NameEn, s.NameAr, s.EmiratesId, s.Position, s.Phone, s.Email,
        s.CanSignContracts, s.Notes, s.IsActive,
    };

    // ------------------------------------------------------------------ tanks of a site

    [HttpPost("{id:long}/tanks")]
    public async Task<IActionResult> AddTank(long id, [FromBody] BuildingsController.TankInput input)
    {
        var p = await RequireSite(id);
        Tank tank;
        if (p is Villa)
        {
            var b = await Db.Buildings.FirstAsync(x => x.Id == p.BuildingId);
            tank = await TanksController.CreateTank(Db, Lookups, b, input.TankNo, input.GasTypeId, input.GasType, input.CapacityLitres, input.CurrentLevelLitres,
                input.SerialNo, input.Manufacturer, input.InstallDate, input.LastInspectionDate, input.NextInspectionDate, input.Status);
            Db.PremisesTanks.Add(new PremisesTank { PremisesId = p.Id, TankId = tank.Id });
        }
        else tank = await CreateSiteTank(p, input);
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: tank {tank.TankNo} added", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { tank.Id, tank.TankNo });
    }

    /// <summary>
    /// A restaurant tank: an asset_tank with building_id = NULL (never part of the building's tanks), code TK-RS-001-T1,
    /// linked through premises_tank. Same validation and initial level history as building tanks.
    /// </summary>
    private async Task<Tank> CreateSiteTank(Premises p, BuildingsController.TankInput i)
    {
        if (i.CapacityLitres <= 0) throw new AppException("Tank capacity must be greater than zero.");
        if ((i.CurrentLevelLitres ?? 0) < 0 || (i.CurrentLevelLitres ?? 0) > i.CapacityLitres) throw new AppException("Tank level must be between 0 and the capacity.");
        await Db.SaveChangesAsync();   // earlier tanks of the same request are saved, so the numbering below sees them
        var existing = await Db.PremisesTanks.Where(l => l.PremisesId == p.Id).Select(l => l.Tank!.TankNo).ToListAsync();
        var tankNo = string.IsNullOrWhiteSpace(i.TankNo) ? $"T{existing.Count + 1}" : i.TankNo.Trim();
        if (existing.Contains(tankNo)) throw new BusinessRuleException($"Tank {tankNo} already exists for {p.Code}.");
        var level = i.CurrentLevelLitres;
        var t = new Tank
        {
            BranchId = p.BranchId, BuildingId = null, TankNo = tankNo, AssetCode = $"TK-{p.Code}-{tankNo}",
            GasTypeId = await TanksController.ResolveGasType(Db, i.GasTypeId, i.GasType ?? "LPG") ?? throw new AppException("Gas type is required."),
            CapacityLitres = i.CapacityLitres, CurrentLevelLitres = level ?? 0, SerialNo = i.SerialNo, Manufacturer = i.Manufacturer,
            InstallDate = i.InstallDate, LastInspectionDate = i.LastInspectionDate, NextInspectionDate = i.NextInspectionDate, IsActive = true,
            StatusId = await Lookups.ResolveAsync(LookupTypes.TankStatus, null, i.Status ?? ((level ?? 0) * 100 / i.CapacityLitres < TankRules.LowLevelPercent ? "low" : "ok"), field: "status"),
            NameEn = $"Gas tank {tankNo} — {p.Code}", NameAr = $"خزان غاز {tankNo} — {p.Code}",
        };
        Db.Tanks.Add(t);
        await Db.SaveChangesAsync();
        Db.PremisesTanks.Add(new PremisesTank { PremisesId = p.Id, TankId = t.Id });
        if (level != null)
            Db.TankLevelHistories.Add(new TankLevelHistory { TankId = t.Id, EventDate = DateTimeOffset.UtcNow, LevelLitres = level.Value, Notes = "Initial level",
                EventTypeId = await Lookups.IdOrNullAsync(LookupTypes.TankEventType, "manual_reading") });
        await Db.SaveChangesAsync();
        return t;
    }

    /// <summary>Remove a tank from a site (only when it never had a refill).</summary>
    [HttpDelete("{id:long}/tanks/{tankId:long}")]
    public async Task<IActionResult> RemoveTank(long id, long tankId)
    {
        var p = await RequireSite(id);
        var link = await Db.PremisesTanks.FirstOrDefaultAsync(l => l.PremisesId == id && l.TankId == tankId) ?? throw new NotFoundException("PremisesTank", tankId);
        if (await Db.RefillRequests.AnyAsync(r => r.TankId == tankId)) throw new BusinessRuleException("The tank has refills. Set its status to out of service instead.");
        Db.PremisesTanks.Remove(link);
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == tankId);
        if (t != null) Db.Tanks.Remove(t);
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: tank removed", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ delivery contracts (price per litre)

    [HttpGet("{id:long}/contracts")]
    public async Task<IActionResult> Contracts(long id)
    {
        await RequireSite(id);
        return Ok(await ContractDtos(Db.DeliveryContracts.AsNoTracking().Where(c => c.PremisesId == id)));
    }

    /// <summary>All delivery contracts (villas and restaurants). Filters: type, status, search.</summary>
    [HttpGet("contracts")]
    public async Task<IActionResult> AllContracts([FromQuery] PageQuery q, [FromQuery] string? type, [FromQuery] string? status)
    {
        var query = Db.DeliveryContracts.AsNoTracking().Where(c => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(c.BranchId));
        if (!string.IsNullOrEmpty(type)) query = query.Where(c => c.Premises!.PremisesType == type);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, status); query = query.Where(c => c.StatusId == s); }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(c => c.ContractNo.Contains(q.Search) || c.Premises!.Code.Contains(q.Search) || (c.Premises.NameAr ?? "").Contains(q.Search) || (c.Premises.NameEn ?? "").Contains(q.Search));
        var total = await query.CountAsync();
        var paged = q.PageSize <= 0 ? query.OrderByDescending(c => c.StartDate).Take(5000)
            : query.OrderByDescending(c => c.StartDate).Skip((Math.Max(1, q.Page) - 1) * Math.Min(q.PageSize, 500)).Take(Math.Min(q.PageSize, 500));
        return Ok(new PagedResult<object>(await ContractDtos(paged, keepOrder: true), total, Math.Max(1, q.Page), q.PageSize));
    }

    [HttpPost("{id:long}/contracts")]
    public async Task<IActionResult> AddContract(long id, [FromBody] ContractInput input)
    {
        var p = await RequireSite(id);
        if (!p.IsActive) throw new BusinessRuleException($"{p.Code} is inactive.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var c = await CreateContract(p, input, input.SignatoryId);
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: delivery contract {c.ContractNo} created", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { c.Id, c.ContractNo });
    }

    /// <summary>Update price / terms of a contract. A price change applies to refills invoiced after the change.</summary>
    [HttpPut("contracts/{contractId:long}")]
    public async Task<IActionResult> UpdateContract(long contractId, [FromBody] ContractInput input)
    {
        var c = await Db.DeliveryContracts.FirstOrDefaultAsync(x => x.Id == contractId) ?? throw new NotFoundException("DeliveryContract", contractId);
        EnsureBranchAccess(c.BranchId);
        var p = await Db.Premises.FirstAsync(x => x.Id == c.PremisesId);
        await ApplyContract(c, p, input, input.SignatoryId);
        await EnsureNoOverlap(c);
        await HttpContext.RequestServices.GetRequiredService<ContractService>().SetTermsAsync(c.Id, input.TermItems);
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: delivery contract {c.ContractNo} updated (price {c.PricePerLitre:0.####}/L)", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("contracts/{contractId:long}/terminate")]
    public async Task<IActionResult> TerminateContract(long contractId, [FromBody] TerminateInput input)
    {
        var c = await Db.DeliveryContracts.FirstOrDefaultAsync(x => x.Id == contractId) ?? throw new NotFoundException("DeliveryContract", contractId);
        EnsureBranchAccess(c.BranchId);
        var date = input.Date ?? Today;
        if (date < c.StartDate) throw new AppException("The termination date cannot be before the contract start.");
        c.StatusId = await LkId(LookupTypes.ContractStatus, "terminated");
        c.TerminatedAt = date; c.EndDate = date; c.IsActive = false;
        if (!string.IsNullOrWhiteSpace(input.Reason)) c.Notes = string.IsNullOrWhiteSpace(c.Notes) ? $"Terminated: {input.Reason}" : $"{c.Notes}\nTerminated: {input.Reason}";
        var p = await Db.Premises.FirstAsync(x => x.Id == c.PremisesId);
        Activity.Log(p.PremisesType, p.Id, "status_change", $"{p.Code}: delivery contract {c.ContractNo} terminated{(string.IsNullOrWhiteSpace(input.Reason) ? "" : " — " + input.Reason)}", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<DeliveryContract> CreateContract(Premises p, ContractInput input, long? signatoryId)
    {
        var c = new DeliveryContract
        {
            BranchId = p.BranchId, BuildingId = p.BuildingId, UnitId = null,
            ContractNo = await Numbers.NextAsync(DeliveryContractPrefix, date: input.StartDate),
            CurrencyId = await Db.Currencies.Where(x => x.Code == "AED").Select(x => (long?)x.Id).FirstOrDefaultAsync(),
            StatusId = await LkId(LookupTypes.ContractStatus, "active"), IsActive = true,
            PremisesId = p.Id,
        };
        await ApplyContract(c, p, input, signatoryId);
        await EnsureNoOverlap(c);
        Db.DeliveryContracts.Add(c);
        await Db.SaveChangesAsync();
        // same terms table as the building gas supply contract (contract_type «supply», contract_id = contract.id)
        await HttpContext.RequestServices.GetRequiredService<ContractService>().SetTermsAsync(c.Id, input.TermItems);
        return c;
    }

    private async Task ApplyContract(DeliveryContract c, Premises p, ContractInput i, long? signatoryId)
    {
        if (i.PricePerLitre <= 0) throw new AppException("The price per litre must be greater than zero.");
        var end = i.EndDate ?? i.StartDate.AddMonths(i.DurationMonths is > 0 ? i.DurationMonths.Value : 12).AddDays(-1);
        if (end < i.StartDate) throw new AppException("The end date cannot be before the start date.");
        if (i.AutoRenew && (i.RenewalPeriodMonths ?? 0) <= 0) throw new AppException("Auto-renew needs a renewal period in months.");
        if (i.TaxRateId != null && !await Db.TaxRates.AnyAsync(t => t.Id == i.TaxRateId)) throw new NotFoundException("TaxRate", i.TaxRateId);
        if (i.PaymentTermsDays < 0) throw new AppException("Payment terms cannot be negative.");
        c.StartDate = i.StartDate; c.EndDate = end; c.AutoRenew = i.AutoRenew; c.RenewalPeriodMonths = i.AutoRenew ? i.RenewalPeriodMonths : null;
        c.PricePerLitre = i.PricePerLitre; c.TaxRateId = i.TaxRateId; c.PaymentTermsDays = i.PaymentTermsDays; c.SignedDate = i.SignedDate; c.Notes = i.Notes;
        c.CustomerPartyId = p.OwnerPartyId;

        if (p is Restaurant)
        {
            // signed by one of the restaurant's own staff who is allowed to sign
            if (signatoryId == null) throw new AppException("Choose the restaurant's authorised signatory who signs the contract.");
            var s = await Db.PremisesSignatories.FirstOrDefaultAsync(x => x.Id == signatoryId && x.PremisesId == p.Id)
                    ?? throw new AppException("The signatory does not belong to this restaurant.");
            if (!s.IsActive || !s.CanSignContracts) throw new BusinessRuleException($"{s.NameAr ?? s.NameEn} is not authorised to sign contracts.");
            c.SignatoryId = s.Id;
            c.SignedByName = string.IsNullOrWhiteSpace(i.SignedByName) ? (s.NameAr ?? s.NameEn) : i.SignedByName.Trim();
        }
        else
        {
            // villa: no tenant — the owner requests and signs
            c.SignatoryId = null;
            c.SignedByName = string.IsNullOrWhiteSpace(i.SignedByName)
                ? await Db.Parties.Where(x => x.Id == p.OwnerPartyId).Select(x => x.NameAr ?? x.NameEn).FirstOrDefaultAsync()
                : i.SignedByName.Trim();
        }
    }

    /// <summary>One active delivery contract per site at a time (periods must not overlap).</summary>
    private async Task EnsureNoOverlap(DeliveryContract c)
    {
        var active = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        var clash = await Db.DeliveryContracts.Where(x => x.PremisesId == c.PremisesId && x.Id != c.Id && x.StatusId == active
                                                          && x.StartDate <= (c.EndDate ?? DateOnly.MaxValue) && (x.EndDate ?? DateOnly.MaxValue) >= c.StartDate)
            .Select(x => x.ContractNo).FirstOrDefaultAsync();
        if (clash != null) throw new BusinessRuleException($"The period overlaps the active contract {clash}. Terminate or shorten it first.");
    }

    private async Task<List<object>> ContractDtos(IQueryable<DeliveryContract> query, bool keepOrder = false)
    {
        await LoadLookups();
        var rows = await query.Select(c => new
        {
            c, PCode = c.Premises!.Code, PType = c.Premises.PremisesType, PEn = c.Premises.NameEn, PAr = c.Premises.NameAr,
            CustEn = c.CustomerParty!.NameEn, CustAr = c.CustomerParty.NameAr, Tax = c.TaxRate != null ? c.TaxRate.Code : null,
            Invoiced = Db.PremisesRefillInvoices.Where(x => x.ContractId == c.Id).Sum(x => (decimal?)x.Invoice!.TotalAmount) ?? 0,
            Litres = Db.PremisesRefillInvoices.Where(x => x.ContractId == c.Id).Sum(x => (decimal?)x.Litres) ?? 0,
        }).ToListAsync();
        if (!keepOrder) rows = rows.OrderByDescending(x => x.c.StartDate).ToList();
        var ids = rows.Select(x => x.c.Id).ToList();
        var terms = await Db.ContractTerms.AsNoTracking().Where(t => t.ContractType == "supply" && t.ContractId != null && ids.Contains(t.ContractId.Value))
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { ContractId = t.ContractId!.Value, t.Id, t.SourceId, t.Category, t.TextAr, t.IsCustom, t.IsModified }).ToListAsync();
        return rows.Select(x => (object)new
        {
            x.c.Id, x.c.ContractNo, x.c.PremisesId, premisesCode = x.PCode, premisesType = x.PType, premises = Loc(x.PEn, x.PAr),
            x.c.BuildingId, x.c.StartDate, x.c.EndDate, x.c.AutoRenew, x.c.RenewalPeriodMonths, x.c.TerminatedAt,
            status = Code(x.c.StatusId), statusLabel = Label(x.c.StatusId), x.c.IsActive,
            x.c.PricePerLitre, x.c.TaxRateId, taxCode = x.Tax, x.c.PaymentTermsDays,
            x.c.CustomerPartyId, customer = Loc(x.CustEn, x.CustAr), x.c.SignatoryId, x.c.SignedByName, x.c.SignedDate, x.c.Notes,
            invoicedTotal = x.Invoiced, invoicedLitres = x.Litres,
            terms = terms.Where(t => t.ContractId == x.c.Id).Select(t => new { t.Id, templateId = t.SourceId, t.Category, text = t.TextAr, t.IsCustom, t.IsModified }),
        }).ToList();
    }

    // ------------------------------------------------------------------ refills → customer invoice (user button)

    /// <summary>
    /// Refills of villa / restaurant tanks with their invoicing state. premisesId or type (villa | restaurant); readyOnly = only
    /// delivered refills that are not invoiced yet ("ready to invoice").
    /// </summary>
    [HttpGet("refills")]
    public async Task<IActionResult> Refills([FromQuery] long? premisesId, [FromQuery] string? type, [FromQuery] bool readyOnly = false)
    {
        if (premisesId != null) await RequireSite(premisesId.Value);
        var rows = await Billing.RefillsAsync(premisesId, string.IsNullOrEmpty(type) ? null : type, readyOnly, BranchScope());
        var siteIds = rows.Select(r => r.PremisesId).Distinct().ToList();
        var sites = await Db.Premises.AsNoTracking().Where(p => siteIds.Contains(p.Id))
            .Select(p => new { p.Id, p.NameEn, p.NameAr, p.BuildingId, BCode = p.Building!.Code }).ToDictionaryAsync(p => p.Id);
        var prices = new Dictionary<long, decimal?>();
        foreach (var r in rows.Where(r => r.ReadyToInvoice))
        {
            var on = r.DeliveredAt != null ? DateOnly.FromDateTime(r.DeliveredAt.Value.ToLocalTime().DateTime) : r.RequestDate;
            prices[r.RefillRequestId] = (await Billing.ContractOnAsync(r.PremisesId, on))?.PricePerLitre;
        }
        return Ok(rows.Select(r => new
        {
            r.RefillRequestId, r.RequestNo, r.PremisesId, premisesType = r.PremisesType, premisesCode = r.PremisesCode,
            premises = sites.TryGetValue(r.PremisesId, out var s) ? Loc(s.NameEn, s.NameAr) : null, buildingCode = s?.BCode,
            r.TankId, r.TankNo, r.RequestDate, r.StatusCode, r.Stage, r.DeliveredAt, r.DeliveredLitres, r.CompletedAt,
            r.InvoiceId, r.InvoiceNo, r.InvoicedAmount, r.ReadyToInvoice,
            pricePerLitre = prices.GetValueOrDefault(r.RefillRequestId),
            estimatedAmount = prices.GetValueOrDefault(r.RefillRequestId) is decimal pr && r.DeliveredLitres is decimal l ? Math.Round(pr * l, 2) : (decimal?)null,
            missingContract = r.ReadyToInvoice && prices.GetValueOrDefault(r.RefillRequestId) == null,
        }));
    }

    /// <summary>Issue (and post) the customer invoice of one delivered villa / restaurant refill.</summary>
    [HttpPost("refills/{refillRequestId:long}/invoice")]
    [RequirePermission("invoices.manage")]
    public async Task<IActionResult> InvoiceRefill(long refillRequestId, [FromBody] RefillInvoiceInput? input)
    {
        await Billing.LinkVillaHostTanksAsync();
        var tankId = await Db.RefillRequests.Where(r => r.Id == refillRequestId).Select(r => (long?)r.TankId).FirstOrDefaultAsync()
                     ?? throw new NotFoundException("RefillRequest", refillRequestId);
        var siteId = await Db.PremisesTanks.Where(l => l.TankId == tankId).Select(l => (long?)l.PremisesId).FirstOrDefaultAsync();
        if (siteId != null) await RequireSite(siteId.Value);
        var inv = await Billing.InvoiceRefillAsync(refillRequestId, input?.IssueDate, input?.Notes);
        return Ok(new { inv.Id, inv.InvoiceNo, inv.Subtotal, inv.VatAmount, inv.TotalAmount });
    }

    // ------------------------------------------------------------------ helpers

    private IQueryable<Premises> Scoped(IQueryable<Premises> q) =>
        Me.IsAdmin || Me.BranchIds.Count == 0 ? q : q.Where(p => Me.BranchIds.Contains(p.BranchId));

    private IReadOnlyCollection<long>? BranchScope() => Me.IsAdmin || Me.BranchIds.Count == 0 ? null : Me.BranchIds.ToList();

    private async Task<Premises> RequireSite(long id)
    {
        var p = await Db.Premises.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Premises", id);
        EnsureBranchAccess(p.BranchId);
        return p;
    }

    private async Task<bool> HasActiveContract(long premisesId)
    {
        var active = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        return await Db.DeliveryContracts.AnyAsync(c => c.PremisesId == premisesId && c.StatusId == active);
    }

    private async Task<Dictionary<long, string>> OpenRefillsByTank(List<long> tankIds)
    {
        var done = new[] { await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed"), await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled") };
        var rows = await Db.RefillRequests.AsNoTracking().Where(r => tankIds.Contains(r.TankId) && !done.Contains(r.StatusId))
            .Select(r => new { r.TankId, r.RequestNo }).ToListAsync();
        return rows.GroupBy(r => r.TankId).ToDictionary(g => g.Key, g => g.First().RequestNo);
    }

    /// <summary>Existing party (PartyId) or a new one (Party). The party gets its own ledger account later (EnsureAccountAsync).</summary>
    private async Task<long> ResolveOwner(OwnerRef? o, long branchId, string what)
    {
        if (o?.PartyId != null)
        {
            if (!await Db.Parties.AnyAsync(p => p.Id == o.PartyId)) throw new NotFoundException("Party", o.PartyId);
            return o.PartyId.Value;
        }
        if (o?.Party != null)
        {
            // a known Emirates ID / trade licence joins the existing party (no duplicates)
            var eid = o.Party.EmiratesId?.Trim(); var tl = o.Party.TradeLicenseNo?.Trim();
            var existing = await Db.Parties.Where(p => (eid != null && eid != "" && p.EmiratesId == eid) || (tl != null && tl != "" && p.TradeLicenseNo == tl))
                .Select(p => (long?)p.Id).FirstOrDefaultAsync();
            if (existing != null) return existing.Value;
            return (await PartiesController.CreateParty(Db, Lookups, Numbers, branchId, o.Party)).Id;
        }
        throw new AppException($"The {what} is required (choose a party or enter a new one).");
    }
}
