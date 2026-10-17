using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_schedule</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_schedule")]
public partial class RefillSchedule : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("delivery_date")]
    public DateOnly DeliveryDate { get; set; }

    [Column("time_slot")]
    [MaxLength(30)]
    public string? TimeSlot { get; set; }

    [Column("civil_defense_confirmed")]
    public bool CivilDefenseConfirmed { get; set; }

    [Column("civil_defense_ref")]
    [MaxLength(50)]
    public string? CivilDefenseRef { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }
}
