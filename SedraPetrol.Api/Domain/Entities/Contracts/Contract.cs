using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>contract</c> — ONE header row for every contract, whatever its type.
/// The fields every contract has live here; the fields of one type live in that type's spec table
/// (table-per-type inheritance, same id):
/// <list type="bullet">
/// <item><c>contract_supply_spec</c> → <see cref="SupplyContract"/> (tenant gas supply contract of a unit)</item>
/// <item><c>contract_amc_spec</c> → <see cref="AmcContract"/> (maintenance contract of a building)</item>
/// </list>
/// A new contract type = a new spec table + a class deriving from <see cref="Contract"/>.
/// Child tables that belong to any contract (contract_term, contract_fee, invoice, journal_line) point to <c>contract.id</c>.
/// </summary>
[Table("contract")]
public abstract class Contract : AuditableEntity
{
    /// <summary>supply | amc — same codes as <c>contract_term.contract_type</c>. Set by the derived class.</summary>
    [Column("contract_type")]
    [MaxLength(20)]
    [Required]
    public string ContractType { get; protected set; } = string.Empty;

    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("contract_no")]
    [MaxLength(30)]
    [Required]
    public string ContractNo { get; set; } = string.Empty;

    /// <summary>Building the contract is for (for a supply contract: the unit's building).</summary>
    [Column("building_id")]
    public long? BuildingId { get; set; }

    /// <summary>Unit the contract is for — set for unit-level contracts (supply), NULL for building-level ones (AMC).</summary>
    [Column("unit_id")]
    public long? UnitId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; }

    [Column("renewal_period_months")]
    public short? RenewalPeriodMonths { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    /// <summary>Lookup value of CONTRACT_STATUS (supply) or AMC_STATUS (amc).</summary>
    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("terminated_at")]
    public DateOnly? TerminatedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}

/// <summary>Contract type codes (<c>contract.contract_type</c>, <c>contract_term.contract_type</c>).</summary>
public static class ContractTypes
{
    public const string Supply = "supply", Amc = "amc";
}
