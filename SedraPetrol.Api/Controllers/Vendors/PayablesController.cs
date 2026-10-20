using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Vendor bills (A/P). Refill purchase invoices and AMC instalments also land here.</summary>
[Route("api/vendor-bills")]
[PermissionModule("payables")]
public class VendorBillsController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record CreateInput(long? BranchId, long VendorId, string BillNo, DateOnly? BillDate, DateOnly? DueDate, string? Notes, List<BillingService.BillLineInput> Items);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? vendorId, [FromQuery] string? status, [FromQuery] string? sourceType,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool openOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.VendorBills.AsNoTracking().Where(b => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(b.BranchId));
        if (vendorId != null) query = query.Where(b => b.VendorId == vendorId);
        if (sourceType != null) query = query.Where(b => b.SourceType == sourceType);
        if (from != null) query = query.Where(b => b.BillDate >= from);
        if (to != null) query = query.Where(b => b.BillDate <= to);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, status); query = query.Where(b => b.StatusId == s); }
        if (openOnly)
        {
            var open = new[] { await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "pending"), await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "partial") };
            query = query.Where(b => open.Contains(b.StatusId));
        }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(b => b.BillNo.Contains(q.Search) || (b.Vendor!.Party!.NameAr ?? "").Contains(q.Search) || (b.Vendor.Party!.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(b => b.BillDate).ThenByDescending(b => b.Id)
            .Select(b => new { B = b, b.Vendor!.Party!.NameEn, b.Vendor.Party!.NameAr }).ToPagedAsync(q, ct);
        var paid = await Billing.PaidByBillAsync(page.Items.Select(x => x.B.Id));
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.B.Id, x.B.BillNo, x.B.VendorId, vendor = Loc(x.NameEn, x.NameAr), x.B.BillDate, x.B.DueDate, x.B.SourceType, x.B.SourceId,
            x.B.Subtotal, x.B.VatAmount, x.B.TotalAmount, paid = paid.GetValueOrDefault(x.B.Id), balance = (x.B.TotalAmount ?? 0) - paid.GetValueOrDefault(x.B.Id),
            status = Code(x.B.StatusId), statusLabel = Label(x.B.StatusId), x.B.Notes, x.B.JournalEntryId,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var b = await Db.VendorBills.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("VendorBill", id);
        EnsureBranchAccess(b.BranchId);
        var items = await Db.VendorBillItems.AsNoTracking().Where(i => i.BillId == id).ToListAsync();
        var payments = await Db.VendorPaymentAllocations.AsNoTracking().Where(a => a.VendorBillId == id)
            .Select(a => new { a.Id, a.VendorPaymentId, a.VendorPayment!.PaymentNo, a.VendorPayment.PaymentDate, a.Amount }).ToListAsync();
        return Ok(new { bill = b, status = Code(b.StatusId), items = items.Select(i => new { i.Id, type = Code(i.ItemTypeId), i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate, i.Amount, i.ExpenseAccountId }), payments });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var branchId = RequireBranch(input.BranchId);
        if (string.IsNullOrWhiteSpace(input.BillNo)) throw new AppException("Vendor invoice number is required.");
        if (await Db.VendorBills.AnyAsync(b => b.VendorId == input.VendorId && b.BillNo == input.BillNo.Trim()))
            throw new BusinessRuleException($"Invoice {input.BillNo} of this vendor is already recorded.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var bill = await Billing.CreateVendorBillAsync(branchId, input.VendorId, input.BillNo.Trim(), input.BillDate ?? Today, input.DueDate, "other", null, input.Items, input.Notes);
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = bill.Id }, new { bill.Id, bill.TotalAmount });
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] InvoicesController.CancelInput input)
    {
        var b = await Db.VendorBills.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("VendorBill", id);
        EnsureBranchAccess(b.BranchId);
        if ((await Billing.PaidByBillAsync(new[] { id })).GetValueOrDefault(id) > 0) throw new BusinessRuleException("Payments are applied to this bill.");
        if (b.SourceType == "refill_request") throw new BusinessRuleException("This bill belongs to a refill request.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (b.JournalEntryId != null) await Posting.ReverseAsync(b.JournalEntryId.Value, Today, $"Vendor bill {b.BillNo} cancelled");
        b.StatusId = await LkId(LookupTypes.BillStatus, "cancelled");
        b.Notes = $"{b.Notes}\nCancelled: {input.Reason}".Trim();
        await Db.AmcPaymentSchedules.Where(s => s.VendorBillId == id).ForEachAsync(s => { s.VendorBillId = null; s.StatusId = null; });
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }
}

