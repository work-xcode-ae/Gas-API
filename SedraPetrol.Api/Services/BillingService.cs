using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

public record InvoiceLineInput(string? ItemType, long? ItemTypeId, string? Description, decimal Quantity, string? Uom, decimal UnitPrice, decimal? VatRate, long? MeterReadingId = null);

public record InvoiceDraft(long BranchId, long TenantId, long? BuildingId, long? ContractId, string? InvoiceType, long? InvoiceTypeId,
    DateOnly IssueDate, DateOnly? DueDate, DateOnly? PeriodStart, DateOnly? PeriodEnd, long? CurrencyId, string? Notes,
    IReadOnlyList<InvoiceLineInput> Lines, bool Issue);

public record AllocationInput(long InvoiceId, decimal Amount);

/// <summary>Tenant billing (A/R) and vendor bills (A/P) rules shared by several controllers.</summary>
public class BillingService
{
    private static readonly string[] OpenInvoiceStatuses = { "pending", "partial", "overdue" };
    private static readonly string[] DeadPaymentStatuses = { "cancelled", "bounced" };

    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly NumberingService _numbers;
    private readonly PostingService _posting;
    private readonly IConfiguration _cfg;

    public BillingService(AppDbContext db, LookupService lookups, NumberingService numbers, PostingService posting, IConfiguration cfg)
    {
        _db = db; _lookups = lookups; _numbers = numbers; _posting = posting; _cfg = cfg;
    }

    public decimal DefaultVatRate => _cfg.GetValue<decimal?>("Billing:DefaultVatRate") ?? 5m;
    public int DueDays => _cfg.GetValue<int?>("Billing:InvoiceDueDays") ?? 30;

    public static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // ------------------------------------------------------------------ A/R

    /// <summary>Paid amount per invoice (cancelled / bounced payments do not count).</summary>
    public async Task<Dictionary<long, decimal>> PaidByInvoiceAsync(IEnumerable<long> invoiceIds)
    {
        var ids = invoiceIds.Distinct().ToList();
        if (ids.Count == 0) return new();
        var dead = await DeadPaymentStatusIdsAsync();
        return await (from a in _db.PaymentAllocations
                      join p in _db.Payments on a.PaymentId equals p.Id
                      where ids.Contains(a.InvoiceId) && (p.StatusId == null || !dead.Contains(p.StatusId.Value))
                      group a by a.InvoiceId into g
                      select new { g.Key, Paid = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Paid);
    }

    public async Task<List<long>> DeadPaymentStatusIdsAsync()
    {
        var list = new List<long>();
        foreach (var c in DeadPaymentStatuses)
            if (await _lookups.IdOrNullAsync(LookupTypes.PaymentStatus, c) is long id) list.Add(id);
        return list;
    }

    public async Task<List<long>> InvoiceStatusIdsAsync(params string[] codes)
    {
        var list = new List<long>();
        foreach (var c in codes)
            if (await _lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, c) is long id) list.Add(id);
        return list;
    }

