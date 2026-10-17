using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>cd_certificate</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("cd_certificate")]
public partial class CdCertificate : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("certificate_no")]
    [MaxLength(50)]
    public string? CertificateNo { get; set; }

    [Column("cert_type_id")]
    public long? CertTypeId { get; set; }

    [Column("license_no")]
    [MaxLength(50)]
    public string? LicenseNo { get; set; }

    [Column("issued_by")]
    [MaxLength(150)]
    public string? IssuedBy { get; set; }

    [Column("issue_date")]
    public DateOnly? IssueDate { get; set; }

    [Column("expiry_date")]
    public DateOnly? ExpiryDate { get; set; }

    [Column("scope")]
    public string? Scope { get; set; }

    [Column("inspector_name")]
    [MaxLength(150)]
    public string? InspectorName { get; set; }

    [Column("inspection_date")]
    public DateOnly? InspectionDate { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(CertTypeId)), JsonIgnore]
    public LookupValue? CertType { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