/// <summary>Payments to vendors with allocation to their bills.</summary>
[Route("api/vendor-payments")]
[PermissionModule("payables")]
public class VendorPaymentsController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record CreateInput(long? BranchId, long VendorId, decimal Amount, DateOnly? Date, string? Method, string? Reference, string? Notes, List<BillAllocation>? Allocations);
    public record BillAllocation(long VendorBillId, decimal Amount);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? vendorId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.VendorPayments.AsNoTracking().Where(p => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(p.BranchId));
        if (vendorId != null) query = query.Where(p => p.VendorId == vendorId);
        if (from != null) query = query.Where(p => p.PaymentDate >= from);
        if (to != null) query = query.Where(p => p.PaymentDate <= to);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(p => p.PaymentNo.Contains(q.Search) || (p.ReferenceNo ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Select(p => new { P = p, p.Vendor!.Party!.NameEn, p.Vendor.Party!.NameAr, Allocated = Db.VendorPaymentAllocations.Where(a => a.VendorPaymentId == p.Id).Sum(a => (decimal?)a.Amount) ?? 0 })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.P.Id, x.P.PaymentNo, x.P.VendorId, vendor = Loc(x.NameEn, x.NameAr), date = x.P.PaymentDate, x.P.Amount, method = Code(x.P.PaymentMethodId),
            reference = x.P.ReferenceNo, status = Code(x.P.StatusId), allocated = x.Allocated, unallocated = x.P.Amount - x.Allocated, x.P.Notes, x.P.JournalEntryId,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var branchId = RequireBranch(input.BranchId);
        if (input.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        if (!await Db.Vendors.AnyAsync(v => v.Id == input.VendorId)) throw new NotFoundException("Vendor", input.VendorId);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var date = input.Date ?? Today;
        var vp = new VendorPayment
        {
            BranchId = branchId, VendorId = input.VendorId, PaymentNo = await Numbers.NextAsync(NumberPrefixes.VendorPayment, date: date), PaymentDate = date,
            Amount = input.Amount, CurrencyId = await Db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            PaymentMethodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "bank_transfer", field: "method"),
            ReferenceNo = input.Reference, PaidBy = Me.UserId, Notes = input.Notes, StatusId = await LkId(LookupTypes.PaymentStatus, "completed"),
        };
        Db.VendorPayments.Add(vp);
        await Db.SaveChangesAsync();
        var total = 0m;
        foreach (var a in input.Allocations ?? new())
        {
            var bill = await Db.VendorBills.FirstOrDefaultAsync(b => b.Id == a.VendorBillId && b.VendorId == input.VendorId) ?? throw new AppException($"Bill {a.VendorBillId} does not belong to this vendor.");
            var balance = (bill.TotalAmount ?? 0) - (await Billing.PaidByBillAsync(new[] { bill.Id })).GetValueOrDefault(bill.Id);
            if (a.Amount <= 0 || a.Amount > balance) throw new AppException($"Allocation for bill {bill.BillNo} must be between 0 and {balance}.");
            total += a.Amount;
            Db.VendorPaymentAllocations.Add(new VendorPaymentAllocation { VendorPaymentId = vp.Id, VendorBillId = bill.Id, Amount = a.Amount });
        }
        if (total > vp.Amount) throw new AppException("Allocations exceed the payment amount.");
        await Db.SaveChangesAsync();
        await Billing.RecalcBillsAsync((input.Allocations ?? new()).Select(a => a.VendorBillId));
        vp.JournalEntryId = (await Posting.PostVendorPaymentAsync(vp))?.Id;
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { vp.Id, vp.PaymentNo });
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] InvoicesController.CancelInput input)
    {
        var vp = await Db.VendorPayments.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("VendorPayment", id);
        EnsureBranchAccess(vp.BranchId);
        if (await Lookups.CodeAsync(vp.StatusId) is "cancelled" or "bounced") throw new BusinessRuleException("Already cancelled.");
        if (await Db.RefillSupplierPayments.AnyAsync(r => r.VendorPaymentId == id)) throw new BusinessRuleException("This payment belongs to a refill request; cancel the refill instead.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        vp.StatusId = await LkId(LookupTypes.PaymentStatus, "cancelled");
        vp.Notes = $"{vp.Notes}\nCancelled: {input.Reason}".Trim();
        await Db.SaveChangesAsync();
        if (vp.JournalEntryId != null) await Posting.ReverseAsync(vp.JournalEntryId.Value, Today, $"Vendor payment {vp.PaymentNo} cancelled");
        await Billing.RecalcBillsAsync(await Db.VendorPaymentAllocations.Where(a => a.VendorPaymentId == id).Select(a => a.VendorBillId).ToListAsync());
        await tx.CommitAsync();
        return NoContent();
    }
}
