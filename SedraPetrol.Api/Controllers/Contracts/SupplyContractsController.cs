using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Gas supply contracts (GasContractDrawer / AddGasContractModal) with fees, balances and lifecycle actions.</summary>
[Route("api/supply-contracts")]
[PermissionModule("contracts")]
public class SupplyContractsController : ApiControllerBase
{
    private ContractService Contracts => HttpContext.RequestServices.GetRequiredService<ContractService>();
    private BalanceQueries Balances => HttpContext.RequestServices.GetRequiredService<BalanceQueries>();
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record UpdateInput(long? MeterId, DateOnly? StartDate, DateOnly? EndDate, bool AutoRenew, short? RenewalPeriodMonths, decimal? DepositAmount,
        bool DepositPaid, DateOnly? DepositDate, string? Terms);
    public record TerminateInput(DateOnly? Date, string? Reason);
    public record RenewInput(DateOnly? NewEndDate, int? Months);
    public record GenerateInput(DateOnly PeriodStart, DateOnly PeriodEnd, bool Issue = true);

    /// <summary>Contracts list (SupplyContractRecord). Filters: buildingId, tenantId, unitId, status, expiringInDays.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? tenantId, [FromQuery] long? unitId,
        [FromQuery] long? branchId, [FromQuery] string? status, [FromQuery] int? expiringInDays, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.SupplyContracts.AsNoTracking().Where(c => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(c.BranchId));
        if (buildingId != null) query = query.Where(c => c.Unit!.BuildingId == buildingId);
        if (tenantId != null) query = query.Where(c => c.TenantId == tenantId);
        if (unitId != null) query = query.Where(c => c.UnitId == unitId);
        if (branchId != null) query = query.Where(c => c.BranchId == branchId);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.ContractStatus, status); query = query.Where(c => c.StatusId == s); }
        if (expiringInDays != null) { var limit = Today.AddDays(expiringInDays.Value); query = query.Where(c => c.IsActive && c.EndDate != null && c.EndDate <= limit); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(c => c.ContractNo.Contains(s) || (c.Tenant!.NameAr ?? "").Contains(s) || (c.Tenant.NameEn ?? "").Contains(s) || c.Unit!.UnitNo.Contains(s));
        }
        var page = await Project(query.OrderByDescending(c => c.IsActive).ThenByDescending(c => c.StartDate).ThenByDescending(c => c.Id)).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    /// <summary>Contract detail with fees, balance and recent invoices.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Project(Db.SupplyContracts.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("SupplyContract", id);
        EnsureBranchAccess(row.C.BranchId);
        var dto = (await ToDtos(new[] { row }))[0];
        var fees = await Db.ContractFees.AsNoTracking().Where(f => f.ContractId == id).OrderBy(f => f.EffectiveFrom).ToListAsync(ct);
        var invoices = await Db.Invoices.AsNoTracking().Where(i => i.ContractId == id).OrderByDescending(i => i.IssueDate).Take(24)
            .Select(i => new { i.Id, i.InvoiceNo, i.IssueDate, i.DueDate, i.TotalAmount, i.StatusId, i.InvoiceTypeId, i.BillingPeriodStart, i.BillingPeriodEnd }).ToListAsync(ct);
        return Ok(new
        {
            contract = dto,
            fees = fees.Select(f => new { f.Id, type = Code(f.FeeTypeId), typeLabel = Label(f.FeeTypeId), cycle = Code(f.CycleId), cycleLabel = Label(f.CycleId),
                                          f.Amount, f.EffectiveFrom, f.EffectiveTo, f.Notes, f.IsActive }),
            invoices = invoices.Select(i => new { i.Id, i.InvoiceNo, i.IssueDate, i.DueDate, amount = i.TotalAmount, status = Code(i.StatusId), type = Code(i.InvoiceTypeId),
                                                  period = i.BillingPeriodStart == null ? null : $"{i.BillingPeriodStart:yyyy-MM-dd} – {i.BillingPeriodEnd:yyyy-MM-dd}" }),
        });
    }

    /// <summary>Create a contract with its fees (AddGasContractModal). Set issueInitialInvoice to bill one-time fees and the deposit.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContractService.ContractInput input)
    {
        if (input.UnitId != null)
            EnsureBranchAccess(await Db.Units.Where(u => u.Id == input.UnitId).Select(u => (long?)u.Building!.BranchId).FirstOrDefaultAsync());
        await using var tx = await Db.Database.BeginTransactionAsync();
        var c = await Contracts.CreateAsync(input);
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new { c.Id, c.ContractNo });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateInput input)
    {
        var c = await Load(id);
        if (input.StartDate != null) c.StartDate = input.StartDate.Value;
        if (input.EndDate != null && input.EndDate < c.StartDate) throw new AppException("End date must be after the start date.");
        c.EndDate = input.EndDate; c.AutoRenew = input.AutoRenew; c.RenewalPeriodMonths = input.RenewalPeriodMonths;
        c.DepositAmount = input.DepositAmount; c.DepositPaid = input.DepositPaid; c.DepositDate = input.DepositDate; c.Terms = input.Terms;
        if (input.MeterId != c.MeterId) c.MeterId = await Contracts.ResolveMeter(c.UnitId, input.MeterId, Today, c.Id);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id)
    {
        var c = await Load(id);
        await using var tx = await Db.Database.BeginTransactionAsync();
        await Contracts.ActivateAsync(c);
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/terminate")]
    public async Task<IActionResult> Terminate(long id, [FromBody] TerminateInput input)
    {
        var c = await Load(id);
        await Contracts.TerminateAsync(c, input.Date ?? Today, input.Reason);
        return NoContent();
    }

    [HttpPost("{id:long}/renew")]
    public async Task<IActionResult> Renew(long id, [FromBody] RenewInput input)
    {
        var c = await Load(id);
        await Contracts.RenewAsync(c, input.NewEndDate, input.Months);
        return Ok(new { c.Id, c.EndDate });
    }

    /// <summary>Only draft contracts can be deleted; active ones must be terminated.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var c = await Load(id);
        if (await Lookups.CodeAsync(c.StatusId) != "draft") throw new BusinessRuleException("Only draft contracts can be deleted. Terminate the contract instead.");
        if (await Db.Invoices.AnyAsync(i => i.ContractId == id)) throw new BusinessRuleException("The contract has invoices.");
        Db.SupplyContracts.Remove(c);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- fees

    [HttpGet("{id:long}/fees")]
    public async Task<IActionResult> Fees(long id)
    {
        await LoadLookups();
        var fees = await Db.ContractFees.AsNoTracking().Where(f => f.ContractId == id).OrderBy(f => f.EffectiveFrom).ToListAsync();
        return Ok(fees.Select(f => new { f.Id, type = Code(f.FeeTypeId), typeLabel = Label(f.FeeTypeId), cycle = Code(f.CycleId), f.Amount, f.EffectiveFrom, f.EffectiveTo, f.Notes, f.IsActive }));
    }

    [HttpPost("{id:long}/fees")]
    public async Task<IActionResult> AddFees(long id, [FromBody] List<ContractService.FeeInput> fees)
    {
        var c = await Load(id);
        await Contracts.AddFeesAsync(c, fees);
        return NoContent();
    }

    /// <summary>Change a fee. To change a price from a date, end the old fee (effectiveTo) and add a new one.</summary>
    [HttpPut("{id:long}/fees/{feeId:long}")]
    public async Task<IActionResult> UpdateFee(long id, long feeId, [FromBody] ContractService.FeeInput input)
    {
        var f = await Db.ContractFees.FirstOrDefaultAsync(x => x.Id == feeId && x.ContractId == id) ?? throw new NotFoundException("ContractFee", feeId);
        if (input.Amount < 0) throw new AppException("Fee amounts cannot be negative.");
        f.Amount = input.Amount;
        if (input.Cycle != null || input.CycleId != null) f.CycleId = (await Lk(LookupTypes.FeeCycle, input.CycleId, input.Cycle, true, "cycle"))!.Value;
        if (input.EffectiveFrom != null) f.EffectiveFrom = input.EffectiveFrom.Value;
        f.EffectiveTo = input.EffectiveTo; f.Notes = input.Notes;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}/fees/{feeId:long}")]
    public async Task<IActionResult> DeleteFee(long id, long feeId)
    {
        var f = await Db.ContractFees.FirstOrDefaultAsync(x => x.Id == feeId && x.ContractId == id) ?? throw new NotFoundException("ContractFee", feeId);
        Db.ContractFees.Remove(f);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Bill the unbilled meter readings (× gas price) and monthly fees of this contract for a period.</summary>
    [HttpPost("{id:long}/generate-invoice")]
    [RequirePermission("invoices.manage")]
    public async Task<IActionResult> GenerateInvoice(long id, [FromBody] GenerateInput input)
    {
        await Load(id);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var inv = await Billing.GenerateConsumptionInvoiceAsync(id, input.PeriodStart, input.PeriodEnd, input.Issue);
        await tx.CommitAsync();
        return inv == null ? Ok(new { created = false, message = "Nothing to bill for this period." }) : Ok(new { created = true, inv.Id, inv.InvoiceNo, inv.TotalAmount });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<SupplyContract> Load(long id)
    {
        var c = await Db.SupplyContracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("SupplyContract", id);
        EnsureBranchAccess(c.BranchId);
        return c;
    }

    private class Row
    {
        public SupplyContract C { get; init; } = null!;
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? UnitNo { get; init; }
        public int? Floor { get; init; }
        public long BuildingId { get; init; }
        public string? BCode { get; init; }
        public string? BEn { get; init; }
        public string? BAr { get; init; }
        public string? MeterNo { get; init; }
    }

    private static IQueryable<Row> Project(IQueryable<SupplyContract> q) => q.Select(c => new Row
    {
        C = c, TenantEn = c.Tenant!.NameEn, TenantAr = c.Tenant.NameAr, UnitNo = c.Unit!.UnitNo, Floor = c.Unit.FloorNo, BuildingId = c.Unit.BuildingId,
        BCode = c.Unit.Building!.Code, BEn = c.Unit.Building.NameEn, BAr = c.Unit.Building.NameAr, MeterNo = c.Meter!.MeterNo,
    });

    private async Task<List<object>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.C.Id).ToList();
        var fees = await Db.ContractFees.AsNoTracking().Where(f => ids.Contains(f.ContractId) && f.IsActive && (f.EffectiveTo == null || f.EffectiveTo >= Today)).ToListAsync();
        var balances = await Balances.ByContractAsync(ids);
        var meterIds = rows.Where(r => r.C.MeterId != null).Select(r => r.C.MeterId!.Value).ToList();
        var lastReadings = await Db.MeterReadings.AsNoTracking().Where(r => meterIds.Contains(r.MeterId))
            .GroupBy(r => r.MeterId).Select(g => g.OrderByDescending(r => r.ReadingDate).First()).ToListAsync();
        var unbilled = await Db.MeterReadings.AsNoTracking().Where(r => meterIds.Contains(r.MeterId) && r.InvoiceId == null)
            .GroupBy(r => r.MeterId).Select(g => new { g.Key, M3 = g.Sum(r => r.Consumption ?? 0) }).ToDictionaryAsync(x => x.Key, x => x.M3);
        var aed = "AED";

        return rows.Select(r =>
        {
            var cf = fees.Where(f => f.ContractId == r.C.Id).ToList();
            decimal? Fee(string type) => cf.Where(f => Code(f.FeeTypeId) == type).OrderByDescending(f => f.EffectiveFrom).Select(f => (decimal?)f.Amount).FirstOrDefault();
            var b = balances.GetValueOrDefault(r.C.Id) ?? new BalanceQueries.Balance(0, 0, 0);
            var lr = r.C.MeterId == null ? null : lastReadings.FirstOrDefault(x => x.MeterId == r.C.MeterId);
            return (object)new
            {
                r.C.Id, r.C.ContractNo, tenantId = r.C.TenantId, tenant = Loc(r.TenantEn, r.TenantAr), unitId = r.C.UnitId, unit = r.UnitNo, floor = r.Floor,
                buildingId = r.BuildingId, buildingCode = r.BCode, building = Loc(r.BEn, r.BAr), meterId = r.C.MeterId, meter = r.MeterNo,
                r.C.StartDate, r.C.EndDate, status = Code(r.C.StatusId), statusLabel = Label(r.C.StatusId), r.C.IsActive,
                monthlyFee = cf.Where(f => Code(f.CycleId) == "monthly").Sum(f => f.Amount), currency = aed,
                gasUnitPrice = Fee("gas_price"), connectionFee = Fee("connection"), disconnectionFee = Fee("disconnection"),
                r.C.DepositAmount, r.C.DepositPaid, r.C.DepositDate,
                lastReading = lr?.ReadingValue, lastReadDate = lr?.ReadingDate, balanceM3 = r.C.MeterId == null ? 0 : unbilled.GetValueOrDefault(r.C.MeterId.Value),
                balanceAmount = b.Outstanding, overdueAmount = b.Overdue, invoiced = b.Invoiced, paid = b.Paid, paymentStatus = b.PaymentStatus,
                r.C.AutoRenew, renewalPeriod = r.C.RenewalPeriodMonths, r.C.Terms, r.C.TerminatedAt,
            };
        }).ToList();
    }
}

[Route("api/contract-fees")]
[PermissionModule("contracts")]
public class ContractFeesController : CrudController<ContractFee> { }
