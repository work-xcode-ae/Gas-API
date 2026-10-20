using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>journal_approval</c>: approval request of a manual journal above the threshold (BRD §14.4).
/// Status: pending | approved | rejected. The approver must differ from the journal's creator; approving posts the journal.
/// </summary>
[Table("journal_approval")]
[Index(nameof(JournalEntryId), Name = "ix_journal_approval_entry")]
[Index(nameof(Status), Name = "ix_journal_approval_status")]
public partial class JournalApproval : AuditableEntity
{
    [Column("journal_entry_id")]
    public long JournalEntryId { get; set; }

    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("threshold")]
    [Precision(14, 2)]
    public decimal Threshold { get; set; }

    [Column("requested_by")]
    public long? RequestedBy { get; set; }

    [Column("requested_at")]
    public DateTimeOffset RequestedAt { get; set; }

    [Column("decided_by")]
    public long? DecidedBy { get; set; }

    [Column("decided_at")]
    public DateTimeOffset? DecidedAt { get; set; }

    [Column("reason"), MaxLength(500)]
    public string? Reason { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
