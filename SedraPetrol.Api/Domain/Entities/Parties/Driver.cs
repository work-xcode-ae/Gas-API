using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Driver role of a party — table <c>driver</c> (<c>party_id</c> = <c>party.id</c>). Name and phone are on <see cref="PartyRole.Party"/>.
/// <c>vendor_id</c> set = driver of a supplier; empty = our own driver.
/// </summary>
[Table("driver")]
public partial class Driver : PartyRole
{
    [Column("vendor_id")]
    public long? VendorId { get; set; }

    [Column("license_no")]
    [MaxLength(50)]
    public string? LicenseNo { get; set; }

    [Column("license_expiry")]
    public DateOnly? LicenseExpiry { get; set; }

    [Column("hazmat_permit_no")]
    [MaxLength(50)]
    public string? HazmatPermitNo { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- person / company fields, stored on party (flat in the API) ----
    [NotMapped, PartyField(nameof(Entities.Party.NameEn)), MaxLength(200)]
    public string? NameEn { get => Party?.NameEn; set => P.NameEn = value; }

    [NotMapped, PartyField(nameof(Entities.Party.NameAr)), MaxLength(200)]
    public string? NameAr { get => Party?.NameAr; set => P.NameAr = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Phone)), MaxLength(30)]
    public string? Phone { get => Party?.Phone; set => P.Phone = value; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }
}
