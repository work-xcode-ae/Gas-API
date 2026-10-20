using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>voucher</c>: general receipt (سند قبض) / payment (سند صرف) vouchers against any ledger account
/// (other income, expenses, owners, employees, petty cash …). Customer receipts live in <c>payment</c> and supplier payments in
/// <c>vendor_payment</c>; the vouchers API shows all three as one list.
/// </summary>
[Table("voucher")]
public partial class Voucher : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("voucher_no"), MaxLength(30)]
    public string VoucherNo { get; set; } = string.Empty;

    /// <summary>receipt | payment</summary>
    [Column("voucher_type"), MaxLength(10)]
    public string VoucherType { get; set; } = "receipt";

    [Column("voucher_date")]
    public DateOnly VoucherDate { get; set; }

    /// <summary>The other side of the entry (income / expense / owner / employee … account).</summary>
    [Column("account_id")]
    public long AccountId { get; set; }

    /// <summary>Received from / paid to (free text).</summary>
    [Column("party_name"), MaxLength(200)]
    public string PartyName { get; set; } = string.Empty;

    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column("payment_method_id")]
    public long? PaymentMethodId { get; set; }

    [Column("reference_no"), MaxLength(100)]
    public string? ReferenceNo { get; set; }

    [Column("cheque_no"), MaxLength(50)]
    public string? ChequeNo { get; set; }

    [Column("cheque_date")]
    public DateOnly? ChequeDate { get; set; }

    [Column("bank_name"), MaxLength(100)]
    public string? BankName { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("description"), MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>posted | cancelled</summary>
    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "posted";

    [Column("cancel_reason"), MaxLength(300)]
    public string? CancelReason { get; set; }

    [Column("journal_entry_id")]
    public long? JournalEntryId { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(AccountId)), JsonIgnore]
    public Account? Account { get; set; }

    [ForeignKey(nameof(PaymentMethodId)), JsonIgnore]
    public LookupValue? PaymentMethod { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
