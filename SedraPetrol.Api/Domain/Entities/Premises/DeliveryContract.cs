using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Gas delivery contract of a villa / restaurant: the customer pays per refill (litres delivered × <see cref="PricePerLitre"/>).
/// Header fields (contract no, building, dates, status …) are in <c>contract</c> with contract_type = "delivery" and
/// <b>unit_id = NULL</b> (so the "one active contract per unit" index of supply contracts is never involved);
/// the delivery-only fields are in table <c>contract_delivery_spec</c> (<c>contract_id</c> = <c>contract.id</c>).
/// </summary>
[Table("contract_delivery_spec")]
public partial class DeliveryContract : Contract
{
    /// <summary><c>contract.contract_type</c> of delivery contracts.</summary>
    public const string TypeCode = "delivery";

    public DeliveryContract() { ContractType = TypeCode; }

    [Column("premises_id")]
    public long PremisesId { get; set; }

    /// <summary>The customer invoiced for refills (villa owner / restaurant owner), a party with the customer role.</summary>
    [Column("customer_party_id")]
    public long CustomerPartyId { get; set; }

    /// <summary>Agreed price per litre delivered, before VAT.</summary>
    [Column("price_per_litre")]
    [Precision(14, 4)]
    public decimal PricePerLitre { get; set; }

    /// <summary>Tax code of the refill invoices (null → the default sales VAT on the invoice date).</summary>
    [Column("tax_rate_id")]
    public long? TaxRateId { get; set; }

    /// <summary>Days from the invoice date to the due date (null → the billing default).</summary>
    [Column("payment_terms_days")]
    public short? PaymentTermsDays { get; set; }

    /// <summary>Restaurant: the authorised signatory who signed (premises_signatory). Villa: null — the owner signs.</summary>
    [Column("signatory_id")]
    public long? SignatoryId { get; set; }

    /// <summary>Name of the person who signed, as printed on the contract (owner or signatory).</summary>
    [Column("signed_by_name")]
    [MaxLength(200)]
    public string? SignedByName { get; set; }

    [Column("signed_date")]
    public DateOnly? SignedDate { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(PremisesId)), JsonIgnore]
    public Premises? Premises { get; set; }

    [ForeignKey(nameof(CustomerPartyId)), JsonIgnore]
    public Party? CustomerParty { get; set; }

    [ForeignKey(nameof(TaxRateId)), JsonIgnore]
    public TaxRate? TaxRate { get; set; }

    [ForeignKey(nameof(SignatoryId)), JsonIgnore]
    public PremisesSignatory? Signatory { get; set; }
}
