using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>city</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("city")]
public partial class City : AuditableEntity
{
    [Column("emirate_id")]
    public long EmirateId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string? Code { get; set; }

    [Column("name_en")]
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(100)]
    public string? NameAr { get; set; }

    [Column("latitude")]
    [Precision(9, 6)]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    [Precision(9, 6)]
    public decimal? Longitude { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(EmirateId)), JsonIgnore]
    public Emirate? Emirate { get; set; }
}
