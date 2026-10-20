using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Tenant invoices (A/R): list, detail, manual invoices, consumption billing run, issue, cancel, overdue.</summary>
[Route("api/invoices")]
[PermissionModule("invoices")]
public class InvoicesController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record ItemInput(string? ItemType, long? ItemTypeId, string? Description, decimal Quantity, string? Uom, decimal UnitPrice, decimal? VatRate, long? TaxRateId = null);
    public record CreateInput(long TenantId, long? ContractId, long? BuildingId, string? Type, DateOnly? IssueDate, DateOnly? DueDate,
        DateOnly? PeriodStart, DateOnly? PeriodEnd, string? Notes, List<ItemInput> Items, bool Issue = true, List<InstallmentInput>? Installments = null);
    /// <summary>One instalment row. Id is sent back for existing rows.</summary>
    public record InstallmentInput(long? Id, DateOnly DueDate, decimal Amount, string? Notes);
    public record ScheduleInput(List<InstallmentInput> Rows, string? Reason);
    public record InstallmentStatusInput(string Status, string? Reason);
    /// <param name="ReleasePayments">Cancel a (partly) paid invoice: its payment allocations are released and the money stays on the customer's account as credit.</param>
    public record CancelInput(string? Reason, bool ReleasePayments = false);
    public record RunInput(long? BuildingId, long? BranchId, List<long>? ContractIds, DateOnly PeriodStart, DateOnly PeriodEnd, bool Issue = true);

    /// <summary>Invoices. Filters: tenantId, buildingId, contractId, status, type, from, to (issue date), overdueOnly, openOnly.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? tenantId, [FromQuery] long? buildingId, [FromQuery] long? contractId,
        [FromQuery] long? branchId, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] bool overdueOnly = false, [FromQuery] bool openOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Filter(tenantId, buildingId, contractId, branchId, from, to);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, status); query = query.Where(i => i.StatusId == s); }
        if (!string.IsNullOrEmpty(type)) { var s = await Lookups.IdOrNullAsync(LookupTypes.InvoiceType, type); query = query.Where(i => i.InvoiceTypeId == s); }
        var open = await Billing.InvoiceStatusIdsAsync("pending", "partial", "overdue");
        if (openOnly || overdueOnly) query = query.Where(i => i.StatusId != null && open.Contains(i.StatusId.Value));
        if (overdueOnly) query = query.Where(i => i.DueDate < Today);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(i => i.InvoiceNo.Contains(q.Search) || (i.Tenant!.Party!.NameAr ?? "").Contains(q.Search) || (i.Tenant.Party!.NameEn ?? "").Contains(q.Search));
        query = q.Sort?.ToLowerInvariant() switch
        {
            "duedate" => q.Desc ? query.OrderByDescending(i => i.DueDate) : query.OrderBy(i => i.DueDate),
            "amount" => q.Desc ? query.OrderByDescending(i => i.TotalAmount) : query.OrderBy(i => i.TotalAmount),
            _ => q.Desc ? query.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id) : query.OrderBy(i => i.IssueDate).ThenBy(i => i.Id),
        };
        var page = await Project(query).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Project(Db.Invoices.AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Invoice", id);
        EnsureBranchAccess(row.I.BranchId);
        var items = await Db.InvoiceItems.AsNoTracking().Where(i => i.InvoiceId == id).OrderBy(i => i.Id).ToListAsync(ct);
        var payments = await Db.PaymentAllocations.AsNoTracking().Where(a => a.InvoiceId == id)
            .Select(a => new { a.Id, a.PaymentId, a.Payment!.ReceiptNo, a.Payment.PaymentDate, a.Amount, a.Payment.PaymentMethodId, a.Payment.StatusId, a.Payment.ReferenceNo }).ToListAsync(ct);
        var journal = row.I.JournalEntryId == null ? null : await Db.JournalEntries.AsNoTracking().Where(j => j.Id == row.I.JournalEntryId).Select(j => new { j.Id, j.JournalNo }).FirstOrDefaultAsync(ct);
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        var schedule = (await Billing.InstallmentsAsync(new[] { id })).GetValueOrDefault(id) ?? new();
        var creditNotes = await Db.CreditNoteAllocations.AsNoTracking().Where(a => a.InvoiceId == id)
            .Select(a => new { a.CreditNoteId, a.CreditNote!.CreditNoteNo, a.CreditNote.IssueDate, a.CreditNote.Reason, a.CreditNote.Status, a.CreditNote.TotalAmount, applied = a.Amount, a.CreditNote.InvoiceId })
            .ToListAsync(ct);
        var ownCreditNotes = await Db.CreditNotes.AsNoTracking().Where(c => c.InvoiceId == id)
            .Select(c => new { c.Id, c.CreditNoteNo, c.IssueDate, c.Reason, c.Status, c.Subtotal, c.VatAmount, c.TotalAmount }).ToListAsync(ct);
        var creditedLines = await Db.CreditNoteLines.AsNoTracking().Where(l => l.InvoiceItemId != null && l.CreditNote!.InvoiceId == id && l.CreditNote.Status == "posted")
            .GroupBy(l => l.InvoiceItemId!.Value).Select(g => new { g.Key, Amt = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Amt, ct);
        return Ok(new
        {
            invoice = (await ToDtos(new[] { row }))[0],
            installments = schedule,
            creditNotes = ownCreditNotes,
            creditApplications = creditNotes,
            creditedByItem = creditedLines,
            customerCredit = (await Billing.CreditSourcesAsync(row.I.TenantId)).Sum(x => x.Free),
            items = items.Select(i => new { i.Id, type = Code(i.ItemTypeId), typeLabel = Label(i.ItemTypeId), i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate, i.TaxRateId, i.Amount,
                                            vat = BillingService.R2((i.Amount ?? 0) * (i.VatRate ?? 0) / 100), i.MeterReadingId }),
            payments = payments.Select(p => new { p.Id, p.PaymentId, p.ReceiptNo, date = p.PaymentDate, p.Amount, method = Code(p.PaymentMethodId), status = Code(p.StatusId), reference = p.ReferenceNo }),
            journal,
            company,
        });
    }

    /// <summary>Create a manual invoice (NewInvoiceModal). issue=false keeps it as a draft.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == input.TenantId) ?? throw new NotFoundException("Tenant", input.TenantId);
        EnsureBranchAccess(tenant.BranchId);
        var buildingId = input.BuildingId;
        if (input.ContractId != null)
        {
            var c = await Db.SupplyContracts.Include(x => x.Unit).FirstOrDefaultAsync(x => x.Id == input.ContractId && x.TenantId == tenant.Id)
                    ?? throw new AppException("The contract does not belong to this tenant.");
            buildingId ??= c.Unit!.BuildingId;
        }
        if (input.Issue && !Me.HasPermission("invoices.post")) throw new AppException("Missing permission: invoices.post", 403);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var inv = await Billing.CreateInvoiceAsync(new InvoiceDraft(tenant.BranchId, tenant.Id, buildingId, input.ContractId, input.Type, null,
            input.IssueDate ?? Today, input.DueDate, input.PeriodStart, input.PeriodEnd, null, input.Notes,
            input.Items.Select(i => new InvoiceLineInput(i.ItemType, i.ItemTypeId, i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate, null, i.TaxRateId)).ToList(), input.Issue));
        Activity.Log("invoice", inv.Id, "create", $"Invoice {inv.InvoiceNo} ({inv.TotalAmount:0.##})", buildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        if (input.Installments is { Count: > 0 }) await SaveSchedule(inv, input.Installments, "created with the invoice");
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = inv.Id }, new { inv.Id, inv.InvoiceNo, inv.TotalAmount });
    }

    /// <summary>Replace the lines of a draft invoice.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateDraft(long id, [FromBody] CreateInput input)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be edited.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var old = await Db.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync();
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        Db.InvoiceItems.RemoveRange(old);
        Db.Invoices.Remove(inv);
        await Db.SaveChangesAsync();
        var created = await Billing.CreateInvoiceAsync(new InvoiceDraft(inv.BranchId, input.TenantId, input.BuildingId ?? inv.BuildingId, input.ContractId ?? inv.ContractId, input.Type, null,
            input.IssueDate ?? inv.IssueDate, input.DueDate, input.PeriodStart, input.PeriodEnd, inv.CurrencyId, input.Notes,
            input.Items.Select(i => new InvoiceLineInput(i.ItemType, i.ItemTypeId, i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate, null, i.TaxRateId)).ToList(), false));
        await tx.CommitAsync();
        return Ok(new { created.Id, created.InvoiceNo, replacedId = id });
    }

    /// <summary>Issue a draft invoice (creates the journal entry).</summary>
    [HttpPost("{id:long}/issue")]
    [RequirePermission("invoices.post")]
    public async Task<IActionResult> Issue(long id)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be issued.");
        await Billing.IssueAsync(inv);
        return NoContent();
    }

    /// <summary>Cancel an invoice: reverses its journal entry and releases its meter readings. Not allowed when payments are applied.</summary>
    [HttpPost("{id:long}/cancel")]
    [RequirePermission("invoices.cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) == "cancelled") throw new BusinessRuleException("The invoice is already cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the cancellation reason.");
        if (await Db.CreditNotes.AnyAsync(c => c.InvoiceId == id && c.Status == "posted"))
            throw new BusinessRuleException("Credit notes were issued against this invoice. Cancel them first, or issue a credit note for the rest instead of cancelling.");
        var paid = (await Billing.PaidByInvoiceAsync(new[] { id })).GetValueOrDefault(id);
        if (paid > 0 && !input.ReleasePayments)
            throw new BusinessRuleException("Payments are applied to this invoice. Confirm releasing them to the customer's credit, or cancel the payments first.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (paid > 0)
        {
            // the receipts stay valid; their allocation to this invoice is removed, so the amount becomes unallocated credit of the customer
            var allocs = await Db.PaymentAllocations.Where(a => a.InvoiceId == id).ToListAsync();
            Db.PaymentAllocations.RemoveRange(allocs);
            Activity.Log("invoice", inv.Id, "payment_release", $"Invoice {inv.InvoiceNo}: {paid:0.##} released to customer credit", inv.BuildingId, inv.BranchId);
        }
        // AMC instalment invoiced by this invoice → can be issued again
        var pendingAmc = await Lookups.IdOrNullAsync(LookupTypes.AmcPaymentStatus, "pending");
        await Db.AmcPaymentSchedules.Where(x => x.InvoiceId == id).ForEachAsync(x => { x.InvoiceId = null; x.StatusId = pendingAmc; });
        if (inv.JournalEntryId != null) await Posting.ReverseAsync(inv.JournalEntryId.Value, Today, $"Invoice {inv.InvoiceNo} cancelled");
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        inv.StatusId = await LkId(LookupTypes.InvoiceStatus, "cancelled");
        inv.Notes = $"{inv.Notes}\nCancelled: {input.Reason}".Trim();
        Activity.Log("invoice", inv.Id, "status_change", $"Invoice {inv.InvoiceNo} cancelled: {input.Reason}", inv.BuildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { inv.Id, releasedToCredit = paid });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteDraft(long id)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be deleted; cancel issued invoices instead.");
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        Db.InvoiceItems.RemoveRange(await Db.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync());
        Db.Invoices.Remove(inv);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ instalment schedule (BRD §7.2)

    /// <summary>Instalments of an invoice with paid / outstanding / status (settlement applied oldest first).</summary>
    [HttpGet("{id:long}/installments")]
    public async Task<IActionResult> Installments(long id)
    {
        var inv = await Load(id);
        var rows = (await Billing.InstallmentsAsync(new[] { id })).GetValueOrDefault(id) ?? new();
        var settled = (await Billing.PaidByInvoiceAsync(new[] { id })).GetValueOrDefault(id);
        return Ok(new { invoiceId = id, total = inv.TotalAmount ?? 0, settled, scheduled = rows.Where(r => r.Status != "cancelled").Sum(r => r.Amount), rows });
    }

    /// <summary>
    /// Create or replace the schedule of an issued invoice. The rows must add up to the invoice total. Instalments that already
    /// received money (fully or partly) must be sent back unchanged (same id, date and amount).
    /// </summary>
    [HttpPut("{id:long}/installments")]
    public async Task<IActionResult> SetInstallments(long id, [FromBody] ScheduleInput input)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) is "draft" or "cancelled") throw new BusinessRuleException("Instalments can be scheduled on issued invoices only.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        await SaveSchedule(inv, input.Rows, input.Reason);
        await tx.CommitAsync();
        return Ok((await Billing.InstallmentsAsync(new[] { id })).GetValueOrDefault(id) ?? new());
    }

    /// <summary>Put an instalment on hold, back to pending, or cancel it (reason required).</summary>
    [HttpPatch("{id:long}/installments/{installmentId:long}/status")]
    public async Task<IActionResult> SetInstallmentStatus(long id, long installmentId, [FromBody] InstallmentStatusInput input)
    {
        var inv = await Load(id);
        var row = await Db.InvoiceInstallments.FirstOrDefaultAsync(x => x.Id == installmentId && x.InvoiceId == id) ?? throw new NotFoundException("Installment", installmentId);
        if (input.Status is not ("pending" or "on_hold" or "cancelled")) throw new AppException("Status must be pending, on_hold or cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the reason.");
        var view = (await Billing.InstallmentsAsync(new[] { id }))[id].First(x => x.Id == installmentId);
        if (input.Status == "cancelled" && view.Paid > 0) throw new BusinessRuleException("This instalment already received money and cannot be cancelled.");
        var old = row.Status;
        row.Status = input.Status;
        row.Notes = $"{row.Notes}\n{DateTime.Today:yyyy-MM-dd} {input.Status}: {input.Reason}".Trim();
        Activity.Log("invoice", inv.Id, "installment", $"Invoice {inv.InvoiceNo} instalment {row.SeqNo}: {old} → {input.Status} ({input.Reason})", inv.BuildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await Billing.RecalcInvoicesAsync(new[] { id });
        return NoContent();
    }

    private async Task SaveSchedule(Invoice inv, List<InstallmentInput> rows, string? reason)
    {
        if (rows.Count == 0) throw new AppException("Add at least one instalment.");
        if (rows.Any(r => r.Amount <= 0)) throw new AppException("Instalment amounts must be greater than zero.");
        if (rows.Any(r => r.DueDate < inv.IssueDate)) throw new AppException("An instalment cannot be due before the invoice date.");
        var total = inv.TotalAmount ?? 0;
        var sum = rows.Sum(r => r.Amount);
        if (Math.Abs(sum - total) > 0.005m) throw new AppException($"The instalments add up to {sum:0.00} but the invoice total is {total:0.00}.");

        var existing = await Db.InvoiceInstallments.Where(x => x.InvoiceId == inv.Id).OrderBy(x => x.SeqNo).ToListAsync();
        if (existing.Count > 0)
        {
            var views = (await Billing.InstallmentsAsync(new[] { inv.Id })).GetValueOrDefault(inv.Id) ?? new();
            foreach (var v in views.Where(v => v.Paid > 0))
            {
                var keep = rows.FirstOrDefault(r => r.Id == v.Id);
                if (keep == null || keep.Amount != v.Amount || keep.DueDate != v.DueDate)
                    throw new BusinessRuleException($"Instalment {v.SeqNo} already received money; keep it unchanged ({v.DueDate:yyyy-MM-dd}, {v.Amount:0.00}).");
            }
            // free the (invoice, seq) unique key first: rows are re-used in place, extra rows are soft-deleted with a parked seq no
            foreach (var e in existing) e.SeqNo = (short)(-(e.Id % 32000) - 1);
            await Db.SaveChangesAsync();
        }
        var ordered = rows.OrderBy(r => r.DueDate).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var r = ordered[i];
            var row = i < existing.Count ? existing[i] : null;
            if (row == null) { row = new InvoiceInstallment { InvoiceId = inv.Id }; Db.InvoiceInstallments.Add(row); }
            row.SeqNo = (short)(i + 1);
            row.DueDate = r.DueDate;
            row.Amount = BillingService.R2(r.Amount);
            row.Notes = r.Notes;
            row.Status = "pending";
        }
        if (existing.Count > ordered.Count) Db.InvoiceInstallments.RemoveRange(existing.Skip(ordered.Count));
        // the invoice is fully due on its last instalment
        inv.DueDate = rows.Max(r => r.DueDate);
        Activity.Log("invoice", inv.Id, "installments", $"Invoice {inv.InvoiceNo}: {rows.Count} instalment(s) scheduled{(string.IsNullOrWhiteSpace(reason) ? "" : $" — {reason}")}", inv.BuildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await Billing.RecalcInvoicesAsync(new[] { inv.Id });
    }

    /// <summary>
    /// Billing run: creates gas-consumption invoices for every active contract of a building / branch (or the given contracts)
    /// from the unbilled readings and monthly fees. Each contract is processed independently.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] RunInput input)
    {
        if (input.PeriodEnd < input.PeriodStart) throw new AppException("Period end must be after the start.");
        var contracts = Db.SupplyContracts.Where(c => c.IsActive);
        if (input.ContractIds is { Count: > 0 }) contracts = contracts.Where(c => input.ContractIds.Contains(c.Id));
        if (input.BuildingId != null) contracts = contracts.Where(c => c.Unit!.BuildingId == input.BuildingId);
        if (input.BranchId != null) contracts = contracts.Where(c => c.BranchId == input.BranchId);
        if (!Me.IsAdmin && Me.BranchIds.Count > 0) contracts = contracts.Where(c => Me.BranchIds.Contains(c.BranchId));
        var list = await contracts.Select(c => new { c.Id, c.ContractNo }).ToListAsync();
        var results = new List<RunResult>();
        foreach (var c in list)
        {
            await using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                var inv = await Billing.GenerateConsumptionInvoiceAsync(c.Id, input.PeriodStart, input.PeriodEnd, input.Issue);
                await tx.CommitAsync();
                results.Add(new RunResult(c.Id, c.ContractNo, inv != null, inv?.Id, inv?.InvoiceNo, inv?.TotalAmount, null));
            }
            catch (AppException ex)
            {
                await tx.RollbackAsync();
                Db.ChangeTracker.Clear();
                results.Add(new RunResult(c.Id, c.ContractNo, false, null, null, null, ex.Message));
            }
        }
        return Ok(new { contracts = list.Count, created = results.Count(r => r.Created), results });
    }

    public record RunResult(long ContractId, string ContractNo, bool Created, long? InvoiceId, string? InvoiceNo, decimal? Amount, string? Error);

    /// <summary>Totals by status and by month (charts on the invoices page / dashboard).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? buildingId, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        var query = Filter(null, buildingId, null, branchId, f, t);
        var byStatus = await query.GroupBy(i => i.StatusId).Select(g => new { g.Key, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount ?? 0) }).ToListAsync();
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled");
        var draft = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "draft");
        var byMonth = await query.Where(i => i.StatusId != cancelled && i.StatusId != draft)
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount ?? 0), Vat = g.Sum(i => i.VatAmount ?? 0) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
        return Ok(new
        {
            from = f, to = t,
            byStatus = byStatus.Select(s => new { status = Code(s.Key), label = Label(s.Key), s.Count, s.Amount }),
            byMonth = byMonth.Select(m => new { month = $"{m.Year}-{m.Month:00}", m.Count, m.Amount, m.Vat }),
            total = byStatus.Where(s => s.Key != cancelled && s.Key != draft).Sum(s => s.Amount),
        });
    }

    /// <summary>Overdue invoices with days overdue and aging bucket (Overdue page). groupBy=tenant returns one row per tenant.</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? branchId, [FromQuery] string? groupBy)
    {
        await LoadLookups();
        var open = await Billing.InvoiceStatusIdsAsync("pending", "partial", "overdue");
        var query = Filter(null, buildingId, null, branchId, null, null).Where(i => i.StatusId != null && open.Contains(i.StatusId.Value) && i.DueDate < Today);
        var rows = await Project(query.OrderBy(i => i.DueDate)).Take(5000).ToListAsync();
        var dtos = await ToDtos(rows);
        var list = rows.Zip(dtos, (r, d) => new { r, d, balance = d.Balance, days = Today.DayNumber - r.I.DueDate.DayNumber })
            .Where(x => x.balance > 0).ToList();
        static string Bucket(int days) => days <= 30 ? "0-30" : days <= 60 ? "31-60" : days <= 90 ? "61-90" : "90+";

        var aging = list.GroupBy(x => Bucket(x.days)).Select(g => new { bucket = g.Key, count = g.Count(), amount = g.Sum(x => x.balance) }).OrderBy(x => x.bucket).ToList();
        if (groupBy == "tenant")
        {
            var tenants = list.GroupBy(x => x.r.I.TenantId).Select(g => new
            {
                tenantId = g.Key, tenant = Loc(g.First().r.TenantEn, g.First().r.TenantAr), phone = g.First().r.TenantPhone,
                building = Loc(g.First().r.BEn, g.First().r.BAr), invoices = g.Count(), amount = g.Sum(x => x.balance), maxDays = g.Max(x => x.days),
                oldestDueDate = g.Min(x => x.r.I.DueDate), bucket = Bucket(g.Max(x => x.days)),
            }).OrderByDescending(x => x.amount).ToList();
            return Ok(new { total = tenants.Sum(t => t.amount), tenants = tenants.Count, aging, items = Page(tenants, q) });
        }
        var items = list.Select(x => new { invoice = x.d, daysOverdue = x.days, bucket = Bucket(x.days) }).ToList();
        return Ok(new { total = list.Sum(x => x.balance), tenants = list.Select(x => x.r.I.TenantId).Distinct().Count(), aging, items = Page(items, q) });
    }

    private static PagedResult<T> Page<T>(List<T> all, PageQuery q)
    {
        var size = q.PageSize <= 0 ? Math.Max(all.Count, 1) : Math.Clamp(q.PageSize, 1, 500);
        var page = Math.Max(1, q.Page);
        return new PagedResult<T>(all.Skip((page - 1) * size).Take(size).ToList(), all.Count, page, size);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Invoice> Load(long id)
    {
        var inv = await Db.Invoices.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Invoice", id);
        EnsureBranchAccess(inv.BranchId);
        return inv;
    }

    private IQueryable<Invoice> Filter(long? tenantId, long? buildingId, long? contractId, long? branchId, DateOnly? from, DateOnly? to)
    {
        var query = Db.Invoices.AsNoTracking().Where(i => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(i.BranchId));
        if (tenantId != null) query = query.Where(i => i.TenantId == tenantId);
        if (buildingId != null) query = query.Where(i => i.BuildingId == buildingId);
        if (contractId != null) query = query.Where(i => i.ContractId == contractId);
        if (branchId != null) query = query.Where(i => i.BranchId == branchId);
        if (from != null) query = query.Where(i => i.IssueDate >= from);
        if (to != null) query = query.Where(i => i.IssueDate <= to);
        return query;
    }

    private class Row
    {
        public Invoice I { get; init; } = null!;
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? TenantPhone { get; init; }
        public string? BCode { get; init; }
        public string? BEn { get; init; }
        public string? BAr { get; init; }
        public string? ContractNo { get; init; }
        public string? UnitNo { get; init; }
    }

    private static IQueryable<Row> Project(IQueryable<Invoice> q) => q.Select(i => new Row
    {
        I = i, TenantEn = i.Tenant!.Party!.NameEn, TenantAr = i.Tenant.Party!.NameAr, TenantPhone = i.Tenant.Party!.Phone, BCode = i.Building!.Code, BEn = i.Building.NameEn, BAr = i.Building.NameAr,
        ContractNo = i.Contract!.ContractNo, UnitNo = i.Contract.Unit!.UnitNo,
    });

    public record InvoiceDto(long Id, string No, string InvoiceNo, long TenantId, string? Customer, long? BuildingId, string? BuildingCode, string? Building,
        long? ContractId, string? ContractNo, string? Unit, DateOnly Date, DateOnly DueDate, string? Period, DateOnly? BillingPeriodStart, DateOnly? BillingPeriodEnd,
        string? Type, string? TypeLabel, decimal Subtotal, decimal Vat, decimal Amount, decimal Paid, decimal Balance, string? Status, string? StatusLabel,
        DateOnly? PaidDate, string? Notes, long? JournalEntryId,
        decimal Credited = 0, DateOnly? NextInstallmentDue = null, decimal NextInstallmentAmount = 0, int InstallmentCount = 0, int OverdueInstallments = 0);

    private async Task<List<InvoiceDto>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.I.Id).ToList();
        var paid = await Billing.PaidByInvoiceAsync(ids);
        var credited = await Billing.CreditedByInvoiceAsync(ids);
        var schedules = await Billing.InstallmentsAsync(ids);
        var lastPay = await Db.PaymentAllocations.AsNoTracking().Where(a => ids.Contains(a.InvoiceId))
            .GroupBy(a => a.InvoiceId).Select(g => new { g.Key, Date = g.Max(a => a.Payment!.PaymentDate) }).ToDictionaryAsync(x => x.Key, x => x.Date);
        return rows.Select(r =>
        {
            var p = paid.GetValueOrDefault(r.I.Id);
            var total = r.I.TotalAmount ?? 0;
            var status = Code(r.I.StatusId);
            var sch = schedules.GetValueOrDefault(r.I.Id);
            var next = sch?.Where(x => x.Outstanding > 0 && x.Status != "cancelled").OrderBy(x => x.DueDate).FirstOrDefault();
            var lateInst = sch?.Count(x => x.Status == "overdue") ?? 0;
            var late = sch != null ? lateInst > 0 : r.I.DueDate < Today;
            if (status is "pending" or "partial" && late && p < total) status = "overdue";
            return new InvoiceDto(r.I.Id, r.I.InvoiceNo, r.I.InvoiceNo, r.I.TenantId, Loc(r.TenantEn, r.TenantAr),
                r.I.BuildingId, r.BCode, Loc(r.BEn, r.BAr), r.I.ContractId, r.ContractNo, r.UnitNo,
                r.I.IssueDate, r.I.DueDate, r.I.BillingPeriodStart == null ? null : $"{r.I.BillingPeriodStart:yyyy-MM-dd} – {r.I.BillingPeriodEnd:yyyy-MM-dd}",
                r.I.BillingPeriodStart, r.I.BillingPeriodEnd, Code(r.I.InvoiceTypeId), Label(r.I.InvoiceTypeId),
                r.I.Subtotal ?? 0, r.I.VatAmount ?? 0, total, p, status == "cancelled" ? 0 : total - p,
                status, Label(r.I.StatusId), status == "paid" ? lastPay.GetValueOrDefault(r.I.Id) : null, r.I.Notes, r.I.JournalEntryId,
                credited.GetValueOrDefault(r.I.Id), next?.DueDate, next?.Outstanding ?? 0, sch?.Count ?? 0, lateInst);
        }).ToList();
    }
}

