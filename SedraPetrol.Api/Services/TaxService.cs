using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>Resolves the tax rate valid on a document date (tax_rate versions with ValidFrom … ValidTo).</summary>
public class TaxService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public TaxService(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }

    /// <summary>Fallback when no tax codes are configured yet (appsettings Billing:DefaultVatRate, else 5).</summary>
    public decimal FallbackRate => _cfg.GetValue<decimal?>("Billing:DefaultVatRate") ?? 5m;

    private static bool Side(TaxRate t, string side) => t.AppliesTo == "both" || t.AppliesTo == side;

    /// <summary>Versions valid on <paramref name="date"/> for a side (sales | purchases).</summary>
    public async Task<List<TaxRate>> ValidOnAsync(DateOnly date, string side) =>
        (await _db.TaxRates.AsNoTracking()
            .Where(t => t.IsActive && t.ValidFrom <= date && (t.ValidTo == null || t.ValidTo >= date))
            .OrderByDescending(t => t.IsDefault).ThenBy(t => t.Code).ToListAsync())
        .Where(t => Side(t, side)).ToList();

    /// <summary>
    /// The tax to apply to a new line: an explicit version id (must be valid on the date — if it is an older/newer version of the
    /// same code the version valid on the date is used), else the default code valid on the date. Null when nothing is configured.
    /// </summary>
    public async Task<TaxRate?> ResolveAsync(DateOnly date, string side, long? taxRateId = null)
    {
        if (taxRateId != null)
        {
            var t = await _db.TaxRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taxRateId) ?? throw new NotFoundException("TaxRate", taxRateId);
            if (t.ValidFrom <= date && (t.ValidTo == null || t.ValidTo >= date) && t.IsActive) return t;
            var same = await _db.TaxRates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == t.Code && x.IsActive && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date));
            return same ?? throw new BusinessRuleException($"Tax code {t.Code} is not valid on {date:yyyy-MM-dd}.");
        }
        var list = await ValidOnAsync(date, side);
        return list.FirstOrDefault(t => t.IsDefault) ?? list.FirstOrDefault(t => t.Category == "standard");
    }

    /// <summary>Rate for a new line: explicit rate wins, then the resolved tax code, then the fallback.</summary>
    public async Task<(decimal Rate, long? TaxRateId)> RateForLineAsync(DateOnly date, string side, long? taxRateId, decimal? explicitRate)
    {
        if (taxRateId != null)
        {
            var t = await ResolveAsync(date, side, taxRateId);
            return (t!.Rate, t.Id);
        }
        if (explicitRate != null)
        {
            // keep the link to a matching code valid on the date (for VAT returns by code)
            var matches = (await ValidOnAsync(date, side)).Where(t => t.Rate == explicitRate.Value).ToList();
            return (explicitRate.Value, matches.Count == 1 ? matches[0].Id : null);
        }
        var d = await ResolveAsync(date, side);
        return d == null ? (FallbackRate, null) : (d.Rate, d.Id);
    }

    /// <summary>Default rate on a date (for forecasts / expected amounts).</summary>
    public async Task<decimal> DefaultRateAsync(DateOnly date, string side) => (await ResolveAsync(date, side))?.Rate ?? FallbackRate;
}
