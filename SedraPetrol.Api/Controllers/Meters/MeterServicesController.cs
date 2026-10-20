using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Meter service orders (MeterServiceModal): connect, disconnect, replace, report_issue.
/// Completing an order applies its effect (meter status, readings, re-assignment, optional fee invoice).
/// </summary>
[Route("api/meter-services")]
[PermissionModule("meters")]
public class MeterServicesController : ApiControllerBase
{
    private MeterOps Ops => HttpContext.RequestServices.GetRequiredService<MeterOps>();

    public record CreateInput(long MeterId, string ServiceType, string? IssueType, string? Priority, string? Reason, DateOnly? RequestedDate,
        decimal? BeforeReading, long? NewMeterId, bool? SafetyCheckPassed, bool? TenantNotified, long? TechnicianId,
        bool CompleteNow = false, decimal? AfterReading = null, decimal? FeeAmount = null);
    public record CompleteInput(DateOnly? CompletedDate, decimal? BeforeReading, decimal? AfterReading, bool? SafetyCheckPassed, bool? TenantNotified,
        long? NewMeterId, decimal? FeeAmount, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? meterId, [FromQuery] long? buildingId, [FromQuery] string? type,
        [FromQuery] string? status, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.MeterServices.AsNoTracking().Where(s => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(s.BranchId));
        if (meterId != null) query = query.Where(s => s.MeterId == meterId || s.NewMeterId == meterId);
        if (buildingId != null) query = query.Where(s => Db.MeterAssignments.Any(a => a.MeterId == s.MeterId && a.Unit!.BuildingId == buildingId));
        if (!string.IsNullOrEmpty(type)) { var t = await Lookups.IdOrNullAsync(LookupTypes.MeterServiceType, type); query = query.Where(s => s.ServiceTypeId == t); }
        if (!string.IsNullOrEmpty(status)) { var t = await Lookups.IdOrNullAsync(LookupTypes.ServiceStatus, status); query = query.Where(s => s.StatusId == t); }
        var page = await query.OrderByDescending(s => s.Id)
            .Select(s => new { S = s, s.Meter!.MeterNo, NewMeterNo = s.NewMeter!.MeterNo, Tech = s.Technician!.NameAr ?? s.Technician.NameEn, ContractNo = s.Contract!.ContractNo })
            .ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(x => Dto(x.S, x.MeterNo, x.NewMeterNo, x.Tech, x.ContractNo)).ToList(), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var x = await Db.MeterServices.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new { S = s, s.Meter!.MeterNo, NewMeterNo = s.NewMeter!.MeterNo, Tech = s.Technician!.NameAr ?? s.Technician.NameEn, ContractNo = s.Contract!.ContractNo })
            .FirstOrDefaultAsync() ?? throw new NotFoundException("MeterService", id);
        return Ok(Dto(x.S, x.MeterNo, x.NewMeterNo, x.Tech, x.ContractNo));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        await LoadLookups();
        var meter = await Db.Meters.FirstOrDefaultAsync(m => m.Id == input.MeterId) ?? throw new NotFoundException("Meter", input.MeterId);
        EnsureBranchAccess(meter.BranchId);
        var typeId = (await Lk(LookupTypes.MeterServiceType, null, input.ServiceType, true, "serviceType"))!.Value;
        var type = input.ServiceType.ToLowerInvariant();
        if (type == "replace" && input.NewMeterId == null && input.CompleteNow) throw new AppException("newMeterId is required to replace a meter.");
        var unitId = await Db.MeterAssignments.Where(a => a.MeterId == meter.Id && a.ToDate == null).Select(a => (long?)a.UnitId).FirstOrDefaultAsync();
        var contract = unitId == null ? null : await Db.SupplyContracts.Where(c => c.IsActive && c.UnitId == unitId).FirstOrDefaultAsync();

