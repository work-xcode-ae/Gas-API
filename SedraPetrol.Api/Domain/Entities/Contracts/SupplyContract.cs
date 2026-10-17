using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>supply_contract</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("supply_contract")]
public partial class SupplyContract : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("contract_no")]
    [MaxLength(30)]
    [Required]
    public string ContractNo { get; set; } = string.Empty;

    [Column("tenant_id")]
    public long TenantId { get; set; }

    [Column("unit_id")]
    public long UnitId { get; set; }

    [Column("meter_id")]
    public long? MeterId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; }

    [Column("renewal_period_months")]
    public short? RenewalPeriodMonths { get; set; }

    [Column("deposit_amount")]
    [Precision(14, 2)]
    public decimal? DepositAmount { get; set; }

    [Column("deposit_paid")]
    public bool DepositPaid { get; set; }

    [Column("deposit_date")]
    public DateOnly? DepositDate { get; set; }

    [Column("terms")]
    public string? Terms { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("terminated_at")]
    public DateOnly? TerminatedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(TenantId)), JsonIgnore]
    public Tenant? Tenant { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
