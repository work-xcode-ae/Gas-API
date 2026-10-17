using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>app_user</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("app_user")]
public partial class AppUser : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("email")]
    [MaxLength(150)]
    [Required]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("password_hash")]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Column("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }
}
