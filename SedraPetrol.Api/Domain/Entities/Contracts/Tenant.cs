using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Customer role of a party — table <c>party_customer</c> (<c>party_id</c> = <c>party.id</c>).
/// Tenants of units and owners we invoice are customers. Name, code, contacts and the ledger account are on <see cref="PartyRole.Party"/>.
/// (The class keeps its old name; every tenant_id column in the database points to this table.)
/// </summary>
[Table("party_customer")]
public partial class Tenant : PartyRole
{
    /// <summary>Branch that manages the customer.</summary>
    [Column("branch_id")]
    public long BranchId { get; set; }

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

    [NotMapped, PartyField(nameof(Entities.Party.EmiratesId)), MaxLength(30)]
    public string? EmiratesId { get => Party?.EmiratesId; set => P.EmiratesId = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Address))]
    public string? Address { get => Party?.Address; set => P.Address = value; }

    /// <summary>Old name of the customer code (= party code).</summary>
    [NotMapped, PartyField(nameof(Entities.Party.PartyCode)), MaxLength(20)]
    public string? TenantCode => Party?.PartyCode;

    [NotMapped, PartyField(nameof(Entities.Party.NationalityId))]
    public long? NationalityId { get => Party?.NationalityId; set => P.NationalityId = value; }

    [NotMapped, PartyField(nameof(Entities.Party.DateOfBirth))]
    public DateOnly? DateOfBirth { get => Party?.DateOfBirth; set => P.DateOfBirth = value; }

    [NotMapped, PartyField(nameof(Entities.Party.CityId))]
    public long? CityId { get => Party?.CityId; set => P.CityId = value; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }
}
