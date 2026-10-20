using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Owners / parties (individuals or companies) with their contacts and bank accounts.</summary>
[Route("api/parties")]
[PermissionModule("parties")]
public class PartiesController : CrudController<Party>
{
    /// <summary>Party with contacts, bank accounts and owned buildings.</summary>
    [HttpGet("{id:long}/details")]
    public async Task<IActionResult> Details(long id)
    {
        await LoadLookups();
        var p = await Db.Parties.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Party", id);
        var contacts = await Db.PartyContacts.AsNoTracking().Where(c => c.PartyId == id).ToListAsync();
        var banks = await Db.PartyBankAccounts.AsNoTracking().Where(c => c.PartyId == id).ToListAsync();
        var buildings = await Db.BuildingOwners.AsNoTracking().Where(o => o.PartyId == id)
            .Select(o => new { o.BuildingId, o.Building!.Code, o.Building.NameEn, o.Building.NameAr, o.OwnershipPercentage, o.IsPrimary }).ToListAsync();
        return Ok(new
        {
            party = p, partyType = Code(p.PartyTypeId), nationality = Label(p.NationalityId),
            contacts = contacts.Select(c => new { c.Id, type = Code(c.ContactTypeId), c.ContactTypeId, c.Value, c.IsDefault, c.IsActive }),
            bankAccounts = banks,
            buildings = buildings.Select(b => new { b.BuildingId, b.Code, name = Loc(b.NameEn, b.NameAr), b.OwnershipPercentage, b.IsPrimary }),
        });
    }

    [HttpGet("{id:long}/contacts")]
    public async Task<IActionResult> Contacts(long id) => Ok(await Db.PartyContacts.AsNoTracking().Where(c => c.PartyId == id).ToListAsync());

    [HttpPost("{id:long}/contacts")]
    public async Task<IActionResult> AddContact(long id, [FromBody] ContactInput input)
    {
        if (!await Db.Parties.AnyAsync(p => p.Id == id)) throw new NotFoundException("Party", id);
        var c = new PartyContact { PartyId = id, ContactTypeId = (await Lk(LookupTypes.ContactType, input.ContactTypeId, input.Type, true, "type"))!.Value, Value = input.Value, IsDefault = input.IsDefault, IsActive = true };
        if (c.IsDefault) await Db.PartyContacts.Where(x => x.PartyId == id && x.ContactTypeId == c.ContactTypeId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
        Db.PartyContacts.Add(c);
        await Db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("{id:long}/contacts/{contactId:long}")]
    public async Task<IActionResult> RemoveContact(long id, long contactId)
    {
        var c = await Db.PartyContacts.FirstOrDefaultAsync(x => x.Id == contactId && x.PartyId == id) ?? throw new NotFoundException("PartyContact", contactId);
        Db.PartyContacts.Remove(c);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:long}/bank-accounts")]
    public async Task<IActionResult> BankAccounts(long id) => Ok(await Db.PartyBankAccounts.AsNoTracking().Where(c => c.PartyId == id).ToListAsync());

    [HttpPost("{id:long}/bank-accounts")]
    public async Task<IActionResult> AddBankAccount(long id, [FromBody] PartyBankAccount input)
    {
        if (!await Db.Parties.AnyAsync(p => p.Id == id)) throw new NotFoundException("Party", id);
        var b = new PartyBankAccount { PartyId = id, BankName = input.BankName, AccountName = input.AccountName, Iban = input.Iban?.Replace(" ", ""), IsDefault = input.IsDefault, IsActive = true };
        Db.PartyBankAccounts.Add(b);
        await Db.SaveChangesAsync();
        return Ok(b);
    }

    [HttpDelete("{id:long}/bank-accounts/{accountId:long}")]
    public async Task<IActionResult> RemoveBankAccount(long id, long accountId)
    {
        var b = await Db.PartyBankAccounts.FirstOrDefaultAsync(x => x.Id == accountId && x.PartyId == id) ?? throw new NotFoundException("PartyBankAccount", accountId);
        Db.PartyBankAccounts.Remove(b);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    public record ContactInput(long? ContactTypeId, string? Type, string Value, bool IsDefault);

    protected override async Task OnCreatingAsync(Party e)
    {
        await Lookups.ResolveAsync(LookupTypes.PartyType, e.PartyTypeId, null, true, "partyTypeId");
        e.AccountId = null;
        if (string.IsNullOrWhiteSpace(e.PartyCode)) e.PartyCode = await Numbers.NextAsync(NumberPrefixes.Party, yearly: false, padding: 6);
        if (!string.IsNullOrWhiteSpace(e.EmiratesId) && await Db.Parties.AnyAsync(x => x.EmiratesId == e.EmiratesId.Trim()))
            throw new BusinessRuleException($"Emirates ID {e.EmiratesId} is already registered.");
    }

    protected override async Task OnUpdatingAsync(Party existing, Party incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming.PartyCode)) incoming.PartyCode = existing.PartyCode;
        incoming.AccountId = existing.AccountId;   // the ledger account is managed by the system
        await Task.CompletedTask;
    }

