using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>
/// AMC revenue is earned over the service period, not on the invoice day.
/// An AMC instalment invoice (item type «amc») credits 2401 Deferred revenue – AMC. This service moves the earned part to
/// revenue (Dr 2401 / Cr maintenance revenue 4102) month by month, in proportion to the days of the instalment's period
/// (invoice billing period start → end), one entry per month-end (or the period end) that has passed.
/// It is self-correcting: each run posts target − already recognised, so a credit note on the invoice is spread too.
/// Entries: source_type «amc_revenue», source_id = invoice id. A month in a closed fiscal period is skipped (logged) and
/// caught up by the next month's entry. Cancelling the invoice reverses its recognition entries (InvoicesController).
/// </summary>
public class AmcRevenueService
{
    public const string SourceType = "amc_revenue";
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly PostingService _posting;
    private readonly ILogger<AmcRevenueService> _log;

    public AmcRevenueService(AppDbContext db, LookupService lookups, PostingService posting, ILogger<AmcRevenueService> log)
    { _db = db; _lookups = lookups; _posting = posting; _log = log; }

    public record Result(int Invoices, int Entries, decimal Amount, List<string> Skipped);

    /// <summary>Recognise everything earned up to <paramref name="asOf"/> (default today). <paramref name="invoiceId"/> limits it to one invoice.</summary>
    public async Task<Result> RecognizeAsync(DateOnly? asOf = null, long? invoiceId = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var upTo = asOf == null || asOf > today ? today : asOf.Value;
        var amcType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "amc");
        if (amcType == null) return new(0, 0, 0, new());
        var dead = new List<long?> { await _lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "draft"), await _lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled") };
        var posted = await _lookups.IdOrNullAsync(LookupTypes.JournalStatus, "posted");

        var invoices = await _db.Invoices.AsNoTracking()
            .Where(i => !dead.Contains(i.StatusId) && i.BillingPeriodStart != null && i.BillingPeriodStart <= upTo
                        && (invoiceId == null || i.Id == invoiceId) && _db.InvoiceItems.Any(it => it.InvoiceId == i.Id && it.ItemTypeId == amcType))
            .Select(i => new { i.Id, i.InvoiceNo, i.BranchId, i.TenantId, i.BuildingId, i.ContractId, i.CurrencyId, Start = i.BillingPeriodStart!.Value, End = i.BillingPeriodEnd ?? i.BillingPeriodStart!.Value, i.IssueDate })
            .ToListAsync();
        if (invoices.Count == 0) return new(0, 0, 0, new());
        var ids = invoices.Select(i => i.Id).ToList();
        var net = await _db.InvoiceItems.AsNoTracking().Where(it => ids.Contains(it.InvoiceId) && it.ItemTypeId == amcType)
            .GroupBy(it => it.InvoiceId).Select(g => new { g.Key, Amt = g.Sum(x => x.Amount ?? 0) }).ToDictionaryAsync(x => x.Key, x => x.Amt);
        var credited = await _db.CreditNoteLines.AsNoTracking().Where(l => l.CreditNote!.Status == "posted" && ids.Contains(l.CreditNote.InvoiceId) && l.ItemTypeId == amcType)
            .GroupBy(l => l.CreditNote!.InvoiceId).Select(g => new { g.Key, Amt = g.Sum(x => x.Amount), Last = g.Max(x => x.CreditNote!.IssueDate) }).ToDictionaryAsync(x => x.Key, x => x);
        var companyId = await _db.Companies.Select(c => c.Id).FirstAsync();
        var deferredAcc = await _db.AccountMappings.Where(m => m.CompanyId == companyId && m.SourceType == MappingSources.InvoiceItem && m.SourceKeyId == amcType && m.IsActive)
            .Select(m => m.CreditAccountId).FirstOrDefaultAsync();
        var maintenanceType = await _lookups.IdOrNullAsync(LookupTypes.FeeType, "maintenance");
        var revenueAcc = await _db.AccountMappings.Where(m => m.CompanyId == companyId && m.SourceType == MappingSources.InvoiceItem && m.SourceKeyId == maintenanceType && m.IsActive)
            .Select(m => m.CreditAccountId).FirstOrDefaultAsync();
        if (deferredAcc == null || revenueAcc == null)
            throw new BusinessRuleException("Map FEE_TYPE «amc» (deferred revenue) and «maintenance» (revenue) under Accounting > Account mappings.");

