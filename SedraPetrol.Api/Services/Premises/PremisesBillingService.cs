using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Restaurants &amp; Villas: gas is sold per refill. A delivered refill of a villa / restaurant tank is invoiced to the site's
/// customer (villa owner / restaurant owner) by a user button: litres delivered × the price per litre of the site's
/// delivery contract. Uses the existing <see cref="BillingService.CreateInvoiceAsync"/> (numbering, VAT via TaxService,
/// posting to the customer's own account) — the refill workflow and its tables are not changed; the link is kept in
/// <c>premises_refill_invoice</c>.
/// Building refills are never touched: only refills of tanks linked in <c>premises_tank</c> are seen here.
/// </summary>
public class PremisesBillingService
{
    private readonly AppDbContext _db;
    private readonly LookupService _lookups;
    private readonly BillingService _billing;
    private readonly PartyService _parties;
    private readonly ActivityLogger _activity;

    public PremisesBillingService(AppDbContext db, LookupService lookups, BillingService billing, PartyService parties, ActivityLogger activity)
    {
        _db = db; _lookups = lookups; _billing = billing; _parties = parties; _activity = activity;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// A villa's tanks are the tanks of its own host building. A tank added from the building screens (tanks yard)
    /// has no premises_tank link yet: add the missing links so it is treated as a villa tank (refill invoicing).
    /// </summary>
    public async Task LinkVillaHostTanksAsync()
    {
        var missing = await (from v in _db.Villas
                             join t in _db.Tanks on (long?)v.BuildingId equals t.BuildingId
                             where !_db.PremisesTanks.Any(l => l.TankId == t.Id)
                             select new { PremisesId = v.Id, TankId = t.Id }).ToListAsync();
        if (missing.Count == 0) return;
        var links = missing.Select(m => new PremisesTank { PremisesId = m.PremisesId, TankId = m.TankId }).ToList();
        _db.PremisesTanks.AddRange(links);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            // another request linked them at the same moment (unique index on tank_id): nothing to do
            foreach (var l in links) _db.Entry(l).State = EntityState.Detached;
        }
    }

    /// <summary>One refill of a villa / restaurant tank and its invoicing state.</summary>
    public record RefillRow(long RefillRequestId, string RequestNo, long PremisesId, string PremisesType, string PremisesCode,
        long TankId, string TankNo, DateOnly RequestDate, string? StatusCode, int Stage, DateTimeOffset? DeliveredAt, decimal? DeliveredLitres,
        DateTimeOffset? CompletedAt, long? InvoiceId, string? InvoiceNo, decimal? InvoicedAmount, bool ReadyToInvoice);

    /// <summary>
    /// Refills of the tanks of the given site (or of every site of a type), newest first.
    /// Ready to invoice = delivered (tank refill recorded with litres &gt; 0), not cancelled, not invoiced yet.
    /// </summary>
    public async Task<List<RefillRow>> RefillsAsync(long? premisesId = null, string? premisesType = null, bool onlyReady = false, IReadOnlyCollection<long>? branchIds = null)
    {
        await LinkVillaHostTanksAsync();
        var cancelled = await _lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        var links = _db.PremisesTanks.AsNoTracking().Where(l => premisesId == null || l.PremisesId == premisesId)
            .Where(l => premisesType == null || l.Premises!.PremisesType == premisesType)
            .Where(l => branchIds == null || branchIds.Count == 0 || branchIds.Contains(l.Premises!.BranchId));
        var rows = await (from l in links
                          join r in _db.RefillRequests.AsNoTracking() on l.TankId equals r.TankId
                          select new
                          {
                              r.Id, r.RequestNo, l.PremisesId, l.Premises!.PremisesType, PCode = l.Premises.Code, r.TankId, l.Tank!.TankNo, r.RequestDate,
                              r.StatusId, r.CurrentStageId, r.CompletedAt,
                              Refill = _db.TankRefills.Where(t => t.RefillRequestId == r.Id).OrderByDescending(t => t.RefillDatetime)
                                  .Select(t => new { t.RefillDatetime, Qty = t.DeliveredQtyLitres ?? ((t.LevelAfterLitres ?? 0) - (t.LevelBeforeLitres ?? 0)) }).FirstOrDefault(),
                              Inv = _db.PremisesRefillInvoices.Where(x => x.RefillRequestId == r.Id)
                                  .Select(x => new { x.InvoiceId, x.Invoice!.InvoiceNo, x.Invoice.TotalAmount }).FirstOrDefault(),
                          }).OrderByDescending(x => x.RequestDate).ThenByDescending(x => x.Id).ToListAsync();
        var result = new List<RefillRow>();
        foreach (var x in rows)
        {
            var status = await _lookups.CodeAsync(x.StatusId);
            var stageCode = await _lookups.CodeAsync(x.CurrentStageId);
            var ready = x.Inv == null && x.Refill != null && x.Refill.Qty > 0 && x.StatusId != cancelled;
            if (onlyReady && !ready) continue;
            result.Add(new RefillRow(x.Id, x.RequestNo, x.PremisesId, x.PremisesType, x.PCode, x.TankId, x.TankNo, x.RequestDate, status,
                int.TryParse(stageCode, out var s) ? s : 0, x.Refill?.RefillDatetime, x.Refill?.Qty, x.CompletedAt,
                x.Inv?.InvoiceId, x.Inv?.InvoiceNo, x.Inv?.TotalAmount, ready));
        }
        return result;
    }

