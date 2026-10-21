using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>Lookup type codes used by the API (lookup_type.code).</summary>
public static class LookupTypes
{
    public const string BuildingType = "BUILDING_TYPE", BuildingStatus = "BUILDING_STATUS",
        UnitType = "UNIT_TYPE", UnitStatus = "UNIT_STATUS",
        MeterType = "METER_TYPE", MeterStatus = "METER_STATUS", MeterServiceType = "METER_SERVICE_TYPE",
        MeterIssueType = "METER_ISSUE_TYPE", ServiceStatus = "SERVICE_STATUS", ReadingMethod = "READING_METHOD", ReadingType = "READING_TYPE",
        ContractStatus = "CONTRACT_STATUS", FeeType = "FEE_TYPE", FeeCycle = "FEE_CYCLE",
        ContractServiceType = "CONTRACT_SERVICE_TYPE", ContractUsageType = "CONTRACT_USAGE_TYPE",
        InvoiceType = "INVOICE_TYPE", InvoiceStatus = "INVOICE_STATUS",
        PaymentMethod = "PAYMENT_METHOD", PaymentStatus = "PAYMENT_STATUS",
        PartyType = "PARTY_TYPE", ContactType = "CONTACT_TYPE", VendorType = "VENDOR_TYPE",
        DocumentType = "DOCUMENT_TYPE", TankStatus = "TANK_STATUS", TankEventType = "TANK_EVENT_TYPE",
        RefillStage = "REFILL_STAGE", RefillStatus = "REFILL_STATUS", Priority = "PRIORITY",
        SrStatus = "SR_STATUS", VisitType = "VISIT_TYPE", VisitStatus = "VISIT_STATUS", VisitChecklist = "VISIT_CHECKLIST", VisitFrequency = "VISIT_FREQUENCY",
        Severity = "SEVERITY", AmcStatus = "AMC_STATUS", AmcTermType = "AMC_TERM_TYPE", AmcPaymentStatus = "AMC_PAYMENT_STATUS",
        ScopeItemType = "SCOPE_ITEM_TYPE", CdCertType = "CD_CERT_TYPE", CdStatus = "CD_STATUS", CdRenewalStatus = "CD_RENEWAL_STATUS",
        AccountType = "ACCOUNT_TYPE", JournalStatus = "JOURNAL_STATUS", BillStatus = "BILL_STATUS", Nationality = "NATIONALITY";
}

public record LookupInfo(long Id, string TypeCode, string Code, string? NameEn, string? NameAr, int SortOrder, long? ParentId, bool IsActive);

public class LookupService
{
    private const string CacheKey = "lookups:all";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public LookupService(AppDbContext db, IMemoryCache cache)
    {
        _db = db; _cache = cache;
    }

    public void Invalidate() => _cache.Remove(CacheKey);

    public async Task<IReadOnlyDictionary<long, LookupInfo>> AllAsync()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<long, LookupInfo>? all) && all != null) return all;
        var rows = await (from v in _db.LookupValues.AsNoTracking()
                          join t in _db.LookupTypes.AsNoTracking() on v.LookupTypeId equals t.Id
                          select new LookupInfo(v.Id, t.Code, v.Code, v.NameEn, v.NameAr, v.SortOrder ?? 0, v.ParentId, v.IsActive))
                         .ToListAsync();
        all = rows.ToDictionary(r => r.Id);
        _cache.Set(CacheKey, all, TimeSpan.FromMinutes(30));
        return all;
    }

    public async Task<LookupInfo?> GetAsync(long? id) =>
        id.HasValue && (await AllAsync()).TryGetValue(id.Value, out var v) ? v : null;

    public async Task<string?> CodeAsync(long? id) => (await GetAsync(id))?.Code;

    public async Task<long?> IdOrNullAsync(string type, string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var all = await AllAsync();
        return all.Values.FirstOrDefault(v => v.TypeCode == type && string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    /// <summary>Id of a lookup value. Throws if the lookup was not seeded.</summary>
    public async Task<long> IdAsync(string type, string code) =>
        await IdOrNullAsync(type, code) ?? throw new AppException($"Lookup value '{type}/{code}' does not exist. Seed the lookup tables first.");

    /// <summary>
    /// Accepts either an id or a code coming from the client and returns a validated id
    /// (the id must belong to the expected lookup type).
    /// </summary>
    public async Task<long?> ResolveAsync(string type, long? id, string? code, bool required = false, string? field = null)
    {
        if (id.HasValue)
        {
            var v = await GetAsync(id);
            if (v == null || v.TypeCode != type)
                throw new AppException($"{field ?? type}: id {id} is not a valid {type} value.");
            return id;
        }
        if (!string.IsNullOrWhiteSpace(code))
        {
            return await IdOrNullAsync(type, code)
                   ?? throw new AppException($"{field ?? type}: '{code}' is not a valid {type} value.");
        }
        if (required) throw new AppException($"{field ?? type} is required.");
        return null;
    }

    public async Task<bool> IsAsync(long? id, string type, string code)
    {
        var v = await GetAsync(id);
        return v != null && v.TypeCode == type && string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<LookupInfo>> ByTypeAsync(string type) =>
        (await AllAsync()).Values.Where(v => v.TypeCode == type).OrderBy(v => v.SortOrder).ThenBy(v => v.Id).ToList();
}
