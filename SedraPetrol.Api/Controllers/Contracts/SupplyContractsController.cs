using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    public record UpdateInput(DateOnly? StartDate, DateOnly? EndDate, bool AutoRenew, short? RenewalPeriodMonths, decimal? DepositAmount,
        bool DepositPaid, DateOnly? DepositDate, string? Terms);
    public record TerminateInput(DateOnly? Date, string? Reason, decimal? ClosingReading, bool IssueFinalInvoice = false);
    public record ActivateInput(decimal? OpeningReading, DateTimeOffset? OpeningReadingDate);
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
            query = query.Where(c => c.ContractNo.Contains(s) || (c.Tenant!.Party!.NameAr ?? "").Contains(s) || (c.Tenant.Party!.NameEn ?? "").Contains(s) || c.Unit!.UnitNo.Contains(s));
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
        var terms = await Db.ContractTerms.AsNoTracking().Where(t => t.ContractType == "supply" && t.ContractId == id).OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { t.Id, templateId = t.SourceId, t.Category, text = t.TextAr, t.IsCustom, t.IsModified }).ToListAsync(ct);
        return Ok(new
        {
            contract = dto,
            terms,
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
        await Db.SaveChangesAsync();
        return NoContent();
    }

    public record TermsInput(List<ContractService.TermItemInput> Items);

    /// <summary>Replace the terms &amp; conditions of this contract only (the Settings list is not changed).</summary>
    [HttpPut("{id:long}/terms")]
    public async Task<IActionResult> SetTerms(long id, [FromBody] TermsInput input)
    {
        var c = await Load(id);
        await Contracts.SetTermsAsync(c.Id, input.Items ?? new());
        Activity.Log("supply_contract", c.Id, "update", $"Terms updated – {c.ContractNo}", null, c.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/activate")]
    public async Task<IActionResult> Activate(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ActivateInput? input)
    {
        var c = await Load(id);
        await using var tx = await Db.Database.BeginTransactionAsync();
        await Contracts.ActivateAsync(c, input?.OpeningReading, input?.OpeningReadingDate);
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/terminate")]
    public async Task<IActionResult> Terminate(long id, [FromBody] TerminateInput input)
    {
        var c = await Load(id);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var invoiceId = await Contracts.TerminateAsync(c, input.Date ?? Today, input.Reason, input.ClosingReading, input.IssueFinalInvoice);
        await tx.CommitAsync();
        return Ok(new { c.Id, invoiceId });
    }

    /// <summary>
    /// Tenancy consumption broken down per meter (a replacement during the tenancy gives one row per meter):
    /// start = opening / installation reading, end = latest reading, consumption = end − start on that meter's own counter.
    /// </summary>
    [HttpGet("{id:long}/consumption")]
    public async Task<IActionResult> Consumption(long id, CancellationToken ct)
    {
        await LoadLookups();
        var c = await Load(id);
        var readings = await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId == id).OrderBy(r => r.ReadingDate).ThenBy(r => r.Id)
            .Select(r => new { r.Id, r.MeterId, r.Meter!.MeterNo, r.ReadingDate, r.ReadingValue, r.Consumption, r.ReadingTypeId, r.InvoiceId, r.Notes }).ToListAsync(ct);
        var meters = readings.GroupBy(r => new { r.MeterId, r.MeterNo }).Select(g =>
        {
            var first = g.First(); var last = g.Last();
            var start = first.ReadingValue - (first.Consumption ?? 0);
            return new
            {
                meterId = g.Key.MeterId, meterNo = g.Key.MeterNo, from = first.ReadingDate, to = last.ReadingDate,
                startReading = start, startType = Code(first.ReadingTypeId), endReading = last.ReadingValue, endType = Code(last.ReadingTypeId),
                consumption = g.Sum(r => r.Consumption ?? 0), billed = g.Where(r => r.InvoiceId != null).Sum(r => r.Consumption ?? 0),
            };
        }).ToList();
        return Ok(new
        {
            contractId = c.Id, c.ContractNo, c.StartDate, c.EndDate, meters, total = meters.Sum(m => m.consumption), billed = meters.Sum(m => m.billed),
            unbilled = meters.Sum(m => m.consumption - m.billed),
            readings = readings.Select(r => new { r.Id, r.MeterId, meterNo = r.MeterNo, date = r.ReadingDate, value = r.ReadingValue, r.Consumption,
                                                   type = Code(r.ReadingTypeId), typeLabel = Label(r.ReadingTypeId), billed = r.InvoiceId != null, r.Notes }),
        });
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

    // ------------------------------------------------------------------ security deposit at move-out

    public record SettleDepositInput(DateOnly? Date, bool ApplyToInvoices = true, bool Refund = true, string? RefundMethod = null, string? Reference = null, string? Notes = null);

    private async Task<long?> DepositAccountIdAsync(long branchId)
    {
        var companyId = await Db.Branches.Where(b => b.Id == branchId).Select(b => b.CompanyId).FirstAsync();
        return await Db.AccountMappings.Where(m => m.CompanyId == companyId && m.SourceType == MappingSources.Deposit && m.IsActive)
            .Select(m => m.CreditAccountId).FirstOrDefaultAsync();
    }

    /// <summary>Deposit still held for this contract = credits − debits on the customer deposits account tagged with the contract.</summary>
    private async Task<decimal> DepositHeldAsync(SupplyContract c, long depositAccountId) =>
        await Db.JournalLines.Where(l => l.AccountId == depositAccountId && l.ContractId == c.Id && l.JournalEntry!.StatusId != null
                && l.JournalEntry.Status!.Code == "posted")
            .SumAsync(l => (decimal?)((l.Credit ?? 0) - (l.Debit ?? 0))) ?? 0;

    /// <summary>The security deposit of the contract: held amount and what the customer still owes.</summary>
    [HttpGet("{id:long}/deposit")]
    public async Task<IActionResult> Deposit(long id)
    {
        var c = await Load(id);
        var acc = await DepositAccountIdAsync(c.BranchId);
        var held = acc == null ? 0 : await DepositHeldAsync(c, acc.Value);
        var open = await Billing.OpenInvoicesAsync(c.TenantId);
        var owed = open.Sum(o => o.Balance);
        return Ok(new { c.DepositAmount, held, owed, canSettle = !c.IsActive && held > 0, applicable = Math.Min(held, owed), refundable = Math.Max(0, held - Math.Min(held, owed)) });
    }

    /// <summary>
    /// Settles the security deposit when the tenancy ends: the held deposit is applied to the customer's open invoices
    /// (Dr Customer deposits / Cr the customer's account — a receipt allocated oldest first) and the remainder is refunded
    /// (Dr Customer deposits / Cr cash or bank by refund method).
    /// </summary>
    [HttpPost("{id:long}/settle-deposit")]
    [RequirePermission("invoices.manage")]
    public async Task<IActionResult> SettleDeposit(long id, [FromBody] SettleDepositInput input)
    {
        var c = await Load(id);
        if (c.IsActive) throw new BusinessRuleException("The deposit is settled after the contract is terminated.");
        var acc = await DepositAccountIdAsync(c.BranchId) ?? throw new BusinessRuleException("Map the customer deposits account (Accounting > mappings).");
        var held = await DepositHeldAsync(c, acc);
        if (held <= 0) throw new BusinessRuleException($"No deposit is held for contract {c.ContractNo}.");
        var date = input.Date ?? Today;
        var parties = HttpContext.RequestServices.GetRequiredService<PartyService>();
        var ar = await parties.AccountIdAsync(c.TenantId) ?? throw new BusinessRuleException("The customer has no ledger account.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        decimal applied = 0, refunded = 0; string? receiptNo = null;
        if (input.ApplyToInvoices)
        {
            var owed = (await Billing.OpenInvoicesAsync(c.TenantId)).Sum(o => o.Balance);
            applied = BillingService.R2(Math.Min(held, owed));
            if (applied > 0)
            {
                var p = new Payment
                {
                    BranchId = c.BranchId, ReceiptNo = await Numbers.NextAsync(NumberPrefixes.Receipt, date: date), TenantId = c.TenantId,
                    PaymentDate = date, Amount = applied, ReferenceNo = $"DEPOSIT {c.ContractNo}", ReceivedBy = Me.UserId,
                    CurrencyId = await Db.Currencies.Where(x => x.Code == "AED").Select(x => (long?)x.Id).FirstOrDefaultAsync(),
                    Notes = input.Notes ?? $"Deposit of contract {c.ContractNo} applied to open invoices",
                    StatusId = await Lk(LookupTypes.PaymentStatus, null, "completed"),
                };
                Db.Payments.Add(p);
                await Db.SaveChangesAsync();
                await Billing.AllocateAsync(p, null);
                p.JournalEntryId = (await Posting.CreateEntryAsync(c.BranchId, date, $"Deposit applied – {c.ContractNo} ({p.ReceiptNo})", "deposit_offset", p.Id, new[]
                {
                    new PostingService.Line(acc, applied, 0, $"Deposit applied – {c.ContractNo}", c.BuildingId, c.UnitId, c.TenantId, null, c.Id),
                    new PostingService.Line(ar, 0, applied, $"Deposit applied – {p.ReceiptNo}", c.BuildingId, c.UnitId, c.TenantId, null, c.Id),
                }, true)).Id;
                await Db.SaveChangesAsync();
                receiptNo = p.ReceiptNo;
            }
        }
        if (input.Refund)
        {
            refunded = BillingService.R2(held - applied);
            if (refunded > 0)
            {
                var companyId = await Db.Branches.Where(b => b.Id == c.BranchId).Select(b => b.CompanyId).FirstAsync();
                var methodId = await Lk(LookupTypes.PaymentMethod, null, input.RefundMethod ?? "cash", field: "refundMethod");
                var cash = await Db.AccountMappings.Where(m => m.CompanyId == companyId && m.SourceType == MappingSources.Payment && m.IsActive && m.SourceKeyId == methodId)
                    .Select(m => m.DebitAccountId).FirstOrDefaultAsync() ?? throw new BusinessRuleException("Map the cash / bank account of the refund method.");
                await Posting.CreateEntryAsync(c.BranchId, date, $"Deposit refund – {c.ContractNo}{(input.Reference == null ? "" : " / " + input.Reference)}", "deposit_refund", c.Id, new[]
                {
                    new PostingService.Line(acc, refunded, 0, $"Deposit refund – {c.ContractNo}", c.BuildingId, c.UnitId, c.TenantId, null, c.Id),
                    new PostingService.Line(cash, 0, refunded, $"Deposit refund – {c.ContractNo}", c.BuildingId, c.UnitId, c.TenantId, null, c.Id),
                }, true);
            }
        }
        Activity.Log("supply_contract", c.Id, "deposit", $"Deposit of {c.ContractNo}: applied {applied:0.##}, refunded {refunded:0.##}", c.BuildingId, c.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { held, applied, refunded, receiptNo, remaining = held - applied - refunded });
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
        public long? MeterId { get; init; }
        public string? MeterNo { get; init; }
    }

    /// <summary>The meter shown on a contract is the unit's currently installed meter (meters belong to units, not tenants).</summary>
    private IQueryable<Row> Project(IQueryable<SupplyContract> q) => q.Select(c => new Row
    {
        C = c, TenantEn = c.Tenant!.Party!.NameEn, TenantAr = c.Tenant.Party!.NameAr, UnitNo = c.Unit!.UnitNo, Floor = c.Unit.FloorNo, BuildingId = c.Unit.BuildingId,
        BCode = c.Unit.Building!.Code, BEn = c.Unit.Building.NameEn, BAr = c.Unit.Building.NameAr,
        MeterId = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => (long?)a.MeterId).FirstOrDefault(),
        MeterNo = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => a.Meter!.MeterNo).FirstOrDefault(),
    });

    private async Task<List<object>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.C.Id).ToList();
        var fees = await Db.ContractFees.AsNoTracking().Where(f => ids.Contains(f.ContractId) && f.IsActive && (f.EffectiveTo == null || f.EffectiveTo >= Today)).ToListAsync();
        var balances = await Balances.ByContractAsync(ids);
        var contractReadings = await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId != null && ids.Contains(r.ContractId.Value))
            .Select(r => new { ContractId = r.ContractId!.Value, r.ReadingDate, r.ReadingValue, r.Consumption, r.InvoiceId, r.ReadingTypeId }).ToListAsync();
        var openingType = await Lookups.IdOrNullAsync(LookupTypes.ReadingType, "opening");
        var closingType = await Lookups.IdOrNullAsync(LookupTypes.ReadingType, "closing");
        var aed = "AED";

        return rows.Select(r =>
        {
            var cf = fees.Where(f => f.ContractId == r.C.Id).ToList();
            decimal? Fee(string type) => cf.Where(f => Code(f.FeeTypeId) == type).OrderByDescending(f => f.EffectiveFrom).Select(f => (decimal?)f.Amount).FirstOrDefault();
            var b = balances.GetValueOrDefault(r.C.Id) ?? new BalanceQueries.Balance(0, 0, 0);
            var cr = contractReadings.Where(x => x.ContractId == r.C.Id).OrderBy(x => x.ReadingDate).ToList();
            var lr = cr.LastOrDefault();
            var opening = cr.FirstOrDefault(x => x.ReadingTypeId == openingType);
            var closing = cr.LastOrDefault(x => x.ReadingTypeId == closingType);
            return (object)new
            {
                r.C.Id, r.C.ContractNo, tenantId = r.C.TenantId, tenant = Loc(r.TenantEn, r.TenantAr), unitId = r.C.UnitId, unit = r.UnitNo, floor = r.Floor,
                buildingId = r.BuildingId, buildingCode = r.BCode, building = Loc(r.BEn, r.BAr), meterId = r.MeterId, meter = r.MeterNo,
                r.C.StartDate, r.C.EndDate, status = Code(r.C.StatusId), statusLabel = Label(r.C.StatusId), r.C.IsActive,
                monthlyFee = cf.Where(f => Code(f.CycleId) == "monthly").Sum(f => f.Amount), currency = aed,
                gasUnitPrice = Fee("gas_price"), connectionFee = Fee("connection"), disconnectionFee = Fee("disconnection"),
                r.C.DepositAmount, r.C.DepositPaid, r.C.DepositDate,
                lastReading = lr?.ReadingValue, lastReadDate = lr?.ReadingDate, balanceM3 = cr.Where(x => x.InvoiceId == null).Sum(x => x.Consumption ?? 0),
                openingReading = opening?.ReadingValue, openingReadDate = opening?.ReadingDate, closingReading = closing?.ReadingValue, closingReadDate = closing?.ReadingDate,
                consumption = cr.Sum(x => x.Consumption ?? 0),
                balanceAmount = b.Outstanding, overdueAmount = b.Overdue, invoiced = b.Invoiced, paid = b.Paid, paymentStatus = b.PaymentStatus,
                r.C.AutoRenew, renewalPeriod = r.C.RenewalPeriodMonths, r.C.Terms, r.C.TerminatedAt,
            };
        }).ToList();
    }
}

[Route("api/contract-fees")]
[PermissionModule("contracts")]
public class ContractFeesController : CrudController<ContractFee> { }
