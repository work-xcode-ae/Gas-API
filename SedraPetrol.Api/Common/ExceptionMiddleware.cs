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
        }
        catch (AppException ex)
        {
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
            await Write(ctx, status, msg, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await Write(ctx, 500, _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.", "server_error");
        }
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
