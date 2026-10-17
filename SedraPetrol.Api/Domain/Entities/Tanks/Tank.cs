using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>tank</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("tank")]
public partial class Tank : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("tank_no")]
    [MaxLength(20)]
    [Required]
    public string TankNo { get; set; } = string.Empty;

    [Column("gas_type_id")]
    public long GasTypeId { get; set; }

    [Column("capacity_litres")]
    [Precision(14, 3)]
    public decimal CapacityLitres { get; set; }

    [Column("current_level_litres")]
    [Precision(14, 3)]
    public decimal? CurrentLevelLitres { get; set; }

    [Column("serial_no")]
    [MaxLength(50)]
    public string? SerialNo { get; set; }

    [Column("manufacturer")]
    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [Column("install_date")]
    public DateOnly? InstallDate { get; set; }

    [Column("last_inspection_date")]
    public DateOnly? LastInspectionDate { get; set; }

    [Column("next_inspection_date")]
    public DateOnly? NextInspectionDate { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(GasTypeId)), JsonIgnore]
    public GasType? GasType { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
