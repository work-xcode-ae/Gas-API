using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor_bill</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor_bill")]
public partial class VendorBill : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("vendor_id")]
    public long VendorId { get; set; }

    [Column("bill_no")]
    [MaxLength(50)]
    [Required]
    public string BillNo { get; set; } = string.Empty;

    [Column("bill_date")]
    public DateOnly BillDate { get; set; }

    [Column("due_date")]
    public DateOnly? DueDate { get; set; }

    [Column("source_type")]
    [MaxLength(30)]
    public string? SourceType { get; set; }

    [Column("source_id")]
    public long? SourceId { get; set; }

    [Column("subtotal")]
    [Precision(14, 2)]
    public decimal? Subtotal { get; set; }

    [Column("vat_amount")]
    [Precision(14, 2)]
    public decimal? VatAmount { get; set; }

    [Column("total_amount")]
    [Precision(14, 2)]
    public decimal? TotalAmount { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("exchange_rate")]
    [Precision(18, 6)]
    public decimal? ExchangeRate { get; set; } = 1m;

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

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
