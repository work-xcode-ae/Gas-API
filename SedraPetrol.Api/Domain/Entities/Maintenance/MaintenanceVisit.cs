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

    /// <summary>Our employee who carried out the visit (when the visit is done by our own team).</summary>
    [Column("technician_employee_id")]
    public long? TechnicianEmployeeId { get; set; }

    /// <summary>Customer satisfaction 1–5 given at the end of the visit.</summary>
    [Column("customer_rating")]
    public byte? CustomerRating { get; set; }

    /// <summary>The customer's comments / feedback on the visit.</summary>
    [Column("customer_feedback"), MaxLength(1000)]
    public string? CustomerFeedback { get; set; }

    /// <summary>Who gave the feedback / signed on site (owner, building manager, tenant …).</summary>
    [Column("feedback_by"), MaxLength(150)]
    public string? FeedbackBy { get; set; }

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

    [ForeignKey(nameof(TechnicianEmployeeId)), JsonIgnore]
    public Employee? TechnicianEmployee { get; set; }
}
