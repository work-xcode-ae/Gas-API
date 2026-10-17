using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>Login (JWT), current user and password change.</summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    public record LoginInput(string Email, string Password);
    public record ChangePasswordInput(string CurrentPassword, string NewPassword);

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
            throw new AppException("Invalid email or password.", StatusCodes.Status401Unauthorized, "invalid_credentials");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user!.PasswordHash = hasher.HashPassword(user, input.Password!);
        user!.LastLoginAt = DateTimeOffset.UtcNow;
        await Db.SaveChangesAsync();

        var profile = await BuildProfile(Db, user!);
        var (token, expires) = HttpContext.RequestServices.GetRequiredService<TokenService>()
            .Create(user!, profile.Roles, profile.Permissions, profile.Branches.Select(b => b.Id));
        return Ok(new { token, expiresAt = expires, user = profile });
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
