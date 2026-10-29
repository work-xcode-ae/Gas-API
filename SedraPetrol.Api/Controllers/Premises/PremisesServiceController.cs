using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Restaurants &amp; Villas — service requests of a villa / restaurant (additive; the building service-request endpoints are unchanged).
/// A villa has no units, so its requests are opened here. Rule (Hisham, 25 Oct 2026): a service request can only be opened while
/// the site has a <b>valid maintenance (AMC) contract</b>: an AMC of the site's building (a villa: its own host building) that is
/// active and covers today (start ≤ today ≤ end). The request is stored in the normal <c>service_request</c> table, so it shows in
/// the building / villa «طلبات الخدمة» tab and the Service Requests page and follows the normal assign / complete workflow.
/// </summary>
[Route("api/premises")]
[PermissionModule("service_requests")]
public class PremisesServiceController : ApiControllerBase
{
    public record PremisesSrInput(string Description, string? Priority, string? Notes, long? AssignedTo, long? AssignedVendorId);

    /// <summary>The maintenance contract that makes the site eligible for service requests today, or why it is not.</summary>
    [HttpGet("{id:long}/maintenance-coverage")]
    public async Task<IActionResult> Coverage(long id)
    {
        var p = await Site(id);
        var (contract, reason) = await ValidAmc(p);
        return Ok(new
        {
            valid = contract != null,
            reason,
            contract = contract == null ? null : new
            {
                contract.Id, contract.ContractNo, contract.StartDate, contract.EndDate, contract.EmergencyIncluded, contract.ResponseTimeHours,
                contract.VisitsTotal, daysLeft = contract.EndDate == null ? (int?)null : contract.EndDate.Value.DayNumber - Today.DayNumber,
                provider = await Db.Vendors.Where(v => v.Id == contract.VendorId).Select(v => v.Party!.NameAr ?? v.Party.NameEn).FirstOrDefaultAsync(),
            },
        });
    }

