using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor_payment</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor_payment")]
public partial class VendorPayment : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("payment_no")]
    [MaxLength(30)]
    [Required]
    public string PaymentNo { get; set; } = string.Empty;

    [Column("vendor_id")]
    public long VendorId { get; set; }

    [Column("payment_date")]
    public DateOnly PaymentDate { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("payment_method_id")]
    public long? PaymentMethodId { get; set; }

    [Column("reference_no")]
    [MaxLength(100)]
    public string? ReferenceNo { get; set; }

    [Column("paid_by")]
    public long? PaidBy { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("journal_entry_id")]
    public long? JournalEntryId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(PaymentMethodId)), JsonIgnore]
    public LookupValue? PaymentMethod { get; set; }

    [ForeignKey(nameof(PaidBy)), JsonIgnore]
    public AppUser? PaidByUser { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
