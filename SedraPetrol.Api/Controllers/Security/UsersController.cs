using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>Users with roles and branch access.</summary>
[Route("api/users")]
[PermissionModule("users")]
public class UsersController : ApiControllerBase
{
    public record UserInput(string Email, string? NameEn, string? NameAr, string? Phone, long? BranchId, string? Password, bool IsActive,
        List<long>? RoleIds, List<string>? Roles, List<long>? BranchIds);
    public record ResetPasswordInput(string NewPassword);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? roleId, [FromQuery] long? branchId, [FromQuery] bool? active, CancellationToken ct)
    {
        var query = Db.AppUsers.AsNoTracking();
        if (roleId != null) query = query.Where(u => Db.UserRoles.Any(r => r.UserId == u.Id && r.RoleId == roleId));
        if (branchId != null) query = query.Where(u => u.BranchId == branchId || Db.UserBranches.Any(b => b.UserId == u.Id && b.BranchId == branchId));
        if (active != null) query = query.Where(u => u.IsActive == active);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(u => u.Email.Contains(q.Search) || (u.NameEn ?? "").Contains(q.Search) || (u.NameAr ?? "").Contains(q.Search));
        var page = await query.OrderBy(u => u.NameEn)
            .Select(u => new
            {
                u.Id, u.Email, u.NameEn, u.NameAr, u.Phone, u.BranchId, branch = u.Branch!.NameEn, u.IsActive, u.LastLoginAt, u.CreatedAt,
                roles = Db.UserRoles.Where(r => r.UserId == u.Id).Select(r => new { r.RoleId, r.Role!.Code, r.Role.NameEn, r.Role.NameAr }).ToList(),
                branches = Db.UserBranches.Where(b => b.UserId == u.Id).Select(b => new { b.BranchId, b.Branch!.Code }).ToList(),
            }).ToPagedAsync(q, ct);
        return Ok(page);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var u = await Db.AppUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("User", id);
        return Ok(await AuthController.BuildProfile(Db, u));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserInput input)
    {
        var email = input.Email?.Trim().ToLowerInvariant() ?? throw new AppException("Email is required.");
        if (await Db.AppUsers.IgnoreQueryFilters().AnyAsync(u => u.Email == email)) throw new BusinessRuleException($"A user with email {email} already exists.");
        ValidatePassword(input.Password);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var u = new AppUser
        {
            CompanyId = Me.CompanyId ?? await Db.Companies.Select(c => c.Id).FirstAsync(), BranchId = input.BranchId, Email = email,
            NameEn = input.NameEn, NameAr = input.NameAr, Phone = input.Phone, IsActive = input.IsActive,
        };
        u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, input.Password!);
        Db.AppUsers.Add(u);
        await Db.SaveChangesAsync();
        await SetRolesAndBranches(u.Id, input);
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = u.Id }, new { u.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UserInput input)
    {
        var u = await Db.AppUsers.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("User", id);
        var email = input.Email.Trim().ToLowerInvariant();
        if (email != u.Email && await Db.AppUsers.IgnoreQueryFilters().AnyAsync(x => x.Email == email && x.Id != id)) throw new BusinessRuleException($"A user with email {email} already exists.");
        if (id == Me.UserId && !input.IsActive) throw new BusinessRuleException("You cannot deactivate yourself.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        u.Email = email; u.NameEn = input.NameEn; u.NameAr = input.NameAr; u.Phone = input.Phone; u.BranchId = input.BranchId; u.IsActive = input.IsActive;
        if (!string.IsNullOrEmpty(input.Password)) { ValidatePassword(input.Password); u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, input.Password); }
        await Db.SaveChangesAsync();
        await SetRolesAndBranches(id, input);
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordInput input)
    {
        var u = await Db.AppUsers.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("User", id);
        ValidatePassword(input.NewPassword);
        u.PasswordHash = new PasswordHasher<AppUser>().HashPassword(u, input.NewPassword);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (id == Me.UserId) throw new BusinessRuleException("You cannot delete yourself.");
        var u = await Db.AppUsers.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("User", id);
        u.IsActive = false;
        Db.AppUsers.Remove(u);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task SetRolesAndBranches(long userId, UserInput input)
    {
        var roleIds = new List<long>(input.RoleIds ?? new());
        if (input.Roles != null) roleIds.AddRange(await Db.Roles.Where(r => input.Roles.Contains(r.Code)).Select(r => r.Id).ToListAsync());
        if (input.RoleIds != null || input.Roles != null)
        {
            await Db.UserRoles.IgnoreQueryFilters().Where(r => r.UserId == userId).ExecuteDeleteAsync();
            Db.UserRoles.AddRange(roleIds.Distinct().Select(r => new UserRole { UserId = userId, RoleId = r }));
        }
        if (input.BranchIds != null)
        {
            await Db.UserBranches.IgnoreQueryFilters().Where(b => b.UserId == userId).ExecuteDeleteAsync();
            Db.UserBranches.AddRange(input.BranchIds.Distinct().Select(b => new UserBranch { UserId = userId, BranchId = b }));
        }
        await Db.SaveChangesAsync();
    }

    internal static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsDigit) || !password.Any(char.IsLetter))
            throw new AppException("Password must be at least 8 characters and contain letters and digits.");
    }
}

