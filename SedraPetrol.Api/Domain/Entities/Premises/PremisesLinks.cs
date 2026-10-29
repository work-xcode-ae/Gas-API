using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>premises_signatory</c> — the restaurant's own staff (customer side) and whether each may sign contracts.
/// A villa has none: the villa owner signs.
/// </summary>
[Table("premises_signatory")]
public partial class PremisesSignatory : AuditableEntity
{
    [Column("premises_id")]
    public long PremisesId { get; set; }

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("emirates_id")]
    [MaxLength(30)]
    public string? EmiratesId { get; set; }

    [Column("position")]
    [MaxLength(100)]
    public string? Position { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    /// <summary>Authorised to sign contracts for the restaurant.</summary>
    [Column("can_sign_contracts")]
    public bool CanSignContracts { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(PremisesId)), JsonIgnore]
    public Premises? Premises { get; set; }
}

/// <summary>
/// Table <c>premises_tank</c> — the gas tanks of a villa / restaurant.
/// A restaurant tank has <c>asset.building_id</c> = NULL on purpose, so it never enters the host building's tank totals
/// (tank insights, building tank list); a villa tank belongs to the villa's own host building.
/// </summary>
[Table("premises_tank")]
public partial class PremisesTank : AuditableEntity
{
    [Column("premises_id")]
    public long PremisesId { get; set; }

    [Column("tank_id")]
    public long TankId { get; set; }

    [ForeignKey(nameof(PremisesId)), JsonIgnore]
    public Premises? Premises { get; set; }

    [ForeignKey(nameof(TankId)), JsonIgnore]
    public Tank? Tank { get; set; }
}

/// <summary>
/// Table <c>premises_refill_invoice</c> — the customer invoice issued for one delivered refill of a villa / restaurant tank
/// (litres delivered × contract price per litre). Links the refill to the invoice without changing <c>refill_request</c>.
/// One refill is invoiced at most once (unique refill_request_id among rows that are not deleted).
/// </summary>
[Table("premises_refill_invoice")]
public partial class PremisesRefillInvoice : AuditableEntity
{
    [Column("premises_id")]
    public long PremisesId { get; set; }

    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("invoice_id")]
    public long InvoiceId { get; set; }

    [Column("litres")]
    [Precision(14, 3)]
    public decimal Litres { get; set; }

    [Column("price_per_litre")]
    [Precision(14, 4)]
    public decimal PricePerLitre { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [ForeignKey(nameof(PremisesId)), JsonIgnore]
    public Premises? Premises { get; set; }

    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public DeliveryContract? Contract { get; set; }

    [ForeignKey(nameof(InvoiceId)), JsonIgnore]
    public Invoice? Invoice { get; set; }
}
