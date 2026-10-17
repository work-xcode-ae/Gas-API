using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>building</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("building")]
public partial class Building : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("building_type_id")]
    public long? BuildingTypeId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("city_id")]
    public long? CityId { get; set; }

    [Column("zone")]
    [MaxLength(100)]
    public string? Zone { get; set; }

    [Column("street")]
    [MaxLength(150)]
    public string? Street { get; set; }

    [Column("po_box")]
    [MaxLength(20)]
    public string? PoBox { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("latitude")]
    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    [Precision(9, 6)]
    public decimal? Longitude { get; set; }

    [Column("total_floors")]
    public int? TotalFloors { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingTypeId)), JsonIgnore]
    public LookupValue? BuildingType { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(CityId)), JsonIgnore]
    public City? City { get; set; }
}
