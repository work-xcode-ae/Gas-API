using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Gas meters (MeterRecord / AddMeterModal / MeterDetailDrawer / MeterReadingModal).</summary>
[Route("api/meters")]
[PermissionModule("meters")]
public class MetersController : ApiControllerBase
{
    private MeterOps Ops => HttpContext.RequestServices.GetRequiredService<MeterOps>();

    public record MeterInput(long? BranchId, string? MeterNo, string? Serial, long? UnitId, long? MeterTypeId, string? Type, string? Brand, string? Model,
        DateOnly? InstallDate, decimal? InitialReading, DateOnly? CalibrationDate, string? Location, string? SimCard, string? Status, string? Notes);
    public record AssignInput(long UnitId, DateOnly? FromDate);
    /// <summary>Add several meters of the same model to stock: one serial per meter.</summary>
    public record StockBulkInput(long? BranchId, List<string> Serials, string? Type, string? Brand, string? Model, DateOnly? CalibrationDate, decimal? InitialReading, string? Notes);
    public record UnassignInput(DateOnly? ToDate, decimal? FinalReading);
    public record ReadingInput(decimal ReadingValue, DateTimeOffset? ReadingDate, string? Method, string? Notes, bool AllowReset = false);

    /// <summary>Meters list. Filters: buildingId, unitId, status, type, unassigned, calibrationDueInDays.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? unitId, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] string? type, [FromQuery] bool unassigned = false, [FromQuery] int? calibrationDueInDays = null, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.Meters.AsNoTracking().Where(m => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(m.BranchId ?? 0));
        if (branchId != null) query = query.Where(m => m.BranchId == branchId);
        if (buildingId != null) query = query.Where(m => Db.MeterAssignments.Any(a => a.MeterId == m.Id && a.ToDate == null && a.Unit!.BuildingId == buildingId));
        if (unitId != null) query = query.Where(m => Db.MeterAssignments.Any(a => a.MeterId == m.Id && a.ToDate == null && a.UnitId == unitId));
        if (unassigned) query = query.Where(m => !Db.MeterAssignments.Any(a => a.MeterId == m.Id && a.ToDate == null));
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.MeterStatus, status); query = query.Where(m => m.StatusId == s); }
        if (!string.IsNullOrEmpty(type)) { var s = await Lookups.IdOrNullAsync(LookupTypes.MeterType, type); query = query.Where(m => m.MeterTypeId == s); }
        if (calibrationDueInDays != null) { var limit = Today.AddDays(-365 + calibrationDueInDays.Value); query = query.Where(m => m.CalibrationDate == null || m.CalibrationDate <= limit); }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(m => m.MeterNo.Contains(q.Search) || m.SerialNo.Contains(q.Search) || (m.SimCardNo ?? "").Contains(q.Search));
        var page = await Project(query.OrderBy(m => m.MeterNo)).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    /// <summary>Meter detail with assignment history, last readings and service history.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Project(Db.Meters.AsNoTracking().Where(m => m.Id == id)).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Meter", id);
        var dto = (await ToDtos(new[] { row }))[0];
        var readings = await Db.MeterReadings.AsNoTracking().Where(r => r.MeterId == id).OrderByDescending(r => r.ReadingDate).Take(24)
            .Select(r => new { r.Id, r.ReadingDate, r.ReadingValue, r.Consumption, r.MethodId, r.ReadingTypeId, r.ContractId, r.InvoiceId, r.Notes, readBy = r.ReadByUser!.NameAr ?? r.ReadByUser.NameEn }).ToListAsync(ct);
        var assignments = await Db.MeterAssignments.AsNoTracking().Where(a => a.MeterId == id).OrderByDescending(a => a.FromDate)
            .Select(a => new { a.Id, a.UnitId, a.Unit!.UnitNo, a.Unit.BuildingId, BCode = a.Unit.Building!.Code, a.FromDate, a.ToDate }).ToListAsync(ct);
        var services = await Db.MeterServices.AsNoTracking().Where(s => s.MeterId == id || s.NewMeterId == id).OrderByDescending(s => s.Id).Take(20).ToListAsync(ct);
        var monthly = readings.Where(r => r.ReadingDate >= DateTimeOffset.UtcNow.AddMonths(-12))
            .GroupBy(r => new { r.ReadingDate.Year, r.ReadingDate.Month })
            .Select(g => new { month = $"{g.Key.Year}-{g.Key.Month:00}", consumption = g.Sum(x => x.Consumption ?? 0) }).OrderBy(x => x.month);
        return Ok(new
        {
            meter = dto,
            readings = readings.Select(r => new { r.Id, r.ReadingDate, r.ReadingValue, r.Consumption, method = Code(r.MethodId), type = Code(r.ReadingTypeId), typeLabel = Label(r.ReadingTypeId),
                                                 r.ContractId, billed = r.InvoiceId != null, r.InvoiceId, r.Notes, r.readBy }),
            monthlyConsumption = monthly,
            assignments,
            services = services.Select(s => new { s.Id, s.ServiceNo, type = Code(s.ServiceTypeId), typeLabel = Label(s.ServiceTypeId), status = Code(s.StatusId), s.RequestedDate, s.CompletedDate, s.Reason }),
        });
    }

    /// <summary>Add a meter (AddMeterModal). With unitId the meter is installed in that unit (meters belong to units, not tenants).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MeterInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Serial)) throw new AppException("Serial number is required.");
        long branchId;
        if (input.UnitId != null)
            branchId = await Db.Units.Where(u => u.Id == input.UnitId).Select(u => (long?)u.Building!.BranchId).FirstOrDefaultAsync() ?? throw new NotFoundException("Unit", input.UnitId);
        else branchId = RequireBranch(input.BranchId);
        EnsureBranchAccess(branchId);
        if (await Db.Meters.AnyAsync(m => m.SerialNo == input.Serial.Trim())) throw new BusinessRuleException($"Serial number {input.Serial} already exists.");
        if (!string.IsNullOrWhiteSpace(input.MeterNo) && await Db.Meters.AnyAsync(m => m.MeterNo == input.MeterNo.Trim()))
            throw new BusinessRuleException($"Meter number {input.MeterNo} already exists.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        var m = new Meter
        {
            BranchId = branchId,
            MeterNo = string.IsNullOrWhiteSpace(input.MeterNo) ? await Numbers.NextAsync(NumberPrefixes.Meter, yearly: false) : input.MeterNo.Trim(),
            SerialNo = input.Serial.Trim(), IsActive = true,
        };
        await Apply(m, input);
        // every meter enters stock first; installing it (below, or later from stock) makes it active
        m.StatusId = await Lk(LookupTypes.MeterStatus, null, input.Status ?? "in_stock", field: "status");
        if (input.UnitId != null) m.InstallDate = null;
        Db.Meters.Add(m);
        await Db.SaveChangesAsync();
        Activity.Log("meter", m.Id, "create", $"Meter {m.MeterNo} added to stock", null, branchId);
        long? buildingId = null;
        if (input.UnitId != null)
        {
            await Ops.AssignAsync(m, input.UnitId.Value, input.InstallDate ?? Today);
            var u = await Db.Units.Where(x => x.Id == input.UnitId).Select(x => new { x.BuildingId, x.UnitNo }).FirstAsync();
            buildingId = u.BuildingId;
            Activity.Log("meter", m.Id, "assign", $"Meter {m.MeterNo} installed in unit {u.UnitNo}", buildingId, branchId);
        }
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = m.Id }, new { m.Id, m.MeterNo });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] MeterInput input)
    {
        var m = await Load(id);
        if (!string.IsNullOrWhiteSpace(input.Serial) && input.Serial.Trim() != m.SerialNo)
        {
            if (await Db.Meters.AnyAsync(x => x.SerialNo == input.Serial.Trim() && x.Id != id)) throw new BusinessRuleException($"Serial number {input.Serial} already exists.");
            m.SerialNo = input.Serial.Trim();
        }
        await Apply(m, input);
        if (input.Status != null) m.StatusId = await Lk(LookupTypes.MeterStatus, null, input.Status, field: "status");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var m = await Load(id);
        if (await Db.MeterAssignments.AnyAsync(a => a.MeterId == id && a.ToDate == null)) throw new BusinessRuleException("Unassign the meter before deleting it.");
        if (await Db.MeterReadings.AnyAsync(r => r.MeterId == id && r.InvoiceId != null)) throw new BusinessRuleException("The meter has billed readings; set it inactive instead.");
        Db.Meters.Remove(m);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Install a meter from stock into a unit.</summary>
    [HttpPost("{id:long}/assign")]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignInput input)
    {
        var m = await Load(id);
        await Ops.AssignAsync(m, input.UnitId, input.FromDate ?? Today);
        var u = await Db.Units.Where(x => x.Id == input.UnitId).Select(x => new { x.BuildingId, x.UnitNo }).FirstOrDefaultAsync();
        Activity.Log("meter", m.Id, "assign", $"Meter {m.MeterNo} installed in unit {u?.UnitNo}", u?.BuildingId, m.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Add many meters to stock at once (same type / brand, one serial each). Returns the created meters and per-serial errors.</summary>
    [HttpPost("stock")]
    public async Task<IActionResult> AddToStock([FromBody] StockBulkInput input)
    {
        var branchId = RequireBranch(input.BranchId);
        EnsureBranchAccess(branchId);
        var serials = (input.Serials ?? new()).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        if (serials.Count == 0) throw new AppException("Enter at least one serial number.");
        if (serials.Count > 500) throw new AppException("At most 500 meters at a time.");
        var dup = serials.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dup.Count > 0) throw new AppException($"Repeated serial numbers: {string.Join(", ", dup)}");
        var existing = await Db.Meters.IgnoreQueryFilters().Where(m => serials.Contains(m.SerialNo)).Select(m => m.SerialNo).ToListAsync();
        if (existing.Count > 0) throw new BusinessRuleException($"Already registered: {string.Join(", ", existing)}");

        var typeId = await Lk(LookupTypes.MeterType, null, input.Type ?? "standard", field: "type");
        var stock = await LkId(LookupTypes.MeterStatus, "in_stock");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var created = new List<Meter>();
        foreach (var serial in serials)
        {
            var m = new Meter
            {
                BranchId = branchId, MeterNo = await Numbers.NextAsync(NumberPrefixes.Meter, yearly: false), SerialNo = serial, IsActive = true,
                MeterTypeId = typeId, Brand = input.Brand, Model = input.Model, CalibrationDate = input.CalibrationDate,
                InitialReading = input.InitialReading ?? 0, LocationNote = input.Notes, StatusId = stock,
            };
            Db.Meters.Add(m);
            await Db.SaveChangesAsync();
            Activity.Log("meter", m.Id, "create", $"Meter {m.MeterNo} added to stock", null, branchId);
            created.Add(m);
        }
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(created.Select(m => new { m.Id, m.MeterNo, serial = m.SerialNo }));
    }

    [HttpPost("{id:long}/unassign")]
    public async Task<IActionResult> Unassign(long id, [FromBody] UnassignInput input)
    {
        var m = await Load(id);
        await Ops.UnassignAsync(m, input.ToDate ?? Today, input.FinalReading);
        return NoContent();
    }

    // ---------------- readings

    [HttpGet("{id:long}/readings")]
    public async Task<IActionResult> Readings(long id, [FromQuery] PageQuery q, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        await LoadLookups();
        var query = Db.MeterReadings.AsNoTracking().Where(r => r.MeterId == id);
        if (from != null) query = query.Where(r => r.ReadingDate >= from);
        if (to != null) query = query.Where(r => r.ReadingDate <= to);
        var page = await query.OrderByDescending(r => r.ReadingDate).ToPagedAsync(q);
        return Ok(new PagedResult<object>(page.Items.Select(r => (object)new { r.Id, r.ReadingDate, r.ReadingValue, r.Consumption, method = Code(r.MethodId), type = Code(r.ReadingTypeId), typeLabel = Label(r.ReadingTypeId),
            r.UnitId, r.ContractId, billed = r.InvoiceId != null, r.InvoiceId, r.Notes }).ToList(),
            page.Total, page.Page, page.PageSize));
    }

    /// <summary>Record a reading (MeterReadingModal). Consumption = value − previous reading.</summary>
    [HttpPost("{id:long}/readings")]
    public async Task<IActionResult> AddReading(long id, [FromBody] ReadingInput input)
    {
        var m = await Load(id);
        if (await Lookups.CodeAsync(m.StatusId) is "retired" or "lost")
            throw new BusinessRuleException($"Meter {m.MeterNo} is retired / lost; readings cannot be recorded.");
        var r = await Ops.AddReadingAsync(m, input.ReadingValue, input.ReadingDate ?? DateTimeOffset.UtcNow, input.Method, input.Notes, input.AllowReset);
        return Ok(new { r.Id, r.ReadingDate, r.ReadingValue, r.Consumption });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Meter> Load(long id)
    {
        var m = await Db.Meters.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Meter", id);
        EnsureBranchAccess(m.BranchId);
        return m;
    }

    private async Task Apply(Meter m, MeterInput i)
    {
        m.MeterTypeId = await Lk(LookupTypes.MeterType, i.MeterTypeId, i.Type ?? "standard", field: "type");
        m.Brand = i.Brand; m.Model = i.Model; m.SimCardNo = i.SimCard; m.CalibrationDate = i.CalibrationDate; m.InstallDate = i.InstallDate;
        if (i.InitialReading != null) m.InitialReading = i.InitialReading;
        m.LocationNote = i.Location ?? i.Notes;
    }

    private class Row
    {
        public Meter M { get; init; } = null!;
        public long? UnitId { get; init; }
        public string? UnitNo { get; init; }
        public int? Floor { get; init; }
        public long? BuildingId { get; init; }
        public string? BCode { get; init; }
        public string? BEn { get; init; }
        public string? BAr { get; init; }
    }

    private IQueryable<Row> Project(IQueryable<Meter> q) =>
        from m in q
        let a = Db.MeterAssignments.Where(a => a.MeterId == m.Id && a.ToDate == null).FirstOrDefault()
        select new Row
        {
            M = m, UnitId = a == null ? null : a.UnitId, UnitNo = a == null ? null : a.Unit!.UnitNo, Floor = a == null ? null : a.Unit!.FloorNo,
            BuildingId = a == null ? null : a.Unit!.BuildingId, BCode = a == null ? null : a.Unit!.Building!.Code,
            BEn = a == null ? null : a.Unit!.Building!.NameEn, BAr = a == null ? null : a.Unit!.Building!.NameAr,
        };

    private async Task<List<object>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.M.Id).ToList();
        var since = DateTimeOffset.UtcNow.AddMonths(-6);
        var last = await Db.MeterReadings.AsNoTracking().Where(r => ids.Contains(r.MeterId))
            .GroupBy(r => r.MeterId).Select(g => g.OrderByDescending(r => r.ReadingDate).First()).ToListAsync();
        var sums = await Db.MeterReadings.AsNoTracking().Where(r => ids.Contains(r.MeterId) && r.ReadingDate >= since)
            .GroupBy(r => r.MeterId).Select(g => new { g.Key, Sum = g.Sum(r => r.Consumption ?? 0) }).ToDictionaryAsync(x => x.Key, x => x.Sum);
        var unbilled = await Db.MeterReadings.AsNoTracking().Where(r => ids.Contains(r.MeterId) && r.InvoiceId == null)
            .GroupBy(r => r.MeterId).Select(g => new { g.Key, Sum = g.Sum(r => r.Consumption ?? 0) }).ToDictionaryAsync(x => x.Key, x => x.Sum);
        // tenant of a meter = active contract of the unit the meter is installed in
        var unitIds = rows.Where(r => r.UnitId != null).Select(r => r.UnitId!.Value).ToList();
        var contracts = await Db.SupplyContracts.AsNoTracking().Where(c => c.IsActive && c.UnitId != null && unitIds.Contains(c.UnitId.Value))
            .Select(c => new { c.UnitId, c.Id, c.ContractNo, c.TenantId, c.Tenant!.Party!.NameEn, c.Tenant.Party!.NameAr }).ToListAsync();
        var balances = await HttpContext.RequestServices.GetRequiredService<BalanceQueries>().ByContractAsync(contracts.Select(c => c.Id));

        return rows.Select(r =>
        {
            var lr = last.FirstOrDefault(x => x.MeterId == r.M.Id);
            var c = r.UnitId == null ? null : contracts.FirstOrDefault(x => x.UnitId == r.UnitId);
            return (object)new
            {
                r.M.Id, meterNo = r.M.MeterNo, serial = r.M.SerialNo, unitId = r.UnitId, unit = r.UnitNo, floor = r.Floor,
                buildingId = r.BuildingId, buildingCode = r.BCode, building = Loc(r.BEn, r.BAr),
                type = Code(r.M.MeterTypeId), typeLabel = Label(r.M.MeterTypeId), status = Code(r.M.StatusId), statusLabel = Label(r.M.StatusId),
                installDate = r.M.InstallDate, lastRead = lr?.ReadingDate, lastReadValue = lr?.ReadingValue ?? r.M.InitialReading ?? 0,
                monthlyAvg = Math.Round(sums.GetValueOrDefault(r.M.Id) / 6m, 2), unbilledM3 = unbilled.GetValueOrDefault(r.M.Id),
                balance = c == null ? 0 : balances.GetValueOrDefault(c.Id)?.Outstanding ?? 0,
                tenantId = c?.TenantId, tenant = c == null ? null : Loc(c.NameEn, c.NameAr), contractId = c?.Id, contractNo = c?.ContractNo,
                brand = r.M.Brand, model = r.M.Model, calibrationDate = r.M.CalibrationDate, simCard = r.M.SimCardNo, initialReading = r.M.InitialReading,
                location = r.M.LocationNote, r.M.BranchId, r.M.IsActive,
                calibrationExpiry = CalibrationExpiry(r.M.CalibrationDate), calibrationState = CalibrationState(r.M.CalibrationDate),
            };
        }).ToList();
    }

    // ---------- calibration validity: a calibration is valid for CalibrationValidMonths (default 12) ----------
    private int CalibrationValidMonths => HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<int?>("Meters:CalibrationValidMonths") ?? 12;
    private DateOnly? CalibrationExpiry(DateOnly? calibrated) => calibrated?.AddMonths(CalibrationValidMonths).AddDays(-1);
    /// <summary>valid | due_soon (≤ 30 days left) | expired | unknown (no calibration date).</summary>
    private string CalibrationState(DateOnly? calibrated)
    {
        var exp = CalibrationExpiry(calibrated);
        if (exp == null) return "unknown";
        if (exp < Today) return "expired";
        return exp <= Today.AddDays(30) ? "due_soon" : "valid";
    }

    /// <summary>Statuses that mean the meter cannot measure / bill (readings are refused).</summary>
    public static readonly string[] OutOfServiceStatuses = { "fault", "damaged", "retired", "lost" };

    public record StatusInput(string Status, DateOnly? Date, string? Reason, DateOnly? CalibrationDate);

    /// <summary>
    /// Change a meter's status (active / fault / damaged / maintenance / disconnected / retired / lost / in_stock, or any value added to the
    /// METER_STATUS lookup). A reason is required for fault, damaged, retired and lost. Retired / lost meters must be removed from the unit first.
    /// Optional new calibration date (e.g. after maintenance). Every change is written to the activity log (status history).
    /// </summary>
    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] StatusInput input)
    {
        await LoadLookups();
        var m = await Load(id);
        var newId = await Lk(LookupTypes.MeterStatus, null, input.Status, required: true, field: "status");
        var code = Code(newId) ?? input.Status;
        var oldCode = Code(m.StatusId);
        var oldLabel = Label(m.StatusId);
        if (newId == m.StatusId && input.CalibrationDate == null) throw new BusinessRuleException("The meter already has this status.");
        if (OutOfServiceStatuses.Contains(code) && string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Write the reason for this status.");
        var date = input.Date ?? Today;
        if (date > Today) throw new AppException("The status date cannot be in the future.");
        var assignment = await Db.MeterAssignments.Include(a => a.Unit).FirstOrDefaultAsync(a => a.MeterId == id && a.ToDate == null);
        if (code is "retired" or "lost" or "in_stock" && assignment != null)
            throw new BusinessRuleException($"The meter is installed in unit {assignment.Unit?.UnitNo}. Remove it from the unit (or replace it) first.");
        if (input.CalibrationDate != null)
        {
            if (input.CalibrationDate > Today) throw new AppException("Calibration date cannot be in the future.");
            m.CalibrationDate = input.CalibrationDate;
        }
        m.StatusId = newId;
        m.IsActive = code is not ("retired" or "lost");
        Activity.Log("meter", m.Id, "status",
            $"Meter {m.MeterNo}: {oldLabel ?? oldCode ?? "—"} → {Label(newId)}{(string.IsNullOrWhiteSpace(input.Reason) ? "" : $" — {input.Reason.Trim()}")}",
            assignment?.Unit?.BuildingId, m.BranchId,
            new { status = oldCode }, new { status = code, date, reason = input.Reason?.Trim(), calibrationDate = input.CalibrationDate });
        await Db.SaveChangesAsync();
        return await Get(id, HttpContext.RequestAborted);
    }

    /// <summary>Status history of a meter (from the activity log).</summary>
    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> StatusHistory(long id)
    {
        var rows = await Db.ActivityLogs.AsNoTracking().Where(a => a.EntityType == "meter" && a.EntityId == id && a.Action == "status")
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new { a.Id, a.OccurredAt, a.Description, a.OldValues, a.NewValues, user = a.User!.NameAr ?? a.User.NameEn }).ToListAsync();
        return Ok(rows.Select(r =>
        {
            System.Text.Json.JsonElement? nv = r.NewValues == null ? null : System.Text.Json.JsonDocument.Parse(r.NewValues).RootElement;
            System.Text.Json.JsonElement? ov = r.OldValues == null ? null : System.Text.Json.JsonDocument.Parse(r.OldValues).RootElement;
            string? Str(System.Text.Json.JsonElement? e, string k) => e != null && e.Value.TryGetProperty(k, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
            return new { r.Id, r.OccurredAt, r.user, from = Str(ov, "status"), to = Str(nv, "status"), date = Str(nv, "date"), reason = Str(nv, "reason"), r.Description };
        }));
    }
}