/// <summary>Roles and their permissions.</summary>
[Route("api/roles")]
[PermissionModule("users")]
public class RolesController : ApiControllerBase
{
    public record RoleInput(string Code, string? NameEn, string? NameAr, List<string>? Permissions);

    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await Db.Roles.AsNoTracking().OrderBy(r => r.Code).Select(r => new
        {
            r.Id, r.Code, r.NameEn, r.NameAr,
            users = Db.UserRoles.Count(u => u.RoleId == r.Id),
            permissions = Db.RolePermissions.Where(p => p.RoleId == r.Id).Select(p => p.Permission!.Code).ToList(),
        }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleInput input)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        if (await Db.Roles.AnyAsync(r => r.Code == code)) throw new BusinessRuleException($"Role {code} already exists.");
        var role = new Role { Code = code, NameEn = input.NameEn, NameAr = input.NameAr };
        Db.Roles.Add(role);
        await Db.SaveChangesAsync();
        await SetPermissions(role.Id, input.Permissions ?? new());
        return Ok(new { role.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] RoleInput input)
    {
        var role = await Db.Roles.FirstOrDefaultAsync(r => r.Id == id) ?? throw new NotFoundException("Role", id);
        role.NameEn = input.NameEn; role.NameAr = input.NameAr;
        if (role.Code != AppClaims.AdminRole) role.Code = input.Code.Trim().ToUpperInvariant();
        await Db.SaveChangesAsync();
        if (input.Permissions != null) await SetPermissions(id, input.Permissions);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var role = await Db.Roles.FirstOrDefaultAsync(r => r.Id == id) ?? throw new NotFoundException("Role", id);
        if (role.Code == AppClaims.AdminRole) throw new BusinessRuleException("The ADMIN role cannot be deleted.");
        if (await Db.UserRoles.AnyAsync(u => u.RoleId == id)) throw new BusinessRuleException("The role is assigned to users.");
        Db.Roles.Remove(role);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task SetPermissions(long roleId, List<string> codes)
    {
        var ids = await Db.Permissions.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToListAsync();
        await Db.RolePermissions.IgnoreQueryFilters().Where(r => r.RoleId == roleId).ExecuteDeleteAsync();
        Db.RolePermissions.AddRange(ids.Select(p => new RolePermission { RoleId = roleId, PermissionId = p }));
        await Db.SaveChangesAsync();
    }
}

[Route("api/permissions")]
[PermissionModule("users")]
public class PermissionsController : ApiControllerBase
{
    /// <summary>All permissions grouped by module (for the role editor).</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var perms = await Db.Permissions.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync();
        return Ok(perms.GroupBy(p => p.Module).Select(g => new { module = g.Key, permissions = g.Select(p => new { p.Id, p.Code, p.NameEn, p.NameAr }) }));
    }
}