        await using var tx = await Db.Database.BeginTransactionAsync();
        var s = new MeterService
        {
            BranchId = meter.BranchId ?? 0, ServiceNo = await Numbers.NextAsync(NumberPrefixes.MeterService), MeterId = meter.Id, ContractId = contract?.Id,
            ServiceTypeId = typeId, IssueTypeId = await Lk(LookupTypes.MeterIssueType, null, input.IssueType, field: "issueType"),
            PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"), Reason = input.Reason,
            RequestedDate = input.RequestedDate ?? Today, BeforeReading = input.BeforeReading, NewMeterId = input.NewMeterId,
            SafetyCheckPassed = input.SafetyCheckPassed, TenantNotified = input.TenantNotified, TechnicianId = input.TechnicianId,
            StatusId = await LkId(LookupTypes.ServiceStatus, "pending"),
        };
        Db.MeterServices.Add(s);
        if (type == "report_issue") await Ops.SetStatusAsync(meter, "fault");
        await Db.SaveChangesAsync();
        var buildingId = unitId == null ? null : await Db.Units.Where(u => u.Id == unitId).Select(u => (long?)u.BuildingId).FirstOrDefaultAsync();
        Activity.Log("meter_service", s.Id, "create", $"{s.ServiceNo}: {type} requested for meter {meter.MeterNo}", buildingId, s.BranchId);
        await Db.SaveChangesAsync();

