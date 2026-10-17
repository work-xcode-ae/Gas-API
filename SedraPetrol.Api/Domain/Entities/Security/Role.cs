using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>role</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("role")]
public partial class Role : AuditableEntity
{
    [Column("code")]
    [MaxLength(50)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(100)]
    public string? NameAr { get; set; }
}
