using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>payment_allocation</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("payment_allocation")]
public partial class PaymentAllocation : AuditableEntity
{
    [Column("payment_id")]
    public long PaymentId { get; set; }

    [Column("invoice_id")]
    public long InvoiceId { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal Amount { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(PaymentId)), JsonIgnore]
    public Payment? Payment { get; set; }

    [ForeignKey(nameof(InvoiceId)), JsonIgnore]
    public Invoice? Invoice { get; set; }
}
