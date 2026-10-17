using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>gas_type</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("gas_type")]
public partial class GasType : AuditableEntity
{
    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(100)]
    public string? NameAr { get; set; }

    [Column("litre_to_m3")]
    [Precision(18, 6)]
    public decimal LitreToM3 { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
