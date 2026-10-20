using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Gas supply contract (tenancy) rules: one active contract per unit, the unit must already have a meter,
/// every tenancy has a duration and its own opening / closing readings, unit occupancy.
/// </summary>
public class ContractService
{
    public record FeeInput
    {
        public string? Type { get; init; }
        public long? FeeTypeId { get; init; }
        /// <summary>monthly | yearly | one_time | per_m3</summary>
        public string? Cycle { get; init; }
        public long? CycleId { get; init; }
        public decimal Amount { get; init; }
        public DateOnly? EffectiveFrom { get; init; }
        public DateOnly? EffectiveTo { get; init; }
        public string? Notes { get; init; }
    }

    /// <summary>One term of a contract: from the setup (TemplateId) — possibly reworded — or written for this contract only.</summary>
    public record TermItemInput
    {
        public long? TemplateId { get; init; }
        public string? Category { get; init; }
        public string Text { get; init; } = "";
    }

    public record ContractInput
    {
        public long? TenantId { get; init; }
        public long? UnitId { get; init; }
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        /// <summary>Alternative to EndDate: contract length in months.</summary>
        public int? DurationMonths { get; init; }
        /// <summary>Meter reading at move-in (required to activate). Saved as the tenancy's opening reading.</summary>
        public decimal? OpeningReading { get; init; }
        public DateTimeOffset? OpeningReadingDate { get; init; }
        public bool AutoRenew { get; init; }
        public short? RenewalPeriodMonths { get; init; }
        public decimal? DepositAmount { get; init; }
        public bool DepositPaid { get; init; }
        public DateOnly? DepositDate { get; init; }
        public string? Terms { get; init; }
        /// <summary>draft | active | pending (default active)</summary>
        public string? Status { get; init; }
        public List<FeeInput>? Fees { get; init; }
        /// <summary>Terms &amp; conditions chosen for this contract (copied — editing the Settings list later does not change them).</summary>
        public List<TermItemInput>? TermItems { get; init; }
        /// <summary>Create an invoice for the one-time fees (connection ...) and the unpaid deposit.</summary>
        public bool IssueInitialInvoice { get; init; }
    }

    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly NumberingService _numbers;
    private readonly ActivityLogger _activity;
    private readonly BillingService _billing;
    private readonly MeterOps _meters;

    public ContractService(AppDbContext db, LookupService lookups, NumberingService numbers, ActivityLogger activity, BillingService billing, MeterOps meters)
    {
        _db = db; _lookups = lookups; _numbers = numbers; _activity = activity; _billing = billing; _meters = meters;
    }

