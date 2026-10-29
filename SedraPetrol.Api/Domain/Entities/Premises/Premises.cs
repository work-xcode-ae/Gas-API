using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>premises</c> — ONE row for every villa or restaurant (a customer site that buys gas per refill).
/// Added by the Restaurants &amp; Villas module; no existing table was changed for it.
/// <list type="bullet">
/// <item><c>premises_villa_spec</c> → <see cref="Villa"/>: a standalone villa. Its host <c>building</c> row (type villa, code VL-…) is
/// created with it, so Civil Defense, AMC, service requests and invoices work through the existing building screens.</item>
/// <item><c>premises_restaurant_spec</c> → <see cref="Restaurant"/>: a restaurant inside an existing building (optionally a unit of it).</item>
/// </list>
/// Gas is sold per refill: litres delivered × the price per litre of the site's delivery contract (<see cref="DeliveryContract"/>).
/// The site's own tanks are linked through <c>premises_tank</c>.
/// </summary>
[Table("premises")]
public abstract class Premises : AuditableEntity
{
    /// <summary>villa | restaurant — set by the derived class.</summary>
    [Column("premises_type")]
    [MaxLength(20)]
    [Required]
    public string PremisesType { get; protected set; } = string.Empty;

    /// <summary>VL-001 (villa) / RS-001 (restaurant).</summary>
    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("branch_id")]
    public long BranchId { get; set; }

    /// <summary>The building the site is in (a villa: its own host building row).</summary>
    [Column("building_id")]
    public long BuildingId { get; set; }

    /// <summary>Optional unit of the building (a restaurant in a shop unit).</summary>
    [Column("unit_id")]
    public long? UnitId { get; set; }

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    /// <summary>The customer who is invoiced and signs: the villa owner / the restaurant owner (a party; it gets the customer role when invoiced).</summary>
    [Column("owner_party_id")]
    public long OwnerPartyId { get; set; }

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

    [ForeignKey(nameof(OwnerPartyId)), JsonIgnore]
    public Party? OwnerParty { get; set; }
}

/// <summary>Premises type codes (<c>premises.premises_type</c>).</summary>
public static class PremisesTypes
{
    public const string Villa = "villa", Restaurant = "restaurant";
}

/// <summary>A standalone villa. Villa-only fields in table <c>premises_villa_spec</c> (<c>premises_id</c> = <c>premises.id</c>).</summary>
[Table("premises_villa_spec")]
public partial class Villa : Premises
{
    public Villa() { PremisesType = PremisesTypes.Villa; }

    [Column("community")]
    [MaxLength(150)]
    public string? Community { get; set; }

    [Column("plot_no")]
    [MaxLength(50)]
    public string? PlotNo { get; set; }

    [Column("villa_no")]
    [MaxLength(50)]
    public string? VillaNo { get; set; }

    /// <summary>Dubai Makani number (or the emirate's equivalent location number).</summary>
    [Column("makani_no")]
    [MaxLength(30)]
    public string? MakaniNo { get; set; }

    [Column("bedrooms")]
    public short? Bedrooms { get; set; }
}

/// <summary>A restaurant inside a building. Restaurant-only fields in table <c>premises_restaurant_spec</c>.</summary>
[Table("premises_restaurant_spec")]
public partial class Restaurant : Premises
{
    public Restaurant() { PremisesType = PremisesTypes.Restaurant; }

    [Column("trade_name")]
    [MaxLength(200)]
    public string? TradeName { get; set; }

    [Column("trade_license_no")]
    [MaxLength(50)]
    public string? TradeLicenseNo { get; set; }

    [Column("trade_license_expiry")]
    public DateOnly? TradeLicenseExpiry { get; set; }

    [Column("seating_capacity")]
    public int? SeatingCapacity { get; set; }

    [Column("burners_count")]
    public int? BurnersCount { get; set; }

    /// <summary>Lookup KITCHEN_TYPE.</summary>
    [Column("kitchen_type_id")]
    public long? KitchenTypeId { get; set; }

    [Column("cuisine")]
    [MaxLength(100)]
    public string? Cuisine { get; set; }

    [Column("operating_hours")]
    [MaxLength(100)]
    public string? OperatingHours { get; set; }

    [ForeignKey(nameof(KitchenTypeId)), JsonIgnore]
    public LookupValue? KitchenType { get; set; }
}
