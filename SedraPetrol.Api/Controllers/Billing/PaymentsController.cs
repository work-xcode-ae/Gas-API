using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Tenant payments / receipts (RecordPaymentModal) with allocation to invoices.</summary>
[Route("api/payments")]
[PermissionModule("payments")]
public class PaymentsController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record CreateInput(long TenantId, decimal Amount, DateOnly? Date, string? Method, string? Reference, string? Notes,
        long? InvoiceId, List<AllocationInput>? Allocations, string? Status);
    public record CancelInput(string? Reason, bool Bounced = false);

    /// <summary>Payments (PaymentRecord). Filters: tenantId, buildingId, method, status, from, to.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? tenantId, [FromQuery] long? buildingId, [FromQuery] long? branchId,
        [FromQuery] string? method, [FromQuery] string? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.Payments.AsNoTracking().Where(p => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(p.BranchId));
        if (tenantId != null) query = query.Where(p => p.TenantId == tenantId);
        if (branchId != null) query = query.Where(p => p.BranchId == branchId);
        if (buildingId != null) query = query.Where(p => Db.SupplyContracts.Any(c => c.TenantId == p.TenantId && c.Unit!.BuildingId == buildingId));
        if (from != null) query = query.Where(p => p.PaymentDate >= from);
        if (to != null) query = query.Where(p => p.PaymentDate <= to);
        if (!string.IsNullOrEmpty(method)) { var m = await Lookups.IdOrNullAsync(LookupTypes.PaymentMethod, method); query = query.Where(p => p.PaymentMethodId == m); }
        if (!string.IsNullOrEmpty(status)) { var m = await Lookups.IdOrNullAsync(LookupTypes.PaymentStatus, status); query = query.Where(p => p.StatusId == m); }
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => p.ReceiptNo.Contains(q.Search) || (p.ReferenceNo ?? "").Contains(q.Search) || (p.Tenant!.Party!.NameAr ?? "").Contains(q.Search) || (p.Tenant.Party!.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Select(p => new Row { P = p, TenantEn = p.Tenant!.Party!.NameEn, TenantAr = p.Tenant.Party!.NameAr, ReceivedBy = p.ReceivedByUser!.NameAr ?? p.ReceivedByUser.NameEn })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var row = await Db.Payments.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new Row { P = p, TenantEn = p.Tenant!.Party!.NameEn, TenantAr = p.Tenant.Party!.NameAr, ReceivedBy = p.ReceivedByUser!.NameAr ?? p.ReceivedByUser.NameEn })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("Payment", id);
        EnsureBranchAccess(row.P.BranchId);
        var allocations = await Db.PaymentAllocations.AsNoTracking().Where(a => a.PaymentId == id)
            .Select(a => new { a.Id, a.InvoiceId, a.Invoice!.InvoiceNo, a.Invoice.IssueDate, a.Invoice.TotalAmount, a.Amount }).ToListAsync();
        return Ok(new { payment = (await ToDtos(new[] { row }))[0], allocations });
    }

    /// <summary>Open invoices of a tenant with their balances (to fill the payment modal).</summary>
    [HttpGet("open-invoices")]
    public async Task<IActionResult> OpenInvoices([FromQuery] long tenantId) =>
        Ok(new
        {
            invoices = (await Billing.OpenInvoicesAsync(tenantId)).OrderBy(o => o.DueDate).Select(o => new { o.Id, o.No, o.DueDate, o.Total, o.Paid, o.Balance }),
            credit = await HttpContext.RequestServices.GetRequiredService<BalanceQueries>().TenantCreditAsync(tenantId),
        });

    /// <summary>
    /// Record a payment. Give invoiceId (pays that invoice), explicit allocations, or nothing (FIFO over open invoices).
    /// Creates receipt RCP-YYYY-NNNN and the journal entry (Dr cash/bank, Cr A/R).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == input.TenantId) ?? throw new NotFoundException("Tenant", input.TenantId);
        EnsureBranchAccess(tenant.BranchId);
        if (input.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        var allocations = input.Allocations;
        if ((allocations == null || allocations.Count == 0) && input.InvoiceId != null)
        {
            var open = (await Billing.OpenInvoicesAsync(tenant.Id)).FirstOrDefault(o => o.Id == input.InvoiceId)
                       ?? throw new AppException("The invoice is not open (already paid, draft or cancelled).");
            allocations = new() { new AllocationInput(open.Id, Math.Min(open.Balance, input.Amount)) };
        }

        await using var tx = await Db.Database.BeginTransactionAsync();
        var date = input.Date ?? Today;
        var p = new Payment
        {
            BranchId = tenant.BranchId, ReceiptNo = await Numbers.NextAsync(NumberPrefixes.Receipt, date: date), TenantId = tenant.Id,
            PaymentDate = date, Amount = input.Amount,
            CurrencyId = await Db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            PaymentMethodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "cash", field: "method"),
            ReferenceNo = input.Reference, ReceivedBy = Me.UserId, Notes = input.Notes,
            StatusId = await Lk(LookupTypes.PaymentStatus, null, input.Status ?? "completed", field: "status"),
        };
        Db.Payments.Add(p);
        await Db.SaveChangesAsync();
        var allocs = await Billing.AllocateAsync(p, allocations);
        p.JournalEntryId = (await Posting.PostPaymentAsync(p))?.Id;
        Activity.Log("payment", p.Id, "create", $"Receipt {p.ReceiptNo}: {p.Amount:0.##}", null, p.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, new { p.Id, p.ReceiptNo, allocated = allocs.Sum(a => a.Amount), unallocated = p.Amount - allocs.Sum(a => a.Amount) });
    }

    /// <summary>Allocate the unallocated part of a payment (tenant credit) to invoices.</summary>
    [HttpPost("{id:long}/allocate")]
    public async Task<IActionResult> Allocate(long id, [FromBody] List<AllocationInput>? allocations)
    {
        var p = await Db.Payments.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Payment", id);
        EnsureBranchAccess(p.BranchId);
        if (await Lookups.CodeAsync(p.StatusId) is "cancelled" or "bounced") throw new BusinessRuleException("The payment is cancelled.");
        var free = await Billing.PaymentFreeAsync(p);
        if (free <= 0) throw new BusinessRuleException("The payment is fully allocated.");
        var temp = new Payment { Id = p.Id, TenantId = p.TenantId, Amount = free };
        var allocs = await Billing.AllocateAsync(temp, allocations);
        return Ok(new { allocated = allocs.Sum(a => a.Amount), remaining = free - allocs.Sum(a => a.Amount) });
    }

    /// <summary>Cancel (or mark bounced) a payment: reverses the journal and re-opens the invoices it paid.</summary>
    [HttpPost("{id:long}/cancel")]
    [RequirePermission("payments.cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var p = await Db.Payments.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Payment", id);
        EnsureBranchAccess(p.BranchId);
        if (await Lookups.CodeAsync(p.StatusId) is "cancelled" or "bounced") throw new BusinessRuleException("The payment is already cancelled.");
        if (await Db.CustomerRefundSources.AnyAsync(r => r.PaymentId == id && r.Refund!.Status == "posted"))
            throw new BusinessRuleException("Part of this receipt was refunded to the customer. Cancel the refund first.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        p.StatusId = await LkId(LookupTypes.PaymentStatus, input.Bounced ? "bounced" : "cancelled");
        p.Notes = $"{p.Notes}\n{(input.Bounced ? "Bounced" : "Cancelled")}: {input.Reason}".Trim();
        await Db.SaveChangesAsync();
        if (p.JournalEntryId != null) await Posting.ReverseAsync(p.JournalEntryId.Value, Today, $"Receipt {p.ReceiptNo} {(input.Bounced ? "bounced" : "cancelled")}");
        var invoiceIds = await Db.PaymentAllocations.Where(a => a.PaymentId == id).Select(a => a.InvoiceId).ToListAsync();
        await Billing.RecalcInvoicesAsync(invoiceIds);
        Activity.Log("payment", p.Id, "status_change", $"Receipt {p.ReceiptNo} {(input.Bounced ? "bounced" : "cancelled")}: {input.Reason}", null, p.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    private class Row
    {
        public Payment P { get; init; } = null!;
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? ReceivedBy { get; init; }
    }

    private async Task<List<object>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.P.Id).ToList();
        var allocs = await Db.PaymentAllocations.AsNoTracking().Where(a => ids.Contains(a.PaymentId))
            .Select(a => new { a.PaymentId, a.InvoiceId, a.Invoice!.InvoiceNo, a.Amount }).ToListAsync();
        var refunded = await Db.CustomerRefundSources.AsNoTracking().Where(r => r.PaymentId != null && ids.Contains(r.PaymentId.Value) && r.Refund!.Status == "posted")
            .GroupBy(r => r.PaymentId!.Value).Select(g => new { g.Key, Amt = g.Sum(x => x.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Amt);
        return rows.Select(r =>
        {
            var mine = allocs.Where(a => a.PaymentId == r.P.Id).ToList();
            var allocated = mine.Sum(a => a.Amount);
            return (object)new
            {
                r.P.Id, receiptNo = r.P.ReceiptNo, date = r.P.PaymentDate, r.P.Amount, method = Code(r.P.PaymentMethodId), methodLabel = Label(r.P.PaymentMethodId),
                invoiceId = mine.FirstOrDefault()?.InvoiceId, invoiceLabel = string.Join(", ", mine.Select(a => a.InvoiceNo)), invoices = mine.Select(a => new { a.InvoiceId, a.InvoiceNo, a.Amount }),
                reference = r.P.ReferenceNo, receivedBy = r.ReceivedBy, r.P.Notes, tenantId = r.P.TenantId, tenant = Loc(r.TenantEn, r.TenantAr),
                status = Code(r.P.StatusId), statusLabel = Label(r.P.StatusId), allocated, refunded = refunded.GetValueOrDefault(r.P.Id),
                unallocated = r.P.Amount - allocated - refunded.GetValueOrDefault(r.P.Id), r.P.JournalEntryId,
            };
        }).ToList();
    }
}

/// <summary>
/// Customer refunds (استرداد للعميل, BRD §7.3): pays back unallocated customer credit (receipts / credit notes), oldest credit first.
/// Posting: Dr A/R, Cr cash/bank (by payment method).
/// </summary>
[Route("api/customer-refunds")]
[PermissionModule("payments")]
public class CustomerRefundsController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record SourceInput(string Kind, long Id, decimal Amount);
    /// <param name="Sources">Optional: which receipts / credit notes to refund from; default oldest credit first.</param>
    public record CreateInput(long TenantId, decimal Amount, DateOnly? Date, string? Method, string? Reference, string? Reason, List<SourceInput>? Sources);
    public record CancelInput(string? Reason);

    /// <summary>Refunds. Filters: tenantId, status, from, to, search.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? tenantId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.CustomerRefunds.AsNoTracking().Where(r => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(r.BranchId));
        if (tenantId != null) query = query.Where(r => r.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        if (from != null) query = query.Where(r => r.RefundDate >= from);
        if (to != null) query = query.Where(r => r.RefundDate <= to);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(r => r.RefundNo.Contains(q.Search) || (r.ReferenceNo ?? "").Contains(q.Search)
                                     || (r.Tenant!.Party!.NameAr ?? "").Contains(q.Search) || (r.Tenant.Party!.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(r => r.RefundDate).ThenByDescending(r => r.Id)
            .Select(r => new RfRow { R = r, TenantEn = r.Tenant!.Party!.NameEn, TenantAr = r.Tenant.Party!.NameAr, TenantCode = r.Tenant.Party!.PartyCode, TenantPhone = r.Tenant.Party!.Phone })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(Dto).ToList(), page.Total, page.Page, page.PageSize));
    }

    /// <summary>One refund with its sources and company header (print).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Db.CustomerRefunds.AsNoTracking().Where(r => r.Id == id)
            .Select(r => new RfRow { R = r, TenantEn = r.Tenant!.Party!.NameEn, TenantAr = r.Tenant.Party!.NameAr, TenantCode = r.Tenant.Party!.PartyCode, TenantPhone = r.Tenant.Party!.Phone })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("CustomerRefund", id);
        EnsureBranchAccess(row.R.BranchId);
        var sources = await Db.CustomerRefundSources.AsNoTracking().Where(s => s.RefundId == id)
            .Select(s => new { s.Id, s.PaymentId, receiptNo = s.Payment != null ? s.Payment.ReceiptNo : null, s.CreditNoteId,
                               creditNoteNo = s.CreditNote != null ? s.CreditNote.CreditNoteNo : null, s.Amount }).ToListAsync(ct);
        var journal = row.R.JournalEntryId == null ? null : await Db.JournalEntries.AsNoTracking().Where(j => j.Id == row.R.JournalEntryId).Select(j => new { j.Id, j.JournalNo }).FirstOrDefaultAsync(ct);
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        return Ok(new { refund = Dto(row), sources, journal, company,
                        customer = new { id = row.R.TenantId, code = row.TenantCode, name = Loc(row.TenantEn, row.TenantAr), phone = row.TenantPhone } });
    }

    /// <summary>Refundable credit of a customer: unallocated receipts and unapplied credit notes, oldest first.</summary>
    [HttpGet("credit")]
    public async Task<IActionResult> Credit([FromQuery] long tenantId)
    {
        var tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId) ?? throw new NotFoundException("Tenant", tenantId);
        EnsureBranchAccess(tenant.BranchId);
        var sources = await Billing.CreditSourcesAsync(tenantId);
        return Ok(new { tenantId, total = sources.Sum(s => s.Free), sources });
    }

    /// <summary>Refund customer credit (RF-YYYY-NNNN). Amount cannot exceed the available credit.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == input.TenantId) ?? throw new NotFoundException("Tenant", input.TenantId);
        EnsureBranchAccess(tenant.BranchId);
        if (input.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the refund reason.");
        var available = await Billing.CreditSourcesAsync(tenant.Id);
        var total = available.Sum(s => s.Free);
        var amount = BillingService.R2(input.Amount);
        if (amount > total) throw new BusinessRuleException($"The customer has only {total:0.00} of unallocated credit.");

        // which credit is paid back
        var picks = new List<CustomerRefundSource>();
        if (input.Sources is { Count: > 0 })
        {
            foreach (var s in input.Sources.Where(s => s.Amount > 0))
            {
                var src = available.FirstOrDefault(a => a.Kind == s.Kind && a.Id == s.Id) ?? throw new AppException($"{s.Kind} {s.Id} has no free credit.");
                if (s.Amount > src.Free) throw new AppException($"{src.No}: only {src.Free:0.00} is free.");
                picks.Add(Src(src.Kind, src.Id, s.Amount));
            }
            if (picks.Sum(p => p.Amount) != amount) throw new AppException("The chosen sources must add up to the refund amount.");
        }
        else
        {
            var left = amount;
            foreach (var src in available)
            {
                if (left <= 0) break;
                var a = Math.Min(left, src.Free);
                left -= a;
                picks.Add(Src(src.Kind, src.Id, a));
            }
        }

        await using var tx = await Db.Database.BeginTransactionAsync();
        var date = input.Date ?? Today;
        var r = new CustomerRefund
        {
            BranchId = tenant.BranchId, RefundNo = await Numbers.NextAsync(NumberPrefixes.CustomerRefund, date: date), TenantId = tenant.Id,
            RefundDate = date, Amount = amount, PaymentMethodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "cash", field: "method"),
            ReferenceNo = input.Reference, Reason = input.Reason.Trim(), Status = "posted",
        };
        Db.CustomerRefunds.Add(r);
        await Db.SaveChangesAsync();
        foreach (var p in picks) p.RefundId = r.Id;
        Db.CustomerRefundSources.AddRange(picks);
        await Db.SaveChangesAsync();
        r.JournalEntryId = (await Posting.PostCustomerRefundAsync(r))?.Id;
        Activity.Log("customer_refund", r.Id, "create", $"Refund {r.RefundNo} to customer: {r.Amount:0.##} — {r.Reason}", null, r.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = r.Id }, new { r.Id, r.RefundNo, r.Amount, posted = r.JournalEntryId != null });
    }

    /// <summary>Cancel a refund: reverses its journal; the credit becomes available again.</summary>
    [HttpPost("{id:long}/cancel")]
    [RequirePermission("payments.cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var r = await Db.CustomerRefunds.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CustomerRefund", id);
        EnsureBranchAccess(r.BranchId);
        if (r.Status == "cancelled") throw new BusinessRuleException("The refund is already cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the cancellation reason.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        r.Status = "cancelled";
        r.CancelReason = input.Reason.Trim();
        await Db.SaveChangesAsync();
        if (r.JournalEntryId != null) await Posting.ReverseAsync(r.JournalEntryId.Value, Today, $"Refund {r.RefundNo} cancelled");
        Activity.Log("customer_refund", r.Id, "status_change", $"Refund {r.RefundNo} cancelled: {input.Reason}", null, r.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    private static CustomerRefundSource Src(string kind, long id, decimal amount) => kind == "receipt"
        ? new CustomerRefundSource { PaymentId = id, Amount = amount }
        : new CustomerRefundSource { CreditNoteId = id, Amount = amount };

    private class RfRow
    {
        public CustomerRefund R { get; init; } = null!;
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? TenantCode { get; init; }
        public string? TenantPhone { get; init; }
    }

    private object Dto(RfRow x) => new
    {
        x.R.Id, refundNo = x.R.RefundNo, date = x.R.RefundDate, x.R.TenantId, tenant = Loc(x.TenantEn, x.TenantAr), x.R.Amount,
        method = Code(x.R.PaymentMethodId), methodLabel = Label(x.R.PaymentMethodId), reference = x.R.ReferenceNo, x.R.Reason,
        x.R.Status, x.R.CancelReason, x.R.JournalEntryId,
    };
}
