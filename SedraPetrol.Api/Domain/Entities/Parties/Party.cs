using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>party</c> — ONE row for every person or company the system deals with: customers (tenants), building owners,
/// suppliers, employees, drivers. Name, ID numbers and contact details live only here.
/// What the party IS for us is a role, in its own table keyed by <c>party_id</c> (a party can hold several roles at once):
/// <list type="bullet">
/// <item><c>party_customer</c> → <see cref="Tenant"/> (we invoice them: tenants, owners billed for AMC / civil defence …)</item>
/// <item><c>party_vendor</c> → <see cref="Vendor"/> (we buy from them)</item>
/// <item><c>employee</c> → <see cref="Employee"/>, <c>driver</c> → <see cref="Driver"/></item>
/// <item><c>building_owner</c> → owner of a building</item>
/// </list>
/// Every party has its own ledger account (<c>account_id</c>) in the chart of accounts — see <c>PartyService</c>.
/// </summary>
[Table("party")]
public partial class Party : AuditableEntity
{
    /// <summary>Unique code of the person / company (P-000123).</summary>
    [Column("party_code")]
    [MaxLength(20)]
    [Required]
    public string PartyCode { get; set; } = string.Empty;

    [Column("branch_id")]
    public long? BranchId { get; set; }

    /// <summary>Lookup PARTY_TYPE: individual | company.</summary>
    [Column("party_type_id")]
    public long PartyTypeId { get; set; }

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("emirates_id")]
    [MaxLength(30)]
    public string? EmiratesId { get; set; }

    [Column("nationality_id")]
    public long? NationalityId { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("trade_license_no")]
    [MaxLength(50)]
    public string? TradeLicenseNo { get; set; }

    /// <summary>Tax registration number (TRN).</summary>
    [Column("tax_number")]
    [MaxLength(30)]
    public string? TaxNumber { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("city_id")]
    public long? CityId { get; set; }

    [Column("po_box")]
    [MaxLength(20)]
    public string? PoBox { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    /// <summary>The party's own ledger account (receivable / payable / staff account), created automatically.</summary>
    [Column("account_id")]
    public long? AccountId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(PartyTypeId)), JsonIgnore]
    public LookupValue? PartyType { get; set; }

    [ForeignKey(nameof(NationalityId)), JsonIgnore]
    public LookupValue? Nationality { get; set; }

    [ForeignKey(nameof(CityId)), JsonIgnore]
    public City? City { get; set; }

    [ForeignKey(nameof(AccountId)), JsonIgnore]
    public Account? Account { get; set; }

    [JsonIgnore] public Tenant? Customer { get; set; }
    [JsonIgnore] public Vendor? Vendor { get; set; }
    [JsonIgnore] public Employee? Employee { get; set; }
    [JsonIgnore] public Driver? Driver { get; set; }
}

/// <summary>Base of a party role table: the key is the party's id (column <c>party_id</c>).</summary>
public abstract class PartyRole : AuditableEntity
{
    /// <summary>The person / company (name, contacts, account). Not validated as API input: the flat fields below fill it.</summary>
    [JsonIgnore, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public Party? Party { get; set; }

    /// <summary>The party, created on first write (JSON input of a new role).</summary>
    protected Party P => Party ??= new Party();

    /// <summary>Code of the person / company (read only).</summary>
    [NotMapped, PartyField(nameof(Entities.Party.PartyCode))]
    public string? PartyCode => Party?.PartyCode;

    /// <summary>The party's own ledger account (read only).</summary>
    [NotMapped, PartyField(nameof(Entities.Party.AccountId))]
    public long? AccountId => Party?.AccountId;
}

/// <summary>
/// Marks a role property that is stored on <c>party</c> (e.g. <c>Vendor.NameAr</c> = <c>party.name_ar</c>).
/// It keeps the API shape of the role (flat JSON) and lets generic filters / sorting query <c>Party.&lt;Name&gt;</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PartyFieldAttribute : Attribute
{
    public string Name { get; }
    public PartyFieldAttribute(string name) { Name = name; }
}
