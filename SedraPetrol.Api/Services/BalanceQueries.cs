using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Balance figures (never stored — computed like the v_contract_balance view in the schema doc).
/// Live invoices = not draft / cancelled. Live payments = not cancelled / bounced.
/// </summary>
public class BalanceQueries
{
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    public BalanceQueries(AppDbContext db, LookupService lookups) { _db = db; _lookups = lookups; }

    public record Balance(decimal Invoiced, decimal Paid, decimal Overdue)
    {
        public decimal Outstanding => Invoiced - Paid;
        public string PaymentStatus => Overdue > 0 ? "overdue" : Outstanding > 0 ? "pending" : "paid";
    }

    public async Task<(List<long?> DeadInvoice, List<long?> DeadPayment)> DeadStatusesAsync()
    {
        var inv = new List<long?> { await _lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "draft"), await _lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled") };
        var pay = new List<long?> { await _lookups.IdOrNullAsync(LookupTypes.PaymentStatus, "cancelled"), await _lookups.IdOrNullAsync(LookupTypes.PaymentStatus, "bounced") };
        return (inv, pay);
    }

    public Task<Dictionary<long, Balance>> ByTenantAsync(IEnumerable<long> tenantIds) =>
        ComputeAsync(tenantIds, byContract: false);

    public Task<Dictionary<long, Balance>> ByContractAsync(IEnumerable<long> contractIds) =>
        ComputeAsync(contractIds, byContract: true);

    private async Task<Dictionary<long, Balance>> ComputeAsync(IEnumerable<long> keys, bool byContract)
    {
        var ids = keys.Distinct().ToList();
        if (ids.Count == 0) return new();
        var (deadInv, deadPay) = await DeadStatusesAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var invoices = _db.Invoices.AsNoTracking().Where(i => !deadInv.Contains(i.StatusId));
        invoices = byContract ? invoices.Where(i => i.ContractId != null && ids.Contains(i.ContractId.Value)) : invoices.Where(i => ids.Contains(i.TenantId));

        var inv = await invoices.Select(i => new
        {
            Key = byContract ? i.ContractId!.Value : i.TenantId,
            Total = i.TotalAmount ?? 0,
            Overdue = i.DueDate < today,
            // settled = receipts allocated + credit notes applied
            Paid = (_db.PaymentAllocations.Where(a => a.InvoiceId == i.Id && !deadPay.Contains(a.Payment!.StatusId)).Sum(a => (decimal?)a.Amount) ?? 0)
                 + (_db.CreditNoteAllocations.Where(a => a.InvoiceId == i.Id && a.CreditNote!.Status == "posted").Sum(a => (decimal?)a.Amount) ?? 0),
        }).ToListAsync();

        return inv.GroupBy(x => x.Key).ToDictionary(g => g.Key, g => new Balance(
            g.Sum(x => x.Total), g.Sum(x => x.Paid), g.Where(x => x.Overdue).Sum(x => Math.Max(0, x.Total - x.Paid))));
    }

    /// <summary>
    /// Credit on account: unallocated receipts + unapplied credit notes − refunds paid out of them.
    /// </summary>
    public async Task<decimal> TenantCreditAsync(long tenantId)
    {
        var (_, deadPay) = await DeadStatusesAsync();
        var paid = await _db.Payments.Where(p => p.TenantId == tenantId && !deadPay.Contains(p.StatusId)).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var allocated = await _db.PaymentAllocations.Where(a => a.Payment!.TenantId == tenantId && !deadPay.Contains(a.Payment.StatusId)).SumAsync(a => (decimal?)a.Amount) ?? 0;
        var credited = await _db.CreditNotes.Where(c => c.TenantId == tenantId && c.Status == "posted").SumAsync(c => (decimal?)c.TotalAmount) ?? 0;
        var applied = await _db.CreditNoteAllocations.Where(a => a.CreditNote!.TenantId == tenantId && a.CreditNote.Status == "posted").SumAsync(a => (decimal?)a.Amount) ?? 0;
        var refunded = await _db.CustomerRefunds.Where(r => r.TenantId == tenantId && r.Status == "posted").SumAsync(r => (decimal?)r.Amount) ?? 0;
        return paid - allocated + credited - applied - refunded;
    }
}
