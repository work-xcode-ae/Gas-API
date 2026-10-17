using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>meter</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("meter")]
public partial class Meter : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("meter_no")]
    [MaxLength(20)]
    [Required]
    public string MeterNo { get; set; } = string.Empty;

    [Column("serial_no")]
    [MaxLength(50)]
    [Required]
    public string SerialNo { get; set; } = string.Empty;

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

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(MeterTypeId)), JsonIgnore]
    public LookupValue? MeterType { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
