using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_stage_history</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_stage_history")]
public partial class RefillStageHistory : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("from_stage_id")]
    public long? FromStageId { get; set; }

    [Column("to_stage_id")]
    public long ToStageId { get; set; }

    [Column("changed_by")]
    public long? ChangedBy { get; set; }

    [Column("changed_at")]
    public DateTimeOffset ChangedAt { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(FromStageId)), JsonIgnore]
    public LookupValue? FromStage { get; set; }

    [ForeignKey(nameof(ToStageId)), JsonIgnore]
    public LookupValue? ToStage { get; set; }

    [ForeignKey(nameof(ChangedBy)), JsonIgnore]
    public AppUser? ChangedByUser { get; set; }
}
