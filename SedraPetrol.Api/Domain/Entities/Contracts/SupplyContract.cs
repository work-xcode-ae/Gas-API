using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Tenant gas supply contract of a unit. Header fields (contract no, unit, dates, status …) are in <c>contract</c>;
/// the supply-only fields are in table <c>contract_supply_spec</c> (<c>contract_id</c> = <c>contract.id</c>).
/// </summary>
[Table("contract_supply_spec")]
public partial class SupplyContract : Contract
{
    public SupplyContract() { ContractType = ContractTypes.Supply; }

    [Column("tenant_id")]
    public long TenantId { get; set; }

    /// <summary>Service type (lookup CONTRACT_SERVICE_TYPE: VIP, night shift, contract delivery, walk in).</summary>
    [Column("service_type_id")]
    public long? ServiceTypeId { get; set; }

    /// <summary>Residential / commercial (lookup CONTRACT_USAGE_TYPE).</summary>
    [Column("usage_type_id")]
    public long? UsageTypeId { get; set; }

    /// <summary>How the customer receives the bills.</summary>
    [Column("bill_by_email")]
    public bool BillByEmail { get; set; }

    [Column("bill_by_post")]
    public bool BillByPost { get; set; }

    /// <summary>One consumption invoice every N months (1 = monthly, 3 = quarterly …).</summary>
    [Column("billing_cycle_months")]
    public short? BillingCycleMonths { get; set; }

    [ForeignKey(nameof(ServiceTypeId)), JsonIgnore]
    public LookupValue? ServiceType { get; set; }

    [ForeignKey(nameof(UsageTypeId)), JsonIgnore]
    public LookupValue? UsageType { get; set; }

    [Column("deposit_amount")]
    [Precision(14, 2)]
    public decimal? DepositAmount { get; set; }

    [Column("deposit_paid")]
    public bool DepositPaid { get; set; }

    [Column("deposit_date")]
    public DateOnly? DepositDate { get; set; }

    /// <summary>Free-text terms / remarks of the tenancy (the structured terms are in <c>contract_term</c>).</summary>
    [Column("terms")]
    public string? Terms { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(TenantId)), JsonIgnore]
    public Tenant? Tenant { get; set; }
}