    public async Task<SupplyContract> CreateAsync(ContractInput i)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == i.TenantId) ?? throw new NotFoundException("Tenant", i.TenantId ?? 0);
        var unit = await _db.Units.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == i.UnitId) ?? throw new NotFoundException("Unit", i.UnitId ?? 0);
        if (!unit.IsActive) throw new BusinessRuleException($"Unit {unit.UnitNo} is deactivated. Activate it before adding a tenant contract.");
        var start = i.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        var end = i.EndDate ?? (i.DurationMonths is > 0 ? start.AddMonths(i.DurationMonths.Value).AddDays(-1) : null);
        if (end != null && end < start) throw new AppException("End date must be after the start date.");
        var statusCode = (i.Status ?? "active").ToLowerInvariant();
        if (statusCode is not ("draft" or "active" or "pending")) throw new AppException("A new contract can only be draft, pending or active.");
        var active = statusCode == "active";
        Meter? meter = null;
        if (active)
        {
            await EnsureUnitFree(unit.Id, null);
            if (end == null) throw new AppException("The contract duration is required (end date or number of months).");
            meter = await RequireMeterAsync(unit);
            if (i.OpeningReading == null) throw new AppException($"The opening reading of meter {meter.MeterNo} is required.");
            var gasType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "gas_price");
            if (!(i.Fees ?? new()).Any(f => f.Amount > 0 && (f.Type == "gas_price" || f.Cycle == "per_m3" || (gasType != null && f.FeeTypeId == gasType))))
                throw new AppException("The gas price (per m³) is required for an active contract, otherwise its consumption cannot be invoiced.");
        }

        var c = new SupplyContract
        {
            BranchId = unit.Building!.BranchId,
            ContractNo = await _numbers.NextAsync(NumberPrefixes.SupplyContract, date: start),
            TenantId = tenant.Id, UnitId = unit.Id, BuildingId = unit.BuildingId, StartDate = start, EndDate = end,
            AutoRenew = i.AutoRenew, RenewalPeriodMonths = i.RenewalPeriodMonths,
            DepositAmount = i.DepositAmount, DepositPaid = i.DepositPaid, DepositDate = i.DepositDate, Terms = i.Terms,
            StatusId = await _lookups.IdAsync(LookupTypes.ContractStatus, statusCode), IsActive = active,
        };
        _db.SupplyContracts.Add(c);
        await _db.SaveChangesAsync();
        if (active) await RecordOpeningAsync(c, meter!, i.OpeningReading!.Value, i.OpeningReadingDate);

        await AddFeesAsync(c, i.Fees ?? new());
        await SetTermsAsync(c.Id, i.TermItems);
        if (active) await SetUnitStatus(unit, "occupied");
        await _db.SaveChangesAsync();

        if (i.IssueInitialInvoice) await InitialInvoiceAsync(c, unit.BuildingId);
        _activity.Log("supply_contract", c.Id, "create", $"Contract {c.ContractNo} created for unit {unit.UnitNo}", unit.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
        return c;
    }

    /// <summary>Replace the terms of a contract. A term is «modified» when its text differs from the setup it came from.</summary>
    public async Task SetTermsAsync(long contractId, IEnumerable<TermItemInput>? items)
    {
        if (items == null) return;
        var list = items.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
        var ids = list.Where(x => x.TemplateId != null).Select(x => x.TemplateId!.Value).Distinct().ToList();
        var templates = await _db.ContractTerms.IgnoreQueryFilters().Where(t => ids.Contains(t.Id) && t.ContractId == null).ToDictionaryAsync(t => t.Id);
        _db.ContractTerms.RemoveRange(await _db.ContractTerms.Where(t => t.ContractType == "supply" && t.ContractId == contractId).ToListAsync());
        var order = 0;
        foreach (var x in list)
        {
            var tpl = x.TemplateId != null && templates.TryGetValue(x.TemplateId.Value, out var t) ? t : null;
            var text = x.Text.Trim();
            _db.ContractTerms.Add(new ContractTerm
            {
                ContractType = "supply", ContractId = contractId, SourceId = tpl?.Id, Category = (x.Category ?? tpl?.Category ?? "general").Trim().ToLowerInvariant(),
                TextAr = text, IsCustom = tpl == null, IsModified = tpl != null && !string.Equals(tpl.TextAr.Trim(), text, StringComparison.Ordinal)
                    && !string.Equals(tpl.TextEn?.Trim(), text, StringComparison.Ordinal),
                SortOrder = (++order) * 10,
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task AddFeesAsync(SupplyContract c, IEnumerable<FeeInput> fees)
    {
        var aed = await _db.Currencies.Where(x => x.Code == "AED").Select(x => (long?)x.Id).FirstOrDefaultAsync();
        foreach (var f in fees)
        {
            if (f.Amount < 0) throw new AppException("Fee amounts cannot be negative.");
            var typeId = await _lookups.ResolveAsync(LookupTypes.FeeType, f.FeeTypeId, f.Type, true, "fee type");
            var defaultCycle = await _lookups.CodeAsync(typeId) switch
            {
                "gas_price" => "per_m3",
                "connection" or "disconnection" or "inspection" or "deposit" => "one_time",
                _ => "monthly"
            };
            _db.ContractFees.Add(new ContractFee
            {
                ContractId = c.Id, FeeTypeId = typeId!.Value,
                CycleId = (await _lookups.ResolveAsync(LookupTypes.FeeCycle, f.CycleId, f.Cycle ?? defaultCycle, true, "cycle"))!.Value,
                Amount = f.Amount, CurrencyId = aed, EffectiveFrom = f.EffectiveFrom ?? c.StartDate, EffectiveTo = f.EffectiveTo, Notes = f.Notes, IsActive = true,
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task ActivateAsync(SupplyContract c, decimal? openingReading = null, DateTimeOffset? openingDate = null)
    {
        var code = await _lookups.CodeAsync(c.StatusId);
        if (code is not ("draft" or "pending")) throw new BusinessRuleException($"Only draft or pending contracts can be activated (status: {code}).");
        await EnsureUnitFree(c.UnitId!.Value, c.Id);
        if (c.EndDate == null) throw new AppException("Set the contract end date before activating it.");
        var unit = await _db.Units.FirstAsync(u => u.Id == c.UnitId);
        var meter = await RequireMeterAsync(unit);
        var gasType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "gas_price");
        var perM3 = await _lookups.IdOrNullAsync(LookupTypes.FeeCycle, "per_m3");
        if (!await _db.ContractFees.AnyAsync(f => f.ContractId == c.Id && f.IsActive && f.Amount > 0 && (f.FeeTypeId == gasType || f.CycleId == perM3)))
            throw new BusinessRuleException($"Contract {c.ContractNo} has no gas price (per m³). Add it to the contract fees before activating.");
        var openingType = await _lookups.IdAsync(LookupTypes.ReadingType, "opening");
        if (!await _db.MeterReadings.AnyAsync(r => r.ContractId == c.Id && r.ReadingTypeId == openingType))
        {
            if (openingReading == null) throw new AppException($"The opening reading of meter {meter.MeterNo} is required.");
            await RecordOpeningAsync(c, meter, openingReading.Value, openingDate);
        }
        c.StatusId = await _lookups.IdAsync(LookupTypes.ContractStatus, "active");
        c.IsActive = true;
        await SetUnitStatus(unit, "occupied");
        _activity.Log("supply_contract", c.Id, "status_change", $"Contract {c.ContractNo} activated", unit.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Ends the tenancy. For an active contract on a unit with a meter the closing reading is required; it is saved as the
    /// tenancy's closing reading. With <paramref name="finalInvoice"/> the unbilled consumption is invoiced. Returns the final invoice id.
    /// </summary>
    public async Task<long?> TerminateAsync(SupplyContract c, DateOnly date, string? reason, decimal? closingReading = null, bool finalInvoice = false)
    {
        var code = await _lookups.CodeAsync(c.StatusId);
        if (code is "terminated" or "expired") throw new BusinessRuleException($"The contract is already {code}.");
        if (date < c.StartDate) throw new AppException("Termination date cannot be before the start date.");
        if (c.IsActive && await _meters.CurrentMeterAsync(c.UnitId!.Value) is { } meter)
        {
            if (closingReading == null) throw new AppException($"The closing reading of meter {meter.MeterNo} is required.");
            // the closing reading is the last one of the tenancy: never place it before a reading already taken that day
            var at = MeterOps.At(date);
            var sameDay = await _db.MeterReadings.Where(r => r.MeterId == meter.Id && r.ReadingDate > at).OrderByDescending(r => r.ReadingDate)
                .Select(r => (DateTimeOffset?)r.ReadingDate).FirstOrDefaultAsync();
            if (sameDay != null && DateOnly.FromDateTime(sameDay.Value.ToLocalTime().DateTime) == date) at = sameDay.Value.AddMinutes(1);
            await _meters.AddReadingAsync(meter, closingReading.Value, at, "manual", $"Closing reading – contract {c.ContractNo}",
                type: "closing", contractId: c.Id, unitId: c.UnitId);
        }
        long? invoiceId = null;
        if (finalInvoice)
            invoiceId = (await _billing.GenerateConsumptionInvoiceAsync(c.Id, c.StartDate, date, issue: true, includeMonthlyFees: false))?.Id;
        c.StatusId = await _lookups.IdAsync(LookupTypes.ContractStatus, "terminated");
        c.IsActive = false;
        c.TerminatedAt = date;
        if (c.EndDate == null || c.EndDate > date) c.EndDate = date;
        if (!string.IsNullOrWhiteSpace(reason)) c.Terms = $"{c.Terms}\n[Terminated {date:yyyy-MM-dd}] {reason}".Trim();
        await _db.ContractFees.Where(f => f.ContractId == c.Id && f.EffectiveTo == null)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.EffectiveTo, date));
        var unit = await _db.Units.FirstAsync(u => u.Id == c.UnitId);
        await SetUnitStatus(unit, "vacant");
        _activity.Log("supply_contract", c.Id, "status_change", $"Contract {c.ContractNo} terminated{(reason == null ? "" : ": " + reason)}", unit.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
        return invoiceId;
    }

    public async Task RenewAsync(SupplyContract c, DateOnly? newEnd, int? months)
    {
        if (!c.IsActive) throw new BusinessRuleException("Only active contracts can be renewed.");
        var baseDate = c.EndDate ?? DateOnly.FromDateTime(DateTime.Today);
        var end = newEnd ?? baseDate.AddMonths(months ?? c.RenewalPeriodMonths ?? 12);
        if (end <= baseDate) throw new AppException("The new end date must be after the current end date.");
        c.EndDate = end;
        _activity.Log("supply_contract", c.Id, "update", $"Contract {c.ContractNo} renewed until {end:yyyy-MM-dd}", null, c.BranchId);
        await _db.SaveChangesAsync();
    }

    private async Task EnsureUnitFree(long unitId, long? exceptContractId)
    {
        var other = await _db.SupplyContracts.Where(x => x.UnitId == unitId && x.IsActive && x.Id != exceptContractId).Select(x => x.ContractNo).FirstOrDefaultAsync();
        if (other != null) throw new BusinessRuleException($"The unit already has an active contract ({other}).");
    }

    /// <summary>A tenant can only move into a unit that already has a gas meter installed.</summary>
    private async Task<Meter> RequireMeterAsync(Unit unit) =>
        await _meters.CurrentMeterAsync(unit.Id)
        ?? throw new BusinessRuleException($"Unit {unit.UnitNo} has no gas meter. Install a meter on the unit before adding a tenant.");

    private async Task RecordOpeningAsync(SupplyContract c, Meter meter, decimal value, DateTimeOffset? when)
    {
        var at = when ?? (c.StartDate >= DateOnly.FromDateTime(DateTime.Today) ? DateTimeOffset.Now : MeterOps.At(c.StartDate));
        await _meters.AddReadingAsync(meter, value, at, "manual", $"Opening reading – contract {c.ContractNo}", type: "opening", contractId: c.Id, unitId: c.UnitId);
    }

    private async Task SetUnitStatus(Unit u, string code)
    {
        var current = await _lookups.CodeAsync(u.StatusId);
        if (current is "maintenance" or "reserved" && code == "vacant") return;
        u.StatusId = await _lookups.IdOrNullAsync(LookupTypes.UnitStatus, code) ?? u.StatusId;
    }

    private async Task InitialInvoiceAsync(SupplyContract c, long buildingId)
    {
        var oneTime = await _lookups.IdOrNullAsync(LookupTypes.FeeCycle, "one_time");
        // all one-time fees (connection, disconnection, inspection …) go on the first invoice
        var fees = await _db.ContractFees.Where(f => f.ContractId == c.Id && f.CycleId == oneTime && f.Amount > 0).ToListAsync();
        var lines = new List<InvoiceLineInput>();
        foreach (var f in fees)
        {
            var lk = await _lookups.GetAsync(f.FeeTypeId);
            lines.Add(new InvoiceLineInput(null, f.FeeTypeId, lk?.NameEn ?? "Fee", 1, "each", f.Amount, lk?.Code == "deposit" ? 0 : null));
        }
        var depositType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "deposit");
        if ((c.DepositAmount ?? 0) > 0 && !c.DepositPaid && fees.All(f => f.FeeTypeId != depositType))
            lines.Add(new InvoiceLineInput("deposit", null, "Security deposit", 1, "each", c.DepositAmount!.Value, 0));
        if (lines.Count == 0) return;
        await _billing.CreateInvoiceAsync(new InvoiceDraft(c.BranchId, c.TenantId, buildingId, c.Id, "connection", null,
            DateOnly.FromDateTime(DateTime.Today), null, null, null, null, $"Initial charges – {c.ContractNo}", lines, true));
    }
}
