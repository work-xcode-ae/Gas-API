using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>visit_finding</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("visit_finding")]
public partial class VisitFinding : AuditableEntity
{
    [Column("visit_id")]
    public long VisitId { get; set; }

    [Column("severity_id")]
    public long? SeverityId { get; set; }

    [Column("description")]
    [Required]
    public string Description { get; set; } = string.Empty;

    [Column("action_taken")]
    public string? ActionTaken { get; set; }

    [Column("service_request_id")]
    public long? ServiceRequestId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VisitId)), JsonIgnore]
    public MaintenanceVisit? Visit { get; set; }

    [ForeignKey(nameof(SeverityId)), JsonIgnore]
    public LookupValue? Severity { get; set; }

    [ForeignKey(nameof(ServiceRequestId)), JsonIgnore]
    public ServiceRequest? ServiceRequest { get; set; }
}
