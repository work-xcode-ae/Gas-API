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
