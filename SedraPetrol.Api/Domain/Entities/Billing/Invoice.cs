using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>invoice</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("invoice")]
public partial class Invoice : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("invoice_no")]
    [MaxLength(30)]
    [Required]
    public string InvoiceNo { get; set; } = string.Empty;

    [Column("tenant_id")]
    public long TenantId { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("contract_id")]
    public long? ContractId { get; set; }

    [Column("invoice_type_id")]
    public long? InvoiceTypeId { get; set; }

    [Column("billing_period_start")]
    public DateOnly? BillingPeriodStart { get; set; }

    [Column("billing_period_end")]
    public DateOnly? BillingPeriodEnd { get; set; }

    [Column("issue_date")]
    public DateOnly IssueDate { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

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

    [ForeignKey(nameof(TenantId)), JsonIgnore]
    public Tenant? Tenant { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public Contract? Contract { get; set; }

    [ForeignKey(nameof(InvoiceTypeId)), JsonIgnore]
    public LookupValue? InvoiceType { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }
}
