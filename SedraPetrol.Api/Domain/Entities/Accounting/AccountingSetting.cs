using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>accounting_setting</c>: one row per company.
/// JournalApprovalThreshold: manual journals whose total debit is above it need approval by another user (accounting.post) before posting; 0 = no approval.
/// OpeningEquityAccountId: account that absorbs the difference of opening balances (created automatically as 3301 when missing).
/// </summary>
[Table("accounting_setting")]
[Index(nameof(CompanyId), IsUnique = true, Name = "ux_accounting_setting_company")]
public partial class AccountingSetting : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("journal_approval_threshold")]
    [Precision(14, 2)]
    public decimal JournalApprovalThreshold { get; set; }

    [Column("opening_equity_account_id")]
    public long? OpeningEquityAccountId { get; set; }

    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(OpeningEquityAccountId)), JsonIgnore]
    public Account? OpeningEquityAccount { get; set; }
}
