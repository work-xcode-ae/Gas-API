using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>amc_term</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("amc_term")]
public partial class AmcTerm : AuditableEntity
{
    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("term_type_id")]
    public long? TermTypeId { get; set; }

    [Column("description")]
    [Required]
    public string Description { get; set; } = string.Empty;

    [Column("is_custom")]
    public bool IsCustom { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public AmcContract? Contract { get; set; }

    [ForeignKey(nameof(TermTypeId)), JsonIgnore]
    public LookupValue? TermType { get; set; }
}
