using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Base class: authenticated JSON API with the common services resolved lazily.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    private T Svc<T>() where T : notnull => HttpContext.RequestServices.GetRequiredService<T>();

    protected AppDbContext Db => Svc<AppDbContext>();
    protected LookupService Lookups => Svc<LookupService>();
    protected NumberingService Numbers => Svc<NumberingService>();
    protected ActivityLogger Activity => Svc<ActivityLogger>();
    protected ICurrentUser Me => Svc<ICurrentUser>();
    protected PostingService Posting => Svc<PostingService>();

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // ---------- localisation (Accept-Language: ar | en, default ar) ----------
    protected bool IsEnglish =>
        Request.Headers.AcceptLanguage.ToString().StartsWith("en", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Request.Query["lang"], "en", StringComparison.OrdinalIgnoreCase);

    /// <summary>Picks the English or Arabic value (falls back to the other one).</summary>
    protected string? Loc(string? en, string? ar) => IsEnglish ? (en ?? ar) : (ar ?? en);

    // ---------- lookup helpers: call "await LoadLookups()" once, then Code()/Label() ----------
    private IReadOnlyDictionary<long, LookupInfo>? _lk;
    protected async Task LoadLookups() => _lk ??= await Lookups.AllAsync();
    protected string? Code(long? id) => id != null && _lk != null && _lk.TryGetValue(id.Value, out var v) ? v.Code : null;
    protected string? Label(long? id) => id != null && _lk != null && _lk.TryGetValue(id.Value, out var v) ? Loc(v.NameEn, v.NameAr) : null;

    /// <summary>Resolve a lookup given as id or code (validates the lookup type).</summary>
    protected Task<long?> Lk(string type, long? id, string? code, bool required = false, string? field = null) =>
        Lookups.ResolveAsync(type, id, code, required, field);
    protected Task<long> LkId(string type, string code) => Lookups.IdAsync(type, code);

    /// <summary>Branch to use for new top-level records when the client does not send one.</summary>
    protected long RequireBranch(long? branchId)
    {
        var b = branchId ?? Me.BranchId ?? (Me.BranchIds.Count > 0 ? Me.BranchIds.First() : null);
        if (b == null) throw new AppException("branchId is required.");
        EnsureBranchAccess(b);
        return b.Value;
    }

    /// <summary>Throws 403 when a non-admin user touches a branch that is not assigned to them.</summary>
    protected void EnsureBranchAccess(long? branchId)
    {
        if (branchId == null || Me.IsAdmin || Me.BranchIds.Count == 0) return;
        if (!Me.BranchIds.Contains(branchId.Value))
            throw new AppException("You do not have access to this branch.", StatusCodes.Status403Forbidden, "forbidden_branch");
    }
}
