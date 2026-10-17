using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>tank_level_history</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("tank_level_history")]
public partial class TankLevelHistory : AuditableEntity
{
    [Column("tank_id")]
    public long TankId { get; set; }

    [Column("event_date")]
    public DateTimeOffset EventDate { get; set; }

    [Column("event_type_id")]
    public long? EventTypeId { get; set; }

    [Column("level_litres")]
    [Precision(14, 3)]
    public decimal LevelLitres { get; set; }

    [Column("tank_refill_id")]
    public long? TankRefillId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(TankId)), JsonIgnore]
    public Tank? Tank { get; set; }

    [ForeignKey(nameof(EventTypeId)), JsonIgnore]
    public LookupValue? EventType { get; set; }

    [ForeignKey(nameof(TankRefillId)), JsonIgnore]
    public TankRefill? TankRefill { get; set; }
}
