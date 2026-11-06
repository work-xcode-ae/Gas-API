using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Meter readings and assignments. A meter belongs to a unit (meter_assignment history); a tenant belongs to a unit
/// (supply_contract). Every reading stores the unit and the tenancy it was taken in, plus its type.
/// Consumption is always value − previous value of the SAME meter; baseline readings (opening, replacement_in) carry 0,
/// so the sum of a contract's readings is its consumption across every meter used during the tenancy.
/// </summary>
public class MeterOps
{
    public static readonly string[] ReadingTypes = { "opening", "periodic", "closing", "replacement_out", "replacement_in", "removal" };
    private static readonly string[] BaselineTypes = { "opening", "replacement_in" };

    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly ICurrentUser _user;
    private readonly PostingService _posting;

    public MeterOps(AppDbContext db, LookupService lookups, ICurrentUser user, PostingService posting) { _db = db; _lookups = lookups; _user = user; _posting = posting; }

    // ------------------------------------------------------------------ meter cost in the ledger

    /// <summary>
    /// Moves a meter's purchase cost between the ledger accounts as the meter moves:
    /// stock (FEE_TYPE meter_purchase bill account, 1402 meters in stock) ⇄ installed (FEE_TYPE fixed_asset account, 1501 fixed assets)
    /// → written_off (5105 meter write-offs). Meters registered without a purchase bill (no cost location) are not posted.
    /// A meter is never moved twice: cost_location records where its cost is now.
    /// </summary>
    public async Task MoveCostAsync(Meter m, string to, DateOnly date, long? buildingId, string why)
    {
        if ((m.PurchaseCost ?? 0) <= 0 || m.CostLocation == null || m.CostLocation == to) return;
        var companyId = await _db.Branches.Where(b => b.Id == m.BranchId).Select(b => (long?)b.CompanyId).FirstOrDefaultAsync()
                        ?? await _db.Companies.Select(c => c.Id).FirstAsync();
        async Task<long> BillAccount(string fee)
        {
            var key = await _lookups.IdOrNullAsync(LookupTypes.FeeType, fee);
            return await _db.AccountMappings.Where(x => x.CompanyId == companyId && x.SourceType == MappingSources.VendorBillItem && x.SourceKeyId == key && x.IsActive)
                       .Select(x => x.DebitAccountId).FirstOrDefaultAsync()
                   ?? throw new BusinessRuleException($"No ledger account is set up for «{fee}» bill items. Set it under Accounting > Account mappings.");
        }
        async Task<long> Acc(string loc) => loc switch
        {
            "stock" => await BillAccount("meter_purchase"),
            "installed" => await BillAccount("fixed_asset"),
            "written_off" => await _db.Accounts.Where(a => a.CompanyId == companyId && a.AccountCode == "5105").Select(a => (long?)a.Id).FirstOrDefaultAsync()
                             ?? throw new BusinessRuleException("Account 5105 (meter write-offs) is missing in the chart of accounts."),
            _ => throw new AppException($"Unknown meter cost location «{loc}»."),
        };
        var cost = m.PurchaseCost!.Value;
        var text = $"Meter {m.MeterNo} ({m.SerialNo}): {why}";
        await _posting.CreateEntryAsync(m.BranchId!.Value, date, text, "meter_cost", m.Id, new[]
        {
            new PostingService.Line(await Acc(to), cost, 0, text, buildingId),
            new PostingService.Line(await Acc(m.CostLocation), 0, cost, text, buildingId),
        }, true);
        m.CostLocation = to;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Meter readings typed by users (single or bulk) are refused while the meter is damaged, faulty, lost or retired: its index cannot be trusted.
    /// Service readings (final reading when it is removed / replaced, contract closing) still go through AddReadingAsync directly.
    /// </summary>
    public async Task EnsureAcceptsReadingsAsync(Meter meter)
    {
        var status = await _lookups.CodeAsync(meter.StatusId);
        if (status is "damaged" or "fault" or "lost" or "retired")
            throw new BusinessRuleException($"Meter {meter.MeterNo} is marked «{await StatusNameAsync(meter.StatusId)}», so readings are not accepted. " +
                                            "Repair it (change its status back to working) or replace the meter first.", "meter_not_readable");
    }

    private async Task<string> StatusNameAsync(long? statusId) =>
        await _db.LookupValues.Where(v => v.Id == statusId).Select(v => v.NameEn ?? v.Code).FirstOrDefaultAsync() ?? "not working";

    /// <summary>A reading time for a business date: now when it is today, otherwise noon of that day.</summary>
    public static DateTimeOffset At(DateOnly d) =>
        d == DateOnly.FromDateTime(DateTime.Today) ? DateTimeOffset.Now : new DateTimeOffset(d.ToDateTime(new TimeOnly(12, 0)));

    /// <summary>The meter currently installed in the unit (open assignment), or null.</summary>
    public async Task<Meter?> CurrentMeterAsync(long unitId) =>
        await _db.MeterAssignments.Where(a => a.UnitId == unitId && a.ToDate == null).Select(a => a.Meter).FirstOrDefaultAsync();

    /// <summary>
    /// Adds a reading. Validates the sequence (not lower than the previous reading of the meter, not higher than a later one).
    /// unitId / contractId default to the meter's current unit and that unit's active contract.
    /// </summary>
    public async Task<MeterReading> AddReadingAsync(Meter meter, decimal value, DateTimeOffset when, string? method, string? notes, bool allowReset = false,
        string type = "periodic", long? contractId = null, long? unitId = null)
    {
        type = (type ?? "periodic").Trim().ToLowerInvariant();
        if (!ReadingTypes.Contains(type)) throw new AppException($"Unknown reading type '{type}'.");
        var baseline = BaselineTypes.Contains(type);
        if (value < 0) throw new AppException("Reading cannot be negative.");
        if (when > DateTimeOffset.UtcNow.AddHours(14)) throw new AppException("Reading date cannot be in the future.");
        var prev = await _db.MeterReadings.Where(r => r.MeterId == meter.Id && r.ReadingDate <= when).OrderByDescending(r => r.ReadingDate).ThenByDescending(r => r.Id).FirstOrDefaultAsync();
        var next = await _db.MeterReadings.Where(r => r.MeterId == meter.Id && r.ReadingDate > when).OrderBy(r => r.ReadingDate).FirstOrDefaultAsync();
        var prevValue = prev?.ReadingValue ?? meter.InitialReading ?? 0;
        if (value < prevValue && !allowReset && type != "replacement_in")
            throw new BusinessRuleException($"Reading {value} is lower than the previous reading {prevValue} of meter {meter.MeterNo}.");
        if (next != null && value > next.ReadingValue)
            throw new BusinessRuleException($"Reading {value} is higher than the later reading {next.ReadingValue} on {next.ReadingDate:yyyy-MM-dd}.");
        if (next?.InvoiceId != null) throw new BusinessRuleException("A later reading is already billed; back-dated readings are not allowed.");

        unitId ??= await _db.MeterAssignments.Where(a => a.MeterId == meter.Id && a.ToDate == null).Select(a => (long?)a.UnitId).FirstOrDefaultAsync();
        if (contractId == null && unitId != null && type != "opening")
        {
            var day = DateOnly.FromDateTime(when.LocalDateTime);
            contractId = await _db.SupplyContracts.Where(c => c.UnitId == unitId && c.IsActive && c.StartDate <= day).Select(c => (long?)c.Id).FirstOrDefaultAsync();
        }

        var reading = new MeterReading
        {
            MeterId = meter.Id, ReadingDate = when, ReadingValue = value,
            Consumption = baseline || (allowReset && value < prevValue) ? 0 : value - prevValue,
            MethodId = await _lookups.ResolveAsync(LookupTypes.ReadingMethod, null, method ?? "manual", field: "method"),
            ReadingTypeId = await _lookups.IdAsync(LookupTypes.ReadingType, type),
            UnitId = unitId, ContractId = contractId, ReadBy = _user.UserId, Notes = notes,
        };
        _db.MeterReadings.Add(reading);
        // keep the chain consistent (a later baseline reading always stays 0)
        if (next != null && !await IsBaselineAsync(next.ReadingTypeId)) next.Consumption = next.ReadingValue - value;
        await _db.SaveChangesAsync();
        return reading;
    }

    private async Task<bool> IsBaselineAsync(long? typeId) => BaselineTypes.Contains(await _lookups.CodeAsync(typeId) ?? "");

    /// <summary>Meter statuses that may be installed in a unit (stock; «active» covers meters registered before the stock flow).</summary>
    public static readonly string[] InstallableStatuses = { "in_stock", "active" };

    /// <summary>Install a meter from stock into a unit: status → active.</summary>
    public async Task AssignAsync(Meter meter, long unitId, DateOnly from)
    {
        var status = await _lookups.CodeAsync(meter.StatusId);
        if (status != null && !InstallableStatuses.Contains(status))
            throw new BusinessRuleException($"Meter {meter.MeterNo} is «{status}» and cannot be installed. Return it to stock first.");
        if (!await _db.Units.AnyAsync(u => u.Id == unitId)) throw new NotFoundException("Unit", unitId);
        if (await _db.MeterAssignments.AnyAsync(a => a.MeterId == meter.Id && a.ToDate == null))
            throw new BusinessRuleException($"Meter {meter.MeterNo} is already installed in another unit. Remove it there first.");
        if (await _db.MeterAssignments.AnyAsync(a => a.UnitId == unitId && a.ToDate == null))
            throw new BusinessRuleException("The unit already has a meter. Use replace meter instead.");
        var lastEnd = await _db.MeterAssignments.Where(a => a.MeterId == meter.Id && a.ToDate != null).MaxAsync(a => (DateOnly?)a.ToDate);
        if (lastEnd != null && from < lastEnd) throw new BusinessRuleException($"The meter was in another unit until {lastEnd:yyyy-MM-dd}.");
        _db.MeterAssignments.Add(new MeterAssignment { MeterId = meter.Id, UnitId = unitId, FromDate = from });
        meter.StatusId = await _lookups.IdOrNullAsync(LookupTypes.MeterStatus, "active") ?? meter.StatusId;
        meter.InstallDate = from;
        await _db.SaveChangesAsync();
        // its cost leaves stock and becomes a fixed asset of the building
        var u = await _db.Units.Where(x => x.Id == unitId).Select(x => new { x.BuildingId, x.UnitNo }).FirstAsync();
        await MoveCostAsync(meter, "installed", from, u.BuildingId, $"installed in unit {u.UnitNo}");
    }

    /// <summary>Removes the meter from its unit. Not allowed while the unit is rented (replace the meter instead).</summary>
    public async Task UnassignAsync(Meter meter, DateOnly to, decimal? finalReading)
    {
        var a = await _db.MeterAssignments.FirstOrDefaultAsync(x => x.MeterId == meter.Id && x.ToDate == null)
                ?? throw new BusinessRuleException("The meter is not assigned.");
        if (to < a.FromDate) throw new AppException("The end date cannot be before the assignment start.");
        if (await _db.SupplyContracts.AnyAsync(c => c.UnitId == a.UnitId && c.IsActive))
            throw new BusinessRuleException("The unit has an active tenant contract. Replace the meter, or end the contract first.");
        if (finalReading != null)
            await AddReadingAsync(meter, finalReading.Value, At(to), "manual", "Final reading – meter removed", type: "removal", unitId: a.UnitId);
        a.ToDate = to;
        // back to stock (a faulty / maintenance meter keeps its status)
        if (await _lookups.CodeAsync(meter.StatusId) is null or "active")
            meter.StatusId = await _lookups.IdOrNullAsync(LookupTypes.MeterStatus, "in_stock") ?? meter.StatusId;
        await _db.SaveChangesAsync();
        await MoveCostAsync(meter, "stock", to, await _db.Units.Where(x => x.Id == a.UnitId).Select(x => (long?)x.BuildingId).FirstOrDefaultAsync(), "removed from the unit");
    }

    public async Task SetStatusAsync(Meter meter, string code) =>
        meter.StatusId = await _lookups.IdAsync(LookupTypes.MeterStatus, code);
}
