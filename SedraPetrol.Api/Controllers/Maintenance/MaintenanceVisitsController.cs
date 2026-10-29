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
    public record LookupCheck(long LookupId, bool IsDone, string? Notes);
    /// <param name="ChecklistItems">Checklist from the «VISIT_CHECKLIST» lookup (Settings). When given, it replaces the visit's checklist.</param>
    public record CompleteInput(DateOnly? VisitDate, string? ReportSummary, List<ChecklistUpdate>? Checklist, List<FindingInput>? Findings, string? Notes,
        List<LookupCheck>? ChecklistItems = null, long? TechnicianEmployeeId = null, byte? CustomerRating = null, string? CustomerFeedback = null, string? FeedbackBy = null);
    public record AssignInput(long? TechnicianEmployeeId, string? TechnicianPhone, DateOnly? ScheduledDate, string? TimeSlot);
    public record FeedbackInput(byte? CustomerRating, string? CustomerFeedback, string? FeedbackBy);
    public class ReportForm
    {
        public string? ReportSummary { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    private FileStorageService Files => HttpContext.RequestServices.GetRequiredService<FileStorageService>();
    private const string Entity = "maintenance_visit";

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? amcContractId, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? premisesId, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.MaintenanceVisits.AsNoTracking().Where(v => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(v.BranchId));
        // visits of a restaurant's own AMC belong to the restaurant, not to its host building
        if (premisesId != null) query = query.Where(v => v.AmcContract!.PremisesId == premisesId);
        else if (buildingId != null) query = query.Where(v => v.BuildingId == buildingId && v.AmcContract!.PremisesId == null);
        if (amcContractId != null) query = query.Where(v => v.AmcContractId == amcContractId);
        if (from != null) query = query.Where(v => v.ScheduledDate >= from);
        if (to != null) query = query.Where(v => v.ScheduledDate <= to);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, status); query = query.Where(v => v.StatusId == s); }
        var page = await query.OrderBy(v => v.ScheduledDate)
            .Select(v => new { V = v, v.AmcContract!.ContractNo, BCode = v.Building!.Code, v.Building.NameEn, v.Building.NameAr, Vendor = v.AmcContract.Vendor!.Party!.NameAr ?? v.AmcContract.Vendor.Party!.NameEn,
                               Tech = v.VendorContact!.NameAr ?? v.VendorContact.NameEn,
                               Emp = v.TechnicianEmployee!.Party!.NameAr ?? v.TechnicianEmployee.Party!.NameEn,
                               Items = Db.VisitChecklistItems.Count(i => i.VisitId == v.Id), Done = Db.VisitChecklistItems.Count(i => i.VisitId == v.Id && i.IsDone),
                               Findings = Db.VisitFindings.Count(f => f.VisitId == v.Id) })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.V.Id, x.V.VisitNo, x.V.AmcContractId, x.ContractNo, x.V.BuildingId, buildingCode = x.BCode, building = Loc(x.NameEn, x.NameAr), vendor = x.Vendor,
            visitType = Code(x.V.VisitTypeId), x.V.PeriodLabel, x.V.ScheduledDate, x.V.TimeSlot, x.V.VisitDate, technician = x.Emp ?? x.Tech, x.V.TechnicianEmployeeId, x.V.TechnicianPhone,
            x.V.CustomerRating, x.V.CustomerFeedback, x.V.FeedbackBy,
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
        var tech = v.TechnicianEmployeeId == null ? null : await Db.Employees.AsNoTracking().Where(e => e.Id == v.TechnicianEmployeeId)
            .Select(e => new { e.Id, e.EmployeeNo, e.Party!.NameAr, e.Party.NameEn, e.Party.Phone, e.JobTitle }).FirstOrDefaultAsync();
        return Ok(new
        {
            visit = v, visitType = Code(v.VisitTypeId), status = Code(v.StatusId),
            technician = tech == null ? null : new { tech.Id, tech.EmployeeNo, name = Loc(tech.NameEn, tech.NameAr), tech.Phone, tech.JobTitle },
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
            BranchId = a.BranchId, VisitNo = await Numbers.NextAsync(NumberPrefixes.Visit, date: input.ScheduledDate), AmcContractId = a.Id, BuildingId = a.BuildingId!.Value,
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
        if (input.ChecklistItems != null)
        {
            var ids = input.ChecklistItems.Select(c => c.LookupId).Distinct().ToList();
            var typeId = await Db.LookupTypes.Where(t => t.Code == LookupTypes.VisitChecklist).Select(t => t.Id).FirstOrDefaultAsync();
            var values = await Db.LookupValues.AsNoTracking().Where(x => ids.Contains(x.Id) && x.LookupTypeId == typeId).ToDictionaryAsync(x => x.Id);
            Db.VisitChecklistItems.RemoveRange(await Db.VisitChecklistItems.Where(i => i.VisitId == id).ToListAsync());
            var order = 0;
            foreach (var c in input.ChecklistItems)
            {
                var lv = values.GetValueOrDefault(c.LookupId) ?? throw new AppException($"Checklist item {c.LookupId} is not in the visit checklist list.");
                Db.VisitChecklistItems.Add(new VisitChecklistItem
                {
                    VisitId = id, ItemText = lv.NameAr ?? lv.NameEn ?? lv.Code, SortOrder = lv.SortOrder ?? (order += 10), IsDone = c.IsDone,
                    Notes = string.IsNullOrWhiteSpace(c.Notes) ? null : c.Notes.Trim(),
                });
            }
        }
        foreach (var f in input.Findings ?? new()) await AddFindingInternal(v, f);
        if (input.VisitDate != null && input.VisitDate > Today) throw new AppException("The visit date cannot be in the future.");
        if (input.TechnicianEmployeeId != null) await SetTechnicianAsync(v, input.TechnicianEmployeeId);
        ApplyFeedback(v, input.CustomerRating, input.CustomerFeedback, input.FeedbackBy);
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

    /// <summary>Assign (or change) the employee who will carry out the visit, and optionally re-schedule it.</summary>
    [HttpPost("{id:long}/assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignInput input)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        EnsureBranchAccess(v.BranchId);
        if (await Lookups.CodeAsync(v.StatusId) is "completed" or "cancelled") throw new BusinessRuleException("The visit is already closed.");
        await SetTechnicianAsync(v, input.TechnicianEmployeeId);
        if (!string.IsNullOrWhiteSpace(input.TechnicianPhone)) v.TechnicianPhone = input.TechnicianPhone.Trim();
        if (input.ScheduledDate != null) v.ScheduledDate = input.ScheduledDate.Value;
        if (input.TimeSlot != null) v.TimeSlot = input.TimeSlot;
        Activity.Log(Entity, v.Id, "assign", $"Visit {v.VisitNo} assigned", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Customer feedback on a completed visit (rating 1–5, comments, who gave it). Can be recorded after completion.</summary>
    [HttpPost("{id:long}/feedback")]
    public async Task<IActionResult> Feedback(long id, [FromBody] FeedbackInput input)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        EnsureBranchAccess(v.BranchId);
        if (await Lookups.CodeAsync(v.StatusId) != "completed") throw new BusinessRuleException("Feedback is recorded on a completed visit.");
        ApplyFeedback(v, input.CustomerRating, input.CustomerFeedback, input.FeedbackBy);
        Activity.Log(Entity, v.Id, "feedback", $"Visit {v.VisitNo}: customer feedback {(v.CustomerRating == null ? "" : v.CustomerRating + "/5")}", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task SetTechnicianAsync(MaintenanceVisit v, long? employeeId)
    {
        if (employeeId == null) { v.TechnicianEmployeeId = null; return; }
        var e = await Db.Employees.AsNoTracking().Where(x => x.Id == employeeId).Select(x => new { x.IsActive, x.Party!.Phone }).FirstOrDefaultAsync()
                ?? throw new NotFoundException("Employee", employeeId.Value);
        if (!e.IsActive) throw new BusinessRuleException("The employee is not active.");
        v.TechnicianEmployeeId = employeeId;
        if (string.IsNullOrWhiteSpace(v.TechnicianPhone)) v.TechnicianPhone = e.Phone;
    }

    private static void ApplyFeedback(MaintenanceVisit v, byte? rating, string? feedback, string? by)
    {
        if (rating is < 1 or > 5) throw new AppException("The rating must be between 1 and 5.");
        if (rating != null) v.CustomerRating = rating;
        if (!string.IsNullOrWhiteSpace(feedback)) v.CustomerFeedback = feedback.Trim();
        if (!string.IsNullOrWhiteSpace(by)) v.FeedbackBy = by.Trim();
    }

    /// <summary>
    /// Upload the visit report: optional summary text and files (pdf / photos …), stored as documents of entity_type «maintenance_visit».
    /// Allowed on any visit that is not cancelled (a report can be added after completion).
    /// </summary>
    [HttpPost("{id:long}/report")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> UploadReport(long id, [FromForm] ReportForm form)
    {
        var v = await Db.MaintenanceVisits.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        EnsureBranchAccess(v.BranchId);
        if (await Lookups.CodeAsync(v.StatusId) == "cancelled") throw new BusinessRuleException("A cancelled visit cannot have a report.");
        var files = (form.Files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();
        if (files.Count == 0 && string.IsNullOrWhiteSpace(form.ReportSummary)) throw new AppException("Add the report text or at least one file.");
        if (files.Count > 10) throw new AppException("You can attach up to 10 files.");

        var saved = new List<(IFormFile File, string Path, long Size)>();
        foreach (var f in files) { var (path, size) = await Files.SaveAsync(f, Entity); saved.Add((f, path, size)); }
        foreach (var (f, path, size) in saved)
        {
            var isImage = (f.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            Db.Documents.Add(new Document
            {
                BranchId = v.BranchId, EntityType = Entity, EntityId = v.Id, FileName = Path.GetFileName(f.FileName), FilePath = path, MimeType = f.ContentType,
                SizeBytes = size, DocumentTypeId = await Lk(LookupTypes.DocumentType, null, isImage ? "photo" : "report", field: "documentType"),
            });
        }
        if (!string.IsNullOrWhiteSpace(form.ReportSummary)) v.ReportSummary = form.ReportSummary.Trim();
        Activity.Log(Entity, v.Id, "report", $"Visit {v.VisitNo}: report uploaded ({saved.Count} file(s))", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Files of a visit (reports, photos).</summary>
    [HttpGet("{id:long}/attachments")]
    public async Task<IActionResult> Attachments(long id)
    {
        var v = await Db.MaintenanceVisits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MaintenanceVisit", id);
        EnsureBranchAccess(v.BranchId);
        var docs = await Db.Documents.AsNoTracking().Where(d => d.EntityType == Entity && d.EntityId == id).OrderBy(d => d.Id)
            .Select(d => new { d.Id, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt }).ToListAsync();
        return Ok(docs);
    }

    [HttpGet("attachments/{docId:long}")]
    public async Task<IActionResult> DownloadAttachment(long docId)
    {
        var d = await Db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == docId && x.EntityType == Entity) ?? throw new NotFoundException("Document", docId);
        var v = await Db.MaintenanceVisits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == d.EntityId) ?? throw new NotFoundException("MaintenanceVisit", d.EntityId);
        EnsureBranchAccess(v.BranchId);
        var full = Files.FullPath(d.FilePath) ?? throw new NotFoundException("File", d.FileName ?? docId.ToString());
        return PhysicalFile(full, d.MimeType ?? "application/octet-stream", d.FileName);
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
