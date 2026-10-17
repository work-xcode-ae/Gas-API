using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>checklist_template</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("checklist_template")]
public partial class ChecklistTemplate : AuditableEntity
{
    [Column("visit_type_id")]
    public long? VisitTypeId { get; set; }

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VisitTypeId)), JsonIgnore]
    public LookupValue? VisitType { get; set; }
}
