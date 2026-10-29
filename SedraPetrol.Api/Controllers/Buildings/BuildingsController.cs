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
        string? TaxNumber, long? CityId, string? PoBox, string? Address, string? Phone, string? Mobile, string? Email, string? BankName, string? Iban,
        List<string>? Phones = null, List<string>? Emails = null);

    public record TankInput(string? TankNo, long? GasTypeId, string? GasType, decimal CapacityLitres, decimal? CurrentLevelLitres, string? SerialNo,
        string? Manufacturer, DateOnly? InstallDate, DateOnly? LastInspectionDate, DateOnly? NextInspectionDate, string? Status);

    public record BuildingInput(long? BranchId, string? NameEn, string? NameAr, long? BuildingTypeId, string? BuildingType, long? StatusId, string? Status,
        long? CityId, string? Zone, string? Street, string? PoBox, string? Address, decimal? Latitude, decimal? Longitude, int? TotalFloors, string? Notes,
        List<OwnerInput>? Owners, List<TankInput>? Tanks, List<UnitsController.UnitInput>? Units);

    // ------------------------------------------------------------------ list & KPIs

    /// <summary>Buildings table with occupancy, meters, tank level and open requests.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BuildingListItem>>> List([FromQuery] PageQuery q, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] string? type, [FromQuery] long? cityId, [FromQuery] string? kind, CancellationToken ct)
    {
        await LoadLookups();
        var query = Scoped(Db.Buildings.AsNoTracking());
        if (branchId != null) query = query.Where(b => b.BranchId == branchId);
        if (cityId != null) query = query.Where(b => b.CityId == cityId);
        if (!string.IsNullOrEmpty(status)) { var sid = await Lookups.IdOrNullAsync(LookupTypes.BuildingStatus, status); query = query.Where(b => b.StatusId == sid); }
        if (!string.IsNullOrEmpty(type)) { var tid = await Lookups.IdOrNullAsync(LookupTypes.BuildingType, type); query = query.Where(b => b.BuildingTypeId == tid); }
        // Restaurants & Villas (approved 24 Oct 2026): optional filter; without "kind" the list is exactly as before.
        // villa = villa buildings, restaurant = buildings that have restaurants, building = everything except villas.
        if (kind == "villa") query = query.Where(b => Db.Premises.Any(p => p.BuildingId == b.Id && p.PremisesType == PremisesTypes.Villa));
        else if (kind == "restaurant") query = query.Where(b => Db.Premises.Any(p => p.BuildingId == b.Id && p.PremisesType == PremisesTypes.Restaurant));
        else if (kind == "building") query = query.Where(b => !Db.Premises.Any(p => p.BuildingId == b.Id && p.PremisesType == PremisesTypes.Villa));
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
            b.Zone, b.Street, b.PoBox, b.Address, b.Latitude, b.Longitude, b.TotalFloors, b.Notes, b.IsActive, b.CreatedAt, b.UpdatedAt,
            emirate = b.City == null ? null : await Db.Emirates.AsNoTracking().Where(e => e.Id == b.City.EmirateId).Select(e => e.NameAr ?? e.NameEn).FirstOrDefaultAsync(ct),
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

    // ------------------------------------------------------------------ tank insights (refills vs tenant meter usage)

    /// <summary>
    /// Does the building need a refill?
    /// <list type="bullet">
    /// <item><b>Gas now</b> = last tank record (level reading or refill, measured) − tenant meter usage after it.
    /// Meter usage is split by days (a reading covers the days since that meter's previous reading, so only the part after the
    /// record counts); days after the last meter reading are estimated with the daily rate.</item>
    /// <item><b>Daily rate</b> = the last complete refill period of the tanks (level after the previous refill − level before the next one,
    /// both measured by the driver); falls back to the last 30 days of meter readings.</item>
    /// <item><b>Refill periods</b>: for each period, what left the tank vs what the meters recorded for the same days (loss check).</item>
    /// </list>
    /// Meters are in m³, tanks in litres: converted with the tanks' gas type (litre_to_m3).
    /// </summary>
    [HttpGet("{id:long}/tank-insights")]
    public async Task<IActionResult> TankInsights(long id, CancellationToken ct = default)
    {
        var b = await Scoped(Db.Buildings.AsNoTracking()).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Building", id);
        var now = DateTimeOffset.UtcNow;

        var tanks = await Db.Tanks.AsNoTracking().Where(t => t.BuildingId == id && t.IsActive).Include(t => t.GasType).OrderBy(t => t.TankNo).ToListAsync(ct);
        var tankIds = tanks.Select(t => t.Id).ToList();
        var capacity = tanks.Sum(t => t.CapacityLitres);
        decimal ShareOf(long tankId) => capacity > 0 ? (tanks.FirstOrDefault(t => t.Id == tankId)?.CapacityLitres ?? 0) / capacity : 0m;
        // m³ → litres with the tanks' gas type (capacity-weighted when tanks hold different gases)
        var factor = capacity > 0 ? tanks.Sum(t => (t.GasType?.LitreToM3 ?? 0) * t.CapacityLitres) / capacity : 0m;
        decimal ToLitres(decimal m3) => factor > 0 ? m3 / factor : 0m;

        var refills = await Db.TankRefills.AsNoTracking().Where(r => tankIds.Contains(r.TankId)).OrderBy(r => r.RefillDatetime)
            .Select(r => new { r.TankId, r.RefillDatetime, r.LevelBeforeLitres, r.LevelAfterLitres,
                               Qty = r.DeliveredQtyLitres ?? ((r.LevelAfterLitres ?? 0) - (r.LevelBeforeLitres ?? 0)) })
            .ToListAsync(ct);
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var openRequests = await Db.RefillRequests.AsNoTracking()
            .Where(r => tankIds.Contains(r.TankId) && r.CompletedAt == null && r.StatusId != cancelled)
            .Select(r => new { r.TankId, r.RequestNo, r.RequestedQtyLitres }).ToListAsync(ct);
        var levelRows = await Db.TankLevelHistories.AsNoTracking().Where(h => tankIds.Contains(h.TankId))
            .OrderBy(h => h.TankId).ThenBy(h => h.EventDate).ThenBy(h => h.Id)
            .Select(h => new { h.Id, h.TankId, h.EventDate, h.LevelLitres }).ToListAsync(ct);

        // ── tenant meter readings of this building (old readings without a unit → by meter assignment) ──
        var readings = Db.MeterReadings.AsNoTracking().Where(r =>
            (r.UnitId != null && r.Unit!.BuildingId == id) ||
            (r.UnitId == null && Db.MeterAssignments.Any(a => a.MeterId == r.MeterId && a.Unit!.BuildingId == id)));
        var monthlyUse = await readings.Where(r => r.Consumption != null && r.Consumption > 0)
            .GroupBy(r => new { r.ReadingDate.Year, r.ReadingDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, M3 = g.Sum(r => r.Consumption ?? 0) }).ToListAsync(ct);
        var firstReading = await readings.Where(r => r.Consumption != null && r.Consumption > 0).MinAsync(r => (DateTimeOffset?)r.ReadingDate, ct);
        var horizon = now.AddDays(-550);
        var rd = await readings.Where(r => r.ReadingDate >= horizon).OrderBy(r => r.MeterId).ThenBy(r => r.ReadingDate).ThenBy(r => r.Id)
            .Select(r => new { r.MeterId, r.ReadingDate, r.Consumption }).ToListAsync(ct);

        // each reading covers (previous reading of the same meter, this reading]
        var intervals = new List<(DateTimeOffset From, DateTimeOffset To, decimal M3)>();
        foreach (var mg in rd.GroupBy(r => r.MeterId))
        {
            DateTimeOffset? prevAt = null;
            foreach (var r in mg)
            {
                if (prevAt != null && (r.Consumption ?? 0) > 0 && r.ReadingDate > prevAt.Value) intervals.Add((prevAt.Value, r.ReadingDate, r.Consumption!.Value));
                prevAt = r.ReadingDate;
            }
        }
        DateTimeOffset? metersReadUntil = rd.Count > 0 ? rd.Max(r => r.ReadingDate) : null;
        // meter usage (m³) between two moments, each reading split by days
        decimal MeterM3(DateTimeOffset from, DateTimeOffset to) => intervals.Sum(iv =>
        {
            var s0 = iv.From > from ? iv.From : from;
            var e0 = iv.To < to ? iv.To : to;
            return e0 <= s0 ? 0m : iv.M3 * (decimal)((e0 - s0).TotalSeconds / (iv.To - iv.From).TotalSeconds);
        });

        // ── daily rate from the meters: last 30 days that have readings ──
        decimal? meterRateM3 = null, meterRateL = null;
        if (metersReadUntil != null && intervals.Count > 0 && factor > 0)
        {
            var winEnd = metersReadUntil.Value;
            var winStart = winEnd.AddDays(-30);
            var earliest = intervals.Min(iv => iv.From);
            if (winStart < earliest) winStart = earliest;
            var winDays = (decimal)(winEnd - winStart).TotalDays;
            if (winDays >= 7) { meterRateM3 = MeterM3(winStart, winEnd) / winDays; meterRateL = ToLitres(meterRateM3.Value); }
        }

        // ── refill periods (per tank): measured usage between two refills vs the meters for the same days ──
        var periods = new List<(long TankId, DateTimeOffset From, DateTimeOffset To, decimal Days, decimal TankUsed, decimal MeterUsed, bool MetersComplete)>();
        foreach (var tg in refills.GroupBy(r => r.TankId))
        {
            var list = tg.OrderBy(r => r.RefillDatetime).ToList();
            for (var i = 1; i < list.Count; i++)
            {
                var p = list[i - 1];
                var c = list[i];
                if (p.LevelAfterLitres == null || c.LevelBeforeLitres == null) continue;
                var used = p.LevelAfterLitres.Value - c.LevelBeforeLitres.Value;
                var days = (decimal)(c.RefillDatetime - p.RefillDatetime).TotalDays;
                if (days < 1 || used < 0) continue;
                periods.Add((tg.Key, p.RefillDatetime, c.RefillDatetime, days, used,
                    ToLitres(MeterM3(p.RefillDatetime, c.RefillDatetime)) * ShareOf(tg.Key),
                    metersReadUntil != null && metersReadUntil.Value >= c.RefillDatetime));
            }
        }
        // building rate from each tank's latest period (last 6 months), scaled when only some tanks have one
        decimal? tankRateL = null;
        var latestByTank = periods.GroupBy(x => x.TankId).Select(g => g.OrderByDescending(x => x.To).First()).Where(x => x.To >= now.AddDays(-180)).ToList();
        if (latestByTank.Count > 0)
        {
            var covered = latestByTank.Sum(x => ShareOf(x.TankId));
            if (covered > 0) tankRateL = latestByTank.Sum(x => x.TankUsed / x.Days) / covered;
        }
        var rate = tankRateL ?? meterRateL;
        var rateSource = tankRateL != null ? "refill_period" : meterRateL != null ? "meters" : null;

        // ── gas now, per tank: last record − meter usage after it (split by days) − estimate after the last meter reading ──
        var tankNow = new List<(long Id, decimal Recorded, DateTimeOffset? RecordedAt, decimal Measured, decimal Estimated, decimal Now)>();
        foreach (var t in tanks)
        {
            var anchor = levelRows.Where(h => h.TankId == t.Id).OrderByDescending(h => h.EventDate).ThenByDescending(h => h.Id).FirstOrDefault();
            var recorded = anchor?.LevelLitres ?? (t.CurrentLevelLitres ?? 0);
            DateTimeOffset? at = anchor?.EventDate;
            decimal measured = 0, estimated = 0;
            if (at != null)
            {
                var measuredEnd = metersReadUntil != null && metersReadUntil.Value > at.Value ? metersReadUntil.Value : at.Value;
                if (measuredEnd > now) measuredEnd = now;
                measured = ToLitres(MeterM3(at.Value, measuredEnd)) * ShareOf(t.Id);
                var estDays = (decimal)(now - measuredEnd).TotalDays;
                if (rate != null && estDays > 0) estimated = rate.Value * ShareOf(t.Id) * estDays;
            }
            tankNow.Add((t.Id, recorded, at, measured, estimated, Math.Max(0, recorded - measured - estimated)));
        }
        var recordedTotal = tankNow.Sum(x => x.Recorded);
        var level = tankNow.Sum(x => x.Now);
        var pct = capacity > 0 ? level * 100 / capacity : 0m;
        var lowLine = capacity * TankRules.LowLevelPercent / 100m;
        decimal? daysToLow = rate is > 0 ? Math.Max(0, (level - lowLine) / rate.Value) : null;
        decimal? daysToEmpty = rate is > 0 ? level / rate.Value : null;
        var status = tanks.Count == 0 ? "no_tanks"
            : pct < TankRules.LowLevelPercent ? "refill_now"
            : daysToLow == null ? "unknown"
            : daysToLow <= 7 ? "refill_soon"
            : "ok";

        // ── what physically left the tanks between level records (refills raise the level and are skipped) ──
        var drops = new List<(DateTimeOffset At, decimal Litres)>();
        foreach (var hg in levelRows.GroupBy(h => h.TankId))
        {
            decimal? prevLevel = null;
            foreach (var h in hg)
            {
                if (prevLevel != null && h.LevelLitres < prevLevel.Value) drops.Add((h.EventDate, prevLevel.Value - h.LevelLitres));
                prevLevel = h.LevelLitres;
            }
        }

        // ── month by month since the building started (first of: creation, first refill, first reading) ──
        var firstRefill = refills.Count > 0 ? refills.Min(r => r.RefillDatetime) : (DateTimeOffset?)null;
        var origin = new[] { (DateTimeOffset?)b.CreatedAt, firstRefill, firstReading }.Where(x => x != null && x.Value.Year > 2000).Min() ?? now;
        var months = new List<object>();
        var cursor = new DateTime(origin.Year, origin.Month, 1);
        var lastMonth = new DateTime(now.Year, now.Month, 1);
        while (cursor <= lastMonth && months.Count < 240)
        {
            var mr = refills.Where(r => r.RefillDatetime.Year == cursor.Year && r.RefillDatetime.Month == cursor.Month).ToList();
            var m3 = monthlyUse.Where(x => x.Year == cursor.Year && x.Month == cursor.Month).Sum(x => x.M3);
            var tankL = drops.Where(x => x.At.Year == cursor.Year && x.At.Month == cursor.Month).Sum(x => x.Litres);
            var meterL = ToLitres(m3);
            months.Add(new
            {
                month = cursor.ToString("yyyy-MM"), refills = mr.Count, deliveredLitres = Math.Round(mr.Sum(r => r.Qty), 1),
                consumptionM3 = Math.Round(m3, 2), consumptionLitres = Math.Round(meterL, 1),
                tankDropLitres = Math.Round(tankL, 1),
                differenceLitres = tankL > 0 ? Math.Round(tankL - meterL, 1) : (decimal?)null,
                differencePct = tankL > 0 ? Math.Round((tankL - meterL) * 100 / tankL, 1) : (decimal?)null,
            });
            cursor = cursor.AddMonths(1);
        }
        var totalM3 = monthlyUse.Sum(x => x.M3);
        var totalDrop = drops.Sum(x => x.Litres);
        decimal? R1(decimal? v) => v == null ? null : Math.Round(v.Value, 1);

        return Ok(new
        {
            buildingId = id, since = origin, litreToM3 = Math.Round(factor, 4), lowLevelPercent = TankRules.LowLevelPercent,
            capacityLitres = capacity,
            // measured at the last tank record → estimated now
            recordedLevelLitres = Math.Round(recordedTotal, 1),
            recordedAt = tankNow.Where(x => x.RecordedAt != null).Select(x => x.RecordedAt).Min(),
            usedSinceRecordMeasuredLitres = Math.Round(tankNow.Sum(x => x.Measured), 1),
            usedSinceRecordEstimatedLitres = Math.Round(tankNow.Sum(x => x.Estimated), 1),
            levelLitres = Math.Round(level, 1), levelPct = Math.Round(pct, 1), refillToFullLitres = Math.Round(capacity - level, 1),
            metersReadUntil,
            // daily rate
            dailyUsageLitres = R1(rate), rateSource,
            refillPeriodRateLitres = R1(tankRateL), meterRateLitres = R1(meterRateL),
            meterRateM3 = meterRateM3 == null ? (decimal?)null : Math.Round(meterRateM3.Value, 2),
            daysToLow = R1(daysToLow), daysToEmpty = R1(daysToEmpty),
            expectedLowDate = daysToLow == null ? (DateTimeOffset?)null : now.AddDays((double)daysToLow.Value),
            status,
            totals = new
            {
                refills = refills.Count, deliveredLitres = Math.Round(refills.Sum(r => r.Qty), 1),
                consumptionM3 = Math.Round(totalM3, 2), consumptionLitres = Math.Round(ToLitres(totalM3), 1),
                months = months.Count, avgRefillsPerMonth = months.Count > 0 ? Math.Round((decimal)refills.Count / months.Count, 2) : 0,
                lastRefill = refills.Count > 0 ? refills.Max(r => r.RefillDatetime) : (DateTimeOffset?)null,
                tankDropLitres = Math.Round(totalDrop, 1),
                differenceLitres = totalDrop > 0 ? Math.Round(totalDrop - ToLitres(totalM3), 1) : (decimal?)null,
                differencePct = totalDrop > 0 ? Math.Round((totalDrop - ToLitres(totalM3)) * 100 / totalDrop, 1) : (decimal?)null,
            },
            refillPeriods = periods.OrderByDescending(x => x.To).Take(12).Select(x => new
            {
                tankId = x.TankId, tankNo = tanks.First(t => t.Id == x.TankId).TankNo, from = x.From, to = x.To, days = Math.Round(x.Days, 1),
                tankUsedLitres = Math.Round(x.TankUsed, 1), dailyLitres = Math.Round(x.TankUsed / x.Days, 1),
                meterLitres = x.MetersComplete ? Math.Round(x.MeterUsed, 1) : (decimal?)null,
                differenceLitres = x.MetersComplete ? Math.Round(x.TankUsed - x.MeterUsed, 1) : (decimal?)null,
                differencePct = x.MetersComplete && x.TankUsed > 0 ? Math.Round((x.TankUsed - x.MeterUsed) * 100 / x.TankUsed, 1) : (decimal?)null,
                metersComplete = x.MetersComplete,
            }),
            tanks = tanks.Select(t =>
            {
                var n = tankNow.First(x => x.Id == t.Id);
                return new
                {
                    t.Id, t.TankNo, t.CapacityLitres,
                    recordedLevelLitres = Math.Round(n.Recorded, 1), recordedAt = n.RecordedAt,
                    levelLitres = Math.Round(n.Now, 1), levelPct = t.CapacityLitres > 0 ? Math.Round(n.Now * 100 / t.CapacityLitres, 1) : 0,
                    refills = refills.Count(r => r.TankId == t.Id),
                    deliveredLitres = Math.Round(refills.Where(r => r.TankId == t.Id).Sum(r => r.Qty), 1),
                    lastRefill = refills.Where(r => r.TankId == t.Id).Select(r => (DateTimeOffset?)r.RefillDatetime).Max(),
                    openRequest = openRequests.Where(r => r.TankId == t.Id).Select(r => r.RequestNo).FirstOrDefault(),
                };
            }),
            openRequests = openRequests.Select(r => new { r.TankId, r.RequestNo, r.RequestedQtyLitres }),
            months,
        });
    }

    // ------------------------------------------------------------------ tanks (added / edited from the building page)

    public record BuildingTankInput(string? TankNo, long? GasTypeId, string? GasType, decimal CapacityLitres, decimal? CurrentLevelLitres,
        string? SerialNo, string? Manufacturer, DateOnly? InstallDate, DateOnly? LastInspectionDate, DateOnly? NextInspectionDate, string? Status);

    /// <summary>Active gas types for the tank form (needs only buildings.view).</summary>
    [HttpGet("gas-types")]
    public async Task<IActionResult> GasTypeOptions()
    {
        var rows = await Db.GasTypes.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Code).Select(g => new { g.Id, g.Code, g.NameEn, g.NameAr }).ToListAsync();
        return Ok(rows.Select(g => new { g.Id, g.Code, name = Loc(g.NameEn, g.NameAr) ?? g.Code }));
    }

    /// <summary>Add a gas tank to the building (buildings.manage — same as adding units).</summary>
    [HttpPost("{id:long}/tanks")]
    public async Task<IActionResult> AddTank(long id, [FromBody] BuildingTankInput input)
    {
        var b = await Scoped(Db.Buildings).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Building", id);
        EnsureBranchAccess(b.BranchId);
        var t = await TanksController.CreateTank(Db, Lookups, b, input.TankNo, input.GasTypeId, input.GasType, input.CapacityLitres, input.CurrentLevelLitres,
            input.SerialNo, input.Manufacturer, input.InstallDate, input.LastInspectionDate, input.NextInspectionDate, input.Status);
        Activity.Log("tank", t.Id, "create", $"Tank {t.TankNo} added", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { t.Id, t.TankNo });
    }

    /// <summary>
    /// People who can take a tank reading: active system users (staff) and drivers — buildings.view.
    /// (There is no employee register yet; switch to it when it exists.)
    /// </summary>
    [HttpGet("staff-options")]
    public async Task<IActionResult> StaffOptions()
    {
        // employees from the employee register when it has any, otherwise the system users
        var staff = await Db.Employees.AsNoTracking().Where(e => e.IsActive)
            .Select(e => new { e.Id, e.Party!.NameAr, e.Party!.NameEn, Email = e.Party!.Email ?? e.EmployeeNo, e.Party!.Phone }).ToListAsync();
        var users = staff.Count > 0 ? staff : await Db.AppUsers.AsNoTracking().Where(u => u.IsActive)
            .Select(u => new { u.Id, u.NameAr, u.NameEn, Email = (string?)u.Email, u.Phone }).ToListAsync();
        var drivers = await Db.Drivers.AsNoTracking().Where(d => d.IsActive)
            .Select(d => new { d.Id, d.Party!.NameAr, d.Party!.NameEn, d.Party!.Phone }).ToListAsync();
        return Ok(new
        {
            employees = users.Select(u => new { u.Id, name = Loc(u.NameEn, u.NameAr) ?? u.Email, u.Phone }).OrderBy(x => x.name),
            drivers = drivers.Select(d => new { d.Id, name = Loc(d.NameEn, d.NameAr) ?? $"Driver {d.Id}", d.Phone }).OrderBy(x => x.name),
        });
    }

    /// <summary>Level history of a building tank (newest first) — needs only buildings.view.</summary>
    [HttpGet("{id:long}/tanks/{tankId:long}/levels")]
    public async Task<IActionResult> TankLevels(long id, long tankId, [FromQuery] int take = 50)
    {
        await LoadLookups();
        var t = await Db.Tanks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tankId && x.BuildingId == id) ?? throw new NotFoundException("Tank", tankId);
        EnsureBranchAccess(t.BranchId);
        var rows = await Db.TankLevelHistories.AsNoTracking().Where(h => h.TankId == tankId)
            .OrderByDescending(h => h.EventDate).ThenByDescending(h => h.Id).Take(Math.Clamp(take, 1, 500))
            .Select(h => new
            {
                h.Id, h.EventDate, h.EventTypeId, h.LevelLitres, h.Notes, h.ReadBy, h.TankRefillId,
                EnteredBy = Db.AppUsers.Where(u => u.Id == h.CreatedBy).Select(u => u.NameAr ?? u.NameEn ?? u.Email).FirstOrDefault(),
            }).ToListAsync();
        return Ok(rows.Select(h => new
        {
            h.Id, h.EventDate, eventType = Code(h.EventTypeId), eventTypeLabel = Label(h.EventTypeId), h.LevelLitres,
            levelPct = t.CapacityLitres > 0 ? Math.Round(h.LevelLitres / t.CapacityLitres * 100, 1) : 0,
            h.Notes, readBy = h.ReadBy, enteredBy = h.EnteredBy, h.TankRefillId,
        }));
    }

    /// <summary>Record a level reading of a building tank (date &amp; time, who read it, remarks) — buildings.manage.</summary>
    [HttpPost("{id:long}/tanks/{tankId:long}/levels")]
    public async Task<IActionResult> AddTankLevel(long id, long tankId, [FromBody] TanksController.LevelInput input)
    {
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == tankId && x.BuildingId == id) ?? throw new NotFoundException("Tank", tankId);
        EnsureBranchAccess(t.BranchId);
        if (input.EventType == "refill") throw new AppException("Refills are recorded through the refill workflow.");
        await TanksController.RecordLevel(Db, Lookups, t, input.LevelLitres, input.EventType ?? "manual_reading", input.EventDate ?? DateTimeOffset.UtcNow,
            input.Notes, null, input.ReadBy);
        Activity.Log("tank", t.Id, "level", $"Tank {t.TankNo} level {input.LevelLitres:0.##} L", id, t.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { t.Id, t.CurrentLevelLitres });
    }

    /// <summary>Edit a tank of the building. The level itself changes through readings / refills.</summary>
    [HttpPut("{id:long}/tanks/{tankId:long}")]
    public async Task<IActionResult> UpdateTank(long id, long tankId, [FromBody] BuildingTankInput input)
    {
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == tankId && x.BuildingId == id) ?? throw new NotFoundException("Tank", tankId);
        EnsureBranchAccess(t.BranchId);
        if (input.CapacityLitres <= 0) throw new AppException("Capacity must be greater than zero.");
        if ((t.CurrentLevelLitres ?? 0) > input.CapacityLitres) throw new AppException("Capacity cannot be less than the current level.");
        if (!string.IsNullOrWhiteSpace(input.TankNo))
        {
            var no = input.TankNo.Trim();
            if (no != t.TankNo && await Db.Tanks.AnyAsync(x => x.BuildingId == id && x.TankNo == no)) throw new BusinessRuleException($"Tank {no} already exists in this building.");
            t.TankNo = no;
        }
        t.GasTypeId = await TanksController.ResolveGasType(Db, input.GasTypeId, input.GasType) ?? t.GasTypeId;
        t.CapacityLitres = input.CapacityLitres;
        t.SerialNo = input.SerialNo; t.Manufacturer = input.Manufacturer; t.InstallDate = input.InstallDate;
        t.LastInspectionDate = input.LastInspectionDate; t.NextInspectionDate = input.NextInspectionDate;
        if (input.Status != null) t.StatusId = await Lk(LookupTypes.TankStatus, null, input.Status, field: "status");
        Activity.Log("tank", t.Id, "update", $"Tank {t.TankNo} updated", id, t.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
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
        if (await Db.AmcContracts.AnyAsync(a => a.IsActive && a.BuildingId == id))
            throw new BusinessRuleException("The building has an active maintenance (AMC) contract. Terminate it first.");
        var openInvoice = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "pending");
        var partial = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "partial");
        var overdue = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "overdue");
        if (await Db.Invoices.AnyAsync(i => i.BuildingId == id && (i.StatusId == openInvoice || i.StatusId == partial || i.StatusId == overdue)))
            throw new BusinessRuleException("The building has unpaid invoices. Collect or cancel them first.");
        var openSr = await OpenSrStatusIds();
        if (await Db.ServiceRequests.AnyAsync(s => s.BuildingId == id && s.StatusId != null && openSr.Contains(s.StatusId.Value)))
            throw new BusinessRuleException("The building has open service requests. Close them first.");
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
            partyId = (await PartiesController.CreateParty(Db, Lookups, Numbers, branchId, o.Party)).Id;
        }
        else throw new AppException("Each owner needs a partyId or a new party.");

        var owner = new BuildingOwner { BuildingId = buildingId, PartyId = partyId, OwnershipPercentage = o.OwnershipPercentage, IsPrimary = o.IsPrimary, FromDate = o.FromDate, ToDate = o.ToDate };
        Db.BuildingOwners.Add(owner);
        await Db.SaveChangesAsync();
        await HttpContext.RequestServices.GetRequiredService<PartyService>().EnsureAccountAsync(partyId);
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
