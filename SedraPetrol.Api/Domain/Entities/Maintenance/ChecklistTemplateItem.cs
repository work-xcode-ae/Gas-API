using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>checklist_template_item</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("checklist_template_item")]
public partial class ChecklistTemplateItem : AuditableEntity
{
    [Column("template_id")]
    public long TemplateId { get; set; }

    [Column("item_text_en")]
    [MaxLength(300)]
    public string? ItemTextEn { get; set; }

    [Column("item_text_ar")]
    [MaxLength(300)]
    public string? ItemTextAr { get; set; }

    [Column("sort_order")]
    public int? SortOrder { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(TemplateId)), JsonIgnore]
    public ChecklistTemplate? Template { get; set; }
}
