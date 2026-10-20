using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Vehicle / tanker — an asset; vehicle fields in table <c>asset_vehicle</c> (<c>asset_id</c> = <c>asset.id</c>). <c>vendor_id</c> set = hired from a supplier.</summary>
[Table("asset_vehicle")]
public partial class Vehicle : Asset
{
    public Vehicle() { AssetType = AssetTypes.Vehicle; }

    [Column("vendor_id")]
    public long? VendorId { get; set; }

    [Column("default_driver_id")]
    public long? DefaultDriverId { get; set; }

    [Column("plate_no")]
    [MaxLength(20)]
    [Required]
    public string PlateNo { get; set; } = string.Empty;

    [Column("capacity_litres")]
    [Precision(14, 3)]
    public decimal? CapacityLitres { get; set; }

    [Column("registration_expiry")]
    public DateOnly? RegistrationExpiry { get; set; }


    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(DefaultDriverId)), JsonIgnore]
    public Driver? DefaultDriver { get; set; }
}
