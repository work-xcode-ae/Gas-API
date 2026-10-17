using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>service_request</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("service_request")]
public partial class ServiceRequest : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("request_no")]
    [MaxLength(30)]
    [Required]
    public string RequestNo { get; set; } = string.Empty;

    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("unit_id")]
    public long? UnitId { get; set; }

    [Column("tenant_id")]
    public long? TenantId { get; set; }

    [Column("meter_id")]
    public long? MeterId { get; set; }

    [Column("description")]
    [Required]
    public string Description { get; set; } = string.Empty;

    [Column("priority_id")]
    public long? PriorityId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("assigned_to")]
    public long? AssignedTo { get; set; }

    [Column("assigned_vendor_id")]
    public long? AssignedVendorId { get; set; }

    [Column("requested_at")]
    public DateTimeOffset? RequestedAt { get; set; }

    [Column("closed_at")]
    public DateTimeOffset? ClosedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(TenantId)), JsonIgnore]
    public Tenant? Tenant { get; set; }

    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(PriorityId)), JsonIgnore]
    public LookupValue? Priority { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(AssignedTo)), JsonIgnore]
    public AppUser? AssignedToUser { get; set; }

    [ForeignKey(nameof(AssignedVendorId)), JsonIgnore]
    public Vendor? AssignedVendor { get; set; }
}
