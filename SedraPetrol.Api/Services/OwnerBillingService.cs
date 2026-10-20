using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Owner receivables: building owners pay us for
///   • maintenance contracts (AMC) — one invoice per payment-schedule instalment, issued automatically on its due date
///     (at contract creation for instalments already due, then by the daily job), posted to the ledger;
///   • Civil Defense certificates — an invoice when the request / renewal is opened: government fees recharged
///     (out of scope of VAT) + our service fee (standard VAT), both per year × duration; a supplementary invoice when
///     the fees actually paid to Civil Defense are higher than invoiced.
/// The owner (a party) is billed through a customer account (tenant table), matched or created automatically.
/// </summary>
public class OwnerBillingService
{
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly NumberingService _numbers;
    private readonly BillingService _billing;
    private readonly ActivityLogger _activity;
    private readonly IConfiguration _cfg;

    private readonly PartyService _parties;

    public OwnerBillingService(AppDbContext db, LookupService lookups, NumberingService numbers, BillingService billing, ActivityLogger activity, IConfiguration cfg, PartyService parties)
    { _db = db; _lookups = lookups; _numbers = numbers; _billing = billing; _activity = activity; _cfg = cfg; _parties = parties; }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Item type of recharged government fees (FEE_TYPE «gov_fees»).</summary>
    public const string GovFeeType = "gov_fees";
    /// <summary>Item type of our certificate service fee (FEE_TYPE «cd_service»).</summary>
    public const string CdServiceType = "cd_service";

    public decimal DefaultCdServiceFee => _cfg.GetValue<decimal?>("CivilDefense:ServiceFeePerYear") ?? 0m;
    public decimal DefaultCdGovFees => _cfg.GetValue<decimal?>("CivilDefense:GovernmentFeesPerYear") ?? 0m;

    // ------------------------------------------------------------------ owner → customer account

    /// <summary>
    /// The customer account of a building owner: the given party, else the building's primary owner.
    /// The owner is the same party; it only gets the customer role (and its ledger account) when it has none.
    /// </summary>
    public async Task<long> OwnerCustomerIdAsync(long branchId, long buildingId, long? ownerPartyId, string reason)
    {
        var partyId = ownerPartyId ?? await _db.BuildingOwners.Where(o => o.BuildingId == buildingId && o.IsPrimary).Select(o => (long?)o.PartyId).FirstOrDefaultAsync()
                      ?? await _db.BuildingOwners.Where(o => o.BuildingId == buildingId).OrderBy(o => o.Id).Select(o => (long?)o.PartyId).FirstOrDefaultAsync()
                      ?? throw new BusinessRuleException("The building has no owner to invoice. Add the building owner first.");
        if (await _db.Tenants.AnyAsync(t => t.Id == partyId)) return partyId;
        var p = await _db.Parties.FirstOrDefaultAsync(x => x.Id == partyId) ?? throw new NotFoundException("Party", partyId);
        await _parties.EnsureCustomerAsync(p, branchId);
        _activity.Log("tenant", p.Id, "create", $"Customer role added to owner {p.PartyCode} {p.NameAr ?? p.NameEn} ({reason})", buildingId, branchId);
        await _db.SaveChangesAsync();
        return p.Id;
    }

    // ------------------------------------------------------------------ AMC instalments

