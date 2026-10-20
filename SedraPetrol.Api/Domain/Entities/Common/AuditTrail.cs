using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>audit_trail</c>: one row per inserted / updated / deleted record, written automatically by
/// <see cref="Data.AppDbContext"/> on every SaveChanges (plus login events). Insert-only — the API exposes no update or delete.
/// </summary>
[Table("audit_trail")]
public class AuditTrail
{
    [Key, Column("id")]
    public long Id { get; set; }

    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>All rows written by the same SaveChanges share this id (one business transaction).</summary>
    [Column("transaction_id")]
    public Guid TransactionId { get; set; }

    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("user_name"), MaxLength(150)]
    public string? UserName { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    /// <summary>insert / update / delete (soft delete) / hard_delete / login / login_failed.</summary>
    [Column("action"), MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    [Column("table_name"), MaxLength(100)]
    public string? TableName { get; set; }

    [Column("entity_type"), MaxLength(100)]
    public string? EntityType { get; set; }

    [Column("entity_id"), MaxLength(100)]
    public string? EntityId { get; set; }

    /// <summary>JSON: { "property": { "old": …, "new": … } } (insert: new only, delete: old only). Secrets are masked.</summary>
    [Column("changes")]
    public string? Changes { get; set; }

    [Column("http_method"), MaxLength(10)]
    public string? HttpMethod { get; set; }

    [Column("request_path"), MaxLength(300)]
    public string? RequestPath { get; set; }

    [Column("ip_address"), MaxLength(64)]
    public string? IpAddress { get; set; }

    /// <summary>Records this change belongs to: the record itself + the records it points to (meter of a reading, unit and building of a contract…).</summary>
    public List<AuditTrailLink> Links { get; set; } = new();
}

/// <summary>
/// Table <c>audit_trail_link</c>: which records an audit row belongs to, so the full history of a record
/// (its own changes + changes of everything linked to it) can be listed. Written automatically with the audit row.
/// </summary>
[Table("audit_trail_link")]
[Microsoft.EntityFrameworkCore.Index(nameof(EntityType), nameof(EntityId), Name = "ix_audit_trail_link_entity")]
public class AuditTrailLink
{
    [Key, Column("id")]
    public long Id { get; set; }

    [Column("audit_trail_id")]
    public long AuditTrailId { get; set; }

    [Column("entity_type"), MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id"), MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>true = the audit row is a change of this record itself; false = a related record.</summary>
    [Column("is_self")]
    public bool IsSelf { get; set; }

    [ForeignKey(nameof(AuditTrailId))]
    public AuditTrail? AuditTrail { get; set; }
}
