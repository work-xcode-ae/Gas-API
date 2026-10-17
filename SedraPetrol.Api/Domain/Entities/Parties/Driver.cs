using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>driver</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("driver")]
public partial class Driver : AuditableEntity
{
    [Column("vendor_id")]
    public long? VendorId { get; set; }

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("license_no")]
    [MaxLength(50)]
    public string? LicenseNo { get; set; }

    [Column("license_expiry")]
    public DateOnly? LicenseExpiry { get; set; }

    [Column("hazmat_permit_no")]
    [MaxLength(50)]
    public string? HazmatPermitNo { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }
}
