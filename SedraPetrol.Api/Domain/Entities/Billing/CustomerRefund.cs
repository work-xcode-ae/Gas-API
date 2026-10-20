using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>customer_refund</c> (استرداد للعميل, BRD §7.3): money paid back to a customer out of their unallocated credit
/// (unallocated receipts and credit notes). Posted Dr A/R, Cr cash/bank. The credit used is recorded in <see cref="CustomerRefundSource"/>.
/// </summary>
[Table("customer_refund")]
[Index(nameof(RefundNo), IsUnique = true, Name = "ux_customer_refund_no")]
[Index(nameof(TenantId), Name = "ix_customer_refund_tenant")]
public partial class CustomerRefund : AuditableEntity
{
    [Column("branch_id")] public long BranchId { get; set; }
    [Column("refund_no"), MaxLength(30)] public string RefundNo { get; set; } = string.Empty;
    [Column("tenant_id")] public long TenantId { get; set; }
    [Column("refund_date")] public DateOnly RefundDate { get; set; }
    [Column("amount"), Precision(14, 2)] public decimal Amount { get; set; }
    [Column("payment_method_id")] public long? PaymentMethodId { get; set; }
    [Column("reference_no"), MaxLength(100)] public string? ReferenceNo { get; set; }
    [Column("reason"), MaxLength(500)] public string Reason { get; set; } = string.Empty;
    /// <summary>posted | cancelled</summary>
    [Column("status"), MaxLength(20)] public string Status { get; set; } = "posted";
    [Column("cancel_reason"), MaxLength(500)] public string? CancelReason { get; set; }
    [Column("journal_entry_id")] public long? JournalEntryId { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore] public Branch? Branch { get; set; }
    [ForeignKey(nameof(TenantId)), JsonIgnore] public Tenant? Tenant { get; set; }
    [ForeignKey(nameof(PaymentMethodId)), JsonIgnore] public LookupValue? PaymentMethod { get; set; }
    [ForeignKey(nameof(JournalEntryId)), JsonIgnore] public JournalEntry? JournalEntry { get; set; }
}

/// <summary>Table <c>customer_refund_source</c>: which receipt or credit note credit a refund used.</summary>
[Table("customer_refund_source")]
public partial class CustomerRefundSource : AuditableEntity
{
    [Column("refund_id")] public long RefundId { get; set; }
    [Column("payment_id")] public long? PaymentId { get; set; }
    [Column("credit_note_id")] public long? CreditNoteId { get; set; }
    [Column("amount"), Precision(14, 2)] public decimal Amount { get; set; }

    [ForeignKey(nameof(RefundId)), JsonIgnore] public CustomerRefund? Refund { get; set; }
    [ForeignKey(nameof(PaymentId)), JsonIgnore] public Payment? Payment { get; set; }
    [ForeignKey(nameof(CreditNoteId)), JsonIgnore] public CreditNote? CreditNote { get; set; }
}
