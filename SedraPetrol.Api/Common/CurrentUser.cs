using System.Security.Claims;

namespace SedraPetrol.Api.Common;

public interface ICurrentUser
{
    long? UserId { get; }
    long? CompanyId { get; }
    long? BranchId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    IReadOnlyCollection<long> BranchIds { get; }
    bool HasPermission(string code);
}

public static class AppClaims
{
    public const string UserId = "uid";
    public const string CompanyId = "cid";
    public const string BranchId = "bid";
    public const string Branches = "branches";
    public const string Permission = "perm";
    public const string AdminRole = "ADMIN";
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    private long? Long(string type) => long.TryParse(Principal?.FindFirst(type)?.Value, out var v) ? v : null;

    public long? UserId => Long(AppClaims.UserId);
    public long? CompanyId => Long(AppClaims.CompanyId);
    public long? BranchId => Long(AppClaims.BranchId);
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => Principal?.IsInRole(AppClaims.AdminRole) == true;

    public IReadOnlyCollection<long> BranchIds =>
        (Principal?.FindFirst(AppClaims.Branches)?.Value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var v) ? v : 0).Where(v => v > 0).ToList();

    public bool HasPermission(string code) =>
        IsAdmin || Principal?.HasClaim(AppClaims.Permission, code) == true;
}
