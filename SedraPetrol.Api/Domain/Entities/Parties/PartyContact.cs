using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>party_contact</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("party_contact")]
public partial class PartyContact : AuditableEntity
{
    [Column("party_id")]
    public long PartyId { get; set; }

    [Column("contact_type_id")]
    public long ContactTypeId { get; set; }

    [Column("value")]
    [MaxLength(150)]
    [Required]
    public string Value { get; set; } = string.Empty;

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(PartyId)), JsonIgnore]
    public Party? Party { get; set; }

    [ForeignKey(nameof(ContactTypeId)), JsonIgnore]
    public LookupValue? ContactType { get; set; }
}
