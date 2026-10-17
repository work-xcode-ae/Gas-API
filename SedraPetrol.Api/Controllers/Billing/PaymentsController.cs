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
            query = query.Where(p => p.ReceiptNo.Contains(q.Search) || (p.ReferenceNo ?? "").Contains(q.Search) || (p.Tenant!.NameAr ?? "").Contains(q.Search) || (p.Tenant.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Select(p => new Row { P = p, TenantEn = p.Tenant!.NameEn, TenantAr = p.Tenant.NameAr, ReceivedBy = p.ReceivedByUser!.NameAr ?? p.ReceivedByUser.NameEn })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var row = await Db.Payments.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new Row { P = p, TenantEn = p.Tenant!.NameEn, TenantAr = p.Tenant.NameAr, ReceivedBy = p.ReceivedByUser!.NameAr ?? p.ReceivedByUser.NameEn })
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
        var used = await Db.PaymentAllocations.Where(a => a.PaymentId == id).SumAsync(a => (decimal?)a.Amount) ?? 0;
        var free = p.Amount - used;
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
        return rows.Select(r =>
        {
            var mine = allocs.Where(a => a.PaymentId == r.P.Id).ToList();
            var allocated = mine.Sum(a => a.Amount);
            return (object)new
            {
                r.P.Id, receiptNo = r.P.ReceiptNo, date = r.P.PaymentDate, r.P.Amount, method = Code(r.P.PaymentMethodId), methodLabel = Label(r.P.PaymentMethodId),
                invoiceId = mine.FirstOrDefault()?.InvoiceId, invoiceLabel = string.Join(", ", mine.Select(a => a.InvoiceNo)), invoices = mine.Select(a => new { a.InvoiceId, a.InvoiceNo, a.Amount }),
                reference = r.P.ReferenceNo, receivedBy = r.ReceivedBy, r.P.Notes, tenantId = r.P.TenantId, tenant = Loc(r.TenantEn, r.TenantAr),
                status = Code(r.P.StatusId), statusLabel = Label(r.P.StatusId), allocated, unallocated = r.P.Amount - allocated, r.P.JournalEntryId,
            };
        }).ToList();
    }
}
