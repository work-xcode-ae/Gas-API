using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>meter_service</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("meter_service")]
public partial class MeterService : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("service_no")]
    [MaxLength(30)]
    public string? ServiceNo { get; set; }

    [Column("meter_id")]
    public long MeterId { get; set; }

    [Column("contract_id")]
    public long? ContractId { get; set; }

    [Column("service_type_id")]
    public long ServiceTypeId { get; set; }

    [Column("issue_type_id")]
    public long? IssueTypeId { get; set; }

    [Column("priority_id")]
    public long? PriorityId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("requested_date")]
    public DateOnly? RequestedDate { get; set; }

    [Column("completed_date")]
    public DateOnly? CompletedDate { get; set; }

    [Column("before_reading")]
    [Precision(14, 3)]
    public decimal? BeforeReading { get; set; }

    [Column("after_reading")]
    [Precision(14, 3)]
    public decimal? AfterReading { get; set; }

    [Column("new_meter_id")]
    public long? NewMeterId { get; set; }

    [Column("safety_check_passed")]
    public bool? SafetyCheckPassed { get; set; }

    [Column("tenant_notified")]
    public bool? TenantNotified { get; set; }

    [Column("technician_id")]
    public long? TechnicianId { get; set; }

    [Column("fee_invoice_item_id")]
    public long? FeeInvoiceItemId { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public SupplyContract? Contract { get; set; }

    [ForeignKey(nameof(ServiceTypeId)), JsonIgnore]
    public LookupValue? ServiceType { get; set; }

    [ForeignKey(nameof(IssueTypeId)), JsonIgnore]
    public LookupValue? IssueType { get; set; }

    [ForeignKey(nameof(PriorityId)), JsonIgnore]
    public LookupValue? Priority { get; set; }

    [ForeignKey(nameof(NewMeterId)), JsonIgnore]
    public Meter? NewMeter { get; set; }

    [ForeignKey(nameof(TechnicianId)), JsonIgnore]
    public AppUser? Technician { get; set; }

    [ForeignKey(nameof(FeeInvoiceItemId)), JsonIgnore]
    public InvoiceItem? FeeInvoiceItem { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
