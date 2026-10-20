using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>bank_statement_line</c>: one line of an imported bank statement. Amount is signed: deposit &gt; 0, withdrawal &lt; 0
/// (a deposit matches a DEBIT on the bank's GL account). Status: unmatched | matched | ignored.
/// Lines covered by a completed reconciliation (<see cref="ReconciliationId"/>) are locked.
/// </summary>
[Table("bank_statement_line")]
[Index(nameof(BankAccountId), nameof(LineDate), Name = "ix_bank_statement_line_account_date")]
[Index(nameof(JournalLineId), Name = "ix_bank_statement_line_journal_line")]
public partial class BankStatementLine : AuditableEntity
{
    [Column("bank_account_id")]
    public long BankAccountId { get; set; }

    [Column("line_date")]
    public DateOnly LineDate { get; set; }

    [Column("description"), MaxLength(500)]
    public string? Description { get; set; }

    [Column("reference"), MaxLength(100)]
    public string? Reference { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    /// <summary>Running balance printed on the statement (optional).</summary>
    [Column("balance")]
    [Precision(14, 2)]
    public decimal? Balance { get; set; }

    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "unmatched";

    [Column("journal_line_id")]
    public long? JournalLineId { get; set; }

    [Column("matched_at")]
    public DateTimeOffset? MatchedAt { get; set; }

    [Column("matched_by")]
    public long? MatchedBy { get; set; }

    [Column("import_batch")]
    public Guid ImportBatch { get; set; }

    [Column("reconciliation_id")]
    public long? ReconciliationId { get; set; }

    [ForeignKey(nameof(BankAccountId)), JsonIgnore]
    public BankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(JournalLineId)), JsonIgnore]
    public JournalLine? JournalLine { get; set; }

    [ForeignKey(nameof(ReconciliationId)), JsonIgnore]
    public BankReconciliation? Reconciliation { get; set; }
}
