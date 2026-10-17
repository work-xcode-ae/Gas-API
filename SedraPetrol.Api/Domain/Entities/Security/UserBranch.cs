using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>user_branch</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("user_branch")]
[PrimaryKey(nameof(UserId), nameof(BranchId))]
public partial class UserBranch : AuditBase
{
    [Column("user_id")]
    public long UserId { get; set; }

    [Column("branch_id")]
    public long BranchId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(UserId)), JsonIgnore]
    public AppUser? User { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }
}
