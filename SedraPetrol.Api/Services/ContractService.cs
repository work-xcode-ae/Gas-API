using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>Gas supply contract rules (one active contract per unit, meter linkage, unit occupancy).</summary>
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

    public record ContractInput
    {
        public long? TenantId { get; init; }
        public long? UnitId { get; init; }
        public long? MeterId { get; init; }
        public DateOnly? StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public bool AutoRenew { get; init; }
        public short? RenewalPeriodMonths { get; init; }
        public decimal? DepositAmount { get; init; }
        public bool DepositPaid { get; init; }
        public DateOnly? DepositDate { get; init; }
        public string? Terms { get; init; }
        /// <summary>draft | active | pending (default active)</summary>
        public string? Status { get; init; }
        public List<FeeInput>? Fees { get; init; }
        /// <summary>Create an invoice for the one-time fees (connection ...) and the unpaid deposit.</summary>
        public bool IssueInitialInvoice { get; init; }
    }

    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly NumberingService _numbers;
    private readonly ActivityLogger _activity;
    private readonly BillingService _billing;

    public ContractService(AppDbContext db, LookupService lookups, NumberingService numbers, ActivityLogger activity, BillingService billing)
    {
        _db = db; _lookups = lookups; _numbers = numbers; _activity = activity; _billing = billing;
    }

    public async Task<SupplyContract> CreateAsync(ContractInput i)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == i.TenantId) ?? throw new NotFoundException("Tenant", i.TenantId ?? 0);
        var unit = await _db.Units.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == i.UnitId) ?? throw new NotFoundException("Unit", i.UnitId ?? 0);
        var start = i.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        if (i.EndDate != null && i.EndDate < start) throw new AppException("End date must be after the start date.");
        var statusCode = (i.Status ?? "active").ToLowerInvariant();
        if (statusCode is not ("draft" or "active" or "pending")) throw new AppException("A new contract can only be draft, pending or active.");
        var active = statusCode == "active";
        if (active) await EnsureUnitFree(unit.Id, null);

        var c = new SupplyContract
        {
            BranchId = unit.Building!.BranchId,
            ContractNo = await _numbers.NextAsync(NumberPrefixes.SupplyContract, date: start),
            TenantId = tenant.Id, UnitId = unit.Id, StartDate = start, EndDate = i.EndDate,
            AutoRenew = i.AutoRenew, RenewalPeriodMonths = i.RenewalPeriodMonths,
            DepositAmount = i.DepositAmount, DepositPaid = i.DepositPaid, DepositDate = i.DepositDate, Terms = i.Terms,
            StatusId = await _lookups.IdAsync(LookupTypes.ContractStatus, statusCode), IsActive = active,
        };
        c.MeterId = await ResolveMeter(unit.Id, i.MeterId, start, null);
        _db.SupplyContracts.Add(c);
        await _db.SaveChangesAsync();

        await AddFeesAsync(c, i.Fees ?? new());
        if (active) await SetUnitStatus(unit, "occupied");
        await _db.SaveChangesAsync();

        if (i.IssueInitialInvoice) await InitialInvoiceAsync(c, unit.BuildingId);
        _activity.Log("supply_contract", c.Id, "create", $"Contract {c.ContractNo} created for unit {unit.UnitNo}", unit.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
        return c;
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

    public async Task ActivateAsync(SupplyContract c)
    {
        var code = await _lookups.CodeAsync(c.StatusId);
        if (code is not ("draft" or "pending")) throw new BusinessRuleException($"Only draft or pending contracts can be activated (status: {code}).");
        await EnsureUnitFree(c.UnitId, c.Id);
        c.MeterId = await ResolveMeter(c.UnitId, c.MeterId, c.StartDate, c.Id);
        c.StatusId = await _lookups.IdAsync(LookupTypes.ContractStatus, "active");
        c.IsActive = true;
        var unit = await _db.Units.FirstAsync(u => u.Id == c.UnitId);
        await SetUnitStatus(unit, "occupied");
        _activity.Log("supply_contract", c.Id, "status_change", $"Contract {c.ContractNo} activated", unit.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
    }

    public async Task TerminateAsync(SupplyContract c, DateOnly date, string? reason)
    {
        var code = await _lookups.CodeAsync(c.StatusId);
        if (code is "terminated" or "expired") throw new BusinessRuleException($"The contract is already {code}.");
        if (date < c.StartDate) throw new AppException("Termination date cannot be before the start date.");
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

    /// <summary>Returns the meter to link: the given one (assigning it to the unit if free) or the unit's current meter.</summary>
    public async Task<long?> ResolveMeter(long unitId, long? meterId, DateOnly from, long? exceptContractId)
    {
        var current = await _db.MeterAssignments.Where(a => a.UnitId == unitId && a.ToDate == null).Select(a => (long?)a.MeterId).FirstOrDefaultAsync();
        if (meterId == null) return current;
        if (!await _db.Meters.AnyAsync(m => m.Id == meterId)) throw new NotFoundException("Meter", meterId);
        if (current != null && current != meterId) throw new BusinessRuleException("Another meter is assigned to this unit. Unassign it or pick that meter.");
        if (current == null)
        {
            var elsewhere = await _db.MeterAssignments.AnyAsync(a => a.MeterId == meterId && a.ToDate == null);
            if (elsewhere) throw new BusinessRuleException("The meter is assigned to another unit.");
            _db.MeterAssignments.Add(new MeterAssignment { MeterId = meterId.Value, UnitId = unitId, FromDate = from });
        }
        var used = await _db.SupplyContracts.AnyAsync(x => x.MeterId == meterId && x.IsActive && x.Id != exceptContractId && x.UnitId != unitId);
        if (used) throw new BusinessRuleException("The meter is linked to another active contract.");
        return meterId;
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
