using Microsoft.AspNetCore.Mvc;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Usage tracking written by the portal into the audit trail: page / record opened (view), print, export, download, logout.
/// Any signed-in user may write (no module permission); reading the trail needs audit.view.
/// </summary>
[Route("api/track")]
public class TrackController : ApiControllerBase
{
    private static readonly HashSet<string> Actions = new() { "view", "print", "export", "download", "logout" };

    public record TrackInput(string Action, string? Path, string? Title, string? EntityType, string? EntityId, string? Details);

    [HttpPost]
    public async Task<IActionResult> Track([FromBody] TrackInput input)
    {
        var action = (input.Action ?? "").Trim().ToLowerInvariant();
        if (!Actions.Contains(action)) return BadRequest(new { message = "Unknown action." });
        static string? Cut(string? s, int max) => string.IsNullOrWhiteSpace(s) ? null : s.Length <= max ? s : s[..max];
        var entityType = Cut(input.EntityType?.Trim(), 100);
        var entityId = Cut(input.EntityId?.Trim(), 100);
        var row = new AuditTrail
        {
            OccurredAt = DateTimeOffset.UtcNow, TransactionId = Guid.NewGuid(), UserId = Me.UserId, UserName = User.Identity?.Name,
            BranchId = Me.BranchId, Action = action, EntityType = entityType, EntityId = entityId,
            Changes = System.Text.Json.JsonSerializer.Serialize(new { title = Cut(input.Title, 200), path = Cut(input.Path, 300), details = Cut(input.Details, 500) }),
            HttpMethod = "UI", RequestPath = Cut(input.Path, 300), IpAddress = Cut(HttpContext.Connection.RemoteIpAddress?.ToString(), 64),
        };
        if (entityType != null && entityId != null) row.Links.Add(new AuditTrailLink { EntityType = entityType, EntityId = entityId, IsSelf = true });
        if (Me.UserId != null) row.Links.Add(new AuditTrailLink { EntityType = "AppUser", EntityId = Me.UserId.Value.ToString() });
        Db.AuditTrails.Add(row);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
