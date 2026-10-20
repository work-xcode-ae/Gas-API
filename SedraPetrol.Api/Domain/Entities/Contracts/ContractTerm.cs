using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>contract_term</c> — ONE table for the terms &amp; conditions setup and the terms of each contract.
/// <list type="bullet">
/// <item><c>contract_id</c> NULL → a setup term (Settings → Terms &amp; conditions), offered to new contracts of <c>contract_type</c>.</item>
/// <item><c>contract_id</c> set → the term as written in that contract (copied from <c>source_id</c>, possibly reworded, or custom).</item>
/// </list>
/// <c>contract_type</c> = supply (tenant gas supply contract) | amc. Editing the setup never changes signed contracts.
/// </summary>
[Table("contract_term")]
[Index(nameof(ContractType), nameof(ContractId))]
public partial class ContractTerm : AuditableEntity
{
    /// <summary>supply | amc</summary>
    [Column("contract_type")]
    [MaxLength(20)]
    [Required]
    public string ContractType { get; set; } = "supply";

    /// <summary>The contract this term belongs to (contract.id). NULL = setup term.</summary>
    [Column("contract_id")]
    public long? ContractId { get; set; }

    /// <summary>For a contract's term: the setup term it was copied from (NULL = written for this contract only).</summary>
    [Column("source_id")]
    public long? SourceId { get; set; }

    /// <summary>general | payment | service | safety | liability | renewal | dispute | other</summary>
    [Column("category")]
    [MaxLength(30)]
    [Required]
    public string Category { get; set; } = "general";

    [Column("text_ar")]
    [Required]
    public string TextAr { get; set; } = string.Empty;

    [Column("text_en")]
    public string? TextEn { get; set; }

    /// <summary>Setup term: pre-selected on new contracts.</summary>
    [Column("is_default")]
    public bool IsDefault { get; set; }

    /// <summary>Contract term: added for this contract only.</summary>
    [Column("is_custom")]
    public bool IsCustom { get; set; }

    /// <summary>Contract term: copied from the setup but reworded for this contract.</summary>
    [Column("is_modified")]
    public bool IsModified { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; }

    /// <summary>Setup term: offered to new contracts.</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public Contract? Contract { get; set; }

    [ForeignKey(nameof(SourceId)), JsonIgnore]
    public ContractTerm? Source { get; set; }
}
