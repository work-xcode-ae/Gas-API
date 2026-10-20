using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Service requests (complaints, leaks, repairs) per building / unit / tenant / meter.
/// Workflow: pending (received) → in_progress (assigned to a technician / vendor) → completed; any open request can be cancelled,
/// a closed one can be reopened (→ pending). Every step is written to activity_log and served by GET {id}/history.
/// </summary>
[Route("api/service-requests")]
[PermissionModule("service_requests")]
public class ServiceRequestsController : ApiControllerBase
{
    public record SrInput(long BuildingId, long? UnitId, long? TenantId, long? MeterId, string Description, string? Priority, long? AssignedTo, long? AssignedVendorId, string? Notes);
    public record AssignInput(long? AssignedTo, long? AssignedVendorId, string? Notes);
    public record StatusInput(string Status, string? Notes);

    /// <summary>multipart/form-data for POST {id}/complete — every field is optional.</summary>
    public class CompleteForm
    {
        public string? Notes { get; set; }
        /// <summary>When the work was finished (defaults to now; cannot be in the future or before the request).</summary>
        public DateTimeOffset? CompletedAt { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    private FileStorageService Files => HttpContext.RequestServices.GetRequiredService<FileStorageService>();

    private const string Entity = "service_request";

    /// <summary>Allowed status moves. Closed requests can only be reopened.</summary>
    private static readonly Dictionary<string, string[]> Transitions = new()
    {
        ["pending"] = new[] { "in_progress", "completed", "cancelled" },
        ["in_progress"] = new[] { "pending", "completed", "cancelled" },
        ["completed"] = new[] { "pending" },
        ["cancelled"] = new[] { "pending" },
    };

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? unitId, [FromQuery] long? tenantId,
        [FromQuery] long? branchId, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] long? assignedTo, [FromQuery] bool openOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.ServiceRequests.AsNoTracking().Where(r => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(r.BranchId));
        if (buildingId != null) query = query.Where(r => r.BuildingId == buildingId);
        if (unitId != null) query = query.Where(r => r.UnitId == unitId);
        if (tenantId != null) query = query.Where(r => r.TenantId == tenantId);
        if (branchId != null) query = query.Where(r => r.BranchId == branchId);
        if (assignedTo != null) query = query.Where(r => r.AssignedTo == assignedTo);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.SrStatus, status); query = query.Where(r => r.StatusId == s); }
        if (!string.IsNullOrEmpty(priority)) { var s = await Lookups.IdOrNullAsync(LookupTypes.Priority, priority); query = query.Where(r => r.PriorityId == s); }
        if (openOnly)
        {
            var open = new[] { await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "pending"), await Lookups.IdOrNullAsync(LookupTypes.SrStatus, "in_progress") };
            query = query.Where(r => open.Contains(r.StatusId));
        }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(r => r.RequestNo.Contains(q.Search) || r.Description.Contains(q.Search));
        var page = await query.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                R = r, BCode = r.Building!.Code, r.Building.NameEn, r.Building.NameAr, UnitNo = r.Unit!.UnitNo, TenantEn = r.Tenant!.Party!.NameEn, TenantAr = r.Tenant.Party!.NameAr,
                MeterNo = r.Meter!.MeterNo, Assignee = r.AssignedToUser!.NameAr ?? r.AssignedToUser.NameEn, AssigneePhone = r.AssignedToUser.Phone,
                Vendor = r.AssignedVendor!.Party!.NameAr ?? r.AssignedVendor.Party!.NameEn, VendorPhone = r.AssignedVendor.Party!.Phone,
                LastChange = Db.ActivityLogs.Where(a => a.EntityType == Entity && a.EntityId == r.Id).Max(a => (DateTimeOffset?)a.OccurredAt),
            }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.R.Id, no = x.R.RequestNo, requestNo = x.R.RequestNo, x.R.BuildingId, buildingCode = x.BCode, building = Loc(x.NameEn, x.NameAr),
            location = $"{Loc(x.NameEn, x.NameAr)}{(x.UnitNo == null ? "" : " – " + x.UnitNo)}", x.R.UnitId, unit = x.UnitNo,
            x.R.TenantId, tenant = Loc(x.TenantEn, x.TenantAr), x.R.MeterId, meterNo = x.MeterNo, x.R.Description, type = x.R.Description,
            priority = Code(x.R.PriorityId), priorityLabel = Label(x.R.PriorityId), status = Code(x.R.StatusId), statusLabel = Label(x.R.StatusId),
            x.R.AssignedTo, assignee = x.Assignee, assigneePhone = x.AssigneePhone, x.R.AssignedVendorId, vendor = x.Vendor, vendorPhone = x.VendorPhone,
            x.R.RequestedAt, x.R.ClosedAt, lastChangeAt = x.LastChange ?? x.R.RequestedAt, x.R.Notes,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var r = await Db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        return Ok(new { request = r, priority = Code(r.PriorityId), status = Code(r.StatusId), statusLabel = Label(r.StatusId) });
    }

    /// <summary>Tracking timeline of one request: creation, assignments and every status change (oldest first).</summary>
    [HttpGet("{id:long}/history")]
    public async Task<IActionResult> History(long id)
    {
        var r = await Db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        var rows = await Db.ActivityLogs.AsNoTracking().Where(a => a.EntityType == Entity && a.EntityId == id)
            .OrderBy(a => a.OccurredAt).ThenBy(a => a.Id)
            .Select(a => new
            {
                a.Id, a.Action, a.Description, a.NewValues, a.OccurredAt,
                User = Db.AppUsers.Where(u => u.Id == a.UserId).Select(u => u.NameAr ?? u.NameEn ?? u.Email).FirstOrDefault(),
            }).ToListAsync();
        return Ok(rows.Select(a =>
        {
            string? from = null, to = null, note = null, assignee = null, vendor = null, completedAt = null, attachments = null;
            if (!string.IsNullOrEmpty(a.NewValues))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(a.NewValues);
                    string? S(string k) => doc.RootElement.TryGetProperty(k, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
                    from = S("from"); to = S("to"); note = S("note"); assignee = S("assignee"); vendor = S("vendor");
                    completedAt = S("completedAt"); attachments = S("attachments");
                }
                catch (System.Text.Json.JsonException) { /* older free-text rows */ }
            }
            return new { a.Id, action = a.Action, a.Description, at = a.OccurredAt, user = a.User, from, to, note, assignee, vendor, completedAt,
                attachments = int.TryParse(attachments, out var n) ? n : 0 };
        }));
    }

    /// <summary>People and vendors a request can be assigned to (does not need the «users» permission).</summary>
    [HttpGet("assignees")]
    public async Task<IActionResult> Assignees()
    {
        var users = await Db.AppUsers.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.NameAr ?? u.NameEn)
            .Select(u => new { u.Id, u.NameEn, u.NameAr, u.Email, u.Phone }).ToListAsync();
        var vendors = await Db.Vendors.AsNoTracking().Where(v => v.IsActive).OrderBy(v => v.Party!.NameAr ?? v.Party!.NameEn)
            .Select(v => new { v.Id, v.Party!.NameEn, v.Party!.NameAr, v.Party!.Phone }).ToListAsync();
        return Ok(new
        {
            users = users.Select(u => new { u.Id, name = Loc(u.NameEn, u.NameAr) ?? u.Email, u.Phone }),
            vendors = vendors.Select(v => new { v.Id, name = Loc(v.NameEn, v.NameAr), v.Phone }),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SrInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        if (string.IsNullOrWhiteSpace(input.Description)) throw new AppException("Description is required.");
        var tenantId = input.TenantId;
        if (tenantId == null && input.UnitId != null)
            tenantId = await Db.SupplyContracts.Where(c => c.UnitId == input.UnitId && c.IsActive).Select(c => (long?)c.TenantId).FirstOrDefaultAsync();
        var assigned = input.AssignedTo != null || input.AssignedVendorId != null;
        var r = new ServiceRequest
        {
            BranchId = b.BranchId, RequestNo = await Numbers.NextAsync(NumberPrefixes.ServiceRequest), BuildingId = b.Id, UnitId = input.UnitId, TenantId = tenantId,
            MeterId = input.MeterId, Description = input.Description.Trim(), PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"),
            AssignedTo = input.AssignedTo, AssignedVendorId = input.AssignedVendorId, Notes = input.Notes, RequestedAt = DateTimeOffset.UtcNow,
            StatusId = await LkId(LookupTypes.SrStatus, assigned ? "in_progress" : "pending"),
        };
        Db.ServiceRequests.Add(r);
        await Db.SaveChangesAsync();
        Activity.Log(Entity, r.Id, "create", $"Service request {r.RequestNo}", b.Id, b.BranchId, newValues: new { to = "pending" });
        if (assigned)
        {
            var (assignee, vendor) = await AssigneeNames(r.AssignedTo, r.AssignedVendorId);
            Activity.Log(Entity, r.Id, "assign", $"{r.RequestNo} assigned", b.Id, b.BranchId,
                newValues: new { from = "pending", to = "in_progress", assignee, vendor });
        }
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = r.Id }, new { r.Id, r.RequestNo });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SrInput input)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        r.UnitId = input.UnitId; r.TenantId = input.TenantId ?? r.TenantId; r.MeterId = input.MeterId; r.Description = input.Description; r.Notes = input.Notes;
        r.PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority");
        r.AssignedTo = input.AssignedTo; r.AssignedVendorId = input.AssignedVendorId;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Assign / re-assign a technician or vendor. A pending request moves to in_progress.</summary>
    [HttpPost("{id:long}/assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignInput input)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        if (input.AssignedTo == null && input.AssignedVendorId == null) throw new AppException("Choose a technician or a vendor.");
        var current = await Lookups.CodeAsync(r.StatusId) ?? "pending";
        if (current is "completed" or "cancelled") throw new BusinessRuleException("The request is closed — reopen it before assigning.");
        r.AssignedTo = input.AssignedTo; r.AssignedVendorId = input.AssignedVendorId;
        var to = current == "pending" ? "in_progress" : current;
        r.StatusId = await LkId(LookupTypes.SrStatus, to);
        var (assignee, vendor) = await AssigneeNames(r.AssignedTo, r.AssignedVendorId);
        Activity.Log(Entity, r.Id, "assign", $"{r.RequestNo} assigned", r.BuildingId, r.BranchId,
            newValues: new { from = current, to, assignee, vendor, note = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim() });
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Change status: pending | in_progress | completed | cancelled (see <see cref="Transitions"/>).</summary>
    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] StatusInput input)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        var current = await Lookups.CodeAsync(r.StatusId) ?? "pending";
        var to = input.Status?.Trim() ?? "";
        if (to == current) return NoContent();
        if (!Transitions.TryGetValue(current, out var allowed) || !allowed.Contains(to))
            throw new BusinessRuleException($"Cannot move a request from «{current}» to «{to}».");
        if (to == "cancelled" && string.IsNullOrWhiteSpace(input.Notes)) throw new AppException("A cancellation reason is required.");
        r.StatusId = await Lk(LookupTypes.SrStatus, null, to, true, "status");
        r.ClosedAt = to is "completed" or "cancelled" ? DateTimeOffset.UtcNow : null;
        Activity.Log(Entity, r.Id, "status_change", $"{r.RequestNo} → {to}", r.BuildingId, r.BranchId,
            oldValues: new { status = current }, newValues: new { from = current, to, note = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim() });
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Close the request as completed with an optional remark, completion date and attachments (photos, reports …).
    /// Files are stored as documents of entity_type «service_request».
    /// </summary>
    [HttpPost("{id:long}/complete")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Complete(long id, [FromForm] CompleteForm form)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        var current = await Lookups.CodeAsync(r.StatusId) ?? "pending";
        if (current is not ("pending" or "in_progress")) throw new BusinessRuleException("Only an open request can be completed.");
        var files = (form.Files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();
        if (files.Count > 10) throw new AppException("You can attach up to 10 files.");

        var completedAt = form.CompletedAt ?? DateTimeOffset.UtcNow;
        if (completedAt > DateTimeOffset.UtcNow.AddMinutes(5)) throw new AppException("The completion date cannot be in the future.");
        if (r.RequestedAt != null && completedAt < r.RequestedAt.Value.AddMinutes(-1)) throw new AppException("The completion date cannot be before the request date.");

        // save the files first: a rejected file (type / size) stops the whole completion
        var saved = new List<(IFormFile File, string Path, long Size)>();
        foreach (var f in files) { var (path, size) = await Files.SaveAsync(f, Entity); saved.Add((f, path, size)); }

        await using var tx = await Db.Database.BeginTransactionAsync();
        foreach (var (f, path, size) in saved)
        {
            var isImage = (f.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            Db.Documents.Add(new Document
            {
                BranchId = r.BranchId, EntityType = Entity, EntityId = r.Id, FileName = Path.GetFileName(f.FileName), FilePath = path, MimeType = f.ContentType,
                SizeBytes = size, DocumentTypeId = await Lk(LookupTypes.DocumentType, null, isImage ? "photo" : "report", field: "documentType"),
            });
        }
        r.StatusId = await LkId(LookupTypes.SrStatus, "completed");
        r.ClosedAt = completedAt;
        var note = string.IsNullOrWhiteSpace(form.Notes) ? null : form.Notes.Trim();
        Activity.Log(Entity, r.Id, "status_change", $"{r.RequestNo} → completed", r.BuildingId, r.BranchId,
            oldValues: new { status = current },
            newValues: new { from = current, to = "completed", note, completedAt = completedAt.ToString("o"), attachments = saved.Count.ToString() });
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    /// <summary>Files attached to a request (completion photos, reports …).</summary>
    [HttpGet("{id:long}/attachments")]
    public async Task<IActionResult> Attachments(long id)
    {
        var r = await Db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        var docs = await Db.Documents.AsNoTracking().Where(d => d.EntityType == Entity && d.EntityId == id).OrderBy(d => d.Id)
            .Select(d => new { d.Id, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt }).ToListAsync();
        return Ok(docs);
    }

    /// <summary>Download one attachment (does not need the «documents» permission).</summary>
    [HttpGet("attachments/{docId:long}")]
    public async Task<IActionResult> DownloadAttachment(long docId)
    {
        var d = await Db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == docId && x.EntityType == Entity) ?? throw new NotFoundException("Document", docId);
        var r = await Db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == d.EntityId) ?? throw new NotFoundException("ServiceRequest", d.EntityId);
        EnsureBranchAccess(r.BranchId);
        var full = Files.FullPath(d.FilePath) ?? throw new NotFoundException("File", d.FileName ?? docId.ToString());
        return PhysicalFile(full, d.MimeType ?? "application/octet-stream", d.FileName);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        Db.ServiceRequests.Remove(r);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<(string? assignee, string? vendor)> AssigneeNames(long? userId, long? vendorId)
    {
        var assignee = userId == null ? null : await Db.AppUsers.Where(u => u.Id == userId).Select(u => u.NameAr ?? u.NameEn ?? u.Email).FirstOrDefaultAsync();
        var vendor = vendorId == null ? null : await Db.Vendors.Where(v => v.Id == vendorId).Select(v => v.Party!.NameAr ?? v.Party!.NameEn).FirstOrDefaultAsync();
        return (assignee, vendor);
    }
}

/// <summary>Civil Defense certificates and their renewal process.</summary>
[Route("api/cd-certificates")]
[PermissionModule("civil_defense")]
public class CdCertificatesController : ApiControllerBase
{
    public record CertInput(long BuildingId, string? CertificateNo, string? CertType, string? LicenseNo, string? IssuedBy, DateOnly? IssueDate, DateOnly? ExpiryDate,
        string? Scope, string? InspectorName, DateOnly? InspectionDate, string? Status, string? Notes);
    public record RenewalInput(DateOnly? StartedAt, short? CurrentStep, string? InspectorName, string? InspectorPhone, string? InspectorDept, DateOnly? RenewalDate, string? Remarks,
        OwnerBillingService.CdBilling? Billing = null);
    public record CompleteRenewalInput(string CertificateNo, DateOnly IssueDate, DateOnly NewExpiryDate, string? LicenseNo, string? Remarks);
    public record NewRequestInput(long BuildingId, string? CertType, string? IssuedBy, string? Scope, DateOnly? StartedAt, string? Notes,
        OwnerBillingService.CdBilling? Billing = null);

    private OwnerBillingService OwnerBilling => HttpContext.RequestServices.GetRequiredService<OwnerBillingService>();

    /// <summary>Default per-year amounts for the request form (appsettings CivilDefense:GovernmentFeesPerYear / ServiceFeePerYear) and the owners of the building.</summary>
    [HttpGet("billing-defaults")]
    public async Task<IActionResult> BillingDefaults([FromQuery] long buildingId)
    {
        var owners = await Db.BuildingOwners.AsNoTracking().Where(o => o.BuildingId == buildingId)
            .OrderByDescending(o => o.IsPrimary).Select(o => new { o.PartyId, o.IsPrimary, o.Party!.NameAr, o.Party.NameEn }).ToListAsync();
        return Ok(new
        {
            govFeesPerYear = OwnerBilling.DefaultCdGovFees, serviceFeePerYear = OwnerBilling.DefaultCdServiceFee, years = 1,
            owners = owners.Select(o => new { partyId = o.PartyId, name = Loc(o.NameEn, o.NameAr), o.IsPrimary }),
        });
    }

    /// <summary>Owner invoice(s) of a request: the main invoice + supplementary ones.</summary>
    private async Task<object?> OwnerInvoiceInfoAsync(CdRenewal r)
    {
        if (r.OwnerInvoiceId == null) return null;
        var main = await Db.Invoices.AsNoTracking().Where(i => i.Id == r.OwnerInvoiceId).Select(i => new { i.Id, i.InvoiceNo, i.IssueDate, i.TotalAmount, i.StatusId, i.TenantId }).FirstOrDefaultAsync();
        if (main == null) return null;
        var paid = await HttpContext.RequestServices.GetRequiredService<BillingService>().PaidByInvoiceAsync(new[] { main.Id });
        await LoadLookups();
        return new { main.Id, main.InvoiceNo, main.IssueDate, total = main.TotalAmount, paid = paid.GetValueOrDefault(main.Id), status = Code(main.StatusId),
                     statusLabel = Label(main.StatusId), customerId = main.TenantId, invoicedGovFees = r.InvoicedGovFees, years = r.DurationYears };
    }

    /// <summary>
    /// Request a NEW certificate for a building that has none: creates a «pending» certificate and starts the same 6-step workflow
    /// (application → fees → schedule inspection → inspection → corrections → issue). Issuing fills this certificate instead of creating another.
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> RequestNew([FromBody] NewRequestInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        var pending = await LkId(LookupTypes.CdStatus, "pending");
        var inProgress = await LkId(LookupTypes.CdRenewalStatus, "in_progress");
        if (await Db.CdCertificates.AnyAsync(c => c.BuildingId == b.Id && c.StatusId == pending))
            throw new BusinessRuleException("A certificate request is already open for this building.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var c = new CdCertificate
        {
            BranchId = b.BranchId, BuildingId = b.Id, CertTypeId = await Lk(LookupTypes.CdCertType, null, input.CertType ?? "gas_installation", field: "certType"),
            IssuedBy = input.IssuedBy, Scope = input.Scope, Notes = input.Notes, StatusId = pending,
        };
        Db.CdCertificates.Add(c);
        await Db.SaveChangesAsync();
        var r = new CdRenewal { CertificateId = c.Id, StartedAt = input.StartedAt ?? Today, CurrentStep = 1, StatusId = inProgress, Remarks = input.Notes };
        Db.CdRenewals.Add(r);
        Activity.Log("cd_certificate", c.Id, "create", "New Civil Defense certificate requested", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        // the owner is invoiced when the request opens (government fees + service fee × years), posted to the ledger
        r.DurationYears = input.Billing?.Years ?? 1;
        var inv = await OwnerBilling.IssueCdInvoiceAsync(c, r, input.Billing ?? new OwnerBillingService.CdBilling(null, null, null, null));
        await tx.CommitAsync();
        return Ok(new { c.Id, renewalId = r.Id, invoiceId = inv?.Id, invoiceNo = inv?.InvoiceNo, invoiceTotal = inv?.TotalAmount });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] string? status, [FromQuery] int? expiringInDays, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.CdCertificates.AsNoTracking().Where(c => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(c.BranchId));
        if (buildingId != null) query = query.Where(c => c.BuildingId == buildingId);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.CdStatus, status); query = query.Where(c => c.StatusId == s); }
        if (expiringInDays != null) { var limit = Today.AddDays(expiringInDays.Value); query = query.Where(c => c.ExpiryDate != null && c.ExpiryDate <= limit); }
        var page = await query.OrderBy(c => c.ExpiryDate).Select(c => new { C = c, c.Building!.Code, c.Building.NameEn, c.Building.NameAr,
            Renewal = Db.CdRenewals.Where(r => r.CertificateId == c.Id).OrderByDescending(r => r.Id).Select(r => new { r.Id, r.CurrentStep, r.StatusId }).FirstOrDefault() }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.C.Id, x.C.BuildingId, buildingCode = x.Code, building = Loc(x.NameEn, x.NameAr), x.C.CertificateNo, certType = Code(x.C.CertTypeId), certTypeLabel = Label(x.C.CertTypeId),
            x.C.LicenseNo, x.C.IssuedBy, x.C.IssueDate, x.C.ExpiryDate, daysToExpiry = x.C.ExpiryDate == null ? (int?)null : x.C.ExpiryDate.Value.DayNumber - Today.DayNumber,
            x.C.Scope, x.C.InspectorName, x.C.InspectionDate, status = Code(x.C.StatusId), statusLabel = Label(x.C.StatusId), x.C.Notes,
            renewal = x.Renewal == null ? null : new { x.Renewal.Id, x.Renewal.CurrentStep, status = Code(x.Renewal.StatusId) },
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var c = await Db.CdCertificates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        var renewals = await Db.CdRenewals.AsNoTracking().Where(r => r.CertificateId == id || r.NewCertificateId == id).OrderByDescending(r => r.Id).ToListAsync();
        return Ok(new { certificate = c, certType = Code(c.CertTypeId), certTypeLabel = Label(c.CertTypeId), status = Code(c.StatusId), renewals = renewals.Select(r => new { renewal = r, status = Code(r.StatusId), producedThis = r.NewCertificateId == id && r.CertificateId != id }) });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CertInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        var c = new CdCertificate { BranchId = b.BranchId, BuildingId = b.Id };
        await Apply(c, input);
        Db.CdCertificates.Add(c);
        await Db.SaveChangesAsync();
        Activity.Log("cd_certificate", c.Id, "create", $"Civil Defense certificate {c.CertificateNo} added", b.Id, b.BranchId);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, new { c.Id });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CertInput input)
    {
        var c = await Db.CdCertificates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        EnsureBranchAccess(c.BranchId);
        await Apply(c, input);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var c = await Db.CdCertificates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        Db.CdCertificates.Remove(c);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Start a renewal (certificate status → renewal_in_progress).</summary>
    [HttpPost("{id:long}/renewals")]
    public async Task<IActionResult> StartRenewal(long id, [FromBody] RenewalInput input)
    {
        var c = await Db.CdCertificates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        var inProgress = await LkId(LookupTypes.CdRenewalStatus, "in_progress");
        if (await Db.CdRenewals.AnyAsync(r => r.CertificateId == id && r.StatusId == inProgress)) throw new BusinessRuleException("A renewal is already in progress.");
        var r = new CdRenewal
        {
            CertificateId = id, StartedAt = input.StartedAt ?? Today, CurrentStep = input.CurrentStep ?? 1, InspectorName = input.InspectorName,
            InspectorPhone = input.InspectorPhone, InspectorDept = input.InspectorDept, RenewalDate = input.RenewalDate, Remarks = input.Remarks, StatusId = inProgress,
        };
        r.DurationYears = input.Billing?.Years ?? 1;
        await using var tx = await Db.Database.BeginTransactionAsync();
        Db.CdRenewals.Add(r);
        c.StatusId = await LkId(LookupTypes.CdStatus, "renewal_in_progress");
        await Db.SaveChangesAsync();
        var inv = await OwnerBilling.IssueCdInvoiceAsync(c, r, input.Billing ?? new OwnerBillingService.CdBilling(null, null, null, null));
        await tx.CommitAsync();
        return Ok(new { r.Id, invoiceId = inv?.Id, invoiceNo = inv?.InvoiceNo, invoiceTotal = inv?.TotalAmount });
    }

    /// <summary>Issue the owner invoice of a request that was opened without one (e.g. amounts were not known).</summary>
    [HttpPost("renewals/{renewalId:long}/owner-invoice")]
    public async Task<IActionResult> IssueOwnerInvoice(long renewalId, [FromBody] OwnerBillingService.CdBilling input)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        var c = await Db.CdCertificates.FirstAsync(x => x.Id == r.CertificateId);
        EnsureBranchAccess(c.BranchId);
        r.DurationYears = input.Years ?? r.DurationYears ?? 1;
        var inv = await OwnerBilling.IssueCdInvoiceAsync(c, r, input with { Skip = false }) ?? throw new AppException("Enter the government fees and / or the service fee.");
        return Ok(new { inv.Id, inv.InvoiceNo, inv.TotalAmount });
    }

    [HttpPut("renewals/{renewalId:long}")]
    public async Task<IActionResult> UpdateRenewal(long renewalId, [FromBody] RenewalInput input)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        if ((r.CurrentStep ?? 1) <= FeeStep && (input.CurrentStep ?? 0) > FeeStep)
        {
            var fees = await FeeBillsAsync(renewalId);
            if (fees.Count == 0) throw new BusinessRuleException("Record the Civil Defense fees invoice first (step 2).");
            if (fees.Any(f => f.Balance > 0)) throw new BusinessRuleException("The Civil Defense fees are not fully paid yet — record the payment before continuing.");
        }
        r.CurrentStep = input.CurrentStep ?? r.CurrentStep; r.InspectorName = input.InspectorName; r.InspectorPhone = input.InspectorPhone;
        r.InspectorDept = input.InspectorDept; r.RenewalDate = input.RenewalDate; r.Remarks = input.Remarks;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Complete the renewal: creates the new certificate (valid) and expires the old one.</summary>
    [HttpPost("renewals/{renewalId:long}/complete")]
    public async Task<IActionResult> CompleteRenewal(long renewalId, [FromBody] CompleteRenewalInput input)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        if (await Lookups.CodeAsync(r.StatusId) != "in_progress") throw new BusinessRuleException("The renewal is not in progress.");
        var old = await Db.CdCertificates.FirstAsync(c => c.Id == r.CertificateId);
        if (input.NewExpiryDate <= input.IssueDate) throw new AppException("The expiry date must be after the issue date.");
        if (await Lookups.CodeAsync(old.StatusId) == "pending")
        {
            // first certificate of the building: the requested certificate itself becomes valid
            old.CertificateNo = input.CertificateNo; old.LicenseNo = input.LicenseNo ?? old.LicenseNo; old.IssueDate = input.IssueDate; old.ExpiryDate = input.NewExpiryDate;
            old.InspectorName = r.InspectorName; old.InspectionDate = r.RenewalDate; old.StatusId = await LkId(LookupTypes.CdStatus, "valid");
            r.NewCertificateId = old.Id; r.NewExpiryDate = input.NewExpiryDate; r.RenewalDate ??= input.IssueDate;
            r.StatusId = await LkId(LookupTypes.CdRenewalStatus, "completed"); r.Remarks = input.Remarks ?? r.Remarks;
            Activity.Log("cd_certificate", old.Id, "update", $"Civil Defense certificate {input.CertificateNo} issued until {input.NewExpiryDate:yyyy-MM-dd}", old.BuildingId, old.BranchId);
            await Db.SaveChangesAsync();
            return Ok(new { old.Id });
        }
        await using var tx = await Db.Database.BeginTransactionAsync();
        var cert = new CdCertificate
        {
            BranchId = old.BranchId, BuildingId = old.BuildingId, CertificateNo = input.CertificateNo, CertTypeId = old.CertTypeId, LicenseNo = input.LicenseNo ?? old.LicenseNo,
            IssuedBy = old.IssuedBy, IssueDate = input.IssueDate, ExpiryDate = input.NewExpiryDate, Scope = old.Scope, InspectorName = r.InspectorName,
            InspectionDate = r.RenewalDate, StatusId = await LkId(LookupTypes.CdStatus, "valid"),
        };
        Db.CdCertificates.Add(cert);
        await Db.SaveChangesAsync();
        old.StatusId = await LkId(LookupTypes.CdStatus, "expired");
        r.NewCertificateId = cert.Id; r.NewExpiryDate = input.NewExpiryDate; r.RenewalDate ??= input.IssueDate;
        r.StatusId = await LkId(LookupTypes.CdRenewalStatus, "completed"); r.Remarks = input.Remarks ?? r.Remarks;
        Activity.Log("cd_certificate", cert.Id, "create", $"Civil Defense certificate renewed until {input.NewExpiryDate:yyyy-MM-dd}", old.BuildingId, old.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { cert.Id });
    }

    // ------------------------------------------------------------------ step 2: Civil Defense fees (vendor bill → payment)

    private const short FeeStep = 2;
    private const string FeeSource = "cd_renewal";
    public record FeeInput(string BillNo, DateOnly? BillDate, DateOnly? DueDate, decimal Amount, decimal? VatRate, string? Description, long? VendorId);
    public record FeePaymentInput(decimal Amount, DateOnly? Date, string? Method, string? Reference, string? Notes);
    private record FeeRow(long Id, string BillNo, DateOnly BillDate, DateOnly? DueDate, decimal Total, decimal Paid, decimal Balance, string Status, int DaysLate, string? Vendor);

    private async Task<List<FeeRow>> FeeBillsAsync(long renewalId)
    {
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var bills = await Db.VendorBills.AsNoTracking().Where(b => b.SourceType == FeeSource && b.SourceId == renewalId && b.StatusId != cancelled)
            .OrderBy(b => b.Id).Select(b => new { b.Id, b.BillNo, b.BillDate, b.DueDate, b.TotalAmount, Vendor = b.Vendor!.Party!.NameAr ?? b.Vendor.Party!.NameEn }).ToListAsync();
        var paid = await HttpContext.RequestServices.GetRequiredService<BillingService>().PaidByBillAsync(bills.Select(b => b.Id));
        return bills.Select(b =>
        {
            var total = b.TotalAmount ?? 0; var p = paid.GetValueOrDefault(b.Id); var bal = Math.Max(0, total - p);
            var st = total > 0 && bal <= 0 ? "paid" : b.DueDate != null && Today > b.DueDate ? "overdue" : p > 0 ? "partial" : "unpaid";
            return new FeeRow(b.Id, b.BillNo, b.BillDate, b.DueDate, total, p, bal, st, st == "overdue" ? Today.DayNumber - b.DueDate!.Value.DayNumber : 0, b.Vendor);
        }).ToList();
    }

    /// <summary>Fees invoices of a renewal / new-certificate request with their payment status.</summary>
    [HttpGet("renewals/{renewalId:long}/fees")]
    public async Task<IActionResult> Fees(long renewalId)
    {
        _ = await Db.CdRenewals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        var rows = await FeeBillsAsync(renewalId);
        var ids = rows.Select(r => r.Id).ToList();
        var payments = await (from a in Db.VendorPaymentAllocations.AsNoTracking()
                              join p in Db.VendorPayments on a.VendorPaymentId equals p.Id
                              where ids.Contains(a.VendorBillId)
                              orderby p.PaymentDate
                              select new { a.VendorBillId, p.PaymentNo, p.PaymentDate, a.Amount, p.ReferenceNo, p.PaymentMethodId }).ToListAsync();
        await LoadLookups();
        var renewal = await Db.CdRenewals.AsNoTracking().FirstAsync(x => x.Id == renewalId);
        var ownerInvoice = await OwnerInvoiceInfoAsync(renewal);
        return Ok(new
        {
            ownerInvoice,
            paid = rows.Count > 0 && rows.All(r => r.Balance <= 0),
            bills = rows.Select(r => new
            {
                r.Id, r.BillNo, r.BillDate, r.DueDate, r.Total, r.Paid, r.Balance, r.Status, r.DaysLate, r.Vendor,
                payments = payments.Where(p => p.VendorBillId == r.Id).Select(p => new { p.PaymentNo, p.PaymentDate, p.Amount, p.ReferenceNo, method = Code(p.PaymentMethodId) }),
            }),
        });
    }

    /// <summary>Record the Civil Defense fees invoice (a vendor bill to the «CIVIL-DEFENSE» authority unless another vendor is given).</summary>
    [HttpPost("renewals/{renewalId:long}/fees")]
    public async Task<IActionResult> AddFee(long renewalId, [FromBody] FeeInput input)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        if (await Lookups.CodeAsync(r.StatusId) != "in_progress") throw new BusinessRuleException("The renewal is not in progress.");
        var c = await Db.CdCertificates.FirstAsync(x => x.Id == r.CertificateId);
        EnsureBranchAccess(c.BranchId);
        if (string.IsNullOrWhiteSpace(input.BillNo)) throw new AppException("The fees invoice / notice number is required.");
        if (input.Amount <= 0) throw new AppException("The fees amount must be greater than zero.");
        var billDate = input.BillDate ?? Today;
        if (billDate > Today) throw new AppException("The invoice date cannot be in the future.");
        var vendorId = input.VendorId ?? await CivilDefenseVendorIdAsync();
        if (await Db.VendorBills.AnyAsync(b => b.VendorId == vendorId && b.BillNo == input.BillNo.Trim()))
            throw new BusinessRuleException($"Invoice {input.BillNo} is already recorded.");
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        await using var tx = await Db.Database.BeginTransactionAsync();
        var bill = await billing.CreateVendorBillAsync(c.BranchId, vendorId, input.BillNo.Trim(), billDate, input.DueDate ?? billDate.AddDays(7), FeeSource, r.Id,
            new[] { new BillingService.BillLineInput("inspection", null, input.Description ?? $"Civil Defense fees – certificate {c.CertificateNo ?? "(new)"}", 1, "each", input.Amount, input.VatRate ?? 0, null) },
            $"Civil Defense certificate {(c.CertificateNo ?? "request")} – renewal #{r.Id}");
        Activity.Log("cd_certificate", c.Id, "fees", $"Civil Defense fees invoice {bill.BillNo} recorded ({bill.TotalAmount:0.##})", c.BuildingId, c.BranchId);
        await Db.SaveChangesAsync();
        // fees actually charged (before VAT) vs. what the owner was invoiced: bill the difference when higher
        var cancelledBill = await Lookups.IdOrNullAsync(LookupTypes.BillStatus, "cancelled");
        var actual = await Db.VendorBills.Where(b => b.SourceType == FeeSource && b.SourceId == r.Id && b.StatusId != cancelledBill).SumAsync(b => (decimal?)(b.Subtotal ?? b.TotalAmount) ?? 0);
        var (supplement, over) = await OwnerBilling.ReconcileCdFeesAsync(c, r, actual);
        await tx.CommitAsync();
        return Ok(new { bill.Id, bill.BillNo, bill.TotalAmount, supplementInvoiceId = supplement?.Id, supplementInvoiceNo = supplement?.InvoiceNo,
                        supplementAmount = supplement?.TotalAmount, overInvoiced = over });
    }

    /// <summary>Pay a fees invoice (vendor payment allocated to the bill). When every fees invoice is paid, step 2 can be completed.</summary>
    [HttpPost("renewals/{renewalId:long}/fees/{billId:long}/pay")]
    public async Task<IActionResult> PayFee(long renewalId, long billId, [FromBody] FeePaymentInput input)
    {
        var bill = await Db.VendorBills.FirstOrDefaultAsync(b => b.Id == billId && b.SourceType == FeeSource && b.SourceId == renewalId)
                   ?? throw new NotFoundException("VendorBill", billId);
        EnsureBranchAccess(bill.BranchId);
        var row = (await FeeBillsAsync(renewalId)).FirstOrDefault(x => x.Id == billId) ?? throw new BusinessRuleException("The invoice is cancelled.");
        if (row.Balance <= 0) throw new BusinessRuleException("This invoice is already paid.");
        if (input.Amount <= 0 || input.Amount > row.Balance) throw new AppException($"The amount must be between 0 and the balance ({row.Balance:0.##}).");
        var date = input.Date ?? Today;
        if (date > Today) throw new AppException("The payment date cannot be in the future.");
        if (date < bill.BillDate) throw new AppException("The payment date cannot be before the invoice date.");
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        await using var tx = await Db.Database.BeginTransactionAsync();
        var vp = new VendorPayment
        {
            BranchId = bill.BranchId, VendorId = bill.VendorId, PaymentNo = await Numbers.NextAsync(NumberPrefixes.VendorPayment, date: date),
            PaymentDate = date, Amount = input.Amount, CurrencyId = bill.CurrencyId,
            PaymentMethodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "bank_transfer", field: "method"),
            ReferenceNo = input.Reference, PaidBy = Me.UserId, Notes = input.Notes ?? $"Civil Defense fees {bill.BillNo}",
            StatusId = await LkId(LookupTypes.PaymentStatus, "completed"),
        };
        Db.VendorPayments.Add(vp);
        await Db.SaveChangesAsync();
        vp.JournalEntryId = (await Posting.PostVendorPaymentAsync(vp))?.Id;
        Db.VendorPaymentAllocations.Add(new VendorPaymentAllocation { VendorPaymentId = vp.Id, VendorBillId = bill.Id, Amount = input.Amount });
        await Db.SaveChangesAsync();
        await billing.RecalcBillsAsync(new[] { bill.Id });
        await tx.CommitAsync();
        return Ok(new { vp.Id, vp.PaymentNo });
    }

    // ------------------------------------------------------------------ step remarks + attachments (e.g. step 5 «معالجة الملاحظات»)

    public class StepNoteForm
    {
        public string? Remarks { get; set; }
        public List<IFormFile>? Files { get; set; }
    }
    private FileStorageService Files => HttpContext.RequestServices.GetRequiredService<FileStorageService>();
    private static string StepEntity(int step) => $"cd_renewal_s{step}";

    /// <summary>Add a remark and/or files to one step of a renewal (files are documents of entity_type «cd_renewal_s{step}»).</summary>
    [HttpPost("renewals/{renewalId:long}/steps/{step:int}/notes")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> AddStepNote(long renewalId, int step, [FromForm] StepNoteForm form)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        var c = await Db.CdCertificates.FirstAsync(x => x.Id == r.CertificateId);
        EnsureBranchAccess(c.BranchId);
        if (step is < 1 or > 6) throw new AppException("Unknown step.");
        var files = (form.Files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();
        var note = string.IsNullOrWhiteSpace(form.Remarks) ? null : form.Remarks.Trim();
        if (files.Count == 0 && note == null) throw new AppException("Add a remark or at least one file.");
        if (files.Count > 10) throw new AppException("You can attach up to 10 files.");
        var saved = new List<(IFormFile File, string Path, long Size)>();
        foreach (var f in files) { var (path, size) = await Files.SaveAsync(f, "cd_renewal"); saved.Add((f, path, size)); }
        foreach (var (f, path, size) in saved)
        {
            var isImage = (f.ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
            Db.Documents.Add(new Document
            {
                BranchId = c.BranchId, EntityType = StepEntity(step), EntityId = r.Id, FileName = Path.GetFileName(f.FileName), FilePath = path, MimeType = f.ContentType,
                SizeBytes = size, DocumentTypeId = await Lk(LookupTypes.DocumentType, null, isImage ? "photo" : "report", field: "documentType"),
            });
        }
        Activity.Log("cd_renewal", r.Id, "step_note", note ?? $"{saved.Count} file(s) attached", c.BuildingId, c.BranchId,
            newValues: new { step, files = saved.Count });
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Remarks and files of every step of a renewal.</summary>
    [HttpGet("renewals/{renewalId:long}/steps")]
    public async Task<IActionResult> StepNotes(long renewalId)
    {
        var r = await Db.CdRenewals.AsNoTracking().FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
        var entities = Enumerable.Range(1, 6).Select(StepEntity).ToList();
        var docs = await Db.Documents.AsNoTracking().Where(d => entities.Contains(d.EntityType) && d.EntityId == renewalId).OrderBy(d => d.Id)
            .Select(d => new { d.Id, d.EntityType, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt }).ToListAsync();
        var logs = await Db.ActivityLogs.AsNoTracking().Where(a => a.EntityType == "cd_renewal" && a.EntityId == renewalId && a.Action == "step_note")
            .OrderBy(a => a.OccurredAt).Select(a => new { a.Description, a.NewValues, a.OccurredAt, user = a.User!.NameAr ?? a.User.NameEn }).ToListAsync();
        int StepOf(string? json) { try { return json == null ? 0 : System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("step").GetInt32(); } catch { return 0; } }
        return Ok(Enumerable.Range(1, 6).Select(n => new
        {
            step = n,
            notes = logs.Where(l => StepOf(l.NewValues) == n).Select(l => new { text = l.Description, at = l.OccurredAt, l.user }),
            files = docs.Where(d => d.EntityType == StepEntity(n)).Select(d => new { d.Id, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt }),
        }));
    }

    [HttpGet("renewals/attachments/{docId:long}")]
    public async Task<IActionResult> DownloadStepFile(long docId)
    {
        var d = await Db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == docId && x.EntityType.StartsWith("cd_renewal_s")) ?? throw new NotFoundException("Document", docId);
        if (d.BranchId != null) EnsureBranchAccess(d.BranchId.Value);
        var full = Files.FullPath(d.FilePath) ?? throw new NotFoundException("File", d.FileName ?? docId.ToString());
        return PhysicalFile(full, d.MimeType ?? "application/octet-stream", d.FileName);
    }

    // ------------------------------------------------------------------ certificate documents (the certificate copy, plans …)

    [HttpGet("{id:long}/documents")]
    public async Task<IActionResult> CertDocuments(long id)
    {
        var c = await Db.CdCertificates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        EnsureBranchAccess(c.BranchId);
        return Ok(await Db.Documents.AsNoTracking().Where(d => d.EntityType == "cd_certificate" && d.EntityId == id).OrderBy(d => d.Id)
            .Select(d => new { d.Id, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt }).ToListAsync());
    }

    [HttpPost("{id:long}/documents")]
    [RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> AddCertDocuments(long id, [FromForm] StepNoteForm form)
    {
        var c = await Db.CdCertificates.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("CdCertificate", id);
        EnsureBranchAccess(c.BranchId);
        var files = (form.Files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();
        if (files.Count == 0) throw new AppException("Choose at least one file.");
        if (files.Count > 10) throw new AppException("You can attach up to 10 files.");
        var saved = new List<(IFormFile File, string Path, long Size)>();
        foreach (var f in files) { var (path, size) = await Files.SaveAsync(f, "cd_certificate"); saved.Add((f, path, size)); }
        foreach (var (f, path, size) in saved)
            Db.Documents.Add(new Document
            {
                BranchId = c.BranchId, EntityType = "cd_certificate", EntityId = c.Id, FileName = Path.GetFileName(f.FileName), FilePath = path, MimeType = f.ContentType,
                SizeBytes = size, DocumentTypeId = await Lk(LookupTypes.DocumentType, null, "certificate", field: "documentType"),
            });
        Activity.Log("cd_certificate", c.Id, "document", $"{saved.Count} document(s) attached to certificate {c.CertificateNo}", c.BuildingId, c.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("documents/{docId:long}")]
    public async Task<IActionResult> DownloadCertDocument(long docId)
    {
        var d = await Db.Documents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == docId && x.EntityType == "cd_certificate") ?? throw new NotFoundException("Document", docId);
        if (d.BranchId != null) EnsureBranchAccess(d.BranchId.Value);
        var full = Files.FullPath(d.FilePath) ?? throw new NotFoundException("File", d.FileName ?? docId.ToString());
        return PhysicalFile(full, d.MimeType ?? "application/octet-stream", d.FileName);
    }

    /// <summary>The Civil Defense authority as a payee: vendor coded «CIVIL-DEFENSE», created when missing.</summary>
    private async Task<long> CivilDefenseVendorIdAsync()
    {
        var id = await Db.Vendors.Where(v => v.Party!.PartyCode == "CIVIL-DEFENSE").Select(v => (long?)v.Id).FirstOrDefaultAsync();
        if (id != null) return id.Value;
        var parties = HttpContext.RequestServices.GetRequiredService<PartyService>();
        var p = await parties.NewAsync(null, new PartyService.PartyInput { PartyType = "company", NameEn = "Civil Defense", NameAr = "الدفاع المدني" });
        p.PartyCode = "CIVIL-DEFENSE";
        var v = await parties.EnsureVendorAsync(p, null, await LkId(LookupTypes.VendorType, "service_vendor"));
        return v.Id;
    }

    private async Task Apply(CdCertificate c, CertInput i)
    {
        c.CertificateNo = i.CertificateNo; c.LicenseNo = i.LicenseNo; c.IssuedBy = i.IssuedBy; c.IssueDate = i.IssueDate; c.ExpiryDate = i.ExpiryDate;
        c.Scope = i.Scope; c.InspectorName = i.InspectorName; c.InspectionDate = i.InspectionDate; c.Notes = i.Notes;
        c.CertTypeId = await Lk(LookupTypes.CdCertType, null, i.CertType ?? "gas_installation", field: "certType");
        c.StatusId = await Lk(LookupTypes.CdStatus, null, i.Status ?? (i.ExpiryDate != null && i.ExpiryDate < Today ? "expired" : "valid"), field: "status");
    }
}

[Route("api/cd-renewals")]
[PermissionModule("civil_defense")]
public class CdRenewalsController : CrudController<CdRenewal> { }
