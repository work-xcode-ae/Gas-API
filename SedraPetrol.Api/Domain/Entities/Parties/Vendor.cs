using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Supplier role of a party — table <c>party_vendor</c> (<c>party_id</c> = <c>party.id</c>).
/// Name, code, TRN, contacts and the ledger account are on <see cref="PartyRole.Party"/>.
/// </summary>
[Table("party_vendor")]
public partial class Vendor : PartyRole
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    /// <summary>Lookup VENDOR_TYPE.</summary>
    [Column("vendor_type_id")]
    public long VendorTypeId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- person / company fields, stored on party (flat in the API) ----
    [NotMapped, PartyField(nameof(Entities.Party.NameEn)), MaxLength(200)]
    public string? NameEn { get => Party?.NameEn; set => P.NameEn = value; }

    [NotMapped, PartyField(nameof(Entities.Party.NameAr)), MaxLength(200)]
    public string? NameAr { get => Party?.NameAr; set => P.NameAr = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Phone)), MaxLength(30)]
    public string? Phone { get => Party?.Phone; set => P.Phone = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Email)), MaxLength(150)]
    public string? Email { get => Party?.Email; set => P.Email = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Address))]
    public string? Address { get => Party?.Address; set => P.Address = value; }

    /// <summary>Supplier code (= party code).</summary>
    [NotMapped, PartyField(nameof(Entities.Party.PartyCode)), MaxLength(20)]
    public string? Code { get => Party?.PartyCode; set { if (!string.IsNullOrWhiteSpace(value)) P.PartyCode = value.Trim(); } }

    [NotMapped, PartyField(nameof(Entities.Party.TradeLicenseNo)), MaxLength(50)]
    public string? TradeLicenseNo { get => Party?.TradeLicenseNo; set => P.TradeLicenseNo = value; }

    /// <summary>Tax registration number (= party.tax_number).</summary>
    [NotMapped, PartyField(nameof(Entities.Party.TaxNumber)), MaxLength(30)]
    public string? Trn { get => Party?.TaxNumber; set => P.TaxNumber = value; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(VendorTypeId)), JsonIgnore]
    public LookupValue? VendorType { get; set; }
}