    protected override async Task OnSavedAsync(Party e, string action)
    {
        if (action != "delete") await HttpContext.RequestServices.GetRequiredService<PartyService>().EnsureAccountAsync(e.Id);
    }

    /// <summary>Create the missing ledger account of every party (customers, owners, suppliers, employees, drivers).</summary>
    [HttpPost("accounts")]
    [RequirePermission("accounting.post")]
    public async Task<IActionResult> EnsureAccounts() =>
        Ok(new { created = await HttpContext.RequestServices.GetRequiredService<PartyService>().EnsureAllAccountsAsync() });

    /// <summary>Creates a party with its phone / mobile / email contacts and optional bank account.</summary>
    internal static async Task<Party> CreateParty(AppDbContext db, LookupService lookups, NumberingService numbers, long? branchId, BuildingsController.NewPartyInput i)
    {
        if (string.IsNullOrWhiteSpace(i.NameAr) && string.IsNullOrWhiteSpace(i.NameEn)) throw new AppException("Owner name is required.");
        var party = new Party
        {
            PartyCode = await numbers.NextAsync(NumberPrefixes.Party, yearly: false, padding: 6),
            Phone = new[] { i.Mobile, i.Phone }.Concat(i.Phones ?? new()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim(),
            Email = new[] { i.Email }.Concat(i.Emails ?? new()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim(),
            BranchId = branchId,
            PartyTypeId = (await lookups.ResolveAsync(LookupTypes.PartyType, null, i.PartyType ?? "individual", true, "partyType"))!.Value,
            NameEn = i.NameEn, NameAr = i.NameAr, EmiratesId = i.EmiratesId, TradeLicenseNo = i.TradeLicenseNo, TaxNumber = i.TaxNumber,
            NationalityId = await lookups.ResolveAsync(LookupTypes.Nationality, null, i.Nationality, field: "nationality"),
            CityId = i.CityId, PoBox = i.PoBox, Address = i.Address, IsActive = true,
        };
        db.Parties.Add(party);
        await db.SaveChangesAsync();
        // Single Phone / Mobile / Email plus the optional Phones / Emails lists (portal wizard).
        // The first value of each kind is the default; duplicates are skipped.
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDefault = new HashSet<string>();
        async Task Contact(string type, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var v = value.Trim();
            if (!added.Add($"{type}:{v}")) return;
            var kind = type == "email" ? "email" : "phone";
            db.PartyContacts.Add(new PartyContact { PartyId = party.Id, ContactTypeId = await lookups.IdAsync(LookupTypes.ContactType, type), Value = v, IsDefault = hasDefault.Add(kind), IsActive = true });
        }
        await Contact("phone", i.Phone);
        await Contact("mobile", i.Mobile);
        await Contact("email", i.Email);
        foreach (var p in i.Phones ?? new()) await Contact("phone", p);
        foreach (var e in i.Emails ?? new()) await Contact("email", e);
        if (!string.IsNullOrWhiteSpace(i.Iban) || !string.IsNullOrWhiteSpace(i.BankName))
            db.PartyBankAccounts.Add(new PartyBankAccount { PartyId = party.Id, BankName = i.BankName, Iban = i.Iban?.Replace(" ", ""), AccountName = i.NameEn ?? i.NameAr, IsDefault = true, IsActive = true });
        await db.SaveChangesAsync();
        return party;
    }
}

[Route("api/party-contacts")]
[PermissionModule("parties")]
public class PartyContactsController : CrudController<PartyContact> { }

[Route("api/party-bank-accounts")]
[PermissionModule("parties")]
public class PartyBankAccountsController : CrudController<PartyBankAccount> { }

[Route("api/building-owners")]
[PermissionModule("buildings")]
public class BuildingOwnersController : CrudController<BuildingOwner> { }
