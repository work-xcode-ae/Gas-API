using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>meter_assignment</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("meter_assignment")]
public partial class MeterAssignment : AuditableEntity
{
    [Column("meter_id")]
    public long MeterId { get; set; }

    [Column("unit_id")]
    public long UnitId { get; set; }

    [Column("from_date")]
    public DateOnly FromDate { get; set; }

    [Column("to_date")]
    public DateOnly? ToDate { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }
}
