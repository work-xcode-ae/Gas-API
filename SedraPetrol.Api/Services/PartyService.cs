using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>
/// One <c>party</c> row per person / company + role rows (customer, supplier, employee, driver, owner).
/// Every party gets its own ledger account in the chart of accounts, under the group of its main role:
/// <list type="bullet">
/// <item>customers and owners → 1202 Customers (under 12 Receivables)</item>
/// <item>suppliers and suppliers' drivers → 2102 Suppliers (under 2 Liabilities)</item>
/// <item>employees and our own drivers → 1203 Employees (under 12 Receivables)</item>
/// </list>
/// The group is created when missing; each party account is the group code + 5 digits (120200001 …).
/// </summary>
public class PartyService
{
    public const string CustomersGroup = "1202", SuppliersGroup = "2102", EmployeesGroup = "1203";

    /// <summary>Person / company details shared by every role.</summary>
    public record PartyInput
    {
        /// <summary>individual | company</summary>
        public string? PartyType { get; init; }
        public long? PartyTypeId { get; init; }
        public string? NameAr { get; init; }
        public string? NameEn { get; init; }
        public string? EmiratesId { get; init; }
        public long? NationalityId { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public string? TradeLicenseNo { get; init; }
        public string? TaxNumber { get; init; }
        public string? Phone { get; init; }
        public string? Email { get; init; }
        public long? CityId { get; init; }
        public string? PoBox { get; init; }
        public string? Address { get; init; }
        public string? Notes { get; init; }
    }

    private readonly AppDbContext _db;
    private readonly NumberingService _numbers;
    private readonly LookupService _lookups;

