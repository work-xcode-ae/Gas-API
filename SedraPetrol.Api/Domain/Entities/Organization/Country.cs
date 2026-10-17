using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>country</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("country")]
public partial class Country : AuditableEntity
{
    [Column("code")]
    [MaxLength(2)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(100)]
    public string? NameAr { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
