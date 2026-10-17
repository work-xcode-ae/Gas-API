using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>lookup_value</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("lookup_value")]
public partial class LookupValue : AuditableEntity
{
    [Column("lookup_type_id")]
    public long LookupTypeId { get; set; }

    [Column("code")]
    [MaxLength(50)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("parent_id")]
    public long? ParentId { get; set; }

    [Column("sort_order")]
    public int? SortOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(LookupTypeId)), JsonIgnore]
    public LookupType? LookupType { get; set; }

    [ForeignKey(nameof(ParentId)), JsonIgnore]
    public LookupValue? Parent { get; set; }
}
