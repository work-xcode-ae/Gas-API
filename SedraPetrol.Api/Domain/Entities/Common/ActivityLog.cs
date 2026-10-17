using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>activity_log</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("activity_log")]
public partial class ActivityLog : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("entity_type")]
    [MaxLength(50)]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    public long? EntityId { get; set; }

    [Column("action")]
    [MaxLength(50)]
    public string? Action { get; set; }

    [Column("severity_id")]
    public long? SeverityId { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("old_values")]
    public string? OldValues { get; set; }

    [Column("new_values")]
    public string? NewValues { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(SeverityId)), JsonIgnore]
    public LookupValue? Severity { get; set; }

    [ForeignKey(nameof(UserId)), JsonIgnore]
    public AppUser? User { get; set; }
}
