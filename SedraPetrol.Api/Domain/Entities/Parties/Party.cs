using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>party</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("party")]
public partial class Party : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("party_type_id")]
    public long PartyTypeId { get; set; }

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("emirates_id")]
    [MaxLength(20)]
    public string? EmiratesId { get; set; }

    [Column("nationality_id")]
    public long? NationalityId { get; set; }

    [Column("trade_license_no")]
    [MaxLength(50)]
    public string? TradeLicenseNo { get; set; }

    [Column("tax_number")]
    [MaxLength(30)]
    public string? TaxNumber { get; set; }

    [Column("city_id")]
    public long? CityId { get; set; }

    [Column("po_box")]
    [MaxLength(20)]
    public string? PoBox { get; set; }

    [Column("address")]
    public string? Address { get; set; }

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
}
