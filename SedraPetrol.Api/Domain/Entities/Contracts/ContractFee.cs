using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>contract_fee</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("contract_fee")]
public partial class ContractFee : AuditableEntity
{
    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("fee_type_id")]
    public long FeeTypeId { get; set; }

    [Column("cycle_id")]
    public long CycleId { get; set; }

    [Column("amount")]
    [Precision(14, 4)]
    public decimal Amount { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("effective_from")]
    public DateOnly EffectiveFrom { get; set; }

    [Column("effective_to")]
    public DateOnly? EffectiveTo { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public SupplyContract? Contract { get; set; }

    [ForeignKey(nameof(FeeTypeId)), JsonIgnore]
    public LookupValue? FeeType { get; set; }

    [ForeignKey(nameof(CycleId)), JsonIgnore]
    public LookupValue? Cycle { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }
}
