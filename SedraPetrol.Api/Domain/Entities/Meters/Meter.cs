using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Gas meter — an asset; meter fields in table <c>asset_meter</c> (<c>asset_id</c> = <c>asset.id</c>). Serial no., status, branch on <c>asset</c>.</summary>
[Table("asset_meter")]
public partial class Meter : Asset
{
    public Meter() { AssetType = AssetTypes.Meter; }


    [Column("meter_no")]
    [MaxLength(20)]
    [Required]
    public string MeterNo { get; set; } = string.Empty;


    [Column("meter_type_id")]
    public long? MeterTypeId { get; set; }

    [Column("brand")]
    [MaxLength(50)]
    public string? Brand { get; set; }

    [Column("model")]
    [MaxLength(50)]
    public string? Model { get; set; }

    [Column("sim_card_no")]
    [MaxLength(30)]
    public string? SimCardNo { get; set; }

    [Column("calibration_date")]
    public DateOnly? CalibrationDate { get; set; }

    [Column("install_date")]
    public DateOnly? InstallDate { get; set; }

    [Column("initial_reading")]
    [Precision(14, 3)]
    public decimal? InitialReading { get; set; }

    [Column("location_note")]
    [MaxLength(200)]
    public string? LocationNote { get; set; }



    // ---- navigation properties (not serialized) ----

    [ForeignKey(nameof(MeterTypeId)), JsonIgnore]
    public LookupValue? MeterType { get; set; }

}
