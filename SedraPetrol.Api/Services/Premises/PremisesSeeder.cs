using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Seed data of the Restaurants &amp; Villas module. Runs after <see cref="DataSeeder"/> and ONLY inserts what is missing:
/// <list type="bullet">
/// <item>BUILDING_TYPE values <c>villa</c>, <c>restaurant</c> (existing values untouched)</item>
/// <item>new lookup type KITCHEN_TYPE</item>
/// <item>INVOICE_TYPE <c>gas_delivery</c> and FEE_TYPE <c>gas_delivery</c> (gas sold per refill, litres)</item>
/// <item>account mapping FEE_TYPE gas_delivery → 4101 Gas sales (only when 4101 exists and no mapping is there yet)</item>
/// </list>
/// Nothing existing is updated or deleted. UNIT_TYPE is deliberately NOT used: DataSeeder.NormalizeUnitTypesAsync rewrites
/// a unit type "villa" to "residential" on every start.
/// </summary>
public class PremisesSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<PremisesSeeder> _log;

    public PremisesSeeder(AppDbContext db, ILogger<PremisesSeeder> log) { _db = db; _log = log; }

    public static readonly (string Type, string En, string Ar, (string Code, string En, string Ar)[] Values)[] Lookups =
    {
        ("BUILDING_TYPE", "Building type", "نوع المبنى", new[] { ("villa", "Villa", "فيلا"), ("restaurant", "Restaurants", "مطاعم") }),
        ("KITCHEN_TYPE", "Kitchen type", "نوع المطبخ", new[] { ("commercial", "Commercial kitchen", "مطبخ تجاري"), ("cafe", "Café / light cooking", "مقهى / طهي خفيف"), ("bakery", "Bakery", "مخبز"), ("central", "Central kitchen", "مطبخ مركزي"), ("other", "Other", "أخرى") }),
        ("INVOICE_TYPE", "Invoice type", "نوع الفاتورة", new[] { ("gas_delivery", "Gas delivery (refill)", "توريد غاز (تعبئة)") }),
        ("FEE_TYPE", "Fee type", "نوع الرسوم", new[] { ("gas_delivery", "Gas delivered (per litre)", "غاز مورّد (لكل لتر)") }),
    };

    public async Task SeedAsync()
    {
        if (!await _db.Database.CanConnectAsync()) return;
        var types = await _db.LookupTypes.IgnoreQueryFilters().ToListAsync();
        var values = await _db.LookupValues.IgnoreQueryFilters().ToListAsync();
        foreach (var (typeCode, en, ar, vals) in Lookups)
        {
            var type = types.FirstOrDefault(t => t.Code == typeCode);
            if (type == null)
            {
                type = new LookupType { Code = typeCode, NameEn = en, NameAr = ar, IsActive = true };
                _db.LookupTypes.Add(type);
                await _db.SaveChangesAsync();
            }
            // new values go after the existing ones
            var order = values.Where(v => v.LookupTypeId == type.Id).Select(v => v.SortOrder ?? 0).DefaultIfEmpty(0).Max();
            foreach (var (code, ven, var) in vals)
            {
                if (values.Any(v => v.LookupTypeId == type.Id && v.Code == code)) continue;
                order += 10;
                _db.LookupValues.Add(new LookupValue { LookupTypeId = type.Id, Code = code, NameEn = ven, NameAr = var, SortOrder = order, IsActive = true });
            }
        }
        await _db.SaveChangesAsync();

        // FEE_TYPE gas_delivery → 4101 Gas sales, for every company that has account 4101 and no mapping for it yet
        var key = await _db.LookupValues.Where(v => v.LookupType!.Code == "FEE_TYPE" && v.Code == "gas_delivery").Select(v => (long?)v.Id).FirstOrDefaultAsync();
        if (key != null)
        {
            foreach (var companyId in await _db.Companies.Select(c => c.Id).ToListAsync())
            {
                if (await _db.AccountMappings.AnyAsync(m => m.CompanyId == companyId && m.SourceType == MappingSources.InvoiceItem && m.SourceKeyId == key)) continue;
                var revenue = await _db.Accounts.Where(a => a.CompanyId == companyId && a.AccountCode == "4101").Select(a => (long?)a.Id).FirstOrDefaultAsync();
                if (revenue == null) continue;   // other chart of accounts: map gas_delivery in Accounting > mappings
                _db.AccountMappings.Add(new AccountMapping { CompanyId = companyId, SourceType = MappingSources.InvoiceItem, SourceKeyId = key, CreditAccountId = revenue, IsActive = true });
            }
            await _db.SaveChangesAsync();
        }
        _log.LogInformation("Restaurants & Villas seed checked.");
    }
}
