using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>refresh_token</c>: one row per issued refresh token (only its SHA-256 hash is stored).
/// Tokens are rotated: every refresh revokes the used token and issues a new one in the same family (= one login / device session).
/// Presenting an already-rotated token again revokes the whole family (token theft detection).
/// A family can never live longer than AbsoluteExpiresAt (re-login required).
/// </summary>
[Table("refresh_token")]
[Index(nameof(TokenHash), IsUnique = true, Name = "ux_refresh_token_hash")]
[Index(nameof(UserId), nameof(RevokedAt), Name = "ix_refresh_token_user")]
[Index(nameof(FamilyId), Name = "ix_refresh_token_family")]
public class RefreshToken
{
    [Key, Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("token_hash"), MaxLength(100)]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>All tokens of one login session share the family id.</summary>
    [Column("family_id")]
    public Guid FamilyId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Hard end of the session (from the first login), whatever the rotations.</summary>
    [Column("absolute_expires_at")]
    public DateTimeOffset AbsoluteExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>rotated | logout | logout_all | password_changed | user_disabled | reuse_detected | admin_revoked | expired</summary>
    [Column("revoked_reason"), MaxLength(30)]
    public string? RevokedReason { get; set; }

    [Column("replaced_by_id")]
    public long? ReplacedById { get; set; }

    [Column("created_ip"), MaxLength(64)]
    public string? CreatedIp { get; set; }

    [Column("user_agent"), MaxLength(300)]
    public string? UserAgent { get; set; }

    /// <summary>When the session started (first login of the family).</summary>
    [Column("session_started_at")]
    public DateTimeOffset SessionStartedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }
}
