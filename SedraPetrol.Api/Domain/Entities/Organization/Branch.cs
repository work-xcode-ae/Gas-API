using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>branch</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("branch")]
public partial class Branch : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("city_id")]
    public long? CityId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("logo_path")]
    [MaxLength(500)]
    public string? LogoPath { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(CityId)), JsonIgnore]
    public City? City { get; set; }
}
