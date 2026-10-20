using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>credit_note</c> (إشعار دائن, BRD §7.3): reduces a customer invoice (full or partial). Posted Dr revenue / Dr output VAT, Cr A/R.
/// Its total is applied to invoices through <see cref="CreditNoteAllocation"/> (normally the source invoice); any part not applied stays
/// as customer credit that can be applied to other invoices or refunded (<see cref="CustomerRefund"/>).
/// </summary>
[Table("credit_note")]
[Index(nameof(CreditNoteNo), IsUnique = true, Name = "ux_credit_note_no")]
[Index(nameof(TenantId), Name = "ix_credit_note_tenant")]
[Index(nameof(InvoiceId), Name = "ix_credit_note_invoice")]
public partial class CreditNote : AuditableEntity
{
    [Column("branch_id")] public long BranchId { get; set; }
    [Column("credit_note_no"), MaxLength(30)] public string CreditNoteNo { get; set; } = string.Empty;
    [Column("tenant_id")] public long TenantId { get; set; }
    /// <summary>The invoice being credited.</summary>
    [Column("invoice_id")] public long InvoiceId { get; set; }
    [Column("building_id")] public long? BuildingId { get; set; }
    [Column("contract_id")] public long? ContractId { get; set; }
    [Column("issue_date")] public DateOnly IssueDate { get; set; }
    [Column("reason"), MaxLength(500)] public string Reason { get; set; } = string.Empty;
    [Column("subtotal"), Precision(14, 2)] public decimal Subtotal { get; set; }
    [Column("vat_amount"), Precision(14, 2)] public decimal VatAmount { get; set; }
    [Column("total_amount"), Precision(14, 2)] public decimal TotalAmount { get; set; }
    /// <summary>posted | cancelled</summary>
    [Column("status"), MaxLength(20)] public string Status { get; set; } = "posted";
    [Column("cancel_reason"), MaxLength(500)] public string? CancelReason { get; set; }
    [Column("journal_entry_id")] public long? JournalEntryId { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore] public Branch? Branch { get; set; }
    [ForeignKey(nameof(TenantId)), JsonIgnore] public Tenant? Tenant { get; set; }
    [ForeignKey(nameof(InvoiceId)), JsonIgnore] public Invoice? Invoice { get; set; }
    [ForeignKey(nameof(JournalEntryId)), JsonIgnore] public JournalEntry? JournalEntry { get; set; }
}

/// <summary>Table <c>credit_note_line</c>: credited net amount per invoice line (VAT at the line's rate).</summary>
[Table("credit_note_line")]
public partial class CreditNoteLine : AuditableEntity
{
    [Column("credit_note_id")] public long CreditNoteId { get; set; }
    [Column("invoice_item_id")] public long? InvoiceItemId { get; set; }
    [Column("item_type_id")] public long? ItemTypeId { get; set; }
    [Column("description"), MaxLength(500)] public string? Description { get; set; }
    [Column("amount"), Precision(14, 2)] public decimal Amount { get; set; }
    [Column("vat_rate"), Precision(5, 2)] public decimal VatRate { get; set; }
    [Column("vat_amount"), Precision(14, 2)] public decimal VatAmount { get; set; }

    [ForeignKey(nameof(CreditNoteId)), JsonIgnore] public CreditNote? CreditNote { get; set; }
    [ForeignKey(nameof(InvoiceItemId)), JsonIgnore] public InvoiceItem? InvoiceItem { get; set; }
}

/// <summary>Table <c>credit_note_allocation</c>: part of a credit note applied to an invoice (settles it like a payment).</summary>
[Table("credit_note_allocation")]
[Index(nameof(InvoiceId), Name = "ix_credit_note_allocation_invoice")]
public partial class CreditNoteAllocation : AuditableEntity
{
    [Column("credit_note_id")] public long CreditNoteId { get; set; }
    [Column("invoice_id")] public long InvoiceId { get; set; }
    [Column("amount"), Precision(14, 2)] public decimal Amount { get; set; }

    [ForeignKey(nameof(CreditNoteId)), JsonIgnore] public CreditNote? CreditNote { get; set; }
    [ForeignKey(nameof(InvoiceId)), JsonIgnore] public Invoice? Invoice { get; set; }
}