    public async Task<Invoice> CreateInvoiceAsync(InvoiceDraft d)
    {
        if (d.Lines.Count == 0) throw new AppException("An invoice needs at least one line.");
        if (!await _db.Tenants.AnyAsync(t => t.Id == d.TenantId)) throw new NotFoundException("Tenant", d.TenantId);

        var typeId = await _lookups.ResolveAsync(LookupTypes.InvoiceType, d.InvoiceTypeId, d.InvoiceType ?? "other", field: "invoiceType");
        var inv = new Invoice
        {
            BranchId = d.BranchId,
            InvoiceNo = await _numbers.NextAsync(NumberPrefixes.Invoice, date: d.IssueDate),
            TenantId = d.TenantId,
            BuildingId = d.BuildingId,
            ContractId = d.ContractId,
            InvoiceTypeId = typeId,
            BillingPeriodStart = d.PeriodStart,
            BillingPeriodEnd = d.PeriodEnd,
            IssueDate = d.IssueDate,
            DueDate = d.DueDate ?? d.IssueDate.AddDays(DueDays),
            CurrencyId = d.CurrencyId ?? await _db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            ExchangeRate = 1,
            StatusId = await _lookups.IdAsync(LookupTypes.InvoiceStatus, d.Issue ? "pending" : "draft"),
            Notes = d.Notes,
        };
        if (inv.DueDate < inv.IssueDate) throw new AppException("Due date cannot be before the issue date.");

        var items = new List<InvoiceItem>();
        foreach (var l in d.Lines)
        {
            if (l.Quantity <= 0) throw new AppException("Line quantity must be greater than zero.");
            if (l.UnitPrice < 0) throw new AppException("Line unit price cannot be negative.");
            var amount = R2(l.Quantity * l.UnitPrice);
            items.Add(new InvoiceItem
            {
                ItemTypeId = await _lookups.ResolveAsync(LookupTypes.FeeType, l.ItemTypeId, l.ItemType, field: "itemType"),
                Description = l.Description,
                Quantity = l.Quantity,
                Uom = l.Uom,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate ?? DefaultVatRate,
                Amount = amount,
                MeterReadingId = l.MeterReadingId,
            });
        }
        inv.Subtotal = items.Sum(i => i.Amount ?? 0);
        inv.VatAmount = R2(items.Sum(i => (i.Amount ?? 0) * (i.VatRate ?? 0) / 100m));
        inv.TotalAmount = inv.Subtotal + inv.VatAmount;

        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();
        foreach (var i in items) i.InvoiceId = inv.Id;
        _db.InvoiceItems.AddRange(items);

        // Mark readings as billed so they are never billed twice.
        var readingIds = items.Where(i => i.MeterReadingId != null).Select(i => i.MeterReadingId!.Value).ToList();
        if (readingIds.Count > 0)
            await _db.MeterReadings.Where(r => readingIds.Contains(r.Id)).ForEachAsync(r => r.InvoiceId = inv.Id);
        await _db.SaveChangesAsync();

        if (d.Issue) await IssueAsync(inv, items);
        return inv;
    }

    /// <summary>draft → pending and create the A/R journal entry.</summary>
    public async Task IssueAsync(Invoice inv, IReadOnlyList<InvoiceItem>? items = null)
    {
        items ??= await _db.InvoiceItems.Where(i => i.InvoiceId == inv.Id).ToListAsync();
        inv.StatusId = await _lookups.IdAsync(LookupTypes.InvoiceStatus, "pending");
        if (inv.JournalEntryId == null)
        {
            var je = await _posting.PostInvoiceAsync(inv, items);
            inv.JournalEntryId = je?.Id;
        }
        await _db.SaveChangesAsync();
        await RecalcInvoicesAsync(new[] { inv.Id });
    }

    /// <summary>Sets pending / partial / paid / overdue from the allocations. Draft and cancelled invoices are left alone.</summary>
    public async Task RecalcInvoicesAsync(IEnumerable<long> invoiceIds)
    {
        var ids = invoiceIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var paid = await PaidByInvoiceAsync(ids);
        var frozen = await InvoiceStatusIdsAsync("draft", "cancelled");
        var invoices = await _db.Invoices.Where(i => ids.Contains(i.Id)).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var inv in invoices)
        {
            if (inv.StatusId != null && frozen.Contains(inv.StatusId.Value)) continue;
            var p = paid.GetValueOrDefault(inv.Id);
            var total = inv.TotalAmount ?? 0;
            var code = p >= total && total > 0 ? "paid"
                     : inv.DueDate < today ? "overdue"
                     : p > 0 ? "partial" : "pending";
            inv.StatusId = await _lookups.IdAsync(LookupTypes.InvoiceStatus, code);
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Allocates a payment to invoices. With no explicit allocations the amount is applied
    /// to the tenant's oldest open invoices first (FIFO). Any remainder stays unallocated (credit).
    /// </summary>
    public async Task<List<PaymentAllocation>> AllocateAsync(Payment pay, IReadOnlyList<AllocationInput>? requested)
    {
        var open = await OpenInvoicesAsync(pay.TenantId);
        var allocations = new List<PaymentAllocation>();
        var remaining = pay.Amount;

        if (requested is { Count: > 0 })
        {
            foreach (var r in requested)
            {
                if (r.Amount <= 0) throw new AppException("Allocation amounts must be positive.");
                var inv = open.FirstOrDefault(o => o.Id == r.InvoiceId)
                          ?? throw new AppException($"Invoice {r.InvoiceId} is not an open invoice of this tenant.");
                if (r.Amount > inv.Balance) throw new AppException($"Allocation {r.Amount} exceeds the balance {inv.Balance} of invoice {inv.No}.");
                if (r.Amount > remaining) throw new AppException("Allocations exceed the payment amount.");
                remaining -= r.Amount;
                allocations.Add(new PaymentAllocation { PaymentId = pay.Id, InvoiceId = r.InvoiceId, Amount = r.Amount });
            }
        }
        else
        {
            foreach (var inv in open.OrderBy(o => o.DueDate).ThenBy(o => o.Id))
            {
                if (remaining <= 0) break;
                var amt = Math.Min(remaining, inv.Balance);
                if (amt <= 0) continue;
                remaining -= amt;
                allocations.Add(new PaymentAllocation { PaymentId = pay.Id, InvoiceId = inv.Id, Amount = amt });
            }
        }
        _db.PaymentAllocations.AddRange(allocations);
        await _db.SaveChangesAsync();
        await RecalcInvoicesAsync(allocations.Select(a => a.InvoiceId));
        return allocations;
    }

    public record OpenInvoice(long Id, string No, DateOnly DueDate, decimal Total, decimal Paid)
    {
        public decimal Balance => Total - Paid;
    }

    public async Task<List<OpenInvoice>> OpenInvoicesAsync(long tenantId)
    {
        var openIds = await InvoiceStatusIdsAsync(OpenInvoiceStatuses);
        var rows = await _db.Invoices.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.StatusId != null && openIds.Contains(i.StatusId.Value))
            .Select(i => new { i.Id, i.InvoiceNo, i.DueDate, Total = i.TotalAmount ?? 0 })
            .ToListAsync();
        var paid = await PaidByInvoiceAsync(rows.Select(r => r.Id));
        return rows.Select(r => new OpenInvoice(r.Id, r.InvoiceNo, r.DueDate, r.Total, paid.GetValueOrDefault(r.Id)))
                   .Where(o => o.Balance > 0).ToList();
    }

