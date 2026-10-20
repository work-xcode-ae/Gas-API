using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>bank_reconciliation</c>: a completed bank reconciliation for one account and period
/// (statement balance = book balance − outstanding book items + unrecorded bank items, difference 0).
/// </summary>
[Table("bank_reconciliation")]
[Index(nameof(BankAccountId), nameof(PeriodTo), Name = "ix_bank_reconciliation_account_period")]
public partial class BankReconciliation : AuditableEntity
{
    [Column("bank_account_id")]
    public long BankAccountId { get; set; }

    [Column("period_from")]
    public DateOnly PeriodFrom { get; set; }

    [Column("period_to")]
    public DateOnly PeriodTo { get; set; }

    [Column("statement_balance")]
    [Precision(14, 2)]
    public decimal StatementBalance { get; set; }

    [Column("book_balance")]
    [Precision(14, 2)]
    public decimal BookBalance { get; set; }

    [Column("outstanding_book")]
    [Precision(14, 2)]
    public decimal OutstandingBook { get; set; }

    [Column("unrecorded_bank")]
    [Precision(14, 2)]
    public decimal UnrecordedBank { get; set; }

    [Column("difference")]
    [Precision(14, 2)]
    public decimal Difference { get; set; }

    /// <summary>completed | reopened</summary>
    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "completed";

    [Column("completed_at")]
    public DateTimeOffset CompletedAt { get; set; }

    [Column("completed_by")]
    public long? CompletedBy { get; set; }

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(BankAccountId)), JsonIgnore]
    public BankAccount? BankAccount { get; set; }
}
