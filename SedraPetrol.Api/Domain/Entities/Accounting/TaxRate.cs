using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>tax_rate</c>: one VERSION of a tax code with its validity period (ValidFrom … ValidTo, ValidTo null = open-ended).
/// Versions of the same Code never overlap. Documents take the version valid on their document date (invoice issue date / bill date)
/// and keep the rate they were created with (InvoiceItem.VatRate + TaxRateId), so changing a rate never alters old documents.
/// </summary>
[Table("tax_rate")]
[Index(nameof(Code), nameof(ValidFrom), IsUnique = true, Name = "ux_tax_rate_code_from")]
public partial class TaxRate : AuditableEntity
{
    [Column("company_id")]
    public long? CompanyId { get; set; }

    /// <summary>Tax code shared by all versions, e.g. VAT-STD, VAT-ZERO, VAT-EXEMPT.</summary>
    [Column("code"), MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Column("name_ar"), MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("name_en"), MaxLength(150)]
    public string? NameEn { get; set; }

    /// <summary>vat (other taxes can be added later).</summary>
    [Column("tax_type"), MaxLength(20)]
    public string TaxType { get; set; } = "vat";

    /// <summary>standard | zero_rated | exempt | out_of_scope | reverse_charge</summary>
    [Column("category"), MaxLength(20)]
    public string Category { get; set; } = "standard";

    [Column("rate")]
    [Precision(5, 2)]
    public decimal Rate { get; set; }

    /// <summary>sales | purchases | both</summary>
    [Column("applies_to"), MaxLength(10)]
    public string AppliesTo { get; set; } = "both";

    [Column("valid_from")]
    public DateOnly ValidFrom { get; set; }

    [Column("valid_to")]
    public DateOnly? ValidTo { get; set; }

    /// <summary>Default code for new lines (per side) while this version is valid.</summary>
    [Column("is_default")]
    public bool IsDefault { get; set; }

    /// <summary>Output VAT (payable) account for sales; empty → account mapping «vat».</summary>
    [Column("output_account_id")]
    public long? OutputAccountId { get; set; }

    /// <summary>Input VAT (recoverable) account for purchases; empty → account mapping «vat_input».</summary>
    [Column("input_account_id")]
    public long? InputAccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(OutputAccountId)), JsonIgnore]
    public Account? OutputAccount { get; set; }

    [ForeignKey(nameof(InputAccountId)), JsonIgnore]
    public Account? InputAccount { get; set; }
}
