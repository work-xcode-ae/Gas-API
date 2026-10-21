using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>bank_account</c>: cash box / bank account / petty cash (BRD §15). The balance is always the general-ledger
/// balance of <see cref="GlAccountId"/> (posted journal lines) — it is never typed in.
/// </summary>
[Table("bank_account")]
[Index(nameof(GlAccountId), IsUnique = true, Name = "ux_bank_account_gl")]
public partial class BankAccount : AuditableEntity
{
    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("name_ar"), MaxLength(150)]
    public string NameAr { get; set; } = string.Empty;

    [Column("name_en"), MaxLength(150)]
    public string? NameEn { get; set; }

    /// <summary>cash | bank | petty_cash</summary>
    [Column("account_type"), MaxLength(20)]
    public string AccountType { get; set; } = "bank";

    [Column("bank_name"), MaxLength(150)]
    public string? BankName { get; set; }

    [Column("account_no"), MaxLength(50)]
    public string? AccountNo { get; set; }

    [Column("iban"), MaxLength(50)]
    public string? Iban { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    /// <summary>Postable general-ledger account that carries this bank / cash balance (one bank account per GL account).</summary>
    [Column("gl_account_id")]
    public long GlAccountId { get; set; }

    [Column("opening_balance")]
    [Precision(14, 2)]
    public decimal OpeningBalance { get; set; }

    [Column("opening_date")]
    public DateOnly? OpeningDate { get; set; }

    /// <summary>Journal that posted the opening balance (once).</summary>
    [Column("opening_journal_id")]
    public long? OpeningJournalId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    [Column("swift_code"), MaxLength(20)]
    public string? SwiftCode { get; set; }

    /// <summary>Printed on customer invoices as "Payment by bank transfer".</summary>
    [Column("show_on_invoice")]
    public bool ShowOnInvoice { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(GlAccountId)), JsonIgnore]
    public Account? GlAccount { get; set; }

    [ForeignKey(nameof(OpeningJournalId)), JsonIgnore]
    public JournalEntry? OpeningJournal { get; set; }
}