    /// <summary>
    /// The active delivery contract of a site on a date (start ≤ date ≤ end, not terminated). Null when there is none.
    /// </summary>
    public async Task<DeliveryContract?> ContractOnAsync(long premisesId, DateOnly date)
    {
        // the contract that was in force on the delivery date, even if it has since been terminated or has expired
        // (a refill delivered under it must still be invoiceable); drafts / pending contracts never price a refill
        var valid = new List<long?>
        {
            await _lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active"),
            await _lookups.IdOrNullAsync(LookupTypes.ContractStatus, "expired"),
            await _lookups.IdOrNullAsync(LookupTypes.ContractStatus, "terminated"),
        };
        return await _db.DeliveryContracts
            .Where(c => c.PremisesId == premisesId && valid.Contains(c.StatusId) && c.StartDate <= date
                        && (c.EndDate == null || c.EndDate >= date) && (c.TerminatedAt == null || c.TerminatedAt >= date))
            .OrderByDescending(c => c.StartDate).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Issue (and post) the customer invoice of one delivered refill of a villa / restaurant tank.
    /// Litres = the delivered quantity recorded on the refill; price = the site's delivery contract on the delivery date.
    /// </summary>
    public async Task<Invoice> InvoiceRefillAsync(long refillRequestId, DateOnly? issueDate, string? notes)
    {
        await LinkVillaHostTanksAsync();
        var r = await _db.RefillRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == refillRequestId) ?? throw new NotFoundException("RefillRequest", refillRequestId);
        var link = await _db.PremisesTanks.AsNoTracking().FirstOrDefaultAsync(l => l.TankId == r.TankId)
                   ?? throw new BusinessRuleException("This refill is for a building tank. Building refills are not invoiced to customers.");
        var site = await _db.Premises.AsNoTracking().FirstOrDefaultAsync(p => p.Id == link.PremisesId) ?? throw new NotFoundException("Premises", link.PremisesId);
        if (await _lookups.IsAsync(r.StatusId, LookupTypes.RefillStatus, "cancelled")) throw new BusinessRuleException("This refill was cancelled.");
        if (await _db.PremisesRefillInvoices.AnyAsync(x => x.RefillRequestId == r.Id)) throw new BusinessRuleException($"Refill {r.RequestNo} is already invoiced.");

        var refill = await _db.TankRefills.AsNoTracking().Where(t => t.RefillRequestId == r.Id).OrderByDescending(t => t.RefillDatetime).FirstOrDefaultAsync()
                     ?? throw new BusinessRuleException($"Refill {r.RequestNo} has not been delivered yet. Record the tank refill first.");
        var litres = refill.DeliveredQtyLitres ?? ((refill.LevelAfterLitres ?? 0) - (refill.LevelBeforeLitres ?? 0));
        if (litres <= 0) throw new BusinessRuleException($"Refill {r.RequestNo} has no delivered quantity.");

        var deliveredOn = DateOnly.FromDateTime(refill.RefillDatetime.ToLocalTime().DateTime);
        var contract = await ContractOnAsync(site.Id, deliveredOn)
                       ?? throw new BusinessRuleException($"{site.Code} has no active delivery contract on {deliveredOn:yyyy-MM-dd}. Add the contract (price per litre) first.");
        if (contract.PricePerLitre <= 0) throw new BusinessRuleException($"Contract {contract.ContractNo} has no price per litre.");

        var date = issueDate ?? Today;
        if (date > Today) throw new AppException("The invoice date cannot be in the future.");
        if (date < deliveredOn) throw new AppException($"The invoice date cannot be before the delivery date ({deliveredOn:yyyy-MM-dd}).");

        // the customer (villa owner / restaurant owner) gets the customer role + own ledger account when it has none
        var party = await _db.Parties.FirstOrDefaultAsync(p => p.Id == contract.CustomerPartyId) ?? throw new NotFoundException("Party", contract.CustomerPartyId);
        if (!await _db.Tenants.AnyAsync(t => t.Id == party.Id))
        {
            await _parties.EnsureCustomerAsync(party, site.BranchId);
            _activity.Log("tenant", party.Id, "create", $"Customer role added to {party.PartyCode} {party.NameAr ?? party.NameEn} (gas delivery {site.Code})", site.BuildingId, site.BranchId);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        var tankNo = await _db.Tanks.Where(t => t.Id == r.TankId).Select(t => t.TankNo).FirstOrDefaultAsync();
        var description = $"Gas delivered — refill {r.RequestNo}, {site.Code} tank {tankNo}, {deliveredOn:yyyy-MM-dd}: {litres:0.###} L × {contract.PricePerLitre:0.####}";
        var inv = await _billing.CreateInvoiceAsync(new InvoiceDraft(
            site.BranchId, party.Id, site.BuildingId, contract.Id, "gas_delivery", null, date,
            contract.PaymentTermsDays != null ? date.AddDays(contract.PaymentTermsDays.Value) : null,
            deliveredOn, deliveredOn, contract.CurrencyId,
            string.IsNullOrWhiteSpace(notes) ? $"{site.Code} — refill {r.RequestNo} (contract {contract.ContractNo})" : notes.Trim(),
            new[] { new InvoiceLineInput("gas_delivery", null, description, litres, "L", contract.PricePerLitre, null, null, contract.TaxRateId) },
            true));
        _db.PremisesRefillInvoices.Add(new PremisesRefillInvoice
        {
            PremisesId = site.Id, RefillRequestId = r.Id, ContractId = contract.Id, InvoiceId = inv.Id,
            Litres = litres, PricePerLitre = contract.PricePerLitre, Amount = inv.Subtotal ?? 0,
        });
        _activity.Log(site.PremisesType, site.Id, "invoice", $"{site.Code}: invoice {inv.InvoiceNo} issued for refill {r.RequestNo} ({litres:0.###} L)", site.BuildingId, site.BranchId);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return inv;
    }
}
