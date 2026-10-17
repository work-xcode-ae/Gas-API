using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Common;

/// <summary>Standard query-string parameters for list endpoints.</summary>
public class PageQuery
{
    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Rows per page (max 500). Use 0 to return everything (max 5000), e.g. for dropdowns.</summary>
    public int PageSize { get; set; } = 20;
    /// <summary>Free-text search.</summary>
    public string? Search { get; set; }
    /// <summary>Property name to sort by (camelCase or PascalCase).</summary>
    public string? Sort { get; set; }
    /// <summary>Sort descending.</summary>
    public bool Desc { get; set; } = true;

    public static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        { "page", "pageSize", "search", "sort", "desc", "includeDeleted" };
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public static class PagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedAsync<T>(this IQueryable<T> query, PageQuery q, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var page = Math.Max(1, q.Page);
        if (q.PageSize <= 0)
        {
            var all = await query.Take(5000).ToListAsync(ct);
            return new PagedResult<T>(all, total, 1, all.Count);
        }
        var size = Math.Clamp(q.PageSize, 1, 500);
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new PagedResult<T>(items, total, page, size);
    }
}
