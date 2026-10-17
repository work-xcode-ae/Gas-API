using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>building_owner</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("building_owner")]
public partial class BuildingOwner : AuditableEntity
{
    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("party_id")]
    public long PartyId { get; set; }

    [Column("ownership_percentage")]
    [Precision(5, 2)]
    public decimal? OwnershipPercentage { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("from_date")]
    public DateOnly? FromDate { get; set; }

    [Column("to_date")]
    public DateOnly? ToDate { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(PartyId)), JsonIgnore]
    public Party? Party { get; set; }
}
