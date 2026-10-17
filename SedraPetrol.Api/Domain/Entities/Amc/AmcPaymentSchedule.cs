using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>amc_payment_schedule</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("amc_payment_schedule")]
public partial class AmcPaymentSchedule : AuditableEntity
{
    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("label")]
    [MaxLength(100)]
    public string? Label { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("vendor_bill_id")]
    public long? VendorBillId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public AmcContract? Contract { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(VendorBillId)), JsonIgnore]
    public VendorBill? VendorBill { get; set; }
}
