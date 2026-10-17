using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>visit_checklist_item</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("visit_checklist_item")]
public partial class VisitChecklistItem : AuditableEntity
{
    [Column("visit_id")]
    public long VisitId { get; set; }

    [Column("template_item_id")]
    public long? TemplateItemId { get; set; }

    [Column("item_text")]
    [MaxLength(300)]
    [Required]
    public string ItemText { get; set; } = string.Empty;

    [Column("sort_order")]
    public int? SortOrder { get; set; }

    [Column("is_done")]
    public bool IsDone { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VisitId)), JsonIgnore]
    public MaintenanceVisit? Visit { get; set; }

    [ForeignKey(nameof(TemplateItemId)), JsonIgnore]
    public ChecklistTemplateItem? TemplateItem { get; set; }
}
