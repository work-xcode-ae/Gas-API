using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>emirate</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("emirate")]
public partial class Emirate : AuditableEntity
{
    [Column("country_id")]
    public long CountryId { get; set; }

    [Column("code")]
    [MaxLength(10)]
    public string? Code { get; set; }

    [Column("name_en")]
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [Column("name_ar")]
    [MaxLength(100)]
    public string? NameAr { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CountryId)), JsonIgnore]
    public Country? Country { get; set; }
}
