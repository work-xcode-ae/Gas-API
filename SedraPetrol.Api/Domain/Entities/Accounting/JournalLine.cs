using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>journal_line</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("journal_line")]
public partial class JournalLine : AuditableEntity
{
    [Column("journal_entry_id")]
    public long JournalEntryId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("description")]
    [MaxLength(300)]
    public string? Description { get; set; }

    [Column("debit")]
    [Precision(14, 2)]
    public decimal? Debit { get; set; }

    [Column("credit")]
    [Precision(14, 2)]
    public decimal? Credit { get; set; }

    [Column("base_debit")]
    [Precision(14, 2)]
    public decimal? BaseDebit { get; set; }

    [Column("base_credit")]
    [Precision(14, 2)]
    public decimal? BaseCredit { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("unit_id")]
    public long? UnitId { get; set; }

    [Column("tenant_id")]
    public long? TenantId { get; set; }

    [Column("vendor_id")]
    public long? VendorId { get; set; }

    [Column("contract_id")]
    public long? ContractId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(JournalEntryId)), JsonIgnore]
    public JournalEntry? JournalEntry { get; set; }

    [ForeignKey(nameof(AccountId)), JsonIgnore]
    public Account? Account { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(TenantId)), JsonIgnore]
    public Tenant? Tenant { get; set; }

    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public Contract? Contract { get; set; }
}
