using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_supplier_payment</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_supplier_payment")]
public partial class RefillSupplierPayment : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("vendor_payment_id")]
    public long VendorPaymentId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(VendorPaymentId)), JsonIgnore]
    public VendorPayment? VendorPayment { get; set; }
}
