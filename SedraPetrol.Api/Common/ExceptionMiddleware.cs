using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Common;

/// <summary>Turns exceptions into RFC 7807 ProblemDetails JSON responses.</summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next; _logger = logger; _env = env;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
            if (ctx.Response.StatusCode == 403) await TrailRejected(ctx, 403, "Permission denied.", "forbidden");
        }
        catch (AppException ex)
        {
            await TrailRejected(ctx, ex.StatusCode, ex.Message, ex.Code);
            await Write(ctx, ex.StatusCode, ex.Message, ex.Code);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            // 2601/2627 = unique index violation, 547 = FK / CHECK constraint violation
            var (status, msg, code) = sql.Number switch
            {
                2601 or 2627 => (409, "A record with the same unique value already exists. " + ExtractIndex(sql.Message), "duplicate"),
                547 => (409, "The operation conflicts with related data (foreign key or check constraint). " + sql.Message, "constraint"),
                _ => (500, _env.IsDevelopment() ? sql.Message : "Database error.", "db_error")
            };
            if (status == 500) _logger.LogError(ex, "Database error");
            await TrailRejected(ctx, status, msg, code);
            await Write(ctx, status, msg, code);
        }
        catch (Exception) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // the browser closed / cancelled the request (page changed, tab closed): nothing to answer, not an error
            _logger.LogDebug("Request cancelled by the client: {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await Write(ctx, 500, _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.", "server_error");
        }
    }

    /// <summary>
    /// Audit trail row for a refused change (validation / business rule / permission / database error) — so attempts are tracked too.
    /// Written through a NEW DbContext so nothing pending from the failed request is saved. GET requests are not logged.
    /// </summary>
    private async Task TrailRejected(HttpContext ctx, int status, string message, string? code)
    {
        if (HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsOptions(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method)) return;
        var path = ctx.Request.Path.Value ?? "";
        if (path.StartsWith("/api/track", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/auth/logout", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            using var scope = ctx.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SedraPetrol.Api.Data.AppDbContext>();
            var me = ctx.RequestServices.GetService<ICurrentUser>();
            // /api/<resource>/<id>/… → record id when the path names one
            var seg = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? recId = seg.Length >= 3 && long.TryParse(seg[2], out _) ? seg[2] : null;
            db.AuditTrails.Add(new SedraPetrol.Api.Domain.Entities.AuditTrail
            {
                OccurredAt = DateTimeOffset.UtcNow, TransactionId = Guid.NewGuid(), UserId = me?.UserId, UserName = ctx.User?.Identity?.Name, BranchId = me?.BranchId,
                Action = "rejected", TableName = seg.Length >= 2 ? seg[1] : null, EntityId = recId,
                Changes = System.Text.Json.JsonSerializer.Serialize(new { status, code, message = message.Length > 500 ? message[..500] : message }),
                HttpMethod = ctx.Request.Method, RequestPath = path.Length > 300 ? path[..300] : path,
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not write the rejected-request audit row"); }
    }

    private static string ExtractIndex(string message)
    {
        var i = message.IndexOf("index '", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return string.Empty;
        var end = message.IndexOf('\'', i + 7);
        return end > i ? $"({message.Substring(i + 7, end - i - 7)})" : string.Empty;
    }

    private static Task Write(HttpContext ctx, int status, string message, string? code)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        var pd = new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Bad request", 404 => "Not found", 409 => "Conflict", 401 => "Unauthorized", 403 => "Forbidden", _ => "Error" },
            Detail = message,
            Instance = ctx.Request.Path
        };
        if (code != null) pd.Extensions["code"] = code;
        return ctx.Response.WriteAsJsonAsync(pd);
    }
}
