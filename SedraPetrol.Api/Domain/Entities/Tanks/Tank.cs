using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Gas tank at a building — an asset; tank fields in table <c>asset_tank</c> (<c>asset_id</c> = <c>asset.id</c>). Building, branch, serial no., status on <c>asset</c>.</summary>
[Table("asset_tank")]
public partial class Tank : Asset
{
    public Tank() { AssetType = AssetTypes.Tank; }



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


    [Column("manufacturer")]
    [MaxLength(100)]
    public string? Manufacturer { get; set; }

    [Column("install_date")]
    public DateOnly? InstallDate { get; set; }

    [Column("last_inspection_date")]
    public DateOnly? LastInspectionDate { get; set; }

    [Column("next_inspection_date")]
    public DateOnly? NextInspectionDate { get; set; }



    // ---- navigation properties (not serialized) ----


    [ForeignKey(nameof(GasTypeId)), JsonIgnore]
    public GasType? GasType { get; set; }

}
