using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>cashflow_item</c>: manually entered expected receipt / payment (BRD §16) that has no source document yet —
/// e.g. rent, salaries until payroll exists, building owner payments until owner payables exist, expected other income.
/// One-off (Recurrence = none) or recurring (monthly / quarterly / yearly) from StartDate until EndDate.
/// It is a forecast only and never touches the ledger.
/// </summary>
[Table("cashflow_item")]
[Index(nameof(StartDate), Name = "ix_cashflow_item_start")]
public partial class CashflowItem : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    /// <summary>in | out</summary>
    [Column("direction"), MaxLength(3)]
    public string Direction { get; set; } = "out";

    /// <summary>Free category, e.g. rent, salaries, owner_payment, utilities, other_income.</summary>
    [Column("category"), MaxLength(50)]
    public string Category { get; set; } = "other";

    [Column("counterparty"), MaxLength(200)]
    public string? Counterparty { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    /// <summary>none | monthly | quarterly | yearly</summary>
    [Column("recurrence"), MaxLength(10)]
    public string Recurrence { get; set; } = "none";

    /// <summary>confirmed | estimate</summary>
    [Column("confidence"), MaxLength(10)]
    public string Confidence { get; set; } = "estimate";

    [Column("description"), MaxLength(500)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }
}
