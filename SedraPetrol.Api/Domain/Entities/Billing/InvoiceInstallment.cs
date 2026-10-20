using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>invoice_installment</c>: payment schedule of one customer invoice (BRD §7.2).
/// The sum of Amount must equal the invoice total. Paid amounts come from payment allocations applied oldest instalment first.
/// Status: pending | partial | paid | overdue | cancelled | on_hold (stored code; overdue/partial/paid are recalculated).
/// </summary>
[Table("invoice_installment")]
[Index(nameof(InvoiceId), nameof(SeqNo), IsUnique = true, Name = "ux_invoice_installment_seq")]
[Index(nameof(DueDate), Name = "ix_invoice_installment_due")]
public partial class InvoiceInstallment : AuditableEntity
{
    [Column("invoice_id")]
    public long InvoiceId { get; set; }

    [Column("seq_no")]
    public short SeqNo { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("status"), MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(InvoiceId)), JsonIgnore]
    public Invoice? Invoice { get; set; }
}
