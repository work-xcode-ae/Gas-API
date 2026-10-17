using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>account_mapping.source_type values.</summary>
public static class MappingSources
{
    public const string InvoiceItem = "invoice_item";      // credit = revenue account per FEE_TYPE (source_key)
    public const string Receivable = "receivable";         // debit  = default A/R account
    public const string Vat = "vat";                       // credit = VAT payable (output VAT)
    public const string VatInput = "vat_input";            // debit  = VAT recoverable (input VAT)
    public const string Payment = "payment";               // debit  = cash / bank per PAYMENT_METHOD (source_key)
    public const string Deposit = "deposit";               // credit = customer deposits liability
    public const string VendorBillItem = "vendor_bill_item"; // debit = expense account
    public const string Payable = "payable";               // credit = default A/P account
    public const string VendorPayment = "vendor_payment";  // credit = cash / bank per PAYMENT_METHOD (source_key)
}

/// <summary>
/// Creates balanced journal entries (journal_entry + journal_line) for invoices, payments,
/// vendor bills and vendor payments using account_mapping. If the chart of accounts or a
/// mapping is missing the document is still saved and no journal is created (returns null).
/// </summary>
public class PostingService
{
    private readonly AppDbContext _db;
    private readonly NumberingService _numbers;
    private readonly LookupService _lookups;
    private readonly ICurrentUser _user;
    private readonly ILogger<PostingService> _log;

    public PostingService(AppDbContext db, NumberingService numbers, LookupService lookups, ICurrentUser user, ILogger<PostingService> log)
    {
        _db = db; _numbers = numbers; _lookups = lookups; _user = user; _log = log;
    }

    public record Line(long AccountId, decimal Debit, decimal Credit, string? Description = null,
        long? BuildingId = null, long? UnitId = null, long? TenantId = null, long? VendorId = null, long? ContractId = null);

    private async Task<long> CompanyIdAsync(long branchId) =>
        await _db.Branches.Where(b => b.Id == branchId).Select(b => (long?)b.CompanyId).FirstOrDefaultAsync()
        ?? throw new AppException($"Branch {branchId} not found.");

