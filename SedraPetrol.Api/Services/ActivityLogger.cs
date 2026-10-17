using System.Text.Json;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Services;

/// <summary>Adds activity_log rows to the current unit of work (saved with SaveChanges).</summary>
public class ActivityLogger
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    public ActivityLogger(AppDbContext db, ICurrentUser user) { _db = db; _user = user; }

    public void Log(string entityType, long? entityId, string action, string description,
        long? buildingId = null, long? branchId = null, object? oldValues = null, object? newValues = null, long? severityId = null)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            BuildingId = buildingId,
            BranchId = branchId,
            SeverityId = severityId,
            OldValues = oldValues == null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues == null ? null : JsonSerializer.Serialize(newValues),
            UserId = _user.UserId,
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}
