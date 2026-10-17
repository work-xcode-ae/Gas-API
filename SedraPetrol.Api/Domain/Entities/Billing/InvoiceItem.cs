using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>invoice_item</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("invoice_item")]
public partial class InvoiceItem : AuditableEntity
{
    [Column("invoice_id")]
    public long InvoiceId { get; set; }

    [Column("item_type_id")]
    public long? ItemTypeId { get; set; }

    [Column("description")]
    [MaxLength(300)]
    public string? Description { get; set; }

    [Column("quantity")]
    [Precision(14, 3)]
    public decimal? Quantity { get; set; }

    [Column("uom")]
    [MaxLength(10)]
    public string? Uom { get; set; }

    [Column("unit_price")]
    [Precision(14, 4)]
    public decimal? UnitPrice { get; set; }

    [Column("vat_rate")]
    [Precision(5, 2)]
    public decimal? VatRate { get; set; } = 5m;

    [Column("amount")]
    [Precision(14, 2)]
    public decimal? Amount { get; set; }

    [Column("meter_reading_id")]
    public long? MeterReadingId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(InvoiceId)), JsonIgnore]
    public Invoice? Invoice { get; set; }

    [ForeignKey(nameof(ItemTypeId)), JsonIgnore]
    public LookupValue? ItemType { get; set; }

    [ForeignKey(nameof(MeterReadingId)), JsonIgnore]
    public MeterReading? MeterReading { get; set; }
}