/// <summary>
/// Credit notes (إشعار دائن, BRD §7.3) against issued customer invoices: full, by amount (VAT reversed proportionally) or by lines.
/// Posting: Dr revenue (per item type) + Dr VAT output, Cr A/R. The note is applied to its invoice first; the rest stays as customer credit.
/// </summary>
[Route("api/credit-notes")]
[PermissionModule("invoices")]
public class CreditNotesController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record LineInput(long InvoiceItemId, decimal Amount);
    /// <param name="Mode">full | amount | lines</param>
    /// <param name="Amount">Credit amount including VAT (mode = amount).</param>
    /// <param name="Lines">Net amounts per invoice line (mode = lines).</param>
    public record CreateInput(long InvoiceId, DateOnly? Date, string? Reason, string? Mode, decimal? Amount, List<LineInput>? Lines, bool ApplyToInvoice = true);
    public record CancelInput(string? Reason);

    /// <summary>Credit notes. Filters: tenantId, invoiceId, status, from, to, search (number / customer / invoice no).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? tenantId, [FromQuery] long? invoiceId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var query = Db.CreditNotes.AsNoTracking().Where(c => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(c.BranchId));
        if (tenantId != null) query = query.Where(c => c.TenantId == tenantId);
        if (invoiceId != null) query = query.Where(c => c.InvoiceId == invoiceId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
        if (from != null) query = query.Where(c => c.IssueDate >= from);
        if (to != null) query = query.Where(c => c.IssueDate <= to);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(c => c.CreditNoteNo.Contains(q.Search) || c.Invoice!.InvoiceNo.Contains(q.Search)
                                     || (c.Tenant!.Party!.NameAr ?? "").Contains(q.Search) || (c.Tenant.Party!.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(c => c.IssueDate).ThenByDescending(c => c.Id).Select(c => new CnRow
        {
            Id = c.Id, CreditNoteNo = c.CreditNoteNo, IssueDate = c.IssueDate, TenantId = c.TenantId, TenantEn = c.Tenant!.Party!.NameEn, TenantAr = c.Tenant.Party!.NameAr,
            InvoiceId = c.InvoiceId, InvoiceNo = c.Invoice!.InvoiceNo, Reason = c.Reason, Subtotal = c.Subtotal, VatAmount = c.VatAmount, TotalAmount = c.TotalAmount,
            Status = c.Status, JournalEntryId = c.JournalEntryId,
            Applied = Db.CreditNoteAllocations.Where(a => a.CreditNoteId == c.Id).Sum(a => (decimal?)a.Amount) ?? 0,
            Refunded = Db.CustomerRefundSources.Where(r => r.CreditNoteId == c.Id && r.Refund!.Status == "posted").Sum(r => (decimal?)r.Amount) ?? 0,
        }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(Dto).ToList(), page.Total, page.Page, page.PageSize));
    }

    /// <summary>One credit note with lines, applications, refunds and company header (print).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        var cn = await Db.CreditNotes.AsNoTracking().Where(c => c.Id == id).Select(c => new CnRow
        {
            Id = c.Id, CreditNoteNo = c.CreditNoteNo, IssueDate = c.IssueDate, TenantId = c.TenantId, TenantEn = c.Tenant!.Party!.NameEn, TenantAr = c.Tenant.Party!.NameAr,
            InvoiceId = c.InvoiceId, InvoiceNo = c.Invoice!.InvoiceNo, Reason = c.Reason, Subtotal = c.Subtotal, VatAmount = c.VatAmount, TotalAmount = c.TotalAmount,
            Status = c.Status, CancelReason = c.CancelReason, JournalEntryId = c.JournalEntryId, BranchId = c.BranchId,
            TenantCode = c.Tenant.Party!.PartyCode, TenantPhone = c.Tenant.Party!.Phone, InvoiceDate = c.Invoice.IssueDate,
            Applied = Db.CreditNoteAllocations.Where(a => a.CreditNoteId == c.Id).Sum(a => (decimal?)a.Amount) ?? 0,
            Refunded = Db.CustomerRefundSources.Where(r => r.CreditNoteId == c.Id && r.Refund!.Status == "posted").Sum(r => (decimal?)r.Amount) ?? 0,
        }).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("CreditNote", id);
        EnsureBranchAccess(cn.BranchId);
        var lines = await Db.CreditNoteLines.AsNoTracking().Where(l => l.CreditNoteId == id).OrderBy(l => l.Id)
            .Select(l => new { l.Id, l.InvoiceItemId, l.Description, l.Amount, l.VatRate, l.VatAmount, total = l.Amount + l.VatAmount }).ToListAsync(ct);
        var allocations = await Db.CreditNoteAllocations.AsNoTracking().Where(a => a.CreditNoteId == id)
            .Select(a => new { a.Id, a.InvoiceId, a.Invoice!.InvoiceNo, a.Invoice.IssueDate, a.Amount }).ToListAsync(ct);
        var refunds = await Db.CustomerRefundSources.AsNoTracking().Where(r => r.CreditNoteId == id)
            .Select(r => new { r.RefundId, r.Refund!.RefundNo, r.Refund.RefundDate, r.Refund.Status, r.Amount }).ToListAsync(ct);
        var journal = cn.JournalEntryId == null ? null : await Db.JournalEntries.AsNoTracking().Where(j => j.Id == cn.JournalEntryId).Select(j => new { j.Id, j.JournalNo }).FirstOrDefaultAsync(ct);
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        return Ok(new { creditNote = Dto(cn), lines, allocations, refunds, journal, company,
                        customer = new { id = cn.TenantId, code = cn.TenantCode, name = Loc(cn.TenantEn, cn.TenantAr), phone = cn.TenantPhone } });
    }

    /// <summary>What can still be credited on an invoice (per line, net of earlier posted credit notes).</summary>
    [HttpGet("creditable/{invoiceId:long}")]
    public async Task<IActionResult> Creditable(long invoiceId)
    {
        var (inv, lines, remainingTotal) = await CreditableAsync(invoiceId);
        var settled = (await Billing.PaidByInvoiceAsync(new[] { invoiceId })).GetValueOrDefault(invoiceId);
        return Ok(new { invoiceId, inv.InvoiceNo, total = inv.TotalAmount ?? 0, balance = (inv.TotalAmount ?? 0) - settled, remaining = remainingTotal,
                        lines = lines.Select(l => new { l.ItemId, l.Description, l.Net, l.VatRate, l.Credited, l.RemainingNet, remainingGross = l.RemainingNet + BillingService.R2(l.RemainingNet * l.VatRate / 100) }) });
    }

    /// <summary>Issue a credit note (CN-YYYY-NNNN) against an issued invoice.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the reason for the credit note.");
        var (inv, items, remainingTotal) = await CreditableAsync(input.InvoiceId);
        EnsureBranchAccess(inv.BranchId);
        if (remainingTotal <= 0) throw new BusinessRuleException("This invoice is already fully credited.");
        var mode = (input.Mode ?? "amount").ToLowerInvariant();
        var lines = new List<CreditNoteLine>();
        CreditNoteLine Line(CreditableLine it, decimal net) => new()
        {
            InvoiceItemId = it.ItemId, ItemTypeId = it.ItemTypeId, Description = it.Description, Amount = BillingService.R2(net),
            VatRate = it.VatRate, VatAmount = BillingService.R2(BillingService.R2(net) * it.VatRate / 100),
        };
        switch (mode)
        {
            case "full":
                lines.AddRange(items.Where(i => i.RemainingNet > 0).Select(i => Line(i, i.RemainingNet)));
                break;
            case "lines":
                if (input.Lines is not { Count: > 0 }) throw new AppException("Choose the invoice lines to credit.");
                foreach (var l in input.Lines.Where(l => l.Amount != 0))
                {
                    var it = items.FirstOrDefault(i => i.ItemId == l.InvoiceItemId) ?? throw new AppException($"Line {l.InvoiceItemId} is not on this invoice.");
                    if (l.Amount < 0) throw new AppException("Credit amounts must be positive.");
                    if (l.Amount > it.RemainingNet) throw new AppException($"«{it.Description}»: at most {it.RemainingNet:0.00} can still be credited.");
                    lines.Add(Line(it, l.Amount));
                }
                break;
            case "amount":
                var target = BillingService.R2(input.Amount ?? 0);
                if (target <= 0) throw new AppException("Enter the credit amount.");
                if (target > remainingTotal) throw new AppException($"At most {remainingTotal:0.00} can still be credited on this invoice.");
                var open = items.Where(i => i.RemainingNet > 0).ToList();
                var gross = open.Sum(i => i.RemainingGross);
                foreach (var it in open) lines.Add(Line(it, it.RemainingNet * target / gross));
                // rounding: make the note total exactly the requested amount (adjust the biggest line's net)
                var diff = target - lines.Sum(l => l.Amount + l.VatAmount);
                if (diff != 0 && lines.Count > 0)
                {
                    var big = lines.OrderByDescending(l => l.Amount).First();
                    big.Amount += diff;
                    var cap = open.First(o => o.ItemId == big.InvoiceItemId).RemainingNet;
                    if (big.Amount > cap) { big.VatAmount += big.Amount - cap; big.Amount = cap; }
                }
                break;
            default: throw new AppException("Mode must be full, amount or lines.");
        }
        lines = lines.Where(l => l.Amount + l.VatAmount > 0).ToList();
        if (lines.Count == 0) throw new AppException("Nothing to credit.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        var date = input.Date ?? Today;
        if (date < inv.IssueDate) throw new AppException("The credit note cannot be dated before the invoice.");
        var cn = new CreditNote
        {
            BranchId = inv.BranchId, CreditNoteNo = await Numbers.NextAsync(NumberPrefixes.CreditNote, date: date), TenantId = inv.TenantId,
            InvoiceId = inv.Id, BuildingId = inv.BuildingId, ContractId = inv.ContractId, IssueDate = date, Reason = input.Reason.Trim(),
            Subtotal = lines.Sum(l => l.Amount), VatAmount = lines.Sum(l => l.VatAmount), Status = "posted",
        };
        cn.TotalAmount = cn.Subtotal + cn.VatAmount;
        Db.CreditNotes.Add(cn);
        await Db.SaveChangesAsync();
        foreach (var l in lines) l.CreditNoteId = cn.Id;
        Db.CreditNoteLines.AddRange(lines);
        await Db.SaveChangesAsync();
        cn.JournalEntryId = (await Posting.PostCreditNoteAsync(cn, lines))?.Id;
        decimal applied = 0;
        if (input.ApplyToInvoice)
        {
            var settled = (await Billing.PaidByInvoiceAsync(new[] { inv.Id })).GetValueOrDefault(inv.Id);
            var bal = (inv.TotalAmount ?? 0) - settled;
            var amt = Math.Min(bal, cn.TotalAmount);
            if (amt > 0) applied = (await Billing.AllocateCreditNoteAsync(cn, amt, new List<AllocationInput> { new(inv.Id, amt) })).Sum(a => a.Amount);
        }
        Activity.Log("credit_note", cn.Id, "create", $"Credit note {cn.CreditNoteNo} on invoice {inv.InvoiceNo}: {cn.TotalAmount:0.##} — {cn.Reason}", inv.BuildingId, inv.BranchId);
        Activity.Log("invoice", inv.Id, "credit_note", $"Credit note {cn.CreditNoteNo}: {cn.TotalAmount:0.##} ({cn.Reason})", inv.BuildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = cn.Id }, new { cn.Id, cn.CreditNoteNo, cn.TotalAmount, applied, credit = cn.TotalAmount - applied, posted = cn.JournalEntryId != null });
    }

    /// <summary>Apply the unapplied part of a credit note to open invoices of the customer (explicit, or oldest due first).</summary>
    [HttpPost("{id:long}/allocate")]
    public async Task<IActionResult> Allocate(long id, [FromBody] List<AllocationInput>? allocations)
    {
        var cn = await Db.CreditNotes.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CreditNote", id);
        EnsureBranchAccess(cn.BranchId);
        if (cn.Status != "posted") throw new BusinessRuleException("The credit note is cancelled.");
        var free = await Billing.CreditNoteFreeAsync(cn);
        if (free <= 0) throw new BusinessRuleException("The credit note is fully used.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var list = await Billing.AllocateCreditNoteAsync(cn, free, allocations);
        Activity.Log("credit_note", cn.Id, "allocate", $"Credit note {cn.CreditNoteNo}: {list.Sum(a => a.Amount):0.##} applied", cn.BuildingId, cn.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { allocated = list.Sum(a => a.Amount), remaining = free - list.Sum(a => a.Amount) });
    }

    /// <summary>Cancel a credit note: reverses its journal and re-opens the invoices it settled. Not allowed once refunded.</summary>
    [HttpPost("{id:long}/cancel")]
    [RequirePermission("invoices.cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var cn = await Db.CreditNotes.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CreditNote", id);
        EnsureBranchAccess(cn.BranchId);
        if (cn.Status == "cancelled") throw new BusinessRuleException("The credit note is already cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the cancellation reason.");
        if (await Db.CustomerRefundSources.AnyAsync(r => r.CreditNoteId == id && r.Refund!.Status == "posted"))
            throw new BusinessRuleException("This credit note was refunded to the customer. Cancel the refund first.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var allocs = await Db.CreditNoteAllocations.Where(a => a.CreditNoteId == id).ToListAsync();
        var invoiceIds = allocs.Select(a => a.InvoiceId).Append(cn.InvoiceId).Distinct().ToList();
        Db.CreditNoteAllocations.RemoveRange(allocs);
        cn.Status = "cancelled";
        cn.CancelReason = input.Reason.Trim();
        await Db.SaveChangesAsync();
        if (cn.JournalEntryId != null) await Posting.ReverseAsync(cn.JournalEntryId.Value, Today, $"Credit note {cn.CreditNoteNo} cancelled");
        await Billing.RecalcInvoicesAsync(invoiceIds);
        Activity.Log("credit_note", cn.Id, "status_change", $"Credit note {cn.CreditNoteNo} cancelled: {input.Reason}", cn.BuildingId, cn.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // ------------------------------------------------------------------ helpers

    private record CreditableLine(long ItemId, long? ItemTypeId, string? Description, decimal Net, decimal VatRate, decimal Credited)
    {
        public decimal RemainingNet => Math.Max(0, Net - Credited);
        public decimal RemainingGross => RemainingNet + BillingService.R2(RemainingNet * VatRate / 100);
    }

    private async Task<(Invoice inv, List<CreditableLine> lines, decimal remainingTotal)> CreditableAsync(long invoiceId)
    {
        var inv = await Db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId) ?? throw new NotFoundException("Invoice", invoiceId);
        EnsureBranchAccess(inv.BranchId);
        if (await Lookups.CodeAsync(inv.StatusId) is "draft" or "cancelled") throw new BusinessRuleException("Credit notes can be issued on issued invoices only.");
        var items = await Db.InvoiceItems.AsNoTracking().Where(i => i.InvoiceId == invoiceId).OrderBy(i => i.Id).ToListAsync();
        var credited = await Db.CreditNoteLines.AsNoTracking().Where(l => l.InvoiceItemId != null && l.CreditNote!.InvoiceId == invoiceId && l.CreditNote.Status == "posted")
            .GroupBy(l => l.InvoiceItemId!.Value).Select(g => new { g.Key, Amt = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Amt);
        var lines = items.Select(i => new CreditableLine(i.Id, i.ItemTypeId, i.Description, i.Amount ?? 0, i.VatRate ?? 0, credited.GetValueOrDefault(i.Id))).ToList();
        var creditedTotal = await Db.CreditNotes.Where(c => c.InvoiceId == invoiceId && c.Status == "posted").SumAsync(c => (decimal?)c.TotalAmount) ?? 0;
        var remaining = Math.Max(0, (inv.TotalAmount ?? 0) - creditedTotal);
        return (inv, lines, remaining);
    }

    private class CnRow
    {
        public long Id { get; init; }
        public string CreditNoteNo { get; init; } = "";
        public DateOnly IssueDate { get; init; }
        public long TenantId { get; init; }
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? TenantCode { get; init; }
        public string? TenantPhone { get; init; }
        public long InvoiceId { get; init; }
        public string? InvoiceNo { get; init; }
        public DateOnly? InvoiceDate { get; init; }
        public string Reason { get; init; } = "";
        public decimal Subtotal { get; init; }
        public decimal VatAmount { get; init; }
        public decimal TotalAmount { get; init; }
        public string Status { get; init; } = "";
        public string? CancelReason { get; init; }
        public long? JournalEntryId { get; init; }
        public long BranchId { get; init; }
        public decimal Applied { get; init; }
        public decimal Refunded { get; init; }
    }

    private object Dto(CnRow c) => new
    {
        c.Id, creditNoteNo = c.CreditNoteNo, date = c.IssueDate, c.TenantId, tenant = Loc(c.TenantEn, c.TenantAr), c.InvoiceId, c.InvoiceNo, invoiceDate = c.InvoiceDate,
        c.Reason, c.Subtotal, c.VatAmount, c.TotalAmount, c.Status, c.CancelReason, c.JournalEntryId, c.Applied, c.Refunded,
        available = c.Status == "posted" ? c.TotalAmount - c.Applied - c.Refunded : 0,
    };
}
