using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>currency</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("currency")]
public partial class Currency : AuditableEntity
{
    [Column("code")]
    [MaxLength(3)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(50)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(50)]
    public string? NameAr { get; set; }

    [Column("symbol")]
    [MaxLength(10)]
    public string? Symbol { get; set; }

    [Column("decimal_places")]
    public short? DecimalPlaces { get; set; } = 2;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
