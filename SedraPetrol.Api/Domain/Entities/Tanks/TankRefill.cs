using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>tank_refill</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("tank_refill")]
public partial class TankRefill : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("tank_id")]
    public long TankId { get; set; }

    [Column("refill_datetime")]
    public DateTimeOffset RefillDatetime { get; set; }

    [Column("vehicle_id")]
    public long? VehicleId { get; set; }

    [Column("driver_id")]
    public long? DriverId { get; set; }

    [Column("level_before_litres")]
    [Precision(14, 3)]
    public decimal? LevelBeforeLitres { get; set; }

    [Column("level_after_litres")]
    [Precision(14, 3)]
    public decimal? LevelAfterLitres { get; set; }

    [Column("delivered_qty_litres")]
    [Precision(14, 3)]
    public decimal? DeliveredQtyLitres { get; set; }

    [Column("safety_check_passed")]
    public bool? SafetyCheckPassed { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(TankId)), JsonIgnore]
    public Tank? Tank { get; set; }

    [ForeignKey(nameof(VehicleId)), JsonIgnore]
    public Vehicle? Vehicle { get; set; }

    [ForeignKey(nameof(DriverId)), JsonIgnore]
    public Driver? Driver { get; set; }
}
