using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>AMC maintenance visits with checklist and findings.</summary>
[Route("api/maintenance-visits")]
[PermissionModule("maintenance")]
public class MaintenanceVisitsController : ApiControllerBase
{
    public record VisitInput(long AmcContractId, DateOnly ScheduledDate, string? VisitType, string? TimeSlot, long? VendorContactId, string? TechnicianPhone,
        string? PeriodLabel, long? TemplateId, string? Notes);
    public record ChecklistUpdate(long Id, bool IsDone, string? Notes);
    public record FindingInput(string? Severity, string Description, string? ActionTaken, bool CreateServiceRequest = false, string? Priority = null);
    public record CompleteInput(DateOnly? VisitDate, string? ReportSummary, List<ChecklistUpdate>? Checklist, List<FindingInput>? Findings, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? amcContractId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.MaintenanceVisits.AsNoTracking().Where(v => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(v.BranchId));
        if (buildingId != null) query = query.Where(v => v.BuildingId == buildingId);
        if (amcContractId != null) query = query.Where(v => v.AmcContractId == amcContractId);
        if (from != null) query = query.Where(v => v.ScheduledDate >= from);
        if (to != null) query = query.Where(v => v.ScheduledDate <= to);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, status); query = query.Where(v => v.StatusId == s); }
        var page = await query.OrderBy(v => v.ScheduledDate)
            .Select(v => new { V = v, v.AmcContract!.ContractNo, BCode = v.Building!.Code, v.Building.NameEn, v.Building.NameAr, Vendor = v.AmcContract.Vendor!.NameAr ?? v.AmcContract.Vendor.NameEn,
                               Tech = v.VendorContact!.NameAr ?? v.VendorContact.NameEn,
                               Items = Db.VisitChecklistItems.Count(i => i.VisitId == v.Id), Done = Db.VisitChecklistItems.Count(i => i.VisitId == v.Id && i.IsDone),
                               Findings = Db.VisitFindings.Count(f => f.VisitId == v.Id) })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.V.Id, x.V.VisitNo, x.V.AmcContractId, x.ContractNo, x.V.BuildingId, buildingCode = x.BCode, building = Loc(x.NameEn, x.NameAr), vendor = x.Vendor,
            visitType = Code(x.V.VisitTypeId), x.V.PeriodLabel, x.V.ScheduledDate, x.V.TimeSlot, x.V.VisitDate, technician = x.Tech, x.V.TechnicianPhone,
            status = Code(x.V.StatusId), statusLabel = Label(x.V.StatusId), checklistTotal = x.Items, checklistDone = x.Done, findings = x.Findings,
            x.V.ReportSummary, x.V.CompletedAt, overdue = Code(x.V.StatusId) == "scheduled" && x.V.ScheduledDate < Today,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var v = await Db.MaintenanceVisits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        var checklist = await Db.VisitChecklistItems.AsNoTracking().Where(i => i.VisitId == id).OrderBy(i => i.SortOrder).ToListAsync();
        var findings = await Db.VisitFindings.AsNoTracking().Where(f => f.VisitId == id).ToListAsync();
        return Ok(new
        {
            visit = v, visitType = Code(v.VisitTypeId), status = Code(v.StatusId),
            checklist = checklist.Select(i => new { i.Id, i.ItemText, i.SortOrder, i.IsDone, i.Notes }),
            findings = findings.Select(f => new { f.Id, severity = Code(f.SeverityId), f.Description, f.ActionTaken, f.ServiceRequestId }),
        });
    }

    /// <summary>Schedule an extra visit (e.g. corrective / emergency). The checklist is copied from a template.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VisitInput input)
    {
        var a = await Db.AmcContracts.FirstOrDefaultAsync(x => x.Id == input.AmcContractId) ?? throw new NotFoundException("AmcContract", input.AmcContractId);
        EnsureBranchAccess(a.BranchId);
        var typeId = await Lk(LookupTypes.VisitType, null, input.VisitType ?? "preventive", field: "visitType");
        var v = new MaintenanceVisit
        {
            BranchId = a.BranchId, VisitNo = await Numbers.NextAsync(NumberPrefixes.Visit, date: input.ScheduledDate), AmcContractId = a.Id, BuildingId = a.BuildingId,
            VisitTypeId = typeId, PeriodLabel = input.PeriodLabel, ScheduledDate = input.ScheduledDate, TimeSlot = input.TimeSlot,
            VendorContactId = input.VendorContactId, TechnicianPhone = input.TechnicianPhone, Notes = input.Notes,
            StatusId = await LkId(LookupTypes.VisitStatus, "scheduled"),
        };
        Db.MaintenanceVisits.Add(v);
        await Db.SaveChangesAsync();
        var template = input.TemplateId ?? await Db.ChecklistTemplates.Where(t => t.VisitTypeId == typeId && t.IsActive).Select(t => (long?)t.Id).FirstOrDefaultAsync();
        if (template != null) await CopyChecklist(Db, v.Id, template.Value);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, new { v.Id, v.VisitNo });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] VisitInput input)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        v.ScheduledDate = input.ScheduledDate; v.TimeSlot = input.TimeSlot; v.VendorContactId = input.VendorContactId;
        v.TechnicianPhone = input.TechnicianPhone; v.PeriodLabel = input.PeriodLabel ?? v.PeriodLabel; v.Notes = input.Notes;
        if (input.VisitType != null) v.VisitTypeId = await Lk(LookupTypes.VisitType, null, input.VisitType, field: "visitType");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        if (await Lookups.CodeAsync(v.StatusId) != "scheduled") throw new BusinessRuleException("Only scheduled visits can be started.");
        v.StatusId = await LkId(LookupTypes.VisitStatus, "in_progress");
        v.VisitDate = Today;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Complete the visit: checklist results, findings (optionally opening service requests) and the report summary.</summary>
    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, [FromBody] CompleteInput input)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        EnsureBranchAccess(v.BranchId);
        if (await Lookups.CodeAsync(v.StatusId) is "completed" or "cancelled") throw new BusinessRuleException("The visit is already closed.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        foreach (var c in input.Checklist ?? new())
        {
            var item = await Db.VisitChecklistItems.FirstOrDefaultAsync(i => i.Id == c.Id && i.VisitId == id) ?? throw new NotFoundException("VisitChecklistItem", c.Id);
            item.IsDone = c.IsDone; item.Notes = c.Notes;
        }
        foreach (var f in input.Findings ?? new()) await AddFindingInternal(v, f);
        v.VisitDate = input.VisitDate ?? v.VisitDate ?? Today;
        v.ReportSummary = input.ReportSummary;
        if (!string.IsNullOrWhiteSpace(input.Notes)) v.Notes = input.Notes;
        v.CompletedAt = DateTimeOffset.UtcNow;
        v.StatusId = await LkId(LookupTypes.VisitStatus, "completed");
        Activity.Log("maintenance_visit", v.Id, "status_change", $"Visit {v.VisitNo} completed", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        if (await Lookups.CodeAsync(v.StatusId) == "completed") throw new BusinessRuleException("A completed visit cannot be cancelled.");
        v.StatusId = await LkId(LookupTypes.VisitStatus, "cancelled");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:long}/checklist/{itemId:long}")]
    public async Task<IActionResult> UpdateChecklistItem(long id, long itemId, [FromBody] ChecklistUpdate input)
    {
        var item = await Db.VisitChecklistItems.FirstOrDefaultAsync(i => i.Id == itemId && i.VisitId == id) ?? throw new NotFoundException("VisitChecklistItem", itemId);
        item.IsDone = input.IsDone; item.Notes = input.Notes;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:long}/checklist")]
    public async Task<IActionResult> AddChecklistItem(long id, [FromBody] VisitChecklistItem input)
    {
        if (!await Db.MaintenanceVisits.AnyAsync(v => v.Id == id)) throw new NotFoundException("MaintenanceVisit", id);
        var max = await Db.VisitChecklistItems.Where(i => i.VisitId == id).MaxAsync(i => (int?)i.SortOrder) ?? 0;
        var item = new VisitChecklistItem { VisitId = id, ItemText = input.ItemText, SortOrder = input.SortOrder ?? max + 10, IsDone = input.IsDone, Notes = input.Notes };
        Db.VisitChecklistItems.Add(item);
        await Db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("{id:long}/findings")]
    public async Task<IActionResult> AddFinding(long id, [FromBody] FindingInput input)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        var f = await AddFindingInternal(v, input);
        await Db.SaveChangesAsync();
        return Ok(new { f.Id, f.ServiceRequestId });
    }

    private async Task<VisitFinding> AddFindingInternal(MaintenanceVisit v, FindingInput input)
    {
        var f = new VisitFinding { VisitId = v.Id, SeverityId = await Lk(LookupTypes.Severity, null, input.Severity ?? "medium", field: "severity"), Description = input.Description, ActionTaken = input.ActionTaken };
        if (input.CreateServiceRequest)
        {
            var sr = new ServiceRequest
            {
                BranchId = v.BranchId, RequestNo = await Numbers.NextAsync(NumberPrefixes.ServiceRequest), BuildingId = v.BuildingId,
                Description = $"[{v.VisitNo}] {input.Description}", PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"),
                StatusId = await LkId(LookupTypes.SrStatus, "pending"), RequestedAt = DateTimeOffset.UtcNow,
                AssignedVendorId = await Db.AmcContracts.Where(a => a.Id == v.AmcContractId).Select(a => (long?)a.VendorId).FirstOrDefaultAsync(),
            };
            Db.ServiceRequests.Add(sr);
            await Db.SaveChangesAsync();
            f.ServiceRequestId = sr.Id;
        }
        Db.VisitFindings.Add(f);
        await Db.SaveChangesAsync();
        return f;
    }

    internal static async Task CopyChecklist(AppDbContext db, long visitId, long templateId)
    {
        var items = await db.ChecklistTemplateItems.AsNoTracking().Where(i => i.TemplateId == templateId).OrderBy(i => i.SortOrder).ToListAsync();
        db.VisitChecklistItems.AddRange(items.Select(i => new VisitChecklistItem
        {
            VisitId = visitId, TemplateItemId = i.Id, ItemText = i.ItemTextAr ?? i.ItemTextEn ?? "", SortOrder = i.SortOrder, IsDone = false,
        }));
        await db.SaveChangesAsync();
    }
}

[Route("api/checklist-templates")]
[PermissionModule("maintenance")]
public class ChecklistTemplatesController : CrudController<ChecklistTemplate>
{
    [HttpGet("{id:long}/items")]
    public async Task<IActionResult> Items(long id) =>
        Ok(await Db.ChecklistTemplateItems.AsNoTracking().Where(i => i.TemplateId == id).OrderBy(i => i.SortOrder).ToListAsync());
}

[Route("api/checklist-template-items")]
[PermissionModule("maintenance")]
public class ChecklistTemplateItemsController : CrudController<ChecklistTemplateItem> { }

[Route("api/visit-checklist-items")]
[PermissionModule("maintenance")]
public class VisitChecklistItemsController : CrudController<VisitChecklistItem> { }

[Route("api/visit-findings")]
[PermissionModule("maintenance")]
public class VisitFindingsController : CrudController<VisitFinding> { }