    public PartyService(AppDbContext db, NumberingService numbers, LookupService lookups)
    {
        _db = db; _numbers = numbers; _lookups = lookups;
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>A new party (added to the context, not saved). Fields left null in <paramref name="i"/> stay empty.</summary>
    public async Task<Party> NewAsync(long? branchId, PartyInput i)
    {
        if (Clean(i.NameAr) == null && Clean(i.NameEn) == null) throw new AppException("The name is required.");
        var p = new Party
        {
            PartyCode = await _numbers.NextAsync(NumberPrefixes.Party, yearly: false, padding: 6),
            BranchId = branchId, IsActive = true,
            PartyTypeId = (await _lookups.ResolveAsync(LookupTypes.PartyType, i.PartyTypeId, i.PartyType ?? "individual", true, "partyType"))!.Value,
        };
        await ApplyAsync(p, i, onlyGiven: false);
        _db.Parties.Add(p);
        return p;
    }

    /// <summary>Copy the person / company details onto the party. <paramref name="onlyGiven"/>: keep the current value where the input is null.</summary>
    public async Task ApplyAsync(Party p, PartyInput i, bool onlyGiven = true)
    {
        string? Pick(string? v, string? cur) => onlyGiven && v == null ? cur : Clean(v);
        T? PickV<T>(T? v, T? cur) where T : struct => onlyGiven && v == null ? cur : v;
        p.NameAr = Pick(i.NameAr, p.NameAr);
        p.NameEn = Pick(i.NameEn, p.NameEn);
        var eid = Pick(i.EmiratesId, p.EmiratesId);
        if (eid != null && eid != p.EmiratesId)
        {
            var other = await _db.Parties.Where(x => x.EmiratesId == eid && x.Id != p.Id).Select(x => x.PartyCode).FirstOrDefaultAsync();
            if (other != null) throw new BusinessRuleException($"Emirates ID {eid} is already registered ({other}).");
        }
        p.EmiratesId = eid;
        p.NationalityId = PickV(i.NationalityId, p.NationalityId);
        p.DateOfBirth = PickV(i.DateOfBirth, p.DateOfBirth);
        p.TradeLicenseNo = Pick(i.TradeLicenseNo, p.TradeLicenseNo);
        p.TaxNumber = Pick(i.TaxNumber, p.TaxNumber);
        p.Phone = Pick(i.Phone, p.Phone);
        p.Email = Pick(i.Email, p.Email);
        p.CityId = PickV(i.CityId, p.CityId);
        p.PoBox = Pick(i.PoBox, p.PoBox);
        p.Address = Pick(i.Address, p.Address);
        p.Notes = Pick(i.Notes, p.Notes);
        if (i.PartyTypeId != null || i.PartyType != null)
            p.PartyTypeId = (await _lookups.ResolveAsync(LookupTypes.PartyType, i.PartyTypeId, i.PartyType, true, "partyType"))!.Value;
        if (p.Account != null) SyncAccountName(p, p.Account);
    }

    /// <summary>
    /// Before a new role row (supplier, employee, driver …) posted as flat JSON is added: the same person (Emirates ID or trade
    /// licence) already in the system is reused, otherwise the new party gets its code and type.
    /// </summary>
    public async Task PrepareRoleAsync(PartyRole role, long? branchId)
    {
        var input = role.Party ?? throw new AppException("The name is required.");
        if (input.Id != 0) return;
        if (Clean(input.NameAr) == null && Clean(input.NameEn) == null) throw new AppException("The name is required.");
        var eid = Clean(input.EmiratesId); var lic = Clean(input.TradeLicenseNo);
        var existing = eid != null ? await _db.Parties.FirstOrDefaultAsync(x => x.EmiratesId == eid)
                     : lic != null ? await _db.Parties.FirstOrDefaultAsync(x => x.TradeLicenseNo == lic) : null;
        if (existing != null)
        {
            if (_db.Find(role.GetType(), existing.Id) != null)
                throw new BusinessRuleException($"{existing.PartyCode} {existing.NameAr ?? existing.NameEn} is already registered in this list.");
            await ApplyAsync(existing, ToInput(input), onlyGiven: true);
            role.Party = existing;
            return;
        }
        input.PartyCode = Clean(input.PartyCode) ?? await _numbers.NextAsync(NumberPrefixes.Party, yearly: false, padding: 6);
        if (await _db.Parties.AnyAsync(x => x.PartyCode == input.PartyCode)) throw new BusinessRuleException($"Code {input.PartyCode} is already used.");
        input.BranchId ??= branchId;
        input.IsActive = true;
        if (input.PartyTypeId == 0)
            input.PartyTypeId = (await _lookups.ResolveAsync(LookupTypes.PartyType, null, lic != null ? "company" : "individual", true, "partyType"))!.Value;
        input.NameAr = Clean(input.NameAr); input.NameEn = Clean(input.NameEn); input.EmiratesId = eid;
    }

    private static PartyInput ToInput(Party p) => new()
    {
        NameAr = p.NameAr, NameEn = p.NameEn, EmiratesId = p.EmiratesId, NationalityId = p.NationalityId, DateOfBirth = p.DateOfBirth,
        TradeLicenseNo = p.TradeLicenseNo, TaxNumber = p.TaxNumber, Phone = p.Phone, Email = p.Email, CityId = p.CityId, PoBox = p.PoBox, Address = p.Address,
    };

    /// <summary>Customer role for a party (created when missing). Saves.</summary>
    public async Task<Tenant> EnsureCustomerAsync(Party p, long branchId)
    {
        var c = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == p.Id);
        if (c == null) { c = new Tenant { Party = p, BranchId = branchId, IsActive = true }; _db.Tenants.Add(c); }
        else if (c.IsDeleted) { c.IsDeleted = false; c.IsActive = true; }
        await _db.SaveChangesAsync();
        await EnsureAccountAsync(p.Id);
        return c;
    }

    /// <summary>Supplier role for a party (created when missing). Saves.</summary>
    public async Task<Vendor> EnsureVendorAsync(Party p, long? branchId, long vendorTypeId)
    {
        var v = await _db.Vendors.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == p.Id);
        if (v == null) { v = new Vendor { Party = p, BranchId = branchId, VendorTypeId = vendorTypeId, IsActive = true }; _db.Vendors.Add(v); }
        else if (v.IsDeleted) { v.IsDeleted = false; v.IsActive = true; }
        await _db.SaveChangesAsync();
        await EnsureAccountAsync(p.Id);
        return v;
    }

    /// <summary>The party's ledger account id (created when missing; null if the chart of accounts has no place for it).</summary>
    public async Task<long?> AccountIdAsync(long partyId)
    {
        var id = await _db.Parties.IgnoreQueryFilters().Where(p => p.Id == partyId).Select(p => p.AccountId).FirstOrDefaultAsync();
        return id ?? await EnsureAccountAsync(partyId);
    }