        if (input.CompleteNow)
            await CompleteInternal(s, meter, new CompleteInput(input.RequestedDate ?? Today, input.BeforeReading, input.AfterReading, input.SafetyCheckPassed,
                input.TenantNotified, input.NewMeterId, input.FeeAmount, null));
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = s.Id }, new { s.Id, s.ServiceNo });
    }

    /// <summary>Complete the order and apply its effect.</summary>
    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, [FromBody] CompleteInput input)
    {
        await LoadLookups();
        var s = await Db.MeterServices.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MeterService", id);
        EnsureBranchAccess(s.BranchId);
        if (Code(s.StatusId) is "completed" or "cancelled") throw new BusinessRuleException($"The service order is already {Code(s.StatusId)}.");
        var meter = await Db.Meters.FirstAsync(m => m.Id == s.MeterId);
        await using var tx = await Db.Database.BeginTransactionAsync();
        await CompleteInternal(s, meter, input);
        await tx.CommitAsync();
        return await Get(id);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        await LoadLookups();
        var s = await Db.MeterServices.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MeterService", id);
        if (Code(s.StatusId) == "completed") throw new BusinessRuleException("A completed service order cannot be cancelled.");
        s.StatusId = await LkId(LookupTypes.ServiceStatus, "cancelled");
        if (Code(s.ServiceTypeId) == "report_issue")
        {
            var meter = await Db.Meters.FirstAsync(m => m.Id == s.MeterId);
            if (Code(meter.StatusId) == "fault") await Ops.SetStatusAsync(meter, "active");
        }
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private async Task CompleteInternal(MeterService s, Meter meter, CompleteInput input)
    {
        var type = Code(s.ServiceTypeId) ?? await Lookups.CodeAsync(s.ServiceTypeId);
        var date = input.CompletedDate ?? Today;
        var when = new DateTimeOffset(date.ToDateTime(TimeOnly.FromDateTime(DateTime.Now)));
        s.BeforeReading = input.BeforeReading ?? s.BeforeReading;
        s.AfterReading = input.AfterReading ?? s.AfterReading;
        s.SafetyCheckPassed = input.SafetyCheckPassed ?? s.SafetyCheckPassed;
        s.TenantNotified = input.TenantNotified ?? s.TenantNotified;
        s.NewMeterId = input.NewMeterId ?? s.NewMeterId;
        if (!string.IsNullOrWhiteSpace(input.Notes)) s.Reason = $"{s.Reason}\n{input.Notes}".Trim();

        switch (type)
        {
            case "disconnect":
                if (s.BeforeReading != null) await Ops.AddReadingAsync(meter, s.BeforeReading.Value, when, "manual", $"Disconnection {s.ServiceNo}");
                await Ops.SetStatusAsync(meter, "disconnected");
                break;
            case "connect":
                if (s.SafetyCheckPassed == false) throw new BusinessRuleException("The safety check must pass before connecting the meter.");
                if (s.AfterReading != null) await Ops.AddReadingAsync(meter, s.AfterReading.Value, when, "manual", $"Connection {s.ServiceNo}");
                await Ops.SetStatusAsync(meter, "active");
                break;
            case "replace":
            {
                if (s.NewMeterId == null) throw new AppException("newMeterId is required to replace a meter.");
                var newMeter = await Db.Meters.FirstOrDefaultAsync(m => m.Id == s.NewMeterId) ?? throw new NotFoundException("Meter", s.NewMeterId);
                var assignment = await Db.MeterAssignments.FirstOrDefaultAsync(a => a.MeterId == meter.Id && a.ToDate == null)
                                 ?? throw new BusinessRuleException("The old meter is not assigned to a unit.");
                if (s.BeforeReading == null) throw new AppException("The final reading of the old meter is required.");
                if (s.AfterReading == null) throw new AppException("The initial reading of the new meter is required.");
                // 1) final reading of the old meter, 2) close its assignment, 3) install the new meter, 4) its initial reading.
                // Both readings are linked to the unit and its active tenancy, so the tenancy keeps consumption from both meters.
                await Ops.AddReadingAsync(meter, s.BeforeReading.Value, when, "manual", $"Final reading – meter replaced ({s.ServiceNo})",
                    type: "replacement_out", unitId: assignment.UnitId);
                assignment.ToDate = date;
                await Ops.SetStatusAsync(meter, "maintenance");
                await Db.SaveChangesAsync();
                newMeter.InitialReading ??= s.AfterReading;
                await Ops.AssignAsync(newMeter, assignment.UnitId, date);
                await Ops.AddReadingAsync(newMeter, s.AfterReading.Value, when.AddSeconds(1), "manual", $"Initial reading – installed ({s.ServiceNo})",
                    type: "replacement_in", unitId: assignment.UnitId);
                await Ops.SetStatusAsync(newMeter, "active");
                break;
            }
            case "report_issue":
                await Ops.SetStatusAsync(meter, "active");
                break;
        }

        // the contract's disconnection fee is already on the first invoice — only an explicit fee is billed here
        var feeAmount = input.FeeAmount;
        if ((feeAmount ?? 0) > 0)
        {
            var contract = s.ContractId == null ? null : await Db.SupplyContracts.Include(c => c.Unit).FirstOrDefaultAsync(c => c.Id == s.ContractId);
            if (contract == null) throw new BusinessRuleException("A service fee needs an active supply contract to bill.");
            var feeType = type switch { "connect" => "connection", "disconnect" => "disconnection", "replace" => "maintenance", _ => "maintenance" };
            var inv = await HttpContext.RequestServices.GetRequiredService<BillingService>().CreateInvoiceAsync(new InvoiceDraft(
                contract.BranchId, contract.TenantId, contract.Unit!.BuildingId, contract.Id, type is "connect" ? "connection" : "maintenance", null, date, null, null, null, null,
                $"Meter service {s.ServiceNo}", new[] { new InvoiceLineInput(feeType, null, $"Meter {type} – {s.ServiceNo}", 1, "each", feeAmount!.Value, null) }, true));
            s.FeeInvoiceItemId = await Db.InvoiceItems.Where(i => i.InvoiceId == inv.Id).Select(i => (long?)i.Id).FirstOrDefaultAsync();
        }

        s.CompletedDate = date;
        s.StatusId = await LkId(LookupTypes.ServiceStatus, "completed");
        Activity.Log("meter_service", s.Id, "status_change", $"{s.ServiceNo}: {type} completed", null, s.BranchId);
        await Db.SaveChangesAsync();
    }

    private object Dto(MeterService s, string? meterNo, string? newMeterNo, string? tech, string? contractNo) => new
    {
        s.Id, s.ServiceNo, s.MeterId, meterNo, s.ContractId, contractNo, type = Code(s.ServiceTypeId), typeLabel = Label(s.ServiceTypeId),
        issueType = Code(s.IssueTypeId), issueTypeLabel = Label(s.IssueTypeId), priority = Code(s.PriorityId), s.Reason, s.RequestedDate, s.CompletedDate,
        s.BeforeReading, s.AfterReading, s.NewMeterId, newMeterNo, s.SafetyCheckPassed, s.TenantNotified, s.TechnicianId, technician = tech,
        s.FeeInvoiceItemId, status = Code(s.StatusId), statusLabel = Label(s.StatusId),
    };
}