    private async Task<long?> MapAsync(long companyId, long? branchId, string source, long? key, bool debit)
    {
        var rows = await _db.AccountMappings.AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.SourceType == source && m.IsActive
                        && (m.BranchId == null || m.BranchId == branchId)
                        && (m.SourceKeyId == null || m.SourceKeyId == key))
            .ToListAsync();
        // most specific first: branch + key, then key, then branch, then generic
        var best = rows.OrderByDescending(m => (m.SourceKeyId != null ? 2 : 0) + (m.BranchId != null ? 1 : 0)).FirstOrDefault();
        return debit ? best?.DebitAccountId : best?.CreditAccountId;
    }

    /// <summary>Creates (and optionally posts) a journal entry. Validates balance and open period.</summary>
    public async Task<JournalEntry> CreateEntryAsync(long branchId, DateOnly date, string description, string sourceType, long? sourceId,
        IEnumerable<Line> lines, bool post, long? currencyId = null)
    {
        var companyId = await CompanyIdAsync(branchId);
        var lineList = lines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
        if (lineList.Count < 2) throw new AppException("A journal entry needs at least two lines.");
        foreach (var l in lineList)
            if ((l.Debit == 0) == (l.Credit == 0) || l.Debit < 0 || l.Credit < 0)
                throw new AppException("Each journal line must have either a positive debit or a positive credit.");
        var dr = lineList.Sum(l => l.Debit);
        var cr = lineList.Sum(l => l.Credit);
        if (dr != cr) throw new AppException($"Journal entry is not balanced (debit {dr} ≠ credit {cr}).");

        var period = await _db.FiscalPeriods.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.StartDate <= date && p.EndDate >= date)
                     ?? throw new AppException($"No fiscal period covers {date:yyyy-MM-dd}. Create it under Accounting > Fiscal periods.");
        if (period.IsClosed) throw new BusinessRuleException($"Fiscal period {period.Year}-{period.PeriodNo:00} is closed.");

        var accountIds = lineList.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts.Where(a => accountIds.Contains(a.Id)).ToListAsync();
        if (accounts.Count != accountIds.Count) throw new AppException("One or more journal accounts do not exist.");
        var bad = accounts.FirstOrDefault(a => !a.IsPostable || a.IsGroup || !a.IsActive);
        if (bad != null) throw new AppException($"Account {bad.AccountCode} is a group or inactive account and cannot be posted to.");

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            BranchId = branchId,
            JournalNo = await _numbers.NextAsync(NumberPrefixes.Journal, date: date),
            EntryDate = date,
            FiscalPeriodId = period.Id,
            CurrencyId = currencyId,
            ExchangeRate = 1,
            Description = description,
            SourceType = sourceType,
            SourceId = sourceId,
            StatusId = await _lookups.IdAsync(LookupTypes.JournalStatus, post ? "posted" : "draft"),
            PostedAt = post ? DateTimeOffset.UtcNow : null,
            PostedBy = post ? _user.UserId : null,
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        _db.JournalLines.AddRange(lineList.Select(l => new JournalLine
        {
            JournalEntryId = entry.Id,
            AccountId = l.AccountId,
            Description = l.Description,
            Debit = l.Debit,
            Credit = l.Credit,
            BaseDebit = l.Debit,
            BaseCredit = l.Credit,
            BuildingId = l.BuildingId,
            UnitId = l.UnitId,
            TenantId = l.TenantId,
            VendorId = l.VendorId,
            ContractId = l.ContractId,
        }));
        await _db.SaveChangesAsync();
        return entry;
    }

    private async Task<JournalEntry?> TryAsync(Func<Task<JournalEntry?>> build, string what)
    {
        try
        {
            return await build();
        }
        catch (AppException ex) when (ex is not BusinessRuleException)
        {
            _log.LogWarning("Journal for {What} was not created: {Message}", what, ex.Message);
            return null;
        }
    }

    /// <summary>Dr A/R, Cr revenue per item, Cr VAT.</summary>
    public Task<JournalEntry?> PostInvoiceAsync(Invoice inv, IReadOnlyList<InvoiceItem> items) => TryAsync(async () =>
    {
        var companyId = await CompanyIdAsync(inv.BranchId);
        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == inv.TenantId);
        var ar = tenant.ReceivableAccountId ?? await MapAsync(companyId, inv.BranchId, MappingSources.Receivable, null, true);
        if (ar == null) return null;
        var lines = new List<Line> { new(ar.Value, inv.TotalAmount ?? 0, 0, $"Invoice {inv.InvoiceNo}", inv.BuildingId, null, inv.TenantId, null, inv.ContractId) };
        foreach (var it in items)
        {
            var acc = await MapAsync(companyId, inv.BranchId, MappingSources.InvoiceItem, it.ItemTypeId, false);
            if (acc == null) return null;
            lines.Add(new(acc.Value, 0, it.Amount ?? 0, it.Description, inv.BuildingId, null, inv.TenantId, null, inv.ContractId));
        }
        if ((inv.VatAmount ?? 0) > 0)
        {
            var vat = await MapAsync(companyId, inv.BranchId, MappingSources.Vat, null, false);
            if (vat == null) return null;
            lines.Add(new(vat.Value, 0, inv.VatAmount!.Value, "Output VAT", inv.BuildingId, null, inv.TenantId, null, inv.ContractId));
        }
        return await CreateEntryAsync(inv.BranchId, inv.IssueDate, $"Invoice {inv.InvoiceNo}", "invoice", inv.Id, lines, true, inv.CurrencyId);
    }, $"invoice {inv.InvoiceNo}");

    /// <summary>Dr cash/bank (by payment method), Cr A/R.</summary>
    public Task<JournalEntry?> PostPaymentAsync(Payment pay) => TryAsync(async () =>
    {
        var companyId = await CompanyIdAsync(pay.BranchId);
        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == pay.TenantId);
        var cash = await MapAsync(companyId, pay.BranchId, MappingSources.Payment, pay.PaymentMethodId, true);
        var ar = tenant.ReceivableAccountId ?? await MapAsync(companyId, pay.BranchId, MappingSources.Receivable, null, true);
        if (cash == null || ar == null) return null;
        return await CreateEntryAsync(pay.BranchId, pay.PaymentDate, $"Receipt {pay.ReceiptNo}", "payment", pay.Id, new[]
        {
            new Line(cash.Value, pay.Amount, 0, $"Receipt {pay.ReceiptNo}", TenantId: pay.TenantId),
            new Line(ar.Value, 0, pay.Amount, $"Receipt {pay.ReceiptNo}", TenantId: pay.TenantId),
        }, true, pay.CurrencyId);
    }, $"payment {pay.ReceiptNo}");

    /// <summary>Dr expense per item, Dr input VAT, Cr A/P.</summary>
    public Task<JournalEntry?> PostVendorBillAsync(VendorBill bill, IReadOnlyList<VendorBillItem> items) => TryAsync(async () =>
    {
        var companyId = await CompanyIdAsync(bill.BranchId);
        var vendor = await _db.Vendors.AsNoTracking().FirstAsync(v => v.Id == bill.VendorId);
        var ap = vendor.PayableAccountId ?? await MapAsync(companyId, bill.BranchId, MappingSources.Payable, null, false);
        if (ap == null) return null;
        var lines = new List<Line>();
        foreach (var it in items)
        {
            var acc = it.ExpenseAccountId ?? await MapAsync(companyId, bill.BranchId, MappingSources.VendorBillItem, it.ItemTypeId, true);
            if (acc == null) return null;
            lines.Add(new(acc.Value, it.Amount ?? 0, 0, it.Description, VendorId: bill.VendorId));
        }
        if ((bill.VatAmount ?? 0) > 0)
        {
            var vat = await MapAsync(companyId, bill.BranchId, MappingSources.VatInput, null, true);
            if (vat == null) return null;
            lines.Add(new(vat.Value, bill.VatAmount!.Value, 0, "Input VAT", VendorId: bill.VendorId));
        }
        lines.Add(new(ap.Value, 0, bill.TotalAmount ?? 0, $"Bill {bill.BillNo}", VendorId: bill.VendorId));
        return await CreateEntryAsync(bill.BranchId, bill.BillDate, $"Vendor bill {bill.BillNo}", "vendor_bill", bill.Id, lines, true, bill.CurrencyId);
    }, $"vendor bill {bill.BillNo}");

    /// <summary>Dr A/P, Cr cash/bank.</summary>
    public Task<JournalEntry?> PostVendorPaymentAsync(VendorPayment vp) => TryAsync(async () =>
    {
        var companyId = await CompanyIdAsync(vp.BranchId);
        var vendor = await _db.Vendors.AsNoTracking().FirstAsync(v => v.Id == vp.VendorId);
        var ap = vendor.PayableAccountId ?? await MapAsync(companyId, vp.BranchId, MappingSources.Payable, null, false);
        var bank = await MapAsync(companyId, vp.BranchId, MappingSources.VendorPayment, vp.PaymentMethodId, false);
        if (ap == null || bank == null) return null;
        return await CreateEntryAsync(vp.BranchId, vp.PaymentDate, $"Vendor payment {vp.PaymentNo}", "vendor_payment", vp.Id, new[]
        {
            new Line(ap.Value, vp.Amount, 0, $"Vendor payment {vp.PaymentNo}", VendorId: vp.VendorId),
            new Line(bank.Value, 0, vp.Amount, $"Vendor payment {vp.PaymentNo}", VendorId: vp.VendorId),
        }, true, vp.CurrencyId);
    }, $"vendor payment {vp.PaymentNo}");

    /// <summary>Creates a posted mirror entry and marks the original as reversed.</summary>
    public async Task<JournalEntry> ReverseAsync(long journalEntryId, DateOnly date, string reason)
    {
        var je = await _db.JournalEntries.FirstOrDefaultAsync(j => j.Id == journalEntryId) ?? throw new NotFoundException("JournalEntry", journalEntryId);
        if (!await _lookups.IsAsync(je.StatusId, LookupTypes.JournalStatus, "posted"))
            throw new BusinessRuleException("Only posted journal entries can be reversed.");
        var lines = await _db.JournalLines.AsNoTracking().Where(l => l.JournalEntryId == je.Id).ToListAsync();
        var rev = await CreateEntryAsync(je.BranchId, date, $"Reversal of {je.JournalNo}: {reason}", je.SourceType ?? "manual", je.SourceId,
            lines.Select(l => new Line(l.AccountId, l.Credit ?? 0, l.Debit ?? 0, l.Description, l.BuildingId, l.UnitId, l.TenantId, l.VendorId, l.ContractId)),
            true, je.CurrencyId);
        je.StatusId = await _lookups.IdAsync(LookupTypes.JournalStatus, "reversed");
        je.ReversedEntryId = rev.Id;
        await _db.SaveChangesAsync();
        return rev;
    }
}