    /// <summary>Create the party's own ledger account when it has none. Returns the account id (null when the group cannot be placed).</summary>
    public async Task<long?> EnsureAccountAsync(long partyId)
    {
        var p = await _db.Parties.IgnoreQueryFilters().Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == partyId);
        if (p == null) return null;
        if (p.Account != null) { SyncAccountName(p, p.Account); await _db.SaveChangesAsync(); return p.Account.Id; }

        var groupCode = await GroupCodeAsync(p.Id);
        var companyId = await _db.Companies.Select(c => (long?)c.Id).FirstOrDefaultAsync();
        if (companyId == null) return null;
        var group = await EnsureGroupAsync(companyId.Value, groupCode);
        if (group == null) return null;

        var codes = await _db.Accounts.IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId && a.ParentAccountId == group.Id && a.AccountCode.StartsWith(groupCode))
            .Select(a => a.AccountCode).ToListAsync();
        var next = codes.Select(c => c.Length == groupCode.Length + 5 && long.TryParse(c[groupCode.Length..], out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        var acc = new Account
        {
            CompanyId = companyId.Value, ParentAccountId = group.Id, AccountTypeId = group.AccountTypeId, CurrencyId = group.CurrencyId,
            AccountCode = $"{groupCode}{next:00000}", Level = (short)((group.Level ?? 1) + 1), IsGroup = false, IsPostable = true, IsActive = true,
        };
        SyncAccountName(p, acc);
        _db.Accounts.Add(acc);
        p.Account = acc;
        await _db.SaveChangesAsync();
        return acc.Id;
    }

    /// <summary>Give every party that has none its ledger account. Returns how many were created.</summary>
    public async Task<int> EnsureAllAccountsAsync()
    {
        var ids = await _db.Parties.Where(p => p.AccountId == null).Select(p => p.Id).ToListAsync();
        var n = 0;
        foreach (var id in ids) if (await EnsureAccountAsync(id) != null) n++;
        return n;
    }

    private static void SyncAccountName(Party p, Account a)
    {
        var ar = $"{p.NameAr ?? p.NameEn} ({p.PartyCode})";
        var en = $"{p.NameEn ?? p.NameAr} ({p.PartyCode})";
        if (a.NameAr != ar) a.NameAr = ar.Length > 200 ? ar[..200] : ar;
        if (a.NameEn != en) a.NameEn = en.Length > 200 ? en[..200] : en;
    }

    /// <summary>Group of the party's main role: customer / owner → 1202, supplier → 2102, employee → 1203, driver → by employer.</summary>
    private async Task<string> GroupCodeAsync(long partyId)
    {
        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == partyId)) return CustomersGroup;
        if (await _db.Vendors.IgnoreQueryFilters().AnyAsync(x => x.Id == partyId)) return SuppliersGroup;
        if (await _db.Employees.IgnoreQueryFilters().AnyAsync(x => x.Id == partyId)) return EmployeesGroup;
        var driver = await _db.Drivers.IgnoreQueryFilters().Where(x => x.Id == partyId).Select(x => new { x.VendorId }).FirstOrDefaultAsync();
        if (driver != null) return driver.VendorId == null ? EmployeesGroup : SuppliersGroup;
        return CustomersGroup;   // owners and anyone else we may invoice
    }

    private async Task<Account?> EnsureGroupAsync(long companyId, string code)
    {
        var g = await _db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == code);
        if (g != null) return g;
        var (parentCode, en, ar) = code switch
        {
            CustomersGroup => ("12", "Customers", "العملاء"),
            SuppliersGroup => ("2", "Suppliers", "الموردون"),
            _ => ("12", "Employees", "حسابات الموظفين"),
        };
        var parent = await _db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountCode == parentCode);
        if (parent == null) return null;
        g = new Account
        {
            CompanyId = companyId, ParentAccountId = parent.Id, AccountTypeId = parent.AccountTypeId, CurrencyId = parent.CurrencyId,
            AccountCode = code, NameEn = en, NameAr = ar, Level = (short)((parent.Level ?? 1) + 1), IsGroup = true, IsPostable = false, IsActive = true,
        };
        _db.Accounts.Add(g);
        await _db.SaveChangesAsync();
        return g;
    }
}
