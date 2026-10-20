using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Read-only audit trail of every transaction (insert / update / delete of every table, plus logins).
/// Rows are written by AppDbContext; there is no endpoint to change or delete them.
/// </summary>
[Route("api/audit-trail")]
[PermissionModule("audit")]
public class AuditTrailController : ApiControllerBase
{
    /// <summary>Filters: table, entityId, userId, action, from, to, transactionId, search (user / record id / changed values).</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] string? table, [FromQuery] string? entityId, [FromQuery] long? userId,
        [FromQuery] string? action, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? transactionId, CancellationToken ct)
    {
        var query = Db.AuditTrails.AsNoTracking().Where(a => Me.IsAdmin || Me.BranchIds.Count == 0 || a.BranchId == null || Me.BranchIds.Contains(a.BranchId.Value));
        if (!string.IsNullOrWhiteSpace(table)) query = query.Where(a => a.TableName == table);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(a => a.EntityId == entityId);
        if (userId != null) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (transactionId != null) query = query.Where(a => a.TransactionId == transactionId);
        if (from != null) { var f = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(4)); query = query.Where(a => a.OccurredAt >= f); }
        if (to != null) { var t = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(4)); query = query.Where(a => a.OccurredAt < t); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(a => (a.UserName ?? "").Contains(s) || a.EntityId == s || (a.TableName ?? "").Contains(s) || (a.Changes ?? "").Contains(s) || (a.RequestPath ?? "").Contains(s));
        }
        var total = await query.CountAsync(ct);
        var size = q.PageSize <= 0 ? 500 : Math.Min(q.PageSize, 500);
        var page = Math.Max(1, q.Page);
        var rows = await query.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(new PagedResult<object>(rows.Select(Dto).ToList(), total, page, size));
    }

    /// <summary>History of one record, oldest first: /api/audit-trail/record?table=invoice&amp;id=15</summary>
    [HttpGet("record")]
    public async Task<IActionResult> Record([FromQuery] string table, [FromQuery] string id, CancellationToken ct)
    {
        var rows = await Db.AuditTrails.AsNoTracking().Where(a => a.TableName == table && a.EntityId == id).OrderBy(a => a.OccurredAt).ThenBy(a => a.Id).ToListAsync(ct);
        return Ok(rows.Select(Dto));
    }

    /// <summary>Tables that have audit rows, with counts (for the filter).</summary>
    [HttpGet("tables")]
    public async Task<IActionResult> Tables(CancellationToken ct) =>
        Ok(await Db.AuditTrails.AsNoTracking().Where(a => a.TableName != null).GroupBy(a => a.TableName)
            .Select(g => new { table = g.Key, count = g.Count(), last = g.Max(a => a.OccurredAt) }).OrderBy(x => x.table).ToListAsync(ct));

    /// <summary>Users that appear in the trail (for the filter).</summary>
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) =>
        Ok(await Db.AuditTrails.AsNoTracking().Where(a => a.UserId != null).GroupBy(a => a.UserId)
            .Select(g => new { userId = g.Key, name = g.Max(a => a.UserName), count = g.Count() }).OrderBy(x => x.name).ToListAsync(ct));

    private static object Dto(AuditTrail a)
    {
        object? changes = null;
        if (!string.IsNullOrEmpty(a.Changes)) { try { changes = JsonSerializer.Deserialize<JsonElement>(a.Changes); } catch { changes = a.Changes; } }
        return new
        {
            a.Id, a.OccurredAt, a.TransactionId, a.UserId, a.UserName, a.BranchId, a.Action, table = a.TableName, a.EntityType, a.EntityId,
            changes, a.HttpMethod, a.RequestPath, a.IpAddress,
        };
    }
}
