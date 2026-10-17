using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vehicle</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vehicle")]
public partial class Vehicle : AuditableEntity
{
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

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(DefaultDriverId)), JsonIgnore]
    public Driver? DefaultDriver { get; set; }
}