/// <summary>Readings across meters (dashboard "recent readings", route reading upload).</summary>
[Route("api/meter-readings")]
[PermissionModule("meters")]
public class MeterReadingsController : ApiControllerBase
{
    public record BulkReading(long? MeterId, string? MeterNo, decimal ReadingValue, DateTimeOffset? ReadingDate, string? Method, string? Notes);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? meterId, [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to, [FromQuery] bool unbilledOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.MeterReadings.AsNoTracking().Where(r => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(r.Meter!.BranchId ?? 0));
        if (meterId != null) query = query.Where(r => r.MeterId == meterId);
        if (buildingId != null) query = query.Where(r => Db.MeterAssignments.Any(a => a.MeterId == r.MeterId && a.Unit!.BuildingId == buildingId));
        if (from != null) query = query.Where(r => r.ReadingDate >= from);
        if (to != null) query = query.Where(r => r.ReadingDate <= to);
        if (unbilledOnly) query = query.Where(r => r.InvoiceId == null);
        var page = await query.OrderByDescending(r => r.ReadingDate)
            .Select(r => new
            {
                r.Id, r.MeterId, r.Meter!.MeterNo, r.ReadingDate, r.ReadingValue, r.Consumption, r.MethodId, r.InvoiceId, r.Notes,
                Unit = Db.MeterAssignments.Where(a => a.MeterId == r.MeterId && a.ToDate == null).Select(a => new { a.Unit!.UnitNo, a.Unit.Building!.Code, a.Unit.Building.NameEn, a.Unit.Building.NameAr }).FirstOrDefault(),
            }).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(page.Items.Select(r => (object)new
        {
            r.Id, r.MeterId, meterNo = r.MeterNo, date = r.ReadingDate, current = r.ReadingValue, previous = r.ReadingValue - (r.Consumption ?? 0), consumption = r.Consumption,
            method = Code(r.MethodId), billed = r.InvoiceId != null, r.InvoiceId, r.Notes, unit = r.Unit?.UnitNo, buildingCode = r.Unit?.Code, building = r.Unit == null ? null : Loc(r.Unit.NameEn, r.Unit.NameAr),
        }).ToList(), page.Total, page.Page, page.PageSize));
    }

    /// <summary>Upload many readings at once (e.g. a reading route). Each row is validated; errors are returned per row.</summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> Bulk([FromBody] List<BulkReading> rows)
    {
        var ops = HttpContext.RequestServices.GetRequiredService<MeterOps>();
        var results = new List<object>();
        foreach (var row in rows)
        {
            try
            {
                var m = row.MeterId != null ? await Db.Meters.FirstOrDefaultAsync(x => x.Id == row.MeterId)
                                            : await Db.Meters.FirstOrDefaultAsync(x => x.MeterNo == row.MeterNo);
                if (m == null) throw new AppException("Meter not found.");
                EnsureBranchAccess(m.BranchId);
                var r = await ops.AddReadingAsync(m, row.ReadingValue, row.ReadingDate ?? DateTimeOffset.UtcNow, row.Method, row.Notes);
                results.Add(new { row.MeterId, row.MeterNo, ok = true, r.Id, r.Consumption });
            }
            catch (AppException ex)
            {
                Db.ChangeTracker.Clear();
                results.Add(new { row.MeterId, row.MeterNo, ok = false, error = ex.Message });
            }
        }
        return Ok(results);
    }

    /// <summary>Delete a reading. Only the latest, unbilled reading of a meter can be deleted.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var r = await Db.MeterReadings.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("MeterReading", id);
        if (r.InvoiceId != null) throw new BusinessRuleException("The reading is already billed.");
        if (await Db.MeterReadings.AnyAsync(x => x.MeterId == r.MeterId && x.ReadingDate > r.ReadingDate)) throw new BusinessRuleException("Only the latest reading can be deleted.");
        Db.MeterReadings.Remove(r);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

[Route("api/meter-topups")]
[PermissionModule("meters")]
public class MeterTopupsController : CrudController<MeterTopup> { }

[Route("api/meter-assignments")]
[PermissionModule("meters")]
public class MeterAssignmentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? meterId, [FromQuery] long? unitId, [FromQuery] bool currentOnly = false) =>
        Ok(await Db.MeterAssignments.AsNoTracking()
            .Where(a => (meterId == null || a.MeterId == meterId) && (unitId == null || a.UnitId == unitId) && (!currentOnly || a.ToDate == null))
            .OrderByDescending(a => a.FromDate)
            .Select(a => new { a.Id, a.MeterId, a.Meter!.MeterNo, a.UnitId, a.Unit!.UnitNo, a.Unit.BuildingId, a.FromDate, a.ToDate }).ToListAsync());
}
