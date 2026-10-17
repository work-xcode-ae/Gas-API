using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>amc_scope_item</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("amc_scope_item")]
public partial class AmcScopeItem : AuditableEntity
{
    [Column("contract_id")]
    public long ContractId { get; set; }

    [Column("item_type_id")]
    public long ItemTypeId { get; set; }

    [Column("description")]
    [Required]
    public string Description { get; set; } = string.Empty;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public AmcContract? Contract { get; set; }

    [ForeignKey(nameof(ItemTypeId)), JsonIgnore]
    public LookupValue? ItemType { get; set; }
}
