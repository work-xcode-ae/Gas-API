using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor")]
public partial class Vendor : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("vendor_type_id")]
    public long VendorTypeId { get; set; }

    [Column("code")]
    [MaxLength(30)]
    public string? Code { get; set; }

    [Column("name_en")]
    [MaxLength(200)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(200)]
    public string? NameAr { get; set; }

    [Column("trade_license_no")]
    [MaxLength(50)]
    public string? TradeLicenseNo { get; set; }

    [Column("trn")]
    [MaxLength(20)]
    public string? Trn { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("payable_account_id")]
    public long? PayableAccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(VendorTypeId)), JsonIgnore]
    public LookupValue? VendorType { get; set; }

    [ForeignKey(nameof(PayableAccountId)), JsonIgnore]
    public Account? PayableAccount { get; set; }
}
