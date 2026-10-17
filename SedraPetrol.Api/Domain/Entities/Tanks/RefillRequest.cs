using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_request</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_request")]
public partial class RefillRequest : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("request_no")]
    [MaxLength(30)]
    [Required]
    public string RequestNo { get; set; } = string.Empty;

    [Column("tank_id")]
    public long TankId { get; set; }

    [Column("request_date")]
    public DateOnly RequestDate { get; set; }

    [Column("requested_by")]
    public long? RequestedBy { get; set; }

    [Column("requested_qty_litres")]
    [Precision(14, 3)]
    public decimal RequestedQtyLitres { get; set; }

    [Column("level_at_request_litres")]
    [Precision(14, 3)]
    public decimal? LevelAtRequestLitres { get; set; }

    [Column("priority_id")]
    public long? PriorityId { get; set; }

    [Column("current_stage_id")]
    public long? CurrentStageId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("approved_by")]
    public long? ApprovedBy { get; set; }

    [Column("approved_at")]
    public DateTimeOffset? ApprovedAt { get; set; }

    [Column("approval_notes")]
    public string? ApprovalNotes { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(TankId)), JsonIgnore]
    public Tank? Tank { get; set; }

    [ForeignKey(nameof(RequestedBy)), JsonIgnore]
    public AppUser? RequestedByUser { get; set; }

    [ForeignKey(nameof(PriorityId)), JsonIgnore]
    public LookupValue? Priority { get; set; }

    [ForeignKey(nameof(CurrentStageId)), JsonIgnore]
    public LookupValue? CurrentStage { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(ApprovedBy)), JsonIgnore]
    public AppUser? ApprovedByUser { get; set; }
}
