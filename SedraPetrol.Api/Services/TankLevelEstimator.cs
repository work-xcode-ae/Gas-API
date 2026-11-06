using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Gas in a building tank NOW = the last measured record (level reading / refill / opening level) − what the tenants' meters
/// used since then, converted m³ → litres with the tank's gas type and shared between the building's active tanks by capacity,
/// − an estimate for the days after the last meter reading (daily rate of the last refill period, else of the last 30 days of readings).
/// Same rules as GET /api/buildings/{id}/tank-insights, so every screen shows the same figure.
/// Nothing is written: measured readings are never changed, and re-issuing / cancelling invoices can never deduct gas twice.
/// Tanks outside a building (restaurant tanks) or without meter usage keep their measured level.
/// </summary>
public class TankLevelEstimator
{
    private readonly AppDbContext _db;
    public TankLevelEstimator(AppDbContext db) { _db = db; }

    /// <param name="Recorded">Last measured level (litres).</param>
    /// <param name="Used">Estimated litres used since that record (meters + days after the last reading).</param>
    /// <param name="Now">Estimated level now (litres, never below 0).</param>
    public record Level(long TankId, decimal Capacity, decimal Recorded, DateTimeOffset? RecordedAt, decimal Used, decimal Now)
    {
        public decimal Pct => Capacity > 0 ? Math.Round(Now * 100 / Capacity, 1) : 0;
        public bool IsLow => Capacity > 0 && Now * 100 / Capacity < TankRules.LowLevelPercent;
    }

    private record T(long Id, long? BuildingId, decimal Capacity, decimal? Current, bool IsActive, decimal Factor);

