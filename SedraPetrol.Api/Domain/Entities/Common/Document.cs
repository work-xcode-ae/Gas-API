using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>document</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("document")]
public partial class Document : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("entity_type")]
    [MaxLength(50)]
    [Required]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    public long EntityId { get; set; }

    [Column("document_type_id")]
    public long? DocumentTypeId { get; set; }

    [Column("file_name")]
    [MaxLength(255)]
    public string? FileName { get; set; }

    [Column("file_path")]
    [MaxLength(500)]
    public string? FilePath { get; set; }

    [Column("mime_type")]
    [MaxLength(100)]
    public string? MimeType { get; set; }

    [Column("size_bytes")]
    public long? SizeBytes { get; set; }

    [Column("expiry_date")]
    public DateOnly? ExpiryDate { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(DocumentTypeId)), JsonIgnore]
    public LookupValue? DocumentType { get; set; }
}
