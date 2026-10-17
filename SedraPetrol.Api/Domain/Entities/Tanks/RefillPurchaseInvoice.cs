using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_purchase_invoice</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_purchase_invoice")]
public partial class RefillPurchaseInvoice : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("vendor_bill_id")]
    public long VendorBillId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(VendorBillId)), JsonIgnore]
    public VendorBill? VendorBill { get; set; }
}
