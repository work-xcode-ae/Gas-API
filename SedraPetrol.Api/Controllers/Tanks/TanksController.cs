using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Building gas tanks, levels and level history.</summary>
[Route("api/tanks")]
[PermissionModule("tanks")]
public class TanksController : ApiControllerBase
{
    public record TankInput(long? BuildingId, string? TankNo, long? GasTypeId, string? GasType, decimal CapacityLitres, decimal? CurrentLevelLitres,
        string? SerialNo, string? Manufacturer, DateOnly? InstallDate, DateOnly? LastInspectionDate, DateOnly? NextInspectionDate, string? Status);

    public record LevelInput(decimal LevelLitres, string? EventType, DateTimeOffset? EventDate, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] bool lowOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.Tanks.AsNoTracking().Where(t => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(t.BranchId));
        if (buildingId != null) query = query.Where(t => t.BuildingId == buildingId);
        if (branchId != null) query = query.Where(t => t.BranchId == branchId);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.TankStatus, status); query = query.Where(t => t.StatusId == s); }
        if (lowOnly) query = query.Where(t => t.CapacityLitres > 0 && (t.CurrentLevelLitres ?? 0) * 100 / t.CapacityLitres < TankRules.LowLevelPercent);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(t => t.TankNo.Contains(q.Search) || t.Building!.Code.Contains(q.Search) || (t.Building.NameAr ?? "").Contains(q.Search));
        var page = await query.OrderBy(t => t.BuildingId).ThenBy(t => t.TankNo)
            .Select(t => new
            {
                Tank = t, t.GasType, BuildingCode = t.Building!.Code, t.Building.NameEn, t.Building.NameAr,
                LastRefill = Db.TankRefills.Where(r => r.TankId == t.Id).Max(r => (DateTimeOffset?)r.RefillDatetime),
                OpenRequest = Db.RefillRequests.Where(r => r.TankId == t.Id && r.CompletedAt == null).OrderByDescending(r => r.Id).Select(r => r.RequestNo).FirstOrDefault(),
            }).ToPagedAsync(q, ct);
        var items = page.Items.Select(x =>
        {
            x.Tank.GasType = x.GasType;
            return (object)new { tank = ToDto(x.Tank, Code, Label, Loc), buildingCode = x.BuildingCode, building = Loc(x.NameEn, x.NameAr), lastRefill = x.LastRefill, openRequest = x.OpenRequest };
        }).ToList();
        return Ok(new PagedResult<object>(items, page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var t = await Db.Tanks.AsNoTracking().Include(x => x.GasType).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tank", id);
        return Ok(ToDto(t, Code, Label, Loc));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TankInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId ?? 0);
        EnsureBranchAccess(b.BranchId);
        var t = await CreateTank(Db, Lookups, b, input.TankNo, input.GasTypeId, input.GasType, input.CapacityLitres, input.CurrentLevelLitres,
            input.SerialNo, input.Manufacturer, input.InstallDate, input.LastInspectionDate, input.NextInspectionDate, input.Status);
        Activity.Log("tank", t.Id, "create", $"Tank {t.TankNo} added", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { t.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TankInput input)
    {
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tank", id);
        EnsureBranchAccess(t.BranchId);
        if (input.CapacityLitres <= 0) throw new AppException("Capacity must be greater than zero.");
        if ((t.CurrentLevelLitres ?? 0) > input.CapacityLitres) throw new AppException("Capacity cannot be less than the current level.");
        if (!string.IsNullOrWhiteSpace(input.TankNo)) t.TankNo = input.TankNo.Trim();
        t.GasTypeId = await ResolveGasType(Db, input.GasTypeId, input.GasType) ?? t.GasTypeId;
        t.CapacityLitres = input.CapacityLitres;
        t.SerialNo = input.SerialNo; t.Manufacturer = input.Manufacturer; t.InstallDate = input.InstallDate;
        t.LastInspectionDate = input.LastInspectionDate; t.NextInspectionDate = input.NextInspectionDate;
        if (input.Status != null) t.StatusId = await Lk(LookupTypes.TankStatus, null, input.Status, field: "status");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tank", id);
        if (await Db.RefillRequests.AnyAsync(r => r.TankId == id && r.CompletedAt == null && r.StatusId != null))
        {
            var cancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
            if (await Db.RefillRequests.AnyAsync(r => r.TankId == id && r.CompletedAt == null && r.StatusId != cancelled))
                throw new BusinessRuleException("The tank has an open refill request.");
        }
        Db.Tanks.Remove(t);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Level history (refills, manual readings, sensor data, adjustments).</summary>
    [HttpGet("{id:long}/levels")]
    public async Task<IActionResult> Levels(long id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] PageQuery q)
    {
        await LoadLookups();
        var t = await Db.Tanks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tank", id);
        var query = Db.TankLevelHistories.AsNoTracking().Where(h => h.TankId == id);
        if (from != null) query = query.Where(h => h.EventDate >= from);
        if (to != null) query = query.Where(h => h.EventDate <= to);
        var page = await query.OrderByDescending(h => h.EventDate).ToPagedAsync(q);
        return Ok(new PagedResult<object>(page.Items.Select(h => (object)new
        {
            h.Id, h.EventDate, eventType = Code(h.EventTypeId), eventTypeLabel = Label(h.EventTypeId), h.LevelLitres,
            levelPct = t.CapacityLitres > 0 ? Math.Round(h.LevelLitres / t.CapacityLitres * 100, 1) : 0, h.TankRefillId, h.Notes
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    /// <summary>Record a manual / sensor reading or an adjustment. Updates the tank's current level.</summary>
    [HttpPost("{id:long}/levels")]
    public async Task<IActionResult> AddLevel(long id, [FromBody] LevelInput input)
    {
        var t = await Db.Tanks.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tank", id);
        EnsureBranchAccess(t.BranchId);
        if (input.EventType == "refill") throw new AppException("Refills are recorded through the refill workflow.");
        await RecordLevel(Db, Lookups, t, input.LevelLitres, input.EventType ?? "manual_reading", input.EventDate ?? DateTimeOffset.UtcNow, input.Notes, null);
        await Db.SaveChangesAsync();
        return Ok(new { t.Id, t.CurrentLevelLitres });
    }

    // ------------------------------------------------------------------ shared helpers

    internal static async Task<long?> ResolveGasType(AppDbContext db, long? id, string? code)
    {
        if (id != null) return await db.GasTypes.AnyAsync(g => g.Id == id) ? id : throw new AppException($"Gas type {id} does not exist.");
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await db.GasTypes.Where(g => g.Code == code).Select(g => (long?)g.Id).FirstOrDefaultAsync()
               ?? throw new AppException($"Gas type '{code}' does not exist.");
    }

    internal static async Task<Tank> CreateTank(AppDbContext db, LookupService lookups, Building b, string? tankNo, long? gasTypeId, string? gasType,
        decimal capacity, decimal? level, string? serial, string? manufacturer, DateOnly? installDate, DateOnly? lastInspection, DateOnly? nextInspection, string? status)
    {
        if (capacity <= 0) throw new AppException("Tank capacity must be greater than zero.");
        if ((level ?? 0) < 0 || (level ?? 0) > capacity) throw new AppException("Tank level must be between 0 and the capacity.");
        var count = await db.Tanks.CountAsync(t => t.BuildingId == b.Id) + db.Tanks.Local.Count(t => t.BuildingId == b.Id && t.Id == 0);
        var t = new Tank
        {
            BranchId = b.BranchId, BuildingId = b.Id,
            TankNo = string.IsNullOrWhiteSpace(tankNo) ? $"T{count + 1}" : tankNo.Trim(),
            GasTypeId = await ResolveGasType(db, gasTypeId, gasType ?? "LPG") ?? throw new AppException("Gas type is required."),
            CapacityLitres = capacity, CurrentLevelLitres = level ?? 0, SerialNo = serial, Manufacturer = manufacturer,
            InstallDate = installDate, LastInspectionDate = lastInspection, NextInspectionDate = nextInspection, IsActive = true,
            StatusId = await lookups.ResolveAsync(LookupTypes.TankStatus, null, status ?? ((level ?? 0) * 100 / capacity < TankRules.LowLevelPercent ? "low" : "ok"), field: "status"),
        };
        if (await db.Tanks.AnyAsync(x => x.BuildingId == b.Id && x.TankNo == t.TankNo)) throw new BusinessRuleException($"Tank {t.TankNo} already exists in this building.");
        db.Tanks.Add(t);
        await db.SaveChangesAsync();
        if (level != null)
            db.TankLevelHistories.Add(new TankLevelHistory { TankId = t.Id, EventDate = DateTimeOffset.UtcNow, LevelLitres = level.Value, Notes = "Initial level",
                EventTypeId = await lookups.IdOrNullAsync(LookupTypes.TankEventType, "manual_reading") });
        return t;
    }

    /// <summary>Adds a tank_level_history row and updates tank.current_level_litres + status (capacity is enforced here).</summary>
    internal static async Task RecordLevel(AppDbContext db, LookupService lookups, Tank t, decimal level, string eventType, DateTimeOffset when, string? notes, long? tankRefillId)
    {
        if (level < 0) throw new AppException("Level cannot be negative.");
        if (level > t.CapacityLitres) throw new BusinessRuleException($"Level {level} L exceeds the tank capacity {t.CapacityLitres} L.");
        db.TankLevelHistories.Add(new TankLevelHistory
        {
            TankId = t.Id, EventDate = when, LevelLitres = level, Notes = notes, TankRefillId = tankRefillId,
            EventTypeId = await lookups.ResolveAsync(LookupTypes.TankEventType, null, eventType, field: "eventType"),
        });
        t.CurrentLevelLitres = level;
        var code = await lookups.CodeAsync(t.StatusId);
        if (code is null or "ok" or "low")
            t.StatusId = await lookups.IdOrNullAsync(LookupTypes.TankStatus, level * 100 / t.CapacityLitres < TankRules.LowLevelPercent ? "low" : "ok");
    }

    internal static object ToDto(Tank t, Func<long?, string?> code, Func<long?, string?> label, Func<string?, string?, string?> loc) => new
    {
        t.Id, t.BuildingId, t.BranchId, t.TankNo, t.GasTypeId, gasType = t.GasType?.Code, gasTypeName = loc(t.GasType?.NameEn, t.GasType?.NameAr),
        t.CapacityLitres, currentLevel = t.CurrentLevelLitres ?? 0,
        levelPct = t.CapacityLitres > 0 ? Math.Round((t.CurrentLevelLitres ?? 0) / t.CapacityLitres * 100, 1) : 0,
        currentLevelM3 = t.GasType == null ? (decimal?)null : Math.Round((t.CurrentLevelLitres ?? 0) * t.GasType.LitreToM3, 3),
        t.SerialNo, t.Manufacturer, t.InstallDate, t.LastInspectionDate, t.NextInspectionDate,
        status = code(t.StatusId), statusLabel = label(t.StatusId), t.IsActive,
    };
}

[Route("api/gas-types")]
[PermissionModule("settings")]
public class GasTypesController : CrudController<GasType>
{
    /// <summary>Everyone signed in can read gas types (dropdowns).</summary>
    [HttpGet("all")]
    [RequirePermission("dashboard.view")]
    public async Task<IActionResult> All() => Ok(await Db.GasTypes.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Code).ToListAsync());
}
