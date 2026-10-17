using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>role_permission</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("role_permission")]
[PrimaryKey(nameof(RoleId), nameof(PermissionId))]
public partial class RolePermission : AuditBase
{
    [Column("role_id")]
    public long RoleId { get; set; }

    [Column("permission_id")]
    public long PermissionId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(RoleId)), JsonIgnore]
    public Role? Role { get; set; }

    [ForeignKey(nameof(PermissionId)), JsonIgnore]
    public Permission? Permission { get; set; }
}
