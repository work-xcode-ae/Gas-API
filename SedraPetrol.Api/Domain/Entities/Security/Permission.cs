using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>permission</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("permission")]
public partial class Permission : AuditableEntity
{
    [Column("code")]
    [MaxLength(100)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("module")]
    [MaxLength(50)]
    public string? Module { get; set; }

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
