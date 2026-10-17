using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>maintenance_visit</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("maintenance_visit")]
public partial class MaintenanceVisit : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("visit_no")]
    [MaxLength(30)]
    public string? VisitNo { get; set; }

    [Column("amc_contract_id")]
    public long AmcContractId { get; set; }

    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("visit_type_id")]
    public long? VisitTypeId { get; set; }

    [Column("period_label")]
    [MaxLength(20)]
    public string? PeriodLabel { get; set; }

    [Column("scheduled_date")]
    public DateOnly ScheduledDate { get; set; }

    [Column("time_slot")]
    [MaxLength(30)]
    public string? TimeSlot { get; set; }

    [Column("visit_date")]
    public DateOnly? VisitDate { get; set; }

    [Column("vendor_contact_id")]
    public long? VendorContactId { get; set; }

    [Column("technician_phone")]
    [MaxLength(30)]
    public string? TechnicianPhone { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("report_summary")]
    public string? ReportSummary { get; set; }

    [Column("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(AmcContractId)), JsonIgnore]
    public AmcContract? AmcContract { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(VisitTypeId)), JsonIgnore]
    public LookupValue? VisitType { get; set; }

    [ForeignKey(nameof(VendorContactId)), JsonIgnore]
    public VendorContact? VendorContact { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
