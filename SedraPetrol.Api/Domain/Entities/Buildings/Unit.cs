using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>unit</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("unit")]
public partial class Unit : AuditableEntity
{
    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("unit_no")]
    [MaxLength(20)]
    [Required]
    public string UnitNo { get; set; } = string.Empty;

    [Column("floor_no")]
    public int? FloorNo { get; set; }

    [Column("unit_type_id")]
    public long? UnitTypeId { get; set; }

    [Column("area_sqm")]
    [Precision(10, 2)]
    public decimal? AreaSqm { get; set; }

    [Column("bedrooms")]
    public short? Bedrooms { get; set; }

    [Column("bathrooms")]
    public short? Bathrooms { get; set; }

    [Column("rent_amount")]
    [Precision(14, 2)]
    public decimal? RentAmount { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("last_inspection_date")]
    public DateOnly? LastInspectionDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(UnitTypeId)), JsonIgnore]
    public LookupValue? UnitType { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
