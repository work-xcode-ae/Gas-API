using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>user_role</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("user_role")]
[PrimaryKey(nameof(UserId), nameof(RoleId))]
public partial class UserRole : AuditBase
{
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("role_id")]
    public long RoleId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(UserId)), JsonIgnore]
    public AppUser? User { get; set; }

    [ForeignKey(nameof(RoleId)), JsonIgnore]
    public Role? Role { get; set; }
}
