using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Tax codes with validity periods (/api/tax-rates).
///   • A tax CODE (VAT-STD, VAT-ZERO …) has one or more VERSIONS, each valid ValidFrom … ValidTo (null = open-ended). Versions of a code never overlap.
///   • New invoice / bill lines take the version valid on the document date; the rate is frozen on the line, so a new version never changes old documents.
///   • A version already used by documents cannot be deleted, and its rate / ValidFrom cannot change — close it (ValidTo) and add a new version instead.
///   • Only one default code per side (sales / purchases) may be valid on any date.
/// </summary>
[Route("api/tax-rates")]
[PermissionModule("accounting")]
public class TaxRatesController : ApiControllerBase
{
    private TaxService Tax => HttpContext.RequestServices.GetRequiredService<TaxService>();
    private static readonly string[] Categories = { "standard", "zero_rated", "exempt", "out_of_scope", "reverse_charge" };
    private static readonly string[] Sides = { "sales", "purchases", "both" };

    public record TaxRateInput(string Code, string? NameAr, string? NameEn, string? Category, decimal Rate, string? AppliesTo,
        DateOnly ValidFrom, DateOnly? ValidTo, bool IsDefault, long? OutputAccountId, long? InputAccountId, bool? IsActive, string? Notes);
    public record NewVersionInput(decimal Rate, DateOnly ValidFrom, DateOnly? ValidTo, string? Notes);

    private static string Status(TaxRate t, DateOnly today) =>
        !t.IsActive ? "inactive" : t.ValidFrom > today ? "future" : t.ValidTo != null && t.ValidTo < today ? "expired" : "current";

