using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>account_mapping</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("account_mapping")]
public partial class AccountMapping : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("source_type")]
    [MaxLength(50)]
    [Required]
    public string SourceType { get; set; } = string.Empty;

    [Column("source_key_id")]
    public long? SourceKeyId { get; set; }

    [Column("debit_account_id")]
    public long? DebitAccountId { get; set; }

    [Column("credit_account_id")]
    public long? CreditAccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(SourceKeyId)), JsonIgnore]
    public LookupValue? SourceKey { get; set; }

    [ForeignKey(nameof(DebitAccountId)), JsonIgnore]
    public Account? DebitAccount { get; set; }

    [ForeignKey(nameof(CreditAccountId)), JsonIgnore]
    public Account? CreditAccount { get; set; }
}
