using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>Meter readings and assignments (consumption is always value − previous value).</summary>
public class MeterOps
{
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly ICurrentUser _user;

    public MeterOps(AppDbContext db, LookupService lookups, ICurrentUser user) { _db = db; _lookups = lookups; _user = user; }

    /// <summary>Adds a reading. Validates it is not lower than the previous reading and not higher than a later one.</summary>
    public async Task<MeterReading> AddReadingAsync(Meter meter, decimal value, DateTimeOffset when, string? method, string? notes, bool allowReset = false)
    {
        if (value < 0) throw new AppException("Reading cannot be negative.");
        if (when > DateTimeOffset.UtcNow.AddHours(14)) throw new AppException("Reading date cannot be in the future.");
        var prev = await _db.MeterReadings.Where(r => r.MeterId == meter.Id && r.ReadingDate <= when).OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id).FirstOrDefaultAsync();
        var next = await _db.MeterReadings.Where(r => r.MeterId == meter.Id && r.ReadingDate > when).OrderBy(r => r.ReadingDate).FirstOrDefaultAsync();
        var prevValue = prev?.ReadingValue ?? meter.InitialReading ?? 0;
        if (value < prevValue && !allowReset)
            throw new BusinessRuleException($"Reading {value} is lower than the previous reading {prevValue}.");
        if (next != null && value > next.ReadingValue)
            throw new BusinessRuleException($"Reading {value} is higher than the later reading {next.ReadingValue} on {next.ReadingDate:yyyy-MM-dd}.");
        if (next?.InvoiceId != null) throw new BusinessRuleException("A later reading is already billed; back-dated readings are not allowed.");

        var reading = new MeterReading
        {
            MeterId = meter.Id, ReadingDate = when, ReadingValue = value, Consumption = allowReset && value < prevValue ? 0 : value - prevValue,
            MethodId = await _lookups.ResolveAsync(LookupTypes.ReadingMethod, null, method ?? "manual", field: "method"),
            ReadBy = _user.UserId, Notes = notes,
        };
        _db.MeterReadings.Add(reading);
        if (next != null) next.Consumption = next.ReadingValue - value; // keep the chain consistent
        await _db.SaveChangesAsync();
        return reading;
    }

    public async Task AssignAsync(Meter meter, long unitId, DateOnly from, bool linkContract)
    {
        if (!await _db.Units.AnyAsync(u => u.Id == unitId)) throw new NotFoundException("Unit", unitId);
        if (await _db.MeterAssignments.AnyAsync(a => a.MeterId == meter.Id && a.ToDate == null))
            throw new BusinessRuleException("The meter is already assigned. Unassign it first.");
        if (await _db.MeterAssignments.AnyAsync(a => a.UnitId == unitId && a.ToDate == null))
            throw new BusinessRuleException("The unit already has a meter.");
        _db.MeterAssignments.Add(new MeterAssignment { MeterId = meter.Id, UnitId = unitId, FromDate = from });
        if (linkContract)
        {
            var c = await _db.SupplyContracts.FirstOrDefaultAsync(x => x.UnitId == unitId && x.IsActive);
            if (c != null) c.MeterId = meter.Id;
        }
        await _db.SaveChangesAsync();
    }

    public async Task UnassignAsync(Meter meter, DateOnly to)
    {
        var a = await _db.MeterAssignments.FirstOrDefaultAsync(x => x.MeterId == meter.Id && x.ToDate == null)
                ?? throw new BusinessRuleException("The meter is not assigned.");
        if (to < a.FromDate) throw new AppException("The end date cannot be before the assignment start.");
        a.ToDate = to;
        await _db.SupplyContracts.Where(c => c.MeterId == meter.Id && c.IsActive).ForEachAsync(c => c.MeterId = null);
        await _db.SaveChangesAsync();
    }

    public async Task SetStatusAsync(Meter meter, string code) =>
        meter.StatusId = await _lookups.IdAsync(LookupTypes.MeterStatus, code);
}
