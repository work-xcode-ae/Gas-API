using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>number_sequence</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("number_sequence")]
public partial class NumberSequence : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("prefix")]
    [MaxLength(10)]
    [Required]
    public string Prefix { get; set; } = string.Empty;

    [Column("year")]
    public short? Year { get; set; }

    [Column("last_value")]
    public int? LastValue { get; set; }

    [Column("padding")]
    public short? Padding { get; set; } = 4;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }
}
