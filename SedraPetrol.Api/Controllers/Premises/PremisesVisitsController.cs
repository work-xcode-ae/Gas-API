using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Restaurants &amp; Villas — maintenance-visit results of a villa / restaurant (additive; the maintenance-visit endpoints are unchanged).
/// A visit is closed either as <b>done</b> (the existing <c>POST /api/maintenance-visits/{id}/complete</c>) or as <b>not done</b> (here):
/// the employee who went to the site, the date, every checklist step (done or not + remark) and the reason are recorded and logged.
/// A not-done visit is stored with the existing «cancelled» status (no new status value, so building screens / reports are unaffected)
/// and is recognised by its activity-log entry (<c>to = "not_done"</c>).
/// </summary>
[Route("api/premises")]
[PermissionModule("maintenance")]
public class PremisesVisitsController : ApiControllerBase
{
    public record StepInput(long Id, bool IsDone, string? Notes);
    public record NewStepInput(string ItemText, bool IsDone, string? Notes);
    public record NotDoneInput(DateOnly? VisitDate, long TechnicianEmployeeId, string Reason, List<StepInput>? Checklist, List<NewStepInput>? NewItems);

    /// <summary>Outcome of every visit of the site's building: done / not done, reason, who recorded it and when.</summary>
    [HttpGet("{id:long}/visits/outcomes")]
    public async Task<IActionResult> Outcomes(long id)
    {
        var p = await Site(id);
        var visits = await Db.MaintenanceVisits.AsNoTracking()
            .Where(v => p.PremisesType == PremisesTypes.Restaurant ? v.AmcContract!.PremisesId == p.Id : v.BuildingId == p.BuildingId && v.AmcContract!.PremisesId == null)
            .Select(v => new { v.Id, v.StatusId, v.Notes, v.VisitDate, v.CompletedAt }).ToListAsync();
        var ids = visits.Select(v => v.Id).ToList();
        var logs = await Db.ActivityLogs.AsNoTracking()
            .Where(a => a.EntityType == "maintenance_visit" && a.EntityId != null && ids.Contains(a.EntityId.Value) && a.Action == "status_change")
            .Select(a => new { EntityId = a.EntityId!.Value, a.Id, a.OccurredAt, a.UserId, a.NewValues, a.Description }).ToListAsync();
        var userIds = logs.Where(l => l.UserId != null).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = await Db.AppUsers.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.NameAr ?? u.NameEn ?? u.Email }).ToDictionaryAsync(u => u.Id, u => u.Name);
        var completed = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, "completed");
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, "cancelled");
        return Ok(visits.Select(v =>
        {
            var last = logs.Where(l => l.EntityId == v.Id).OrderByDescending(l => l.OccurredAt).ThenByDescending(l => l.Id).FirstOrDefault();
            var notDone = v.StatusId == cancelled && last?.NewValues != null && last.NewValues.Contains("\"not_done\"");
            string? outcome = v.StatusId == completed ? "done" : notDone ? "not_done" : null;
            return new
            {
                v.Id, outcome,
                reason = notDone ? ReasonOf(v.Notes) : null,
                recordedAt = outcome == null ? null : last?.OccurredAt ?? v.CompletedAt,
                recordedBy = outcome == null || last?.UserId == null ? null : users.GetValueOrDefault(last.UserId.Value),
            };
        }));
    }

    /// <summary>Close a visit as «لم تتم» (not carried out): employee who visited, date, steps (done / not + remark) and the reason.</summary>
    [HttpPost("{id:long}/visits/{visitId:long}/not-done")]
    public async Task<IActionResult> NotDone(long id, long visitId, [FromBody] NotDoneInput input)
    {
        var p = await Site(id);
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == visitId && x.BuildingId == p.BuildingId) ?? throw new NotFoundException("MaintenanceVisit", visitId);
        EnsureBranchAccess(v.BranchId);
        if (await Lookups.CodeAsync(v.StatusId) is "completed" or "cancelled") throw new BusinessRuleException("الزيارة مغلقة بالفعل.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("اذكر سبب عدم إتمام الزيارة.");
        if (input.VisitDate != null && input.VisitDate > Today) throw new AppException("تاريخ الزيارة لا يمكن أن يكون في المستقبل.");
        var emp = await Db.Employees.AsNoTracking().Where(e => e.Id == input.TechnicianEmployeeId)
            .Select(e => new { e.IsActive, e.Party!.Phone, Name = e.Party.NameAr ?? e.Party.NameEn }).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Employee", input.TechnicianEmployeeId);
        if (!emp.IsActive) throw new BusinessRuleException("الموظف غير نشط.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        foreach (var c in input.Checklist ?? new())
        {
            var item = await Db.VisitChecklistItems.FirstOrDefaultAsync(i => i.Id == c.Id && i.VisitId == v.Id) ?? throw new NotFoundException("VisitChecklistItem", c.Id);
            item.IsDone = c.IsDone; item.Notes = string.IsNullOrWhiteSpace(c.Notes) ? null : c.Notes.Trim();
        }
        var order = await Db.VisitChecklistItems.Where(i => i.VisitId == v.Id).MaxAsync(i => (int?)i.SortOrder) ?? 0;
        foreach (var n in input.NewItems ?? new())
            Db.VisitChecklistItems.Add(new VisitChecklistItem { VisitId = v.Id, ItemText = n.ItemText, SortOrder = order += 10, IsDone = n.IsDone,
                Notes = string.IsNullOrWhiteSpace(n.Notes) ? null : n.Notes.Trim() });

        var reason = input.Reason.Trim();
        v.TechnicianEmployeeId = input.TechnicianEmployeeId;
        if (string.IsNullOrWhiteSpace(v.TechnicianPhone)) v.TechnicianPhone = emp.Phone;
        v.VisitDate = input.VisitDate ?? Today;
        v.Notes = $"{NotDonePrefix}{reason}";
        v.StatusId = await LkId(LookupTypes.VisitStatus, "cancelled");
        Activity.Log("maintenance_visit", v.Id, "status_change", $"Visit {v.VisitNo} not done ({p.Code}): {reason}", v.BuildingId, v.BranchId,
            newValues: new { to = "not_done", note = reason, technician = emp.Name, visitDate = v.VisitDate?.ToString("yyyy-MM-dd") });
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: visit {v.VisitNo} not done — {reason}", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    private const string NotDonePrefix = "لم تتم: ";
    private static string? ReasonOf(string? notes) => notes != null && notes.StartsWith(NotDonePrefix) ? notes[NotDonePrefix.Length..] : notes;

    private async Task<Premises> Site(long id)
    {
        var p = await Db.Premises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Premises", id);
        EnsureBranchAccess(p.BranchId);
        return p;
    }
}
