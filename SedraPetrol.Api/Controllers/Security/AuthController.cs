using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Login, token refresh (rotating refresh tokens), logout, sessions, current user and password change.
/// Access token = short JWT (Jwt:ExpiryMinutes, default 15). Refresh token = opaque random string, stored hashed,
/// rotated on every use (Jwt:RefreshTokenDays sliding, Jwt:RefreshTokenMaxDays absolute).
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    public record LoginInput(string Email, string Password);
    public record ChangePasswordInput(string CurrentPassword, string NewPassword, string? RefreshToken = null);
    public record RefreshInput(string? RefreshToken);

    private RefreshTokenService Refresh => HttpContext.RequestServices.GetRequiredService<RefreshTokenService>();

    /// <summary>Access token (JWT with fresh roles / permissions / branches from the database) + refresh token.</summary>
    private async Task<object> TokenPairAsync(AppUser user, Profile profile, RefreshToken? previous = null)
    {
        var rt = await Refresh.IssueAsync(user.Id, previous);
        var (token, expires) = HttpContext.RequestServices.GetRequiredService<TokenService>()
            .Create(user, profile.Roles, profile.Permissions, profile.Branches.Select(b => b.Id), rt.FamilyId);
        return new { token, expiresAt = expires, refreshToken = rt.Token, refreshExpiresAt = rt.ExpiresAt, sessionExpiresAt = rt.SessionExpiresAt, user = profile };
    }

    /// <summary>Returns a bearer token plus the user's profile, roles, permissions and branches.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginInput input)
    {
        var email = input.Email?.Trim() ?? "";
        var user = await Db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        var hasher = new PasswordHasher<AppUser>();
        var result = user == null || !user.IsActive ? PasswordVerificationResult.Failed : Verify(user, input.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            await LoginTrail("login_failed", user?.Id, email);
            throw new AppException("Invalid email or password.", StatusCodes.Status401Unauthorized, "invalid_credentials");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user!.PasswordHash = hasher.HashPassword(user, input.Password!);
        user!.LastLoginAt = DateTimeOffset.UtcNow;
        await Db.SaveChangesAsync();
        await LoginTrail("login", user.Id, user.NameEn ?? user.Email);

        var profile = await BuildProfile(Db, user!);
        await Refresh.CleanupAsync(user.Id);
        return Ok(await TokenPairAsync(user, profile));
    }

    /// <summary>
    /// New access token from a refresh token (no access token needed). The refresh token is rotated: use the new one next time.
    /// 401 codes: refresh_missing | refresh_invalid | refresh_expired | refresh_revoked | refresh_stale (renewed by another tab — re-read storage) | user_disabled.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshInput input)
    {
        var (user, next) = await Refresh.RotateAsync(input.RefreshToken);
        var profile = await BuildProfile(Db, user);
        var (token, expires) = HttpContext.RequestServices.GetRequiredService<TokenService>()
            .Create(user, profile.Roles, profile.Permissions, profile.Branches.Select(b => b.Id), next.FamilyId);
        return Ok(new { token, expiresAt = expires, refreshToken = next.Token, refreshExpiresAt = next.ExpiresAt, sessionExpiresAt = next.SessionExpiresAt, user = profile });
    }

    /// <summary>Ends this session (revokes its refresh token family). Works with an expired access token.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshInput input)
    {
        var userId = await Refresh.RevokeByTokenAsync(input.RefreshToken, "logout");
        if (userId != null)
        {
            Db.AuditTrails.Add(new AuditTrail
            {
                OccurredAt = DateTimeOffset.UtcNow, TransactionId = Guid.NewGuid(), UserId = userId, UserName = User.Identity?.Name, Action = "logout",
                TableName = "app_user", EntityType = "AppUser", EntityId = userId.ToString(), HttpMethod = "POST", RequestPath = "/api/auth/logout",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Links = new() { new AuditTrailLink { EntityType = "AppUser", EntityId = userId.Value.ToString(), IsSelf = true } },
            });
            await Db.SaveChangesAsync();
        }
        return NoContent();
    }

    /// <summary>Signs out every device of the current user; with keepCurrent the session of the given refresh token stays.</summary>
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll([FromBody] RefreshInput? input, [FromQuery] bool keepCurrent = true)
    {
        var keep = keepCurrent ? await Refresh.FamilyOfAsync(input?.RefreshToken) : null;
        var n = await Refresh.RevokeAllAsync(Me.UserId!.Value, "logout_all", keep);
        return Ok(new { revoked = n });
    }

    /// <summary>Active sessions (devices) of the current user. Send X-Refresh-Token to mark the current one.</summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> Sessions()
    {
        var current = await Refresh.FamilyOfAsync(Request.Headers["X-Refresh-Token"].ToString());
        return Ok(await Refresh.SessionsAsync(Me.UserId!.Value, current));
    }

    /// <summary>End one of my sessions.</summary>
    [HttpDelete("sessions/{familyId:guid}")]
    public async Task<IActionResult> EndSession(Guid familyId)
    {
        if (!await Db.RefreshTokens.AnyAsync(t => t.FamilyId == familyId && t.UserId == Me.UserId)) throw new NotFoundException("Session", familyId);
        await Refresh.RevokeFamilyAsync(familyId, "logout");
        return NoContent();
    }

    /// <summary>Audit trail row for a login attempt (successful or not).</summary>
    private async Task LoginTrail(string action, long? userId, string? name)
    {
        Db.AuditTrails.Add(new AuditTrail
        {
            OccurredAt = DateTimeOffset.UtcNow, TransactionId = Guid.NewGuid(), UserId = userId, UserName = name, Action = action,
            TableName = "app_user", EntityType = "AppUser", EntityId = userId?.ToString(), HttpMethod = "POST", RequestPath = "/api/auth/login",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Links = userId == null ? new() : new() { new AuditTrailLink { EntityType = "AppUser", EntityId = userId.Value.ToString(), IsSelf = true } },
        });
        await Db.SaveChangesAsync();
    }

    /// <summary>The signed-in user (refreshes roles, permissions and branches from the database).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> MeEndpoint()
    {
        var user = await Db.AppUsers.FirstOrDefaultAsync(u => u.Id == Me.UserId) ?? throw new AppException("User not found.", 401);
        return Ok(await BuildProfile(Db, user));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordInput input)
    {
        var user = await Db.AppUsers.FirstOrDefaultAsync(u => u.Id == Me.UserId) ?? throw new AppException("User not found.", 401);
        var hasher = new PasswordHasher<AppUser>();
        if (Verify(user, input.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new AppException("The current password is wrong.");
        UsersController.ValidatePassword(input.NewPassword);
        user.PasswordHash = hasher.HashPassword(user, input.NewPassword);
        await Db.SaveChangesAsync();
        // every other device must sign in again with the new password
        await Refresh.RevokeAllAsync(user.Id, "password_changed", await Refresh.FamilyOfAsync(input.RefreshToken));
        return NoContent();
    }

    /// <summary>Verifies a password; a missing or malformed stored hash counts as a failure.</summary>
    private static PasswordVerificationResult Verify(AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(user.PasswordHash) || string.IsNullOrEmpty(password)) return PasswordVerificationResult.Failed;
        try { return new PasswordHasher<AppUser>().VerifyHashedPassword(user, user.PasswordHash, password); }
        catch (FormatException) { return PasswordVerificationResult.Failed; }
    }

    public record BranchInfo(long Id, string Code, string? NameEn, string? NameAr);
    public record Profile(long Id, string Email, string? NameEn, string? NameAr, string? Phone, long CompanyId, string? Company, long? BranchId,
        List<string> Roles, List<string> Permissions, List<BranchInfo> Branches, bool IsAdmin, DateTimeOffset? LastLoginAt);

    internal static async Task<Profile> BuildProfile(AppDbContext db, AppUser user)
    {
        var roles = await db.UserRoles.Where(r => r.UserId == user.Id).Select(r => r.Role!.Code).ToListAsync();
        var isAdmin = roles.Contains(AppClaims.AdminRole);
        var perms = isAdmin
            ? await db.Permissions.Where(p => p.IsActive).Select(p => p.Code).ToListAsync()
            : await (from ur in db.UserRoles
                     join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
                     join p in db.Permissions on rp.PermissionId equals p.Id
                     where ur.UserId == user.Id && p.IsActive
                     select p.Code).Distinct().ToListAsync();
        var branches = isAdmin
            ? await db.Branches.Where(b => b.IsActive).Select(b => new BranchInfo(b.Id, b.Code, b.NameEn, b.NameAr)).ToListAsync()
            : await db.UserBranches.Where(ub => ub.UserId == user.Id).Select(ub => new BranchInfo(ub.BranchId, ub.Branch!.Code, ub.Branch.NameEn, ub.Branch.NameAr)).ToListAsync();
        if (!isAdmin && branches.Count == 0 && user.BranchId != null)
            branches = await db.Branches.Where(b => b.Id == user.BranchId).Select(b => new BranchInfo(b.Id, b.Code, b.NameEn, b.NameAr)).ToListAsync();
        var company = await db.Companies.Where(c => c.Id == user.CompanyId).Select(c => c.NameEn).FirstOrDefaultAsync();
        return new Profile(user.Id, user.Email, user.NameEn, user.NameAr, user.Phone, user.CompanyId, company, user.BranchId,
            roles, perms, branches, isAdmin, user.LastLoginAt);
    }
}
