using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>journal_entry</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("journal_entry")]
public partial class JournalEntry : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("journal_no")]
    [MaxLength(30)]
    [Required]
    public string JournalNo { get; set; } = string.Empty;

    [Column("entry_date")]
    public DateOnly EntryDate { get; set; }

    [Column("fiscal_period_id")]
    public long FiscalPeriodId { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("exchange_rate")]
    [Precision(18, 6)]
    public decimal? ExchangeRate { get; set; } = 1m;

    [Column("description")]
    public string? Description { get; set; }

    [Column("source_type")]
    [MaxLength(50)]
    public string? SourceType { get; set; }

    [Column("source_id")]
    public long? SourceId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("posted_at")]
    public DateTimeOffset? PostedAt { get; set; }

    [Column("posted_by")]
    public long? PostedBy { get; set; }

    [Column("reversed_entry_id")]
    public long? ReversedEntryId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(FiscalPeriodId)), JsonIgnore]
    public FiscalPeriod? FiscalPeriod { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(PostedBy)), JsonIgnore]
    public AppUser? PostedByUser { get; set; }

    [ForeignKey(nameof(ReversedEntryId)), JsonIgnore]
    public JournalEntry? ReversedEntry { get; set; }
}