    /// <summary>Open a service request for a villa / restaurant — refused when there is no valid maintenance contract.</summary>
    [HttpPost("{id:long}/service-requests")]
    public async Task<IActionResult> Create(long id, [FromBody] PremisesSrInput input)
    {
        var p = await Site(id);
        if (!p.IsActive) throw new BusinessRuleException($"{p.Code} is inactive.");
        if (string.IsNullOrWhiteSpace(input.Description)) throw new AppException("Description is required.");
        var (amc, reason) = await ValidAmc(p);
        if (amc == null) throw new BusinessRuleException(reason!, "no_valid_amc");

        // the customer of the site (villa owner / restaurant owner) when it already has the customer role
        long? tenantId = await Db.Tenants.AnyAsync(t => t.Id == p.OwnerPartyId) ? p.OwnerPartyId : null;
        var assigned = input.AssignedTo != null || input.AssignedVendorId != null;
        var r = new ServiceRequest
        {
            BranchId = p.BranchId, RequestNo = await Numbers.NextAsync(NumberPrefixes.ServiceRequest), BuildingId = p.BuildingId, UnitId = p.UnitId,
            TenantId = tenantId, Description = input.Description.Trim(),
            PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"),
            AssignedTo = input.AssignedTo, AssignedVendorId = input.AssignedVendorId, RequestedAt = DateTimeOffset.UtcNow,
            Notes = string.Join("\n", new[] { $"{p.Code} — عقد الصيانة {amc.ContractNo}", input.Notes?.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x))),
            StatusId = await LkId(LookupTypes.SrStatus, assigned ? "in_progress" : "pending"),
        };
        Db.ServiceRequests.Add(r);
        await Db.SaveChangesAsync();
        Activity.Log("service_request", r.Id, "create", $"Service request {r.RequestNo} ({p.Code}, AMC {amc.ContractNo})", p.BuildingId, p.BranchId,
            newValues: new { to = "pending", note = $"{r.Description}\n{p.Code} — عقد الصيانة {amc.ContractNo}" });
        if (assigned) Activity.Log("service_request", r.Id, "assign", $"{r.RequestNo} assigned", p.BuildingId, p.BranchId, newValues: new { from = "pending", to = "in_progress" });
        Activity.Log(p.PremisesType, p.Id, "update", $"{p.Code}: service request {r.RequestNo} opened (AMC {amc.ContractNo})", p.BuildingId, p.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { r.Id, r.RequestNo, amcContractNo = amc.ContractNo });
    }

    /// <summary>
    /// Who created / last changed each service request of the site's building, and how many logged steps it has
    /// (from the activity log; the audit columns are the fallback). Used by the villa request tracker cards.
    /// </summary>
    [HttpGet("{id:long}/service-requests/audit")]
    public async Task<IActionResult> Audit(long id)
    {
        var p = await Site(id);
        var q = Db.ServiceRequests.AsNoTracking().Where(r => r.BuildingId == p.BuildingId);
        if (p.PremisesType != PremisesTypes.Villa)
        {
            // a restaurant shares its building with other units → only its own requests: those opened for it here
            // (linked in the activity log when created) and, when it occupies a unit, the requests of that unit
            var own = await OwnRequestNos(p);
            q = q.Where(r => own.Contains(r.RequestNo) || (p.UnitId != null && r.UnitId == p.UnitId));
        }
        var reqs = await q.Select(r => new { r.Id, r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy }).ToListAsync();
        var ids = reqs.Select(r => r.Id).ToList();
        var logs = await Db.ActivityLogs.AsNoTracking().Where(a => a.EntityType == "service_request" && a.EntityId != null && ids.Contains(a.EntityId.Value))
            .Select(a => new { a.Id, EntityId = a.EntityId!.Value, a.OccurredAt, a.UserId }).ToListAsync();
        var userIds = logs.Where(l => l.UserId != null).Select(l => l.UserId!.Value)
            .Concat(reqs.SelectMany(r => new[] { r.CreatedBy, r.UpdatedBy }).Where(u => u != null).Select(u => u!.Value)).Distinct().ToList();
        var users = await Db.AppUsers.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.NameAr ?? u.NameEn ?? u.Email }).ToDictionaryAsync(u => u.Id, u => u.Name);
        string? N(long? u) => u != null && users.TryGetValue(u.Value, out var n) ? n : null;
        return Ok(reqs.Select(r =>
        {
            var l = logs.Where(x => x.EntityId == r.Id).OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ToList();
            var first = l.FirstOrDefault(); var last = l.LastOrDefault();
            return new
            {
                r.Id,
                createdAt = first?.OccurredAt ?? r.CreatedAt, createdBy = N(first?.UserId ?? r.CreatedBy),
                updatedAt = last?.OccurredAt ?? r.UpdatedAt ?? r.CreatedAt, updatedBy = N(last?.UserId ?? r.UpdatedBy ?? r.CreatedBy),
                steps = l.Count,
            };
        }));
    }

    /// <summary>Request numbers opened for this site through <see cref="Create"/> (from its own activity-log entries).</summary>
    private async Task<List<string>> OwnRequestNos(Premises p)
    {
        var descs = await Db.ActivityLogs.AsNoTracking()
            .Where(a => a.EntityType == p.PremisesType && a.EntityId == p.Id && a.Description != null && a.Description.Contains(": service request "))
            .Select(a => a.Description!).ToListAsync();
        return descs.Select(d => System.Text.RegularExpressions.Regex.Match(d, @": service request (\S+) opened"))
            .Where(m => m.Success).Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    private async Task<Premises> Site(long id)
    {
        var p = await Db.Premises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Premises", id);
        EnsureBranchAccess(p.BranchId);
        return p;
    }

    /// <summary>Active AMC of the site's building covering today; otherwise null + the reason (Arabic, shown to the user).</summary>
    private async Task<(AmcContract? Contract, string? Reason)> ValidAmc(Premises p)
    {
        var active = await Lookups.IdOrNullAsync(LookupTypes.AmcStatus, "active");
        var all = await Db.AmcContracts.AsNoTracking().Where(a => a.BuildingId == p.BuildingId).OrderByDescending(a => a.EndDate).ToListAsync();
        var ok = all.FirstOrDefault(a => a.IsActive && a.StatusId == active && a.StartDate <= Today && (a.EndDate == null || a.EndDate >= Today));
        if (ok != null) return (ok, null);
        var what = p.PremisesType == PremisesTypes.Villa ? "للفيلا" : "للمبنى الذي يقع فيه المطعم";
        if (all.Count == 0) return (null, $"لا يوجد عقد صيانة {what}. أضف عقد صيانة (AMC) ساري أولاً.");
        var future = all.FirstOrDefault(a => a.IsActive && a.StatusId == active && a.StartDate > Today);
        if (future != null) return (null, $"عقد الصيانة {future.ContractNo} يبدأ في {future.StartDate:yyyy-MM-dd} — لا يمكن فتح طلب خدمة قبل بدايته.");
        var last = all[0];
        return (null, $"عقد الصيانة {last.ContractNo} غير ساري (انتهى أو أُلغي في {last.EndDate:yyyy-MM-dd}). جدّد العقد لفتح طلبات الخدمة.");
    }
}
