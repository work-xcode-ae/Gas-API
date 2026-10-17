using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor_payment_allocation</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor_payment_allocation")]
public partial class VendorPaymentAllocation : AuditableEntity
{
    [Column("vendor_payment_id")]
    public long VendorPaymentId { get; set; }

    [Column("vendor_bill_id")]
    public long VendorBillId { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorPaymentId)), JsonIgnore]
    public VendorPayment? VendorPayment { get; set; }

    [ForeignKey(nameof(VendorBillId)), JsonIgnore]
    public VendorBill? VendorBill { get; set; }
}
