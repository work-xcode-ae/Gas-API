using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor_contact</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor_contact")]
public partial class VendorContact : AuditableEntity
{
    [Column("vendor_id")]
    public long VendorId { get; set; }

    [Column("name_en")]
    [MaxLength(150)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("position")]
    [MaxLength(100)]
    public string? Position { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }
}