    /// <summary>Invoice one AMC instalment to the owner (issued and posted).</summary>
    public async Task<Invoice> IssueAmcLineAsync(AmcContract a, AmcPaymentSchedule line, DateOnly? issueDate = null)
    {
        if (line.InvoiceId != null) throw new BusinessRuleException("This instalment is already invoiced.");
        if (line.VendorBillId != null) throw new BusinessRuleException("This instalment is linked to a vendor bill.");
        if (line.DueDate > Today) throw new BusinessRuleException($"The invoice of this instalment can be issued from {line.DueDate:yyyy-MM-dd}.");
        var date = issueDate ?? Today;
        if (date < line.DueDate) throw new AppException($"The invoice date cannot be before the instalment date ({line.DueDate:yyyy-MM-dd}).");
        if (date > Today) throw new AppException("The invoice date cannot be in the future.");

        var customerId = await OwnerCustomerIdAsync(a.BranchId, a.BuildingId!.Value, a.OwnerPartyId, $"AMC {a.ContractNo}");
        var next = await _db.AmcPaymentSchedules.Where(s => s.ContractId == a.Id && s.DueDate > line.DueDate).OrderBy(s => s.DueDate).Select(s => (DateOnly?)s.DueDate).FirstOrDefaultAsync();
        var periodEnd = next?.AddDays(-1) ?? a.EndDate;
        var inv = await _billing.CreateInvoiceAsync(new InvoiceDraft(a.BranchId, customerId, a.BuildingId, a.Id, "maintenance", null, date, null,
            line.DueDate, periodEnd, a.CurrencyId, $"AMC {a.ContractNo} — {line.Label}",
            new[] { new InvoiceLineInput("maintenance", null, $"Annual maintenance contract {a.ContractNo} — {line.Label} ({line.DueDate:yyyy-MM-dd} → {periodEnd:yyyy-MM-dd})", 1, "each", line.Amount, null) },
            true));
        line.InvoiceId = inv.Id;
        line.StatusId = await _lookups.IdAsync(LookupTypes.AmcPaymentStatus, "invoiced");
        _activity.Log("amc_contract", a.Id, "invoice", $"{a.ContractNo}: invoice {inv.InvoiceNo} issued for {line.Label}", a.BuildingId, a.BranchId);
        await _db.SaveChangesAsync();
        return inv;
    }

    /// <summary>
    /// Issue every instalment that is due (due date ≤ today, not invoiced, not a vendor bill) of active AMC contracts.
    /// One contract failing (e.g. no owner) does not stop the others. Returns the number of invoices issued.
    /// </summary>
    public async Task<int> IssueDueAmcAsync(long? contractId = null, ILogger? log = null)
    {
        var active = await _lookups.IdOrNullAsync(LookupTypes.AmcStatus, "active");
        var today = Today;
        var due = await _db.AmcPaymentSchedules
            .Where(s => s.InvoiceId == null && s.VendorBillId == null && s.DueDate <= today
                        && s.Contract!.IsActive && s.Contract.StatusId == active && (contractId == null || s.ContractId == contractId))
            .OrderBy(s => s.ContractId).ThenBy(s => s.DueDate).ToListAsync();
        var n = 0;
        foreach (var g in due.GroupBy(s => s.ContractId))
        {
            var a = await _db.AmcContracts.FirstAsync(x => x.Id == g.Key);
            foreach (var line in g)
            {
                try { await IssueAmcLineAsync(a, line, line.DueDate); n++; }
                catch (AppException ex)
                {
                    log?.LogWarning("AMC {Contract}: instalment {Line} not invoiced: {Message}", a.ContractNo, line.Label, ex.Message);
                    break;   // keep the instalments of this contract in order
                }
            }
        }
        return n;
    }

    // ------------------------------------------------------------------ Civil Defense certificates

    public record CdBilling(long? OwnerPartyId, short? Years, decimal? GovFeesPerYear, decimal? ServiceFeePerYear, bool Skip = false);

