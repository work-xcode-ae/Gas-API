using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>company</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("company")]
public partial class Company : AuditableEntity
{
    [Column("code")]
    [MaxLength(20)]
    [Required]
    public string Code { get; set; } = string.Empty;

    [Column("name_en")]
    [MaxLength(200)]
    [Required]
    public string NameEn { get; set; } = string.Empty;

    [Column("name_ar")]
    [MaxLength(200)]
    [Required]
    public string NameAr { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("trn")]
    [MaxLength(20)]
    public string? Trn { get; set; }

    [Column("logo_path")]
    [MaxLength(500)]
    public string? LogoPath { get; set; }

    [Column("logo_dark_path")]
    [MaxLength(500)]
    public string? LogoDarkPath { get; set; }

    [Column("stamp_path")]
    [MaxLength(500)]
    public string? StampPath { get; set; }

    [Column("phone")]
    [MaxLength(30)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("website")]
    [MaxLength(150)]
    public string? Website { get; set; }

    [Column("base_currency_id")]
    public long? BaseCurrencyId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- printed documents (invoice / contracts) ----
    [Column("po_box"), MaxLength(30)] public string? PoBox { get; set; }
    [Column("fax"), MaxLength(30)] public string? Fax { get; set; }
    /// <summary>Customer care / enquiries number printed on invoices ("Contact us").</summary>
    [Column("customer_care_phone"), MaxLength(30)] public string? CustomerCarePhone { get; set; }
    /// <summary>Who collects cash payments ("For paying your invoice by Cash please contact").</summary>
    [Column("cash_contact_name"), MaxLength(100)] public string? CashContactName { get; set; }
    [Column("cash_contact_mobile"), MaxLength(30)] public string? CashContactMobile { get; set; }
    /// <summary>"Mode of payment" line printed on invoices.</summary>
    [Column("invoice_payment_note"), MaxLength(500)] public string? InvoicePaymentNote { get; set; }
    /// <summary>Who signs contracts for the company (name and title, English / Arabic).</summary>
    [Column("signatory_name_en"), MaxLength(150)] public string? SignatoryNameEn { get; set; }
    [Column("signatory_name_ar"), MaxLength(150)] public string? SignatoryNameAr { get; set; }
    [Column("signatory_title_en"), MaxLength(150)] public string? SignatoryTitleEn { get; set; }
    [Column("signatory_title_ar"), MaxLength(150)] public string? SignatoryTitleAr { get; set; }
    /// <summary>Full A4 letterhead image (header + footer) used as the invoice background. Empty = the built-in one.</summary>
    [Column("letterhead_path"), MaxLength(500)] public string? LetterheadPath { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BaseCurrencyId)), JsonIgnore]
    public Currency? BaseCurrency { get; set; }
}