    public async Task<Dictionary<long, Level>> ForTanksAsync(IEnumerable<long> tankIds, CancellationToken ct = default)
    {
        var ids = tankIds.Distinct().ToList();
        if (ids.Count == 0) return new();
        var asked = await _db.Tanks.AsNoTracking().Where(t => ids.Contains(t.Id))
            .Select(t => new T(t.Id, t.BuildingId, t.CapacityLitres, t.CurrentLevelLitres, t.IsActive, t.GasType != null ? t.GasType.LitreToM3 : 0)).ToListAsync(ct);
        var buildingIds = asked.Where(t => t.BuildingId != null && t.IsActive).Select(t => t.BuildingId!.Value).Distinct().ToList();
        // every active tank of those buildings takes its share of the building's usage
        var all = buildingIds.Count == 0 ? new List<T>() : await _db.Tanks.AsNoTracking().Where(t => t.BuildingId != null && buildingIds.Contains(t.BuildingId.Value) && t.IsActive)
            .Select(t => new T(t.Id, t.BuildingId, t.CapacityLitres, t.CurrentLevelLitres, t.IsActive, t.GasType != null ? t.GasType.LitreToM3 : 0)).ToListAsync(ct);
        var allIds = all.Select(t => t.Id).Union(ids).ToList();

        var levelRows = await _db.TankLevelHistories.AsNoTracking().Where(h => allIds.Contains(h.TankId))
            .Select(h => new { h.Id, h.TankId, h.EventDate, h.LevelLitres }).ToListAsync(ct);
        var anchors = levelRows.GroupBy(h => h.TankId).ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.EventDate).ThenByDescending(h => h.Id).First());
        var refills = await _db.TankRefills.AsNoTracking().Where(r => allIds.Contains(r.TankId))
            .Select(r => new { r.TankId, r.RefillDatetime, r.LevelBeforeLitres, r.LevelAfterLitres }).ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var horizon = now.AddDays(-550);
        var readings = await _db.MeterReadings.AsNoTracking()
            .Where(r => r.ReadingDate >= horizon &&
                        ((r.UnitId != null && buildingIds.Contains(r.Unit!.BuildingId)) ||
                         (r.UnitId == null && _db.MeterAssignments.Any(a => a.MeterId == r.MeterId && buildingIds.Contains(a.Unit!.BuildingId)))))
            .Select(r => new
            {
                r.Id, r.MeterId, r.ReadingDate, r.Consumption,
                BuildingId = r.UnitId != null ? r.Unit!.BuildingId
                    : _db.MeterAssignments.Where(a => a.MeterId == r.MeterId && buildingIds.Contains(a.Unit!.BuildingId)).Select(a => a.Unit!.BuildingId).FirstOrDefault(),
            }).ToListAsync(ct);

        var result = new Dictionary<long, Level>();
        foreach (var bid in buildingIds)
        {
            var tanks = all.Where(t => t.BuildingId == bid).ToList();
            var capacity = tanks.Sum(t => t.Capacity);
            decimal ShareOf(long tankId) => capacity > 0 ? (tanks.FirstOrDefault(t => t.Id == tankId)?.Capacity ?? 0) / capacity : 0m;
            var factor = capacity > 0 ? tanks.Sum(t => t.Factor * t.Capacity) / capacity : 0m;
            decimal ToLitres(decimal m3) => factor > 0 ? m3 / factor : 0m;

            // each reading covers (previous reading of the same meter, this reading]
            var rd = readings.Where(r => r.BuildingId == bid).OrderBy(r => r.MeterId).ThenBy(r => r.ReadingDate).ThenBy(r => r.Id).ToList();
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
            decimal MeterM3(DateTimeOffset from, DateTimeOffset to) => intervals.Sum(iv =>
            {
                var s0 = iv.From > from ? iv.From : from;
                var e0 = iv.To < to ? iv.To : to;
                return e0 <= s0 ? 0m : iv.M3 * (decimal)((e0 - s0).TotalSeconds / (iv.To - iv.From).TotalSeconds);
            });

            // daily rate: last refill period of each tank (last 6 months), else the last 30 days of meter readings
            decimal? meterRateL = null;
            if (metersReadUntil != null && intervals.Count > 0 && factor > 0)
            {
                var winEnd = metersReadUntil.Value;
                var winStart = winEnd.AddDays(-30);
                var earliest = intervals.Min(iv => iv.From);
                if (winStart < earliest) winStart = earliest;
                var winDays = (decimal)(winEnd - winStart).TotalDays;
                if (winDays >= 7) meterRateL = ToLitres(MeterM3(winStart, winEnd) / winDays);
            }
            var periods = new List<(long TankId, DateTimeOffset To, decimal Days, decimal Used)>();
            foreach (var tg in refills.Where(r => tanks.Any(t => t.Id == r.TankId)).GroupBy(r => r.TankId))
            {
                var list = tg.OrderBy(r => r.RefillDatetime).ToList();
                for (var i = 1; i < list.Count; i++)
                {
                    var p = list[i - 1]; var c = list[i];
                    if (p.LevelAfterLitres == null || c.LevelBeforeLitres == null) continue;
                    var used = p.LevelAfterLitres.Value - c.LevelBeforeLitres.Value;
                    var days = (decimal)(c.RefillDatetime - p.RefillDatetime).TotalDays;
                    if (days < 1 || used < 0) continue;
                    periods.Add((tg.Key, c.RefillDatetime, days, used));
                }
            }
            decimal? tankRateL = null;
            var latestByTank = periods.GroupBy(x => x.TankId).Select(g => g.OrderByDescending(x => x.To).First()).Where(x => x.To >= now.AddDays(-180)).ToList();
            if (latestByTank.Count > 0)
            {
                var covered = latestByTank.Sum(x => ShareOf(x.TankId));
                if (covered > 0) tankRateL = latestByTank.Sum(x => x.Used / x.Days) / covered;
            }
            var rate = tankRateL ?? meterRateL;

            foreach (var t in tanks)
            {
                var anchor = anchors.GetValueOrDefault(t.Id);
                var recorded = anchor?.LevelLitres ?? (t.Current ?? 0);
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
                var used = measured + estimated;
                result[t.Id] = new Level(t.Id, t.Capacity, Math.Round(recorded, 1), at, Math.Round(used, 1), Math.Round(Math.Max(0, recorded - used), 1));
            }
        }
        // tanks outside a building / inactive: the measured level is the level
        foreach (var t in asked.Where(t => !result.ContainsKey(t.Id)))
        {
            var anchor = anchors.GetValueOrDefault(t.Id);
            var lvl = t.Current ?? 0;
            result[t.Id] = new Level(t.Id, t.Capacity, lvl, anchor?.EventDate, 0, lvl);
        }
        return result.Where(kv => ids.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>Estimated levels of every tank matching a filter (used for low-level counts and alerts).</summary>
    public async Task<Dictionary<long, Level>> ForQueryAsync(IQueryable<Domain.Entities.Tank> tanks, CancellationToken ct = default) =>
        await ForTanksAsync(await tanks.Select(t => t.Id).ToListAsync(ct), ct);
}