    /// <summary>
    /// Owner invoice when a certificate request / renewal is opened. Lines: government fees (out of scope VAT) and our service fee (standard VAT),
    /// each per year × years. Nothing is issued when both amounts are 0 or Skip is set.
    /// </summary>
    public async Task<Invoice?> IssueCdInvoiceAsync(CdCertificate c, CdRenewal r, CdBilling input, DateOnly? date = null)
    {
        if (input.Skip) return null;
        if (r.OwnerInvoiceId != null) throw new BusinessRuleException("The owner invoice of this request is already issued.");
        var years = Math.Clamp((int)(input.Years ?? 1), 1, 10);
        var gov = Math.Round((input.GovFeesPerYear ?? DefaultCdGovFees) * years, 2);
        var service = Math.Round((input.ServiceFeePerYear ?? DefaultCdServiceFee) * years, 2);
        if (gov < 0 || service < 0) throw new AppException("Fees cannot be negative.");
        if (gov == 0 && service == 0) return null;
        var issue = date ?? r.StartedAt ?? Today;
        if (issue > Today) issue = Today;

        var customerId = await OwnerCustomerIdAsync(c.BranchId, c.BuildingId, input.OwnerPartyId, "Civil Defense certificate");
        var label = c.CertificateNo == null ? "new certificate request" : $"certificate {c.CertificateNo} renewal";
        var lines = new List<InvoiceLineInput>();
        var oos = await OutOfScopeTaxIdAsync(issue);
        if (gov > 0)
            lines.Add(new InvoiceLineInput(GovFeeType, null, $"Civil Defense government fees — {label} ({years} year{(years > 1 ? "s" : "")})", years, "year",
                Math.Round(gov / years, 2), oos == null ? 0 : null, null, oos));
        if (service > 0)
            lines.Add(new InvoiceLineInput(CdServiceType, null, $"Certificate processing service — {label} ({years} year{(years > 1 ? "s" : "")})", years, "year",
                Math.Round(service / years, 2), null));
        var inv = await _billing.CreateInvoiceAsync(new InvoiceDraft(c.BranchId, customerId, c.BuildingId, null, "other", null, issue, null,
            issue, issue.AddYears(years).AddDays(-1), null, $"Civil Defense — {label} (request #{r.Id})", lines, true));
        r.OwnerInvoiceId = inv.Id;
        r.InvoicedGovFees = gov;
        _activity.Log("cd_certificate", c.Id, "invoice", $"Owner invoice {inv.InvoiceNo} issued for {label}: government fees {gov:0.##} + service {service:0.##}", c.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
        return inv;
    }

    /// <summary>
    /// After a Civil Defense fees bill is recorded: when the fees actually charged exceed what was invoiced to the owner,
    /// issue a supplementary owner invoice for the difference. Returns (supplement invoice, amount invoiced in excess when lower).
    /// </summary>
    public async Task<(Invoice? Supplement, decimal OverInvoiced)> ReconcileCdFeesAsync(CdCertificate c, CdRenewal r, decimal actualGovFees)
    {
        if (r.OwnerInvoiceId == null) return (null, 0);
        var invoiced = r.InvoicedGovFees ?? 0;
        var diff = Math.Round(actualGovFees - invoiced, 2);
        if (diff <= 0) return (null, -diff);
        var owner = await _db.Invoices.AsNoTracking().Where(i => i.Id == r.OwnerInvoiceId).Select(i => new { i.TenantId, i.InvoiceNo }).FirstAsync();
        var oos = await OutOfScopeTaxIdAsync(Today);
        var inv = await _billing.CreateInvoiceAsync(new InvoiceDraft(c.BranchId, owner.TenantId, c.BuildingId, null, "other", null, Today, null, null, null, null,
            $"Additional Civil Defense fees (invoice {owner.InvoiceNo}, request #{r.Id})",
            new[] { new InvoiceLineInput(GovFeeType, null, $"Additional Civil Defense government fees (actual {actualGovFees:0.##} − invoiced {invoiced:0.##})", 1, "each", diff, oos == null ? 0 : null, null, oos) },
            true));
        r.InvoicedGovFees = actualGovFees;
        _activity.Log("cd_certificate", c.Id, "invoice", $"Supplementary owner invoice {inv.InvoiceNo} for additional government fees {diff:0.##}", c.BuildingId, c.BranchId);
        await _db.SaveChangesAsync();
        return (inv, 0);
    }

    /// <summary>The out-of-scope tax code (VAT-OOS) valid on the date, if configured.</summary>
    private async Task<long?> OutOfScopeTaxIdAsync(DateOnly date)
    {
        try
        {
            return await _db.TaxRates.Where(t => t.IsActive && t.Category == "out_of_scope" && t.ValidFrom <= date && (t.ValidTo == null || t.ValidTo >= date))
                .OrderBy(t => t.Code).Select(t => (long?)t.Id).FirstOrDefaultAsync();
        }
        catch { return null; }
    }
}
