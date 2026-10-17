using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>account</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("account")]
public partial class Account : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("parent_account_id")]
    public long? ParentAccountId { get; set; }

    [Column("account_type_id")]
    public long? AccountTypeId { get; set; }

    [Column("account_code")]
    [MaxLength(20)]
    [Required]
    public string AccountCode { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("level")]
    public short? Level { get; set; }

    [Column("is_group")]
    public bool IsGroup { get; set; }

    [Column("is_postable")]
    public bool IsPostable { get; set; } = true;

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(ParentAccountId)), JsonIgnore]
    public Account? ParentAccount { get; set; }

    [ForeignKey(nameof(AccountTypeId)), JsonIgnore]
    public LookupValue? AccountType { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }
}
