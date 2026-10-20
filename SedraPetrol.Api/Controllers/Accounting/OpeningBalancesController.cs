using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Opening balances at the cut-over date (BRD §5.2, risk "inaccurate opening balances"):
///   • GL accounts  → one balanced journal (source «opening»); the difference goes to the opening-balance equity account.
///   • customers    → one issued invoice per customer, type «opening_balance» (Dr receivable / Cr opening equity), so it shows in statements and aging.
///   • suppliers    → one vendor bill per supplier, source «opening» (Dr opening equity / Cr payable).
/// Receivable / payable control accounts are not entered in the GL screen: their balances come from the customer / supplier openings.
/// A party can have only one (non-cancelled) opening balance.
/// </summary>
[Route("api/opening-balances")]
[PermissionModule("accounting")]
public class OpeningBalancesController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record GlLine(long AccountId, decimal Debit, decimal Credit, string? Description);
    public record GlInput(long? BranchId, DateOnly Date, List<GlLine> Lines);
    /// <summary>Customer by id or by code (CSV import). Amount &gt; 0 = the customer owes us.</summary>
    public record PartyLine(long? Id, string? Code, decimal Amount, DateOnly? DueDate, string? Reference, string? Notes);
    public record PartyInput(long? BranchId, DateOnly Date, List<PartyLine> Lines);

    private const string OpeningCode = "opening_balance";

    private async Task<long> CompanyIdAsync() => Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync();

    /// <summary>The opening-balance equity account (setting, else account 3301, created under the equity group when missing).</summary>
    private async Task<long> OpeningEquityAccountAsync()
    {
        var companyId = await CompanyIdAsync();
        var setting = await Db.AccountingSettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);
        if (setting?.OpeningEquityAccountId is long id) return id;
        var acc = await Db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "3301");
        if (acc == null)
        {
            var equityType = await Lookups.IdOrNullAsync(LookupTypes.AccountType, "equity");
            var parent = await Db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == "3")
                         ?? await Db.Accounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.IsGroup && a.AccountTypeId == equityType);
            acc = new Account
            {
                CompanyId = companyId, AccountCode = "3301", NameEn = "Opening balance equity", NameAr = "حقوق ملكية - أرصدة افتتاحية",
                AccountTypeId = equityType, ParentAccountId = parent?.Id, Level = (short)((parent?.Level ?? 1) + 1), IsGroup = false, IsPostable = true, IsActive = true,
                CurrencyId = await Db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            };
            Db.Accounts.Add(acc);
            await Db.SaveChangesAsync();
        }
        if (setting == null) { setting = new AccountingSetting { CompanyId = companyId }; Db.AccountingSettings.Add(setting); }
        setting.OpeningEquityAccountId = acc.Id;
        await Db.SaveChangesAsync();
        return acc.Id;
    }

    /// <summary>Makes sure a lookup value exists (INVOICE_TYPE / FEE_TYPE «opening_balance»).</summary>
    private async Task EnsureLookupAsync(string typeCode, string code, string en, string ar)
    {
        var type = await Db.LookupTypes.FirstOrDefaultAsync(t => t.Code == typeCode) ?? throw new AppException($"Lookup type {typeCode} is missing.");
        if (await Db.LookupValues.IgnoreQueryFilters().AnyAsync(v => v.LookupTypeId == type.Id && v.Code == code)) return;
        Db.LookupValues.Add(new LookupValue { LookupTypeId = type.Id, Code = code, NameEn = en, NameAr = ar, SortOrder = 900, IsActive = true });
        await Db.SaveChangesAsync();
        Lookups.Invalidate();
    }

    /// <summary>Receivable / payable control accounts (from the default mappings).</summary>
    private async Task<HashSet<long>> ControlAccountsAsync()
    {
        var companyId = await CompanyIdAsync();
        var ids = await Db.AccountMappings.AsNoTracking().Where(m => m.CompanyId == companyId && m.SourceKeyId == null
                && (m.SourceType == MappingSources.Receivable || m.SourceType == MappingSources.Payable))
            .Select(m => new { m.DebitAccountId, m.CreditAccountId }).ToListAsync();
        return ids.SelectMany(x => new[] { x.DebitAccountId, x.CreditAccountId }).Where(x => x != null).Select(x => x!.Value).ToHashSet();
    }

    /// <summary>What has been entered so far.</summary>
    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        await LoadLookups();
        var openingType = await Lookups.IdOrNullAsync(LookupTypes.InvoiceType, OpeningCode);
        var cancelledInv = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled");
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var reversed = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "reversed");
        var journals = await Db.JournalEntries.AsNoTracking().Where(j => j.SourceType == "opening")
            .Select(j => new { j.Id, j.JournalNo, j.EntryDate, j.BranchId, j.StatusId, Total = Db.JournalLines.Where(l => l.JournalEntryId == j.Id).Sum(l => l.Debit ?? 0) })
            .OrderByDescending(j => j.EntryDate).ToListAsync(ct);
        var customers = openingType == null ? new List<CustomerRow>() : (await Db.Invoices.AsNoTracking()
            .Where(i => i.InvoiceTypeId == openingType && i.StatusId != cancelledInv)
            .Select(i => new { i.Id, i.InvoiceNo, i.TenantId, Code = i.Tenant!.Party!.PartyCode, Name = i.Tenant.Party!.NameAr ?? i.Tenant.Party!.NameEn, i.IssueDate, i.DueDate, Amount = i.TotalAmount ?? 0 })
            .ToListAsync(ct)).Select(i => new CustomerRow(i.Id, i.InvoiceNo, i.TenantId, i.Code, i.Name, i.IssueDate, i.DueDate, i.Amount)).OrderBy(i => i.Name).ToList();
        var suppliers = await Db.VendorBills.AsNoTracking().Where(b => b.SourceType == "opening" && b.StatusId != cancelledBill)
            .Select(b => new { b.Id, b.BillNo, b.VendorId, code = b.Vendor!.Party!.PartyCode, name = b.Vendor.Party!.NameAr ?? b.Vendor.Party!.NameEn, date = b.BillDate, b.DueDate, amount = b.TotalAmount ?? 0 })
            .OrderBy(b => b.name).ToListAsync(ct);
        var equityId = await Db.AccountingSettings.AsNoTracking().Select(s => s.OpeningEquityAccountId).FirstOrDefaultAsync(ct);
        var equity = equityId == null ? null : await Db.Accounts.AsNoTracking().Where(a => a.Id == equityId).Select(a => a.AccountCode + " " + (a.NameAr ?? a.NameEn)).FirstOrDefaultAsync(ct);
        return Ok(new
        {
            openingEquityAccount = equity,
            journals = journals.Select(j => new { j.Id, j.JournalNo, j.EntryDate, j.BranchId, status = Code(j.StatusId), reversed = j.StatusId == reversed, j.Total }),
            customers, customersTotal = customers.Sum(c => c.Amount),
            suppliers, suppliersTotal = suppliers.Sum(s => s.amount),
            controlAccounts = await ControlAccountsAsync(),
        });
    }

    public record CustomerRow(long Id, string InvoiceNo, long TenantId, string? Code, string? Name, DateOnly Date, DateOnly DueDate, decimal Amount);

    /// <summary>GL opening journal. The difference between debits and credits is booked to the opening-balance equity account.</summary>
    [HttpPost("gl")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Gl([FromBody] GlInput input)
    {
        var branchId = RequireBranch(input.BranchId);
        var lines = input.Lines.Where(l => l.Debit != 0 || l.Credit != 0).ToList();
        if (lines.Count == 0) throw new AppException("Enter at least one account balance.");
        if (lines.Any(l => l.Debit < 0 || l.Credit < 0 || (l.Debit > 0 && l.Credit > 0))) throw new AppException("Each line has either a debit or a credit, not both and not negative.");
        if (lines.GroupBy(l => l.AccountId).Any(g => g.Count() > 1)) throw new AppException("An account is entered twice.");
        var control = await ControlAccountsAsync();
        if (lines.Any(l => control.Contains(l.AccountId)))
            throw new BusinessRuleException("Receivable / payable control accounts come from the customer and supplier opening balances — remove them from this list.");
        var reversed = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "reversed");
        if (await Db.JournalEntries.AnyAsync(j => j.SourceType == "opening" && j.BranchId == branchId && j.StatusId != reversed))
            throw new BusinessRuleException("An opening journal already exists for this branch. Reverse it first to enter it again.");
        var equity = await OpeningEquityAccountAsync();
        if (lines.Any(l => l.AccountId == equity)) throw new AppException("Do not enter the opening-balance equity account; it receives the difference automatically.");
        var diff = lines.Sum(l => l.Debit) - lines.Sum(l => l.Credit);
        var post = lines.Select(l => new PostingService.Line(l.AccountId, l.Debit, l.Credit, l.Description ?? "Opening balance")).ToList();
        if (diff > 0) post.Add(new PostingService.Line(equity, 0, diff, "Opening balance difference"));
        if (diff < 0) post.Add(new PostingService.Line(equity, -diff, 0, "Opening balance difference"));
        await using var tx = await Db.Database.BeginTransactionAsync();
        var je = await Posting.CreateEntryAsync(branchId, input.Date, $"Opening balances as of {input.Date:yyyy-MM-dd}", "opening", null, post, true);
        Activity.Log("journal_entry", je.Id, "opening", $"Opening balances journal {je.JournalNo} ({lines.Count} accounts)", null, branchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { je.Id, je.JournalNo, difference = diff });
    }

    /// <summary>Customer opening balances: one issued invoice per customer.</summary>
    [HttpPost("customers")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Customers([FromBody] PartyInput input)
    {
        if (input.Lines.Count == 0) throw new AppException("Enter at least one customer.");
        if (input.Lines.Any(l => l.Amount <= 0)) throw new AppException("Opening amounts must be greater than zero (record customer credits as an advance receipt).");
        await EnsureLookupAsync(LookupTypes.InvoiceType, OpeningCode, "Opening balance", "رصيد افتتاحي");
        await EnsureLookupAsync(LookupTypes.FeeType, OpeningCode, "Opening balance", "رصيد افتتاحي");
        var equity = await OpeningEquityAccountAsync();
        // revenue mapping for the «opening_balance» item → opening equity (so nothing is booked as revenue)
        var companyId = await CompanyIdAsync();
        var feeId = await LkId(LookupTypes.FeeType, OpeningCode);
        if (!await Db.AccountMappings.AnyAsync(m => m.CompanyId == companyId && m.SourceType == MappingSources.InvoiceItem && m.SourceKeyId == feeId && m.BranchId == null))
        {
            Db.AccountMappings.Add(new AccountMapping { CompanyId = companyId, SourceType = MappingSources.InvoiceItem, SourceKeyId = feeId, CreditAccountId = equity, IsActive = true });
            await Db.SaveChangesAsync();
        }
        var openingType = await LkId(LookupTypes.InvoiceType, OpeningCode);
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled");
        var created = new List<object>();
        await using var tx = await Db.Database.BeginTransactionAsync();
        foreach (var l in input.Lines)
        {
            var t = l.Id != null ? await Db.Tenants.FirstOrDefaultAsync(x => x.Id == l.Id)
                  : await Db.Tenants.FirstOrDefaultAsync(x => x.Party!.PartyCode == (l.Code ?? "").Trim());
            if (t == null) throw new AppException($"Customer {(l.Id?.ToString() ?? l.Code)} was not found.");
            if (await Db.Invoices.AnyAsync(i => i.TenantId == t.Id && i.InvoiceTypeId == openingType && i.StatusId != cancelled))
                throw new BusinessRuleException($"Customer {t.Party!.PartyCode} already has an opening balance. Cancel it first to enter it again.");
            var inv = await Billing.CreateInvoiceAsync(new InvoiceDraft(input.BranchId ?? t.BranchId, t.Id, null, null, OpeningCode, null, input.Date, l.DueDate ?? input.Date,
                null, null, null, l.Notes ?? $"Opening balance as of {input.Date:yyyy-MM-dd}{(string.IsNullOrWhiteSpace(l.Reference) ? "" : $" — {l.Reference}")}",
                new[] { new InvoiceLineInput(OpeningCode, null, $"Opening balance {input.Date:yyyy-MM-dd}{(string.IsNullOrWhiteSpace(l.Reference) ? "" : $" ({l.Reference})")}", 1, "each", l.Amount, 0) },
                true));
            created.Add(new { inv.Id, inv.InvoiceNo, t.Party!.PartyCode, l.Amount });
        }
        Activity.Log("invoice", null, "opening", $"Customer opening balances: {created.Count} customer(s), {input.Lines.Sum(l => l.Amount):0.##}");
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(created);
    }

    /// <summary>Supplier opening balances: one vendor bill per supplier (source «opening»).</summary>
    [HttpPost("suppliers")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> Suppliers([FromBody] PartyInput input)
    {
        if (input.Lines.Count == 0) throw new AppException("Enter at least one supplier.");
        if (input.Lines.Any(l => l.Amount <= 0)) throw new AppException("Opening amounts must be greater than zero.");
        await EnsureLookupAsync(LookupTypes.FeeType, OpeningCode, "Opening balance", "رصيد افتتاحي");
        var equity = await OpeningEquityAccountAsync();
        var branchId = RequireBranch(input.BranchId);
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var created = new List<object>();
        await using var tx = await Db.Database.BeginTransactionAsync();
        foreach (var l in input.Lines)
        {
            var v = l.Id != null ? await Db.Vendors.FirstOrDefaultAsync(x => x.Id == l.Id)
                  : await Db.Vendors.FirstOrDefaultAsync(x => x.Party!.PartyCode == (l.Code ?? "").Trim());
            if (v == null) throw new AppException($"Supplier {(l.Id?.ToString() ?? l.Code)} was not found.");
            if (await Db.VendorBills.AnyAsync(b => b.VendorId == v.Id && b.SourceType == "opening" && b.StatusId != cancelled))
                throw new BusinessRuleException($"Supplier {v.Party!.PartyCode ?? v.Party!.NameAr} already has an opening balance. Cancel it first to enter it again.");
            var billNo = string.IsNullOrWhiteSpace(l.Reference) ? $"OB-{input.Date:yyyyMMdd}-{v.Id}" : l.Reference.Trim();
            var bill = await Billing.CreateVendorBillAsync(v.BranchId ?? branchId, v.Id, billNo, input.Date, l.DueDate ?? input.Date, "opening", null,
                new[] { new BillingService.BillLineInput(OpeningCode, null, $"Opening balance {input.Date:yyyy-MM-dd}", 1, "each", l.Amount, 0, equity) },
                l.Notes ?? $"Opening balance as of {input.Date:yyyy-MM-dd}");
            created.Add(new { bill.Id, bill.BillNo, v.Party!.PartyCode, l.Amount });
        }
        Activity.Log("vendor_bill", null, "opening", $"Supplier opening balances: {created.Count} supplier(s), {input.Lines.Sum(l => l.Amount):0.##}", null, branchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(created);
    }
}
