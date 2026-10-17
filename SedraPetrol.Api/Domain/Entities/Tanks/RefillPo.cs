using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>refill_po</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("refill_po")]
public partial class RefillPo : AuditableEntity
{
    [Column("refill_request_id")]
    public long RefillRequestId { get; set; }

    [Column("vendor_id")]
    public long VendorId { get; set; }

    [Column("po_number")]
    [MaxLength(30)]
    [Required]
    public string PoNumber { get; set; } = string.Empty;

    [Column("po_date")]
    public DateOnly PoDate { get; set; }

    [Column("qty_litres")]
    [Precision(14, 3)]
    public decimal? QtyLitres { get; set; }

    [Column("unit_price")]
    [Precision(14, 4)]
    public decimal? UnitPrice { get; set; }

    [Column("total_amount")]
    [Precision(14, 2)]
    public decimal? TotalAmount { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RefillRequestId)), JsonIgnore]
    public RefillRequest? RefillRequest { get; set; }

    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }
}
