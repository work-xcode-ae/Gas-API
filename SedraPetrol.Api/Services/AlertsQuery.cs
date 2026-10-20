using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>Builds the expiring-items list for alerts and dashboard notifications.</summary>
public class AlertsQuery
{
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    public AlertsQuery(AppDbContext db, LookupService lookups) { _db = db; _lookups = lookups; }

    public record Alert(string Type, string Severity, long EntityId, string Reference, string? BuildingName, DateOnly? DueDate, int? DaysLeft, string MessageEn, string MessageAr);

    public async Task<List<Alert>> GetAsync(int days, IReadOnlyCollection<long> branches)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var limit = today.AddDays(days);
        var all = branches.Count == 0;
        var list = new List<Alert>();
        string Sev(DateOnly? d) => d == null ? "info" : d < today ? "critical" : d.Value.DayNumber - today.DayNumber <= 7 ? "high" : "medium";
        int? Left(DateOnly? d) => d == null ? null : d.Value.DayNumber - today.DayNumber;

        var cdExpired = await _lookups.IdOrNullAsync(LookupTypes.CdStatus, "expired");
        foreach (var c in await _db.CdCertificates.AsNoTracking().Where(c => (all || branches.Contains(c.BranchId)) && c.ExpiryDate != null && c.ExpiryDate <= limit && c.StatusId != cdExpired
                                                                              && !_db.CdCertificates.Any(n => n.BuildingId == c.BuildingId && n.CertTypeId == c.CertTypeId && n.ExpiryDate > c.ExpiryDate))
                     .Select(c => new { c.Id, c.CertificateNo, c.ExpiryDate, c.Building!.NameAr, c.Building.NameEn }).ToListAsync())
            list.Add(new Alert("cd_certificate", Sev(c.ExpiryDate), c.Id, c.CertificateNo ?? $"#{c.Id}", c.NameAr ?? c.NameEn, c.ExpiryDate, Left(c.ExpiryDate),
                $"Civil Defense certificate {c.CertificateNo} expires {c.ExpiryDate:yyyy-MM-dd}", $"شهادة الدفاع المدني {c.CertificateNo} تنتهي في {c.ExpiryDate:yyyy-MM-dd}"));

        var amcActive = await _lookups.IdOrNullAsync(LookupTypes.AmcStatus, "active");
        foreach (var a in await _db.AmcContracts.AsNoTracking().Where(a => (all || branches.Contains(a.BranchId)) && a.StatusId == amcActive && a.EndDate <= limit)
                     .Select(a => new { a.Id, a.ContractNo, a.EndDate, a.Building!.NameAr, a.Building.NameEn }).ToListAsync())
            list.Add(new Alert("amc_contract", Sev(a.EndDate), a.Id, a.ContractNo, a.NameAr ?? a.NameEn, a.EndDate, Left(a.EndDate),
                $"Maintenance contract {a.ContractNo} ends {a.EndDate:yyyy-MM-dd}", $"عقد الصيانة {a.ContractNo} ينتهي في {a.EndDate:yyyy-MM-dd}"));

        foreach (var c in await _db.SupplyContracts.AsNoTracking().Where(c => (all || branches.Contains(c.BranchId)) && c.IsActive && !c.AutoRenew && c.EndDate != null && c.EndDate <= limit)
                     .Select(c => new { c.Id, c.ContractNo, c.EndDate, c.Unit!.Building!.NameAr, c.Unit.Building.NameEn }).ToListAsync())
            list.Add(new Alert("supply_contract", Sev(c.EndDate), c.Id, c.ContractNo, c.NameAr ?? c.NameEn, c.EndDate, Left(c.EndDate),
                $"Gas contract {c.ContractNo} ends {c.EndDate:yyyy-MM-dd}", $"عقد الغاز {c.ContractNo} ينتهي في {c.EndDate:yyyy-MM-dd}"));

        var calibLimit = limit.AddYears(-1);
        foreach (var m in await _db.Meters.AsNoTracking().Where(m => (all || branches.Contains(m.BranchId ?? 0)) && m.IsActive && m.CalibrationDate != null && m.CalibrationDate <= calibLimit)
                     .Select(m => new { m.Id, m.MeterNo, m.CalibrationDate }).Take(200).ToListAsync())
        {
            var due = m.CalibrationDate!.Value.AddYears(1);
            list.Add(new Alert("meter_calibration", Sev(due), m.Id, m.MeterNo, null, due, Left(due),
                $"Meter {m.MeterNo} calibration due {due:yyyy-MM-dd}", $"معايرة العداد {m.MeterNo} مستحقة في {due:yyyy-MM-dd}"));
        }

