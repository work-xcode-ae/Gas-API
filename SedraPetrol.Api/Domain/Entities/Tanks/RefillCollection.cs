using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_collection</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_collection")]
public partial class RefillCollection : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("vehicle_id")]
    public long? VehicleId { get; set; }

    [Column("driver_id")]
    public long? DriverId { get; set; }

    [Column("collection_date")]
    public DateTimeOffset? CollectionDate { get; set; }

    [Column("meter_before")]
    [Precision(14, 3)]
    public decimal? MeterBefore { get; set; }

    [Column("meter_after")]
    [Precision(14, 3)]
    public decimal? MeterAfter { get; set; }

    [Column("collected_qty_litres")]
    [Precision(14, 3)]
    public decimal? CollectedQtyLitres { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(VehicleId)), JsonIgnore]
    public Vehicle? Vehicle { get; set; }

    [ForeignKey(nameof(DriverId)), JsonIgnore]
    public Driver? Driver { get; set; }
}
