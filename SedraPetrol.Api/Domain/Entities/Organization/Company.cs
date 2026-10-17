using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>company</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("company")]
public partial class Company : AuditableEntity
{
    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    [Required]
    public string NameEn { get; set; } = string.Empty;

    [Column("name_ar")]
    [MaxLength(200)]
    [Required]
    public string NameAr { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("trn")]
    [MaxLength(20)]
    public string? Trn { get; set; }

    [Column("logo_path")]
    [MaxLength(500)]
    public string? LogoPath { get; set; }

    [Column("logo_dark_path")]
    [MaxLength(500)]
    public string? LogoDarkPath { get; set; }

    [Column("stamp_path")]
    [MaxLength(500)]
    public string? StampPath { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("website")]
    [MaxLength(150)]
    public string? Website { get; set; }

    [Column("base_currency_id")]
    public long? BaseCurrencyId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BaseCurrencyId)), JsonIgnore]
    public Currency? BaseCurrency { get; set; }
}
