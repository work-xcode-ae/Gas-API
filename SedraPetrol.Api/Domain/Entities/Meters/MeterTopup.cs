using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>meter_topup</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("meter_topup")]
public partial class MeterTopup : AuditableEntity
{
    [Column("meter_id")]
    public long MeterId { get; set; }

    [Column("topup_date")]
    public DateTimeOffset TopupDate { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("quantity_m3")]
    [Precision(14, 3)]
    public decimal? QuantityM3 { get; set; }

    [Column("payment_id")]
    public long? PaymentId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(PaymentId)), JsonIgnore]
    public Payment? Payment { get; set; }
}
