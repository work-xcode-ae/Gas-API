using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>vendor_bill_item</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("vendor_bill_item")]
public partial class VendorBillItem : AuditableEntity
{
    [Column("bill_id")]
    public long BillId { get; set; }

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

    /// <summary>Tax code version applied to this line (tax_rate); the rate itself is frozen in VatRate.</summary>
    [Column("tax_rate_id")]
    public long? TaxRateId { get; set; }

    [Column("amount")]
    [Precision(14, 2)]
    public decimal? Amount { get; set; }

    [Column("expense_account_id")]
    public long? ExpenseAccountId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BillId)), JsonIgnore]
    public VendorBill? Bill { get; set; }

    [ForeignKey(nameof(ItemTypeId)), JsonIgnore]
    public LookupValue? ItemType { get; set; }

    [ForeignKey(nameof(ExpenseAccountId)), JsonIgnore]
    public Account? ExpenseAccount { get; set; }
}