    /// <summary>
    /// Builds a gas-consumption invoice for one supply contract: all unbilled readings of the
    /// contract's meter up to <paramref name="periodEnd"/> × the per-m3 gas price, plus monthly fees.
    /// Returns null when there is nothing to bill.
    /// </summary>
    public async Task<Invoice?> GenerateConsumptionInvoiceAsync(long contractId, DateOnly periodStart, DateOnly periodEnd, bool issue)
    {
        var c = await _db.SupplyContracts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contractId) ?? throw new NotFoundException("SupplyContract", contractId);
        var unit = await _db.Units.AsNoTracking().FirstAsync(u => u.Id == c.UnitId);
        var fees = await _db.ContractFees.AsNoTracking()
            .Where(f => f.ContractId == c.Id && f.IsActive && f.EffectiveFrom <= periodEnd && (f.EffectiveTo == null || f.EffectiveTo >= periodStart))
            .ToListAsync();
        var gasPriceType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "gas_price");
        var perM3 = await _lookups.IdOrNullAsync(LookupTypes.FeeCycle, "per_m3");
        var monthly = await _lookups.IdOrNullAsync(LookupTypes.FeeCycle, "monthly");

        var lines = new List<InvoiceLineInput>();
        if (c.MeterId != null)
        {
            var readings = await _db.MeterReadings
                .Where(r => r.MeterId == c.MeterId && r.InvoiceId == null && r.ReadingDate <= periodEnd.ToDateTime(TimeOnly.MaxValue)
                            && r.ReadingDate >= c.StartDate.ToDateTime(TimeOnly.MinValue))
                .OrderBy(r => r.ReadingDate).ToListAsync();
            var qty = readings.Sum(r => r.Consumption ?? 0);
            var price = fees.Where(f => f.FeeTypeId == gasPriceType || f.CycleId == perM3).OrderByDescending(f => f.EffectiveFrom).FirstOrDefault();
            if (readings.Count > 0 && qty > 0)
            {
                if (price == null) throw new BusinessRuleException($"Contract {c.ContractNo} has no gas price (per m³) fee.");
                // one line per reading keeps the reading → invoice link for every billed reading
                foreach (var r in readings.Where(r => (r.Consumption ?? 0) > 0))
                    lines.Add(new InvoiceLineInput("gas_price", null, $"Gas consumption – reading {r.ReadingDate:yyyy-MM-dd} ({r.ReadingValue:0.###} m³)",
                        r.Consumption!.Value, "m3", price.Amount, null, r.Id));
            }
        }

        foreach (var f in fees.Where(f => f.CycleId == monthly && f.FeeTypeId != gasPriceType))
        {
            var label = (await _lookups.GetAsync(f.FeeTypeId))?.NameEn ?? "Monthly fee";
            lines.Add(new InvoiceLineInput(null, f.FeeTypeId, $"{label} ({periodStart:MMM yyyy})", 1, "month", f.Amount, null));
        }
        if (lines.Count == 0) return null;

        return await CreateInvoiceAsync(new InvoiceDraft(c.BranchId, c.TenantId, unit.BuildingId, c.Id, "gas_consumption", null,
            DateOnly.FromDateTime(DateTime.Today), null, periodStart, periodEnd, null, null, lines, issue));
    }

    // ------------------------------------------------------------------ A/P

    public async Task<Dictionary<long, decimal>> PaidByBillAsync(IEnumerable<long> billIds)
    {
        var ids = billIds.Distinct().ToList();
        if (ids.Count == 0) return new();
        var dead = await DeadPaymentStatusIdsAsync();
        return await (from a in _db.VendorPaymentAllocations
                      join p in _db.VendorPayments on a.VendorPaymentId equals p.Id
                      where ids.Contains(a.VendorBillId) && (p.StatusId == null || !dead.Contains(p.StatusId.Value))
                      group a by a.VendorBillId into g
                      select new { g.Key, Paid = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Paid);
    }

    public async Task RecalcBillsAsync(IEnumerable<long> billIds)
    {
        var ids = billIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var paid = await PaidByBillAsync(ids);
        var frozen = new List<long>();
        foreach (var c in new[] { "draft", "cancelled" })
            if (await _lookups.IdOrNullAsync(LookupTypes.BillStatus, c) is long id) frozen.Add(id);
        foreach (var b in await _db.VendorBills.Where(b => ids.Contains(b.Id)).ToListAsync())
        {
            if (b.StatusId != null && frozen.Contains(b.StatusId.Value)) continue;
            var p = paid.GetValueOrDefault(b.Id);
            var total = b.TotalAmount ?? 0;
            var code = p >= total && total > 0 ? "paid" : p > 0 ? "partial" : "pending";
            b.StatusId = await _lookups.IdAsync(LookupTypes.BillStatus, code);
        }
        await _db.SaveChangesAsync();
    }

    public record BillLineInput(string? ItemType, long? ItemTypeId, string? Description, decimal Quantity, string? Uom, decimal UnitPrice, decimal? VatRate, long? ExpenseAccountId);

    public async Task<VendorBill> CreateVendorBillAsync(long branchId, long vendorId, string billNo, DateOnly billDate, DateOnly? dueDate,
        string? sourceType, long? sourceId, IReadOnlyList<BillLineInput> lines, string? notes, long? currencyId = null)
    {
        if (lines.Count == 0) throw new AppException("A vendor bill needs at least one line.");
        if (!await _db.Vendors.AnyAsync(v => v.Id == vendorId)) throw new NotFoundException("Vendor", vendorId);
        var bill = new VendorBill
        {
            BranchId = branchId, VendorId = vendorId, BillNo = billNo, BillDate = billDate, DueDate = dueDate ?? billDate.AddDays(DueDays),
            SourceType = sourceType, SourceId = sourceId, Notes = notes, ExchangeRate = 1,
            CurrencyId = currencyId ?? await _db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            StatusId = await _lookups.IdAsync(LookupTypes.BillStatus, "pending"),
        };
        var items = new List<VendorBillItem>();
        foreach (var l in lines)
        {
            if (l.Quantity <= 0) throw new AppException("Line quantity must be greater than zero.");
            items.Add(new VendorBillItem
            {
                ItemTypeId = await _lookups.ResolveAsync(LookupTypes.FeeType, l.ItemTypeId, l.ItemType, field: "itemType"),
                Description = l.Description, Quantity = l.Quantity, Uom = l.Uom, UnitPrice = l.UnitPrice,
                VatRate = l.VatRate ?? DefaultVatRate, Amount = R2(l.Quantity * l.UnitPrice), ExpenseAccountId = l.ExpenseAccountId,
            });
        }
        bill.Subtotal = items.Sum(i => i.Amount ?? 0);
        bill.VatAmount = R2(items.Sum(i => (i.Amount ?? 0) * (i.VatRate ?? 0) / 100m));
        bill.TotalAmount = bill.Subtotal + bill.VatAmount;
        _db.VendorBills.Add(bill);
        await _db.SaveChangesAsync();
        foreach (var i in items) i.BillId = bill.Id;
        _db.VendorBillItems.AddRange(items);
        await _db.SaveChangesAsync();
        var je = await _posting.PostVendorBillAsync(bill, items);
        bill.JournalEntryId = je?.Id;
        await _db.SaveChangesAsync();
        return bill;
    }
}
