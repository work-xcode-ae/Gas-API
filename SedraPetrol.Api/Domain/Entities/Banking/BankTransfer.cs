using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>bank_transfer</c>: money moved between two bank / cash accounts (BT-YYYY-NNNN), posted Dr to / Cr from (+ fees).</summary>
[Table("bank_transfer")]
[Index(nameof(TransferNo), IsUnique = true, Name = "ux_bank_transfer_no")]
public partial class BankTransfer : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("transfer_no"), MaxLength(30)]
    public string TransferNo { get; set; } = string.Empty;

    [Column("transfer_date")]
    public DateOnly TransferDate { get; set; }

    [Column("from_account_id")]
    public long FromAccountId { get; set; }

    [Column("to_account_id")]
    public long ToAccountId { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    /// <summary>Bank charges paid from the source account (optional).</summary>
    [Column("fees")]
    [Precision(14, 2)]
    public decimal Fees { get; set; }

    [Column("fees_account_id")]
    public long? FeesAccountId { get; set; }

    [Column("reference_no"), MaxLength(100)]
    public string? ReferenceNo { get; set; }

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>posted | cancelled</summary>
    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "posted";

    [Column("cancel_reason"), MaxLength(300)]
    public string? CancelReason { get; set; }

    [Column("journal_entry_id")]
    public long? JournalEntryId { get; set; }

    [ForeignKey(nameof(FromAccountId)), JsonIgnore]
    public BankAccount? FromAccount { get; set; }

    [ForeignKey(nameof(ToAccountId)), JsonIgnore]
    public BankAccount? ToAccount { get; set; }

    [ForeignKey(nameof(FeesAccountId)), JsonIgnore]
    public Account? FeesAccount { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