        // already recognised per invoice = credits on revenue − debits, from live (posted) recognition entries and their reversals
        var done = await _db.JournalLines.AsNoTracking()
            .Where(l => l.JournalEntry!.SourceType == SourceType && l.JournalEntry.SourceId != null && ids.Contains(l.JournalEntry.SourceId.Value)
                        && l.AccountId == revenueAcc && l.JournalEntry.StatusId != null && l.JournalEntry.Status!.Code != "draft")
            .GroupBy(l => l.JournalEntry!.SourceId!.Value).Select(g => new { g.Key, Amt = g.Sum(x => (x.Credit ?? 0) - (x.Debit ?? 0)), Last = g.Max(x => x.JournalEntry!.EntryDate) })
            .ToDictionaryAsync(x => x.Key, x => x);

        int entries = 0, touched = 0; decimal total = 0; var skipped = new List<string>();
        foreach (var inv in invoices)
        {
            var cn = credited.GetValueOrDefault(inv.Id);
            var amount = net.GetValueOrDefault(inv.Id) - (cn?.Amt ?? 0);
            // nothing is dated before the invoice (or its latest credit note): month-ends that passed before that are caught up in one entry on that date
            var floor = cn != null && cn.Last > inv.IssueDate ? cn.Last : inv.IssueDate;
            var days = inv.End.DayNumber - inv.Start.DayNumber + 1;
            if (days <= 0) continue;
            var recognised = done.GetValueOrDefault(inv.Id)?.Amt ?? 0;
            var lastPosted = done.GetValueOrDefault(inv.Id)?.Last;   // a checkpoint before the latest entry was already handled
            var any = false;
            // one checkpoint per month-end inside the period, plus the period end
            for (var m = new DateOnly(inv.Start.Year, inv.Start.Month, 1); m <= inv.End; m = m.AddMonths(1))
            {
                var point = m.AddMonths(1).AddDays(-1);
                if (point > inv.End) point = inv.End;
                if (point > upTo) break;
                var postOn = point;
                if (point < floor)
                {
                    var nextPoint = point.AddDays(1).AddMonths(1).AddDays(-1);
                    if (nextPoint > inv.End) nextPoint = inv.End;
                    if (point < inv.End && nextPoint < floor) continue;   // a later month-end is still before the floor
                    postOn = floor;
                    if (postOn > upTo) break;
                }
                if (lastPosted != null && lastPosted > postOn) continue;
                var elapsed = point.DayNumber - inv.Start.DayNumber + 1;
                var target = point == inv.End ? amount : BillingService.R2(amount * elapsed / days);
                var diff = target - recognised;
                if (diff == 0) continue;
                try
                {
                    var text = postOn == point ? $"AMC revenue {point:yyyy-MM} – invoice {inv.InvoiceNo}" : $"AMC revenue up to {point:yyyy-MM} – invoice {inv.InvoiceNo}";
                    var lines = diff > 0
                        ? new[] { new PostingService.Line(deferredAcc.Value, diff, 0, text, inv.BuildingId, null, inv.TenantId, null, inv.ContractId),
                                  new PostingService.Line(revenueAcc.Value, 0, diff, text, inv.BuildingId, null, inv.TenantId, null, inv.ContractId) }
                        : new[] { new PostingService.Line(revenueAcc.Value, -diff, 0, text, inv.BuildingId, null, inv.TenantId, null, inv.ContractId),
                                  new PostingService.Line(deferredAcc.Value, 0, -diff, text, inv.BuildingId, null, inv.TenantId, null, inv.ContractId) };
                    await _posting.CreateEntryAsync(inv.BranchId, postOn, text, SourceType, inv.Id, lines, true, inv.CurrencyId);
                    recognised += diff; total += diff; entries++; any = true; lastPosted = postOn;
                }
                catch (BusinessRuleException ex)
                {
                    // e.g. the month is closed (checked before anything is added): the next month-end catches the amount up
                    skipped.Add($"{inv.InvoiceNo} {point:yyyy-MM}: {ex.Message}");
                    _log.LogWarning("AMC revenue {Invoice} {Month} not recognised: {Message}", inv.InvoiceNo, point.ToString("yyyy-MM"), ex.Message);
                }
            }
            if (any) touched++;
        }
        return new(touched, entries, total, skipped);
    }

    /// <summary>Reverse the recognition entries of an invoice (it is being cancelled).</summary>
    public async Task ReverseForInvoiceAsync(long invoiceId, DateOnly date, string reason)
    {
        var posted = await _lookups.IdOrNullAsync(LookupTypes.JournalStatus, "posted");
        var list = await _db.JournalEntries.Where(j => j.SourceType == SourceType && j.SourceId == invoiceId && j.StatusId == posted && j.ReversedEntryId == null
                                                       && !j.Description!.StartsWith("Reversal of")).Select(j => j.Id).ToListAsync();
        foreach (var id in list) await _posting.ReverseAsync(id, date, reason);
    }
}
