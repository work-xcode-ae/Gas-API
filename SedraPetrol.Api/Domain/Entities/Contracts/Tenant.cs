using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>tenant</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("tenant")]
public partial class Tenant : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("tenant_code")]
    [MaxLength(20)]
    [Required]
    public string TenantCode { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("emirates_id")]
    [MaxLength(20)]
    public string? EmiratesId { get; set; }

    [Column("nationality_id")]
    public long? NationalityId { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("city_id")]
    public long? CityId { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("receivable_account_id")]
    public long? ReceivableAccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(NationalityId)), JsonIgnore]
    public LookupValue? Nationality { get; set; }

    [ForeignKey(nameof(CityId)), JsonIgnore]
    public City? City { get; set; }

    [ForeignKey(nameof(ReceivableAccountId)), JsonIgnore]
    public Account? ReceivableAccount { get; set; }
}
