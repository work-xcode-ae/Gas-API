using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>cd_renewal</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("cd_renewal")]
public partial class CdRenewal : AuditableEntity
{
    [Column("certificate_id")]
    public long CertificateId { get; set; }

    [Column("started_at")]
    public DateOnly? StartedAt { get; set; }

    [Column("current_step")]
    public short? CurrentStep { get; set; }

    [Column("inspector_name")]
    [MaxLength(150)]
    public string? InspectorName { get; set; }

    [Column("inspector_phone")]
    [MaxLength(30)]
    public string? InspectorPhone { get; set; }

    [Column("inspector_dept")]
    [MaxLength(150)]
    public string? InspectorDept { get; set; }

    [Column("renewal_date")]
    public DateOnly? RenewalDate { get; set; }

    [Column("new_expiry_date")]
    public DateOnly? NewExpiryDate { get; set; }

    [Column("new_certificate_id")]
    public long? NewCertificateId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CertificateId)), JsonIgnore]
    public CdCertificate? Certificate { get; set; }

    [ForeignKey(nameof(NewCertificateId)), JsonIgnore]
    public CdCertificate? NewCertificate { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
