using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Service requests (complaints, leaks, repairs) per building / unit / tenant / meter.</summary>
[Route("api/service-requests")]
[PermissionModule("service_requests")]
public class ServiceRequestsController : ApiControllerBase
{
    public record SrInput(long BuildingId, long? UnitId, long? TenantId, long? MeterId, string Description, string? Priority, long? AssignedTo, long? AssignedVendorId, string? Notes);
    public record AssignInput(long? AssignedTo, long? AssignedVendorId);
    public record StatusInput(string Status, string? Notes);

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
                R = r, BCode = r.Building!.Code, r.Building.NameEn, r.Building.NameAr, UnitNo = r.Unit!.UnitNo, TenantEn = r.Tenant!.NameEn, TenantAr = r.Tenant.NameAr,
                MeterNo = r.Meter!.MeterNo, Assignee = r.AssignedToUser!.NameAr ?? r.AssignedToUser.NameEn, Vendor = r.AssignedVendor!.NameAr ?? r.AssignedVendor.NameEn,
            }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => (object)new
        {
            x.R.Id, no = x.R.RequestNo, requestNo = x.R.RequestNo, x.R.BuildingId, buildingCode = x.BCode, building = Loc(x.NameEn, x.NameAr),
            location = $"{Loc(x.NameEn, x.NameAr)}{(x.UnitNo == null ? "" : " – " + x.UnitNo)}", x.R.UnitId, unit = x.UnitNo,
            x.R.TenantId, tenant = Loc(x.TenantEn, x.TenantAr), x.R.MeterId, meterNo = x.MeterNo, x.R.Description, type = x.R.Description,
            priority = Code(x.R.PriorityId), status = Code(x.R.StatusId), statusLabel = Label(x.R.StatusId), x.R.AssignedTo, assignee = x.Assignee,
            x.R.AssignedVendorId, vendor = x.Vendor, x.R.RequestedAt, x.R.ClosedAt, x.R.Notes,
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var r = await Db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        return Ok(new { request = r, priority = Code(r.PriorityId), status = Code(r.StatusId) });
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
        var r = new ServiceRequest
        {
            BranchId = b.BranchId, RequestNo = await Numbers.NextAsync(NumberPrefixes.ServiceRequest), BuildingId = b.Id, UnitId = input.UnitId, TenantId = tenantId,
            MeterId = input.MeterId, Description = input.Description.Trim(), PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"),
            AssignedTo = input.AssignedTo, AssignedVendorId = input.AssignedVendorId, Notes = input.Notes, RequestedAt = DateTimeOffset.UtcNow,
            StatusId = await LkId(LookupTypes.SrStatus, input.AssignedTo != null || input.AssignedVendorId != null ? "in_progress" : "pending"),
        };
        Db.ServiceRequests.Add(r);
        await Db.SaveChangesAsync();
        Activity.Log("service_request", r.Id, "create", $"Service request {r.RequestNo}", b.Id, b.BranchId);
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

    [HttpPost("{id:long}/assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignInput input)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        r.AssignedTo = input.AssignedTo; r.AssignedVendorId = input.AssignedVendorId;
        if (await Lookups.CodeAsync(r.StatusId) == "pending") r.StatusId = await LkId(LookupTypes.SrStatus, "in_progress");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Change status: pending | in_progress | completed | cancelled.</summary>
    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] StatusInput input)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        EnsureBranchAccess(r.BranchId);
        r.StatusId = await Lk(LookupTypes.SrStatus, null, input.Status, true, "status");
        r.ClosedAt = input.Status is "completed" or "cancelled" ? DateTimeOffset.UtcNow : null;
        if (!string.IsNullOrWhiteSpace(input.Notes)) r.Notes = $"{r.Notes}\n{input.Notes}".Trim();
        Activity.Log("service_request", r.Id, "status_change", $"{r.RequestNo} → {input.Status}", r.BuildingId, r.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var r = await Db.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("ServiceRequest", id);
        Db.ServiceRequests.Remove(r);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

/// <summary>Civil Defense certificates and their renewal process.</summary>
[Route("api/cd-certificates")]
[PermissionModule("civil_defense")]
public class CdCertificatesController : ApiControllerBase
{
    public record CertInput(long BuildingId, string? CertificateNo, string? CertType, string? LicenseNo, string? IssuedBy, DateOnly? IssueDate, DateOnly? ExpiryDate,
        string? Scope, string? InspectorName, DateOnly? InspectionDate, string? Status, string? Notes);
    public record RenewalInput(DateOnly? StartedAt, short? CurrentStep, string? InspectorName, string? InspectorPhone, string? InspectorDept, DateOnly? RenewalDate, string? Remarks);
    public record CompleteRenewalInput(string CertificateNo, DateOnly IssueDate, DateOnly NewExpiryDate, string? LicenseNo, string? Remarks);

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
        var renewals = await Db.CdRenewals.AsNoTracking().Where(r => r.CertificateId == id).OrderByDescending(r => r.Id).ToListAsync();
        return Ok(new { certificate = c, certType = Code(c.CertTypeId), status = Code(c.StatusId), renewals = renewals.Select(r => new { renewal = r, status = Code(r.StatusId) }) });
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
        Db.CdRenewals.Add(r);
        c.StatusId = await LkId(LookupTypes.CdStatus, "renewal_in_progress");
        await Db.SaveChangesAsync();
        return Ok(new { r.Id });
    }

    [HttpPut("renewals/{renewalId:long}")]
    public async Task<IActionResult> UpdateRenewal(long renewalId, [FromBody] RenewalInput input)
    {
        var r = await Db.CdRenewals.FirstOrDefaultAsync(x => x.Id == renewalId) ?? throw new NotFoundException("CdRenewal", renewalId);
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