        foreach (var t in await _db.Tanks.AsNoTracking().Where(t => (all || branches.Contains(t.BranchId ?? 0)) && t.NextInspectionDate != null && t.NextInspectionDate <= limit)
                     .Select(t => new { t.Id, t.TankNo, t.NextInspectionDate, t.Building!.NameAr, t.Building.NameEn }).ToListAsync())
            list.Add(new Alert("tank_inspection", Sev(t.NextInspectionDate), t.Id, t.TankNo, t.NameAr ?? t.NameEn, t.NextInspectionDate, Left(t.NextInspectionDate),
                $"Tank {t.TankNo} inspection due {t.NextInspectionDate:yyyy-MM-dd}", $"فحص الخزان {t.TankNo} مستحق في {t.NextInspectionDate:yyyy-MM-dd}"));

        var completed = await _lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed");
        var cancelled = await _lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        foreach (var t in await _db.Tanks.AsNoTracking().Where(t => (all || branches.Contains(t.BranchId ?? 0)) && t.CapacityLitres > 0 && (t.CurrentLevelLitres ?? 0) * 100 / t.CapacityLitres < TankRules.LowLevelPercent
                                                                     && !_db.RefillRequests.Any(r => r.TankId == t.Id && r.StatusId != completed && r.StatusId != cancelled))
                     .Select(t => new { t.Id, t.TankNo, t.CapacityLitres, t.CurrentLevelLitres, t.Building!.NameAr, t.Building.NameEn }).ToListAsync())
        {
            var pct = Math.Round((t.CurrentLevelLitres ?? 0) / t.CapacityLitres * 100, 0);
            list.Add(new Alert("low_tank", pct < 10 ? "critical" : "high", t.Id, t.TankNo, t.NameAr ?? t.NameEn, null, null,
                $"Tank {t.TankNo} is at {pct}% with no refill request", $"الخزان {t.TankNo} عند {pct}% ولا يوجد طلب تعبئة"));
        }

        foreach (var d in await _db.Drivers.AsNoTracking().Where(d => d.IsActive && d.LicenseExpiry != null && d.LicenseExpiry <= limit).Select(d => new { d.Id, d.Party!.NameEn, d.Party!.NameAr, d.LicenseExpiry }).ToListAsync())
            list.Add(new Alert("driver_license", Sev(d.LicenseExpiry), d.Id, d.NameAr ?? d.NameEn ?? $"#{d.Id}", null, d.LicenseExpiry, Left(d.LicenseExpiry),
                $"Driver {d.NameEn} license expires {d.LicenseExpiry:yyyy-MM-dd}", $"رخصة السائق {d.NameAr} تنتهي في {d.LicenseExpiry:yyyy-MM-dd}"));

        foreach (var v in await _db.Vehicles.AsNoTracking().Where(v => v.IsActive && v.RegistrationExpiry != null && v.RegistrationExpiry <= limit).Select(v => new { v.Id, v.PlateNo, v.RegistrationExpiry }).ToListAsync())
            list.Add(new Alert("vehicle_registration", Sev(v.RegistrationExpiry), v.Id, v.PlateNo, null, v.RegistrationExpiry, Left(v.RegistrationExpiry),
                $"Vehicle {v.PlateNo} registration expires {v.RegistrationExpiry:yyyy-MM-dd}", $"تسجيل المركبة {v.PlateNo} ينتهي في {v.RegistrationExpiry:yyyy-MM-dd}"));

        foreach (var d in await _db.Documents.AsNoTracking().Where(d => (all || d.BranchId == null || branches.Contains(d.BranchId.Value)) && d.ExpiryDate != null && d.ExpiryDate <= limit && d.ExpiryDate >= today.AddDays(-30))
                     .Select(d => new { d.Id, d.FileName, d.EntityType, d.ExpiryDate }).Take(200).ToListAsync())
            list.Add(new Alert("document", Sev(d.ExpiryDate), d.Id, d.FileName ?? $"#{d.Id}", null, d.ExpiryDate, Left(d.ExpiryDate),
                $"Document {d.FileName} ({d.EntityType}) expires {d.ExpiryDate:yyyy-MM-dd}", $"المستند {d.FileName} ينتهي في {d.ExpiryDate:yyyy-MM-dd}"));

        var order = new Dictionary<string, int> { ["critical"] = 0, ["high"] = 1, ["medium"] = 2, ["info"] = 3 };
        return list.OrderBy(a => order.GetValueOrDefault(a.Severity, 9)).ThenBy(a => a.DaysLeft ?? int.MinValue).ToList();
    }
}
