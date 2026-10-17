using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>party_bank_account</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("party_bank_account")]
public partial class PartyBankAccount : AuditableEntity
{
    [Column("party_id")]
    public long PartyId { get; set; }

    [Column("bank_name")]
    [MaxLength(150)]
    public string? BankName { get; set; }

    [Column("account_name")]
    [MaxLength(150)]
    public string? AccountName { get; set; }

    [Column("iban")]
    [MaxLength(34)]
    public string? Iban { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(PartyId)), JsonIgnore]
    public Party? Party { get; set; }
}