    private async Task<Dictionary<long, int>> UsageAsync(IEnumerable<long> ids)
    {
        var list = ids.ToList();
        var inv = await Db.InvoiceItems.Where(i => i.TaxRateId != null && list.Contains(i.TaxRateId.Value)).GroupBy(i => i.TaxRateId!.Value)
            .Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        var bill = await Db.VendorBillItems.Where(i => i.TaxRateId != null && list.Contains(i.TaxRateId.Value)).GroupBy(i => i.TaxRateId!.Value)
            .Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        return inv.Concat(bill).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Sum(x => x.N));
    }

    private object Dto(TaxRate t, Dictionary<long, int> usage, Dictionary<long, string> accNames) => new
    {
        t.Id, t.Code, t.NameAr, t.NameEn, Name = Loc(t.NameEn, t.NameAr), t.TaxType, t.Category, t.Rate, t.AppliesTo,
        t.ValidFrom, t.ValidTo, t.IsDefault, t.IsActive, t.Notes,
        t.OutputAccountId, OutputAccount = t.OutputAccountId is long o ? accNames.GetValueOrDefault(o) : null,
        t.InputAccountId, InputAccount = t.InputAccountId is long i ? accNames.GetValueOrDefault(i) : null,
        Status = Status(t, Today), UsedLines = usage.GetValueOrDefault(t.Id),
    };

    private async Task<Dictionary<long, string>> AccNamesAsync(IEnumerable<TaxRate> rows)
    {
        var ids = rows.SelectMany(r => new[] { r.OutputAccountId, r.InputAccountId }).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        return await Db.Accounts.Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => $"{a.AccountCode} — {(IsEnglish ? a.NameEn ?? a.NameAr : a.NameAr ?? a.NameEn)}");
    }

    /// <summary>All versions (optionally one code / status), newest first within a code.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? code, [FromQuery] string? status, [FromQuery] bool includeInactive = true)
    {
        var q = Db.TaxRates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(code)) q = q.Where(t => t.Code == code);
        if (!includeInactive) q = q.Where(t => t.IsActive);
        var rows = await q.OrderBy(t => t.Code).ThenByDescending(t => t.ValidFrom).ToListAsync();
        var usage = await UsageAsync(rows.Select(r => r.Id));
        var names = await AccNamesAsync(rows);
        var list = rows.Select(r => Dto(r, usage, names)).ToList();
        if (!string.IsNullOrWhiteSpace(status)) list = list.Where(x => (string)((dynamic)x).Status == status).ToList();
        return Ok(list);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var t = await Db.TaxRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("TaxRate", id);
        return Ok(Dto(t, await UsageAsync(new[] { id }), await AccNamesAsync(new[] { t })));
    }

    /// <summary>Codes valid on a date for a side — for the invoice / bill line select. Default first.</summary>
    [HttpGet("options")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> Options([FromQuery] DateOnly? date, [FromQuery] string side = "sales")
    {
        var d = date ?? Today;
        var list = await Tax.ValidOnAsync(d, side);
        return Ok(new
        {
            Date = d, Side = side, FallbackRate = list.Count == 0 ? Tax.FallbackRate : (decimal?)null,
            Items = list.Select(t => new { t.Id, t.Code, Name = Loc(t.NameEn, t.NameAr), t.Category, t.Rate, t.IsDefault, t.ValidFrom, t.ValidTo }),
        });
    }

    /// <summary>The rate that applies on a date (default code, or the given code).</summary>
    [HttpGet("effective")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> Effective([FromQuery] DateOnly? date, [FromQuery] string side = "sales", [FromQuery] string? code = null)
    {
        var d = date ?? Today;
        TaxRate? t;
        if (!string.IsNullOrWhiteSpace(code))
            t = await Db.TaxRates.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && x.IsActive && x.ValidFrom <= d && (x.ValidTo == null || x.ValidTo >= d));
        else t = await Tax.ResolveAsync(d, side);
        return Ok(new
        {
            Date = d, Side = side, Found = t != null, Rate = t?.Rate ?? Tax.FallbackRate,
            TaxRate = t == null ? null : new { t.Id, t.Code, Name = Loc(t.NameEn, t.NameAr), t.Category, t.Rate, t.ValidFrom, t.ValidTo },
        });
    }

    private async Task ValidateAsync(TaxRate t, long? selfId)
    {
        t.Code = (t.Code ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(t.Code)) throw new AppException("Code is required.");
        if (string.IsNullOrWhiteSpace(t.NameAr) && string.IsNullOrWhiteSpace(t.NameEn)) throw new AppException("Name is required.");
        if (!Categories.Contains(t.Category)) throw new AppException($"Category must be one of: {string.Join(", ", Categories)}.");
        if (!Sides.Contains(t.AppliesTo)) throw new AppException("appliesTo must be sales, purchases or both.");
        if (t.Rate < 0 || t.Rate > 100) throw new AppException("Rate must be between 0 and 100.");
        if (t.Category is "zero_rated" or "exempt" or "out_of_scope" && t.Rate != 0) throw new AppException("Zero-rated / exempt / out-of-scope codes must have rate 0.");
        if (t.ValidTo != null && t.ValidTo < t.ValidFrom) throw new AppException("«Valid to» must be on or after «valid from».");
        foreach (var accId in new[] { t.OutputAccountId, t.InputAccountId }.Where(x => x != null))
            if (!await Db.Accounts.AnyAsync(a => a.Id == accId && a.IsPostable && a.IsActive)) throw new AppException("Tax account must be an active postable account.");

        var end = t.ValidTo ?? DateOnly.MaxValue;
        var overlap = await Db.TaxRates.Where(x => x.Code == t.Code && x.Id != (selfId ?? 0)
                && x.ValidFrom <= end && (x.ValidTo == null || x.ValidTo >= t.ValidFrom))
            .Select(x => new { x.ValidFrom, x.ValidTo }).FirstOrDefaultAsync();
        if (overlap != null)
            throw new BusinessRuleException($"The period overlaps another version of {t.Code} ({overlap.ValidFrom:yyyy-MM-dd} → {(overlap.ValidTo?.ToString("yyyy-MM-dd") ?? "open")}). Use «new version» or change the dates.", "tax_overlap");

        if (t.IsDefault && t.IsActive)
        {
            var sides = t.AppliesTo == "both" ? new[] { "sales", "purchases", "both" } : new[] { t.AppliesTo, "both" };
            var clash = await Db.TaxRates.Where(x => x.IsDefault && x.IsActive && x.Code != t.Code && sides.Contains(x.AppliesTo)
                    && x.ValidFrom <= end && (x.ValidTo == null || x.ValidTo >= t.ValidFrom))
                .Select(x => x.Code).FirstOrDefaultAsync();
            if (clash != null) throw new BusinessRuleException($"{clash} is already the default for this period. Untick its default first.", "tax_default_clash");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaxRateInput input)
    {
        var t = new TaxRate
        {
            CompanyId = Me.CompanyId, Code = input.Code, NameAr = input.NameAr?.Trim(), NameEn = input.NameEn?.Trim(),
            Category = input.Category ?? "standard", Rate = input.Rate, AppliesTo = input.AppliesTo ?? "both",
            ValidFrom = input.ValidFrom, ValidTo = input.ValidTo, IsDefault = input.IsDefault,
            OutputAccountId = input.OutputAccountId, InputAccountId = input.InputAccountId, IsActive = input.IsActive ?? true, Notes = input.Notes,
        };
        await ValidateAsync(t, null);
        Db.TaxRates.Add(t);
        await Db.SaveChangesAsync();
        return await Get(t.Id);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, TaxRateInput input)
    {
        var t = await Db.TaxRates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("TaxRate", id);
        var used = (await UsageAsync(new[] { id })).GetValueOrDefault(id) > 0;
        if (used)
        {
            if (input.Rate != t.Rate || input.ValidFrom != t.ValidFrom || !string.Equals(input.Code?.Trim(), t.Code, StringComparison.OrdinalIgnoreCase)
                || (input.Category ?? t.Category) != t.Category)
                throw new BusinessRuleException("This version is used by invoices / bills: its code, rate, category and start date are locked. Close it with «valid to» and add a new version.", "tax_in_use");
            // shortening the period must not cut off documents already using it
            if (input.ValidTo != null)
            {
                var to = input.ValidTo.Value;
                var lastInv = await Db.InvoiceItems.Where(i => i.TaxRateId == id).Join(Db.Invoices, i => i.InvoiceId, v => v.Id, (i, v) => (DateOnly?)v.IssueDate).MaxAsync();
                var lastBill = await Db.VendorBillItems.Where(i => i.TaxRateId == id).Join(Db.VendorBills, i => i.BillId, b => b.Id, (i, b) => (DateOnly?)b.BillDate).MaxAsync();
                var last = new[] { lastInv, lastBill }.Max();
                if (last != null && last > to) throw new BusinessRuleException($"Documents dated up to {last:yyyy-MM-dd} use this version; «valid to» cannot be earlier.", "tax_in_use");
            }
        }
        t.Code = input.Code; t.NameAr = input.NameAr?.Trim(); t.NameEn = input.NameEn?.Trim(); t.Category = input.Category ?? t.Category;
        t.Rate = input.Rate; t.AppliesTo = input.AppliesTo ?? t.AppliesTo; t.ValidFrom = input.ValidFrom; t.ValidTo = input.ValidTo;
        t.IsDefault = input.IsDefault; t.OutputAccountId = input.OutputAccountId; t.InputAccountId = input.InputAccountId;
        t.IsActive = input.IsActive ?? t.IsActive; t.Notes = input.Notes;
        await ValidateAsync(t, id);
        await Db.SaveChangesAsync();
        return await Get(id);
    }

    /// <summary>
    /// New version of a code from a date (e.g. VAT changes from 5% to 10% on 2027-01-01):
    /// the version covering that date is closed the day before, and a copy with the new rate starts on ValidFrom.
    /// </summary>
    [HttpPost("{id:long}/new-version")]
    public async Task<IActionResult> NewVersion(long id, NewVersionInput input)
    {
        var src = await Db.TaxRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("TaxRate", id);
        var versions = await Db.TaxRates.Where(x => x.Code == src.Code).OrderBy(x => x.ValidFrom).ToListAsync();
        if (versions.Any(v => v.ValidFrom >= input.ValidFrom))
            throw new BusinessRuleException($"A version of {src.Code} already starts on or after {input.ValidFrom:yyyy-MM-dd}. Edit that version instead.", "tax_overlap");
        var covering = versions.LastOrDefault(v => v.ValidFrom < input.ValidFrom && (v.ValidTo == null || v.ValidTo >= input.ValidFrom));
        if (covering != null)
        {
            var close = input.ValidFrom.AddDays(-1);
            var lastInv = await Db.InvoiceItems.Where(i => i.TaxRateId == covering.Id).Join(Db.Invoices, i => i.InvoiceId, v => v.Id, (i, v) => (DateOnly?)v.IssueDate).MaxAsync();
            var lastBill = await Db.VendorBillItems.Where(i => i.TaxRateId == covering.Id).Join(Db.VendorBills, i => i.BillId, b => b.Id, (i, b) => (DateOnly?)b.BillDate).MaxAsync();
            var last = new[] { lastInv, lastBill }.Max();
            if (last != null && last > close)
                throw new BusinessRuleException($"Documents dated up to {last:yyyy-MM-dd} already use the current rate; the new version must start after that date.", "tax_in_use");
            covering.ValidTo = close;
        }
        var nv = new TaxRate
        {
            CompanyId = src.CompanyId, Code = src.Code, NameAr = src.NameAr, NameEn = src.NameEn, TaxType = src.TaxType, Category = src.Category,
            Rate = input.Rate, AppliesTo = src.AppliesTo, ValidFrom = input.ValidFrom, ValidTo = input.ValidTo, IsDefault = src.IsDefault,
            OutputAccountId = src.OutputAccountId, InputAccountId = src.InputAccountId, IsActive = true, Notes = input.Notes ?? src.Notes,
        };
        await ValidateAsync(nv, covering?.Id);   // the covering version is being closed in the same save
        Db.TaxRates.Add(nv);
        await Db.SaveChangesAsync();
        return await Get(nv.Id);
    }

    /// <summary>Deactivate / reactivate (used versions cannot be deleted).</summary>
    [HttpPost("{id:long}/active")]
    public async Task<IActionResult> SetActive(long id, [FromQuery] bool value)
    {
        var t = await Db.TaxRates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("TaxRate", id);
        t.IsActive = value;
        if (value) await ValidateAsync(t, id);
        await Db.SaveChangesAsync();
        return await Get(id);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await Db.TaxRates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("TaxRate", id);
        if ((await UsageAsync(new[] { id })).GetValueOrDefault(id) > 0)
            throw new BusinessRuleException("This version is used by invoices / bills and cannot be deleted. Deactivate it or close its period instead.", "tax_in_use");
        t.IsDeleted = true;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>VAT summary by tax code for a period (sales output / purchases input) — basis for the VAT return.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var deadInv = new List<long?>();
        foreach (var c in new[] { "draft", "cancelled" })
            if (await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, c) is long lid) deadInv.Add(lid);
        var deadBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var sales = await Db.InvoiceItems
            .Join(Db.Invoices, i => i.InvoiceId, v => v.Id, (i, v) => new { i, v })
            .Where(x => x.v.IssueDate >= from && x.v.IssueDate <= to && !deadInv.Contains(x.v.StatusId))
            .GroupBy(x => new { x.i.TaxRateId, x.i.VatRate })
            .Select(g => new { g.Key.TaxRateId, g.Key.VatRate, Net = g.Sum(x => x.i.Amount ?? 0) }).ToListAsync();
        var purchases = await Db.VendorBillItems
            .Join(Db.VendorBills, i => i.BillId, b => b.Id, (i, b) => new { i, b })
            .Where(x => x.b.BillDate >= from && x.b.BillDate <= to && x.b.StatusId != deadBill)
            .GroupBy(x => new { x.i.TaxRateId, x.i.VatRate })
            .Select(g => new { g.Key.TaxRateId, g.Key.VatRate, Net = g.Sum(x => x.i.Amount ?? 0) }).ToListAsync();
        var ids = sales.Select(s => s.TaxRateId).Concat(purchases.Select(p => p.TaxRateId)).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var codes = await Db.TaxRates.IgnoreQueryFilters().Where(t => ids.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => new { t.Code, Name = Loc(t.NameEn, t.NameAr), t.Category });
        object Row(long? id, decimal? rate, decimal net) => new
        {
            TaxRateId = id, Code = id is long k && codes.TryGetValue(k, out var c) ? c.Code : null,
            Name = id is long k2 && codes.TryGetValue(k2, out var c2) ? c2.Name : null,
            Category = id is long k3 && codes.TryGetValue(k3, out var c3) ? c3.Category : null,
            Rate = rate ?? 0, Net = Math.Round(net, 2), Vat = Math.Round(net * (rate ?? 0) / 100m, 2),
        };
        var s = sales.Select(x => Row(x.TaxRateId, x.VatRate, x.Net)).ToList();
        var p = purchases.Select(x => Row(x.TaxRateId, x.VatRate, x.Net)).ToList();
        decimal VatOf(IEnumerable<object> rows) => rows.Sum(r => (decimal)((dynamic)r).Vat);
        return Ok(new { From = from, To = to, Sales = s, Purchases = p, OutputVat = VatOf(s), InputVat = VatOf(p), NetPayable = VatOf(s) - VatOf(p) });
    }
}
