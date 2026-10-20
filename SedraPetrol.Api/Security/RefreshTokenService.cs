using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Security;

/// <summary>
/// Refresh tokens with rotation and reuse detection.
///   • Issue: random 64-byte token, only the SHA-256 hash is stored.
///   • Rotate: the presented token is revoked ("rotated") and a new one is issued in the same family; the family ends at AbsoluteExpiresAt.
///   • Reuse of an already-rotated token ⇒ the whole family is revoked (stolen token) — except within the grace window (two tabs refreshing together).
///   • Revocation on logout, logout-all, password change, user disabled, admin action.
/// </summary>
public class RefreshTokenService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _opt;
    private readonly IHttpContextAccessor _http;
    private readonly IMemoryCache _cache;

    public RefreshTokenService(AppDbContext db, IOptions<JwtOptions> opt, IHttpContextAccessor http, IMemoryCache cache)
    { _db = db; _opt = opt.Value; _http = http; _cache = cache; }

    private void Forget(IEnumerable<Guid> families) { foreach (var f in families.Distinct()) _cache.Remove($"session-alive:{f}"); }

    public record Issued(string Token, DateTimeOffset ExpiresAt, DateTimeOffset SessionExpiresAt, Guid FamilyId);

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private (string? Ip, string? Agent) Client()
    {
        var ctx = _http.HttpContext;
        var ua = ctx?.Request.Headers.UserAgent.ToString();
        return (ctx?.Connection.RemoteIpAddress?.ToString(), string.IsNullOrEmpty(ua) ? null : ua.Length > 300 ? ua[..300] : ua);
    }

    /// <summary>New session (login) or next token of an existing session (rotation).</summary>
    public async Task<Issued> IssueAsync(long userId, RefreshToken? previous = null)
    {
        var now = DateTimeOffset.UtcNow;
        var raw = Base64Url(RandomNumberGenerator.GetBytes(64));
        var (ip, agent) = Client();
        var absolute = previous?.AbsoluteExpiresAt ?? now.AddDays(_opt.RefreshTokenMaxDays);
        var expires = now.AddDays(_opt.RefreshTokenDays);
        if (expires > absolute) expires = absolute;
        var row = new RefreshToken
        {
            UserId = userId, TokenHash = Hash(raw), FamilyId = previous?.FamilyId ?? Guid.NewGuid(), CreatedAt = now, ExpiresAt = expires,
            AbsoluteExpiresAt = absolute, SessionStartedAt = previous?.SessionStartedAt ?? now, CreatedIp = ip, UserAgent = agent,
        };
        _db.RefreshTokens.Add(row);
        await _db.SaveChangesAsync();
        if (previous != null) { previous.ReplacedById = row.Id; await _db.SaveChangesAsync(); }
        return new Issued(raw, expires, absolute, row.FamilyId);
    }

    /// <summary>Validates and rotates a refresh token. Returns the user and the new token.</summary>
    public async Task<(AppUser User, Issued Next)> RotateAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw Unauthorized("refresh_missing", "Session expired. Please sign in again.");
        var hash = Hash(raw.Trim());
        var now = DateTimeOffset.UtcNow;
        var row = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash)
                  ?? throw Unauthorized("refresh_invalid", "Session expired. Please sign in again.");

        if (row.RevokedAt != null)
        {
            if (row.RevokedReason == "rotated" && row.RevokedAt > now.AddSeconds(-_opt.RefreshReuseGraceSeconds))
                throw Unauthorized("refresh_stale", "The session was just renewed in another tab.");   // client re-reads its storage
            if (row.RevokedReason == "rotated")
            {
                // an old token was used again: someone else may hold it → end the whole session
                await RevokeFamilyAsync(row.FamilyId, "reuse_detected");
                await TrailAsync(row.UserId, "refresh_reuse", new { familyId = row.FamilyId, message = "Rotated refresh token reused — session revoked" });
            }
            throw Unauthorized("refresh_revoked", "Session ended. Please sign in again.");
        }
        if (row.ExpiresAt <= now || row.AbsoluteExpiresAt <= now)
        {
            row.RevokedAt = now; row.RevokedReason = "expired";
            await _db.SaveChangesAsync();
            throw Unauthorized("refresh_expired", "Session expired. Please sign in again.");
        }
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == row.UserId);
        if (user == null || !user.IsActive)
        {
            await RevokeFamilyAsync(row.FamilyId, "user_disabled");
            throw Unauthorized("user_disabled", "The account is disabled.");
        }
        row.RevokedAt = now; row.RevokedReason = "rotated";
        var next = await IssueAsync(user.Id, row);
        return (user, next);
    }

    public async Task<int> RevokeFamilyAsync(Guid familyId, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await _db.RefreshTokens.Where(t => t.FamilyId == familyId && t.RevokedAt == null).ToListAsync();
        rows.ForEach(t => { t.RevokedAt = now; t.RevokedReason = reason; });
        await _db.SaveChangesAsync();
        Forget(new[] { familyId });
        return rows.Count;
    }

    /// <summary>Revoke the session that owns this raw token (logout). Unknown tokens are ignored.</summary>
    public async Task<long?> RevokeByTokenAsync(string? raw, string reason)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var hash = Hash(raw.Trim());
        var row = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (row == null) return null;
        await RevokeFamilyAsync(row.FamilyId, reason);
        return row.UserId;
    }

    /// <summary>End all sessions of a user (optionally keeping one).</summary>
    public async Task<int> RevokeAllAsync(long userId, string reason, Guid? keepFamily = null)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await _db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null && (keepFamily == null || t.FamilyId != keepFamily)).ToListAsync();
        rows.ForEach(t => { t.RevokedAt = now; t.RevokedReason = reason; });
        await _db.SaveChangesAsync();
        Forget(rows.Select(r => r.FamilyId));
        if (rows.Count > 0) await TrailAsync(userId, "sessions_revoked", new { reason, count = rows.Select(r => r.FamilyId).Distinct().Count() });
        return rows.Count;
    }

    /// <summary>Family id of a raw token (to keep the current session on "log out other devices").</summary>
    public async Task<Guid?> FamilyOfAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var hash = Hash(raw.Trim());
        return await _db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => (Guid?)t.FamilyId).FirstOrDefaultAsync();
    }

    /// <summary>Active sessions of a user (one row per family: the live token).</summary>
    public async Task<List<object>> SessionsAsync(long userId, Guid? currentFamily = null)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await _db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now && t.AbsoluteExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();
        return rows.GroupBy(t => t.FamilyId).Select(g => g.First()).Select(t => (object)new
        {
            id = t.FamilyId, startedAt = t.SessionStartedAt, lastActiveAt = t.CreatedAt, expiresAt = t.ExpiresAt, sessionEndsAt = t.AbsoluteExpiresAt,
            ip = t.CreatedIp, device = t.UserAgent, current = currentFamily == t.FamilyId,
        }).ToList();
    }

    /// <summary>Remove tokens that ended more than 30 days ago (housekeeping, called on login).</summary>
    public async Task CleanupAsync(long userId)
    {
        var limit = DateTimeOffset.UtcNow.AddDays(-30);
        var old = await _db.RefreshTokens.Where(t => t.UserId == userId && (t.AbsoluteExpiresAt < limit || (t.RevokedAt != null && t.RevokedAt < limit))).ToListAsync();
        if (old.Count == 0) return;
        _db.RefreshTokens.RemoveRange(old);
        await _db.SaveChangesAsync();
    }

    private async Task TrailAsync(long userId, string action, object details)
    {
        var ctx = _http.HttpContext;
        _db.AuditTrails.Add(new AuditTrail
        {
            OccurredAt = DateTimeOffset.UtcNow, TransactionId = Guid.NewGuid(), UserId = userId, UserName = ctx?.User?.Identity?.Name, Action = action,
            TableName = "refresh_token", EntityType = "AppUser", EntityId = userId.ToString(),
            Changes = System.Text.Json.JsonSerializer.Serialize(details), HttpMethod = ctx?.Request.Method, RequestPath = ctx?.Request.Path.Value,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString(),
            Links = new() { new AuditTrailLink { EntityType = "AppUser", EntityId = userId.ToString(), IsSelf = true } },
        });
        await _db.SaveChangesAsync();
    }

    private static AppException Unauthorized(string code, string message) => new(message, StatusCodes.Status401Unauthorized, code);
    private static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>Is the session of an access token still alive? Cached 20 seconds per session to keep requests cheap.</summary>
public static class SessionCheck
{
    public static async Task<bool> IsRevokedAsync(IServiceProvider sp, Guid family)
    {
        var cache = sp.GetRequiredService<IMemoryCache>();
        var key = $"session-alive:{family}";
        if (cache.TryGetValue(key, out bool cached)) return !cached;
        var db = sp.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;
        // alive while the family still has a live (not revoked) token — rotation always leaves its successor live
        var alive = await db.RefreshTokens.AsNoTracking().AnyAsync(t => t.FamilyId == family && t.RevokedAt == null && t.AbsoluteExpiresAt > now);
        cache.Set(key, alive, TimeSpan.FromSeconds(20));
        return !alive;
    }

    /// <summary>Forget the cached state (called after revoking) so the next request is checked at once.</summary>
    public static void Forget(IServiceProvider sp, Guid family) =>
        sp.GetRequiredService<IMemoryCache>().Remove($"session-alive:{family}");
}
