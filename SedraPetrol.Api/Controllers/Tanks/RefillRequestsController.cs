using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Tank refill workflow (RefillRequestDrawer):
/// 0 request → 1 purchase order → 2 supplier payment → 3 collection → 4 purchase invoice
/// → 5 delivery schedule (+ Civil Defense) → 6 tank refill → 7 management approval → 8 completed.
/// Every step writes refill_stage_history.
/// </summary>
[Route("api/refill-requests")]
[PermissionModule("tanks")]
public class RefillRequestsController : ApiControllerBase
{
    private static readonly string[] StatusByStage =
        { "requested", "po_issued", "paid", "collecting", "invoiced", "scheduled", "delivering", "pending_approval", "completed" };

    // ------------------------------------------------------------------ inputs
    public record CreateInput(long TankId, decimal RequestedQty, string? Priority, DateOnly? RequestDate, string? Notes);
    public record PoInput(long VendorId, string? PoNumber, DateOnly? PoDate, decimal? Qty, decimal UnitPrice, string? Notes);
    public record PaymentInput(decimal Amount, DateOnly? Date, string? Method, string? Reference, string? Notes);
    public record CollectionInput(long? VehicleId, string? TankerPlate, long? DriverId, string? DriverName, DateTimeOffset? CollectionDate,
        decimal? MeterBefore, decimal? MeterAfter, decimal? CollectedQty);
    public record InvoiceInput(string InvoiceNumber, DateOnly? InvoiceDate, decimal? Amount, decimal? VatRate, DateOnly? DueDate);
    public record ScheduleInput(DateOnly DeliveryDate, string? TimeSlot, bool CivilDefenseConfirmed, string? CivilDefenseRef);
    public record RefillInput(DateTimeOffset? RefillDateTime, DateOnly? RefillDate, string? RefillTime, long? VehicleId, string? TankerPlate,
        long? DriverId, string? DriverName, decimal LevelBefore, decimal LevelAfter, bool? SafetyCheckPassed, string? Notes);
    public record ApproveInput(string? Notes);
    public record CancelInput(string? Reason);

    // ------------------------------------------------------------------ queries

    /// <summary>Refill requests (RefillRequestItem shape incl. stageData). Filters: buildingId, tankId, status, stage, openOnly.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? tankId, [FromQuery] long? branchId,
        [FromQuery] string? status, [FromQuery] int? stage, [FromQuery] bool openOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Db.RefillRequests.AsNoTracking().Where(r => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(r.BranchId));
        if (tankId != null) query = query.Where(r => r.TankId == tankId);
        if (branchId != null) query = query.Where(r => r.BranchId == branchId);
        if (buildingId != null) query = query.Where(r => r.Tank!.BuildingId == buildingId);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, status); query = query.Where(r => r.StatusId == s); }
        if (stage != null) { var s = await Lookups.IdOrNullAsync(LookupTypes.RefillStage, stage.ToString()); query = query.Where(r => r.CurrentStageId == s); }
        if (openOnly)
        {
            var done = new[] { await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed"), await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled") };
            query = query.Where(r => !done.Contains(r.StatusId));
        }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(r => r.RequestNo.Contains(q.Search) || r.Tank!.Building!.Code.Contains(q.Search));
        var page = await query.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.Id).ToPagedAsync(q, ct);
        var dtos = await BuildDtos(page.Items);
        return Ok(new PagedResult<object>(dtos, page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var r = await Db.RefillRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("RefillRequest", id);
        return Ok((await BuildDtos(new[] { r }))[0]);
    }

    [HttpGet("{id:long}/history")]
    public async Task<IActionResult> History(long id)
    {
        await LoadLookups();
        var rows = await Db.RefillStageHistories.AsNoTracking().Where(h => h.RefillRequestId == id).OrderBy(h => h.ChangedAt)
            .Select(h => new { h.Id, h.FromStageId, h.ToStageId, h.ChangedAt, h.Remarks, user = h.ChangedByUser!.NameAr ?? h.ChangedByUser.NameEn }).ToListAsync();
        return Ok(rows.Select(h => new { h.Id, fromStage = Code(h.FromStageId), toStage = Code(h.ToStageId), toStageLabel = Label(h.ToStageId), h.ChangedAt, h.Remarks, h.user }));
    }

    // ------------------------------------------------------------------ stage 0: request

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var tank = await Db.Tanks.FirstOrDefaultAsync(t => t.Id == input.TankId) ?? throw new NotFoundException("Tank", input.TankId);
        EnsureBranchAccess(tank.BranchId);
        if (input.RequestedQty <= 0) throw new AppException("Requested quantity must be greater than zero.");
        var free = tank.CapacityLitres - (tank.CurrentLevelLitres ?? 0);
        if (input.RequestedQty > free) throw new BusinessRuleException($"Requested {input.RequestedQty} L but the tank has only {free} L free capacity.");
        var completed = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "completed");
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.RefillStatus, "cancelled");
        if (await Db.RefillRequests.AnyAsync(r => r.TankId == tank.Id && r.StatusId != completed && r.StatusId != cancelled))
            throw new BusinessRuleException("This tank already has an open refill request.");

        var r = new RefillRequest
        {
            BranchId = tank.BranchId,
            RequestNo = await Numbers.NextAsync(NumberPrefixes.Refill),
            TankId = tank.Id,
            RequestDate = input.RequestDate ?? Today,
            RequestedBy = Me.UserId,
            RequestedQtyLitres = input.RequestedQty,
            LevelAtRequestLitres = tank.CurrentLevelLitres,
            PriorityId = await Lk(LookupTypes.Priority, null, input.Priority ?? "medium", field: "priority"),
            CurrentStageId = await LkId(LookupTypes.RefillStage, "0"),
            StatusId = await LkId(LookupTypes.RefillStatus, "requested"),
            Notes = input.Notes,
        };
        Db.RefillRequests.Add(r);
        await Db.SaveChangesAsync();
        await History(r, null, 0, "Refill requested");
        Activity.Log("refill_request", r.Id, "create", $"Refill {r.RequestNo} requested ({r.RequestedQtyLitres} L)", tank.BuildingId, tank.BranchId);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = r.Id }, new { r.Id, r.RequestNo });
    }

    // ------------------------------------------------------------------ stage 1: purchase order

    [HttpPost("{id:long}/po")]
    public async Task<IActionResult> PurchaseOrder(long id, [FromBody] PoInput input)
    {
        var (r, _) = await Load(id, expectedStage: 0);
        var vendor = await Db.Vendors.FirstOrDefaultAsync(v => v.Id == input.VendorId) ?? throw new NotFoundException("Vendor", input.VendorId);
        var qty = input.Qty ?? r.RequestedQtyLitres;
        if (qty <= 0 || input.UnitPrice < 0) throw new AppException("Quantity and unit price must be positive.");
        Db.RefillPos.Add(new RefillPo
        {
            RefillRequestId = r.Id, VendorId = vendor.Id,
            PoNumber = string.IsNullOrWhiteSpace(input.PoNumber) ? await Numbers.NextAsync(NumberPrefixes.PurchaseOrder) : input.PoNumber.Trim(),
            PoDate = input.PoDate ?? Today, QtyLitres = qty, UnitPrice = input.UnitPrice, TotalAmount = BillingService.R2(qty * input.UnitPrice),
            CurrencyId = await Db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync(),
        });
        await Advance(r, 1, input.Notes ?? $"PO issued to {vendor.NameEn ?? vendor.NameAr}");
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 2: supplier payment

    [HttpPost("{id:long}/payment")]
    public async Task<IActionResult> SupplierPayment(long id, [FromBody] PaymentInput input)
    {
        var (r, _) = await Load(id, expectedStage: 1);
        var po = await Db.RefillPos.FirstAsync(p => p.RefillRequestId == id);
        if (input.Amount <= 0) throw new AppException("Payment amount must be greater than zero.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var vp = new VendorPayment
        {
            BranchId = r.BranchId, VendorId = po.VendorId,
            PaymentNo = await Numbers.NextAsync(NumberPrefixes.VendorPayment, date: input.Date ?? Today),
            PaymentDate = input.Date ?? Today, Amount = input.Amount, CurrencyId = po.CurrencyId,
            PaymentMethodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "bank_transfer", field: "method"),
            ReferenceNo = input.Reference, PaidBy = Me.UserId, Notes = input.Notes ?? $"Refill {r.RequestNo} / PO {po.PoNumber}",
            StatusId = await LkId(LookupTypes.PaymentStatus, "completed"),
        };
        Db.VendorPayments.Add(vp);
        await Db.SaveChangesAsync();
        vp.JournalEntryId = (await Posting.PostVendorPaymentAsync(vp))?.Id;
        Db.RefillSupplierPayments.Add(new RefillSupplierPayment { RefillRequestId = r.Id, VendorPaymentId = vp.Id });
        await Advance(r, 2, $"Supplier paid {vp.Amount:0.##} ({vp.PaymentNo})");
        await tx.CommitAsync();
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 3: collection at supplier

    [HttpPost("{id:long}/collection")]
    public async Task<IActionResult> Collection(long id, [FromBody] CollectionInput input)
    {
        var (r, _) = await Load(id, expectedStage: 2);
        var po = await Db.RefillPos.FirstAsync(p => p.RefillRequestId == id);
        var qty = input.CollectedQty ?? (input.MeterAfter - input.MeterBefore);
        if (qty is null or <= 0) throw new AppException("Collected quantity must be greater than zero (meter after must be greater than meter before).");
        var vehicleId = await ResolveVehicle(input.VehicleId, input.TankerPlate, po.VendorId);
        var driverId = await ResolveDriver(input.DriverId, input.DriverName, po.VendorId);
        Db.RefillCollections.Add(new RefillCollection
        {
            RefillRequestId = r.Id, VehicleId = vehicleId, DriverId = driverId, CollectionDate = input.CollectionDate ?? DateTimeOffset.UtcNow,
            MeterBefore = input.MeterBefore, MeterAfter = input.MeterAfter, CollectedQtyLitres = qty,
        });
        await Advance(r, 3, $"Collected {qty:0.###} L");
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 4: purchase invoice (vendor bill)

    [HttpPost("{id:long}/purchase-invoice")]
    public async Task<IActionResult> PurchaseInvoice(long id, [FromBody] InvoiceInput input)
    {
        var (r, _) = await Load(id, expectedStage: 3);
        var po = await Db.RefillPos.FirstAsync(p => p.RefillRequestId == id);
        var col = await Db.RefillCollections.FirstAsync(c => c.RefillRequestId == id);
        if (string.IsNullOrWhiteSpace(input.InvoiceNumber)) throw new AppException("Supplier invoice number is required.");
        if (await Db.VendorBills.AnyAsync(b => b.VendorId == po.VendorId && b.BillNo == input.InvoiceNumber))
            throw new BusinessRuleException($"Invoice {input.InvoiceNumber} of this supplier is already recorded.");
        var qty = col.CollectedQtyLitres ?? po.QtyLitres ?? r.RequestedQtyLitres;
        var price = input.Amount != null ? Math.Round(input.Amount.Value / qty, 4) : po.UnitPrice ?? 0;

        await using var tx = await Db.Database.BeginTransactionAsync();
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var bill = await billing.CreateVendorBillAsync(r.BranchId, po.VendorId, input.InvoiceNumber.Trim(), input.InvoiceDate ?? Today, input.DueDate,
            "refill_request", r.Id,
            new[] { new BillingService.BillLineInput("gas_purchase", null, $"Gas purchase – {r.RequestNo} / PO {po.PoNumber}", qty, "L", price, input.VatRate, null) },
            null, po.CurrencyId);
        Db.RefillPurchaseInvoices.Add(new RefillPurchaseInvoice { RefillRequestId = r.Id, VendorBillId = bill.Id });

        // apply the stage-2 supplier payment to this bill
        var sp = await Db.RefillSupplierPayments.FirstOrDefaultAsync(p => p.RefillRequestId == id);
        if (sp != null)
        {
            var vp = await Db.VendorPayments.FirstAsync(p => p.Id == sp.VendorPaymentId);
            var already = await Db.VendorPaymentAllocations.Where(a => a.VendorPaymentId == vp.Id).SumAsync(a => (decimal?)a.Amount) ?? 0;
            var amt = Math.Min(vp.Amount - already, bill.TotalAmount ?? 0);
            if (amt > 0) Db.VendorPaymentAllocations.Add(new VendorPaymentAllocation { VendorPaymentId = vp.Id, VendorBillId = bill.Id, Amount = amt });
            await Db.SaveChangesAsync();
            await billing.RecalcBillsAsync(new[] { bill.Id });
        }
        await Advance(r, 4, $"Purchase invoice {bill.BillNo} recorded ({bill.TotalAmount:0.##})");
        await tx.CommitAsync();
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 5: schedule + Civil Defense

    [HttpPost("{id:long}/schedule")]
    public async Task<IActionResult> Schedule(long id, [FromBody] ScheduleInput input)
    {
        var (r, stage) = await Load(id);
        if (stage != 4 && stage != 5) throw new BusinessRuleException($"The delivery can only be scheduled after the purchase invoice (current stage {stage}).");
        var s = await Db.RefillSchedules.FirstOrDefaultAsync(x => x.RefillRequestId == id);
        if (s == null) { s = new RefillSchedule { RefillRequestId = id }; Db.RefillSchedules.Add(s); }
        s.DeliveryDate = input.DeliveryDate; s.TimeSlot = input.TimeSlot; s.CivilDefenseConfirmed = input.CivilDefenseConfirmed; s.CivilDefenseRef = input.CivilDefenseRef;
        if (stage == 4) await Advance(r, 5, $"Delivery scheduled {input.DeliveryDate:yyyy-MM-dd} {input.TimeSlot}");
        else await Db.SaveChangesAsync();
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 6: tank refill

    [HttpPost("{id:long}/refill")]
    public async Task<IActionResult> Refill(long id, [FromBody] RefillInput input)
    {
        var (r, _) = await Load(id, expectedStage: 5);
        var schedule = await Db.RefillSchedules.FirstAsync(s => s.RefillRequestId == id);
        if (!schedule.CivilDefenseConfirmed) throw new BusinessRuleException("Civil Defense confirmation is required before refilling the tank.");
        var tank = await Db.Tanks.FirstAsync(t => t.Id == r.TankId);
        if (input.LevelAfter <= input.LevelBefore) throw new AppException("The level after refill must be greater than the level before.");
        if (input.LevelAfter > tank.CapacityLitres) throw new BusinessRuleException($"Level after refill ({input.LevelAfter} L) exceeds the tank capacity ({tank.CapacityLitres} L).");
        var po = await Db.RefillPos.FirstAsync(p => p.RefillRequestId == id);

        var when = input.RefillDateTime
                   ?? (input.RefillDate is { } d
                       ? new DateTimeOffset(d.ToDateTime(TimeOnly.TryParse(input.RefillTime, out var tm) ? tm : TimeOnly.FromDateTime(DateTime.Now)), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now))
                       : DateTimeOffset.UtcNow);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var refill = new TankRefill
        {
            RefillRequestId = r.Id, TankId = tank.Id, RefillDatetime = when,
            VehicleId = await ResolveVehicle(input.VehicleId, input.TankerPlate, po.VendorId),
            DriverId = await ResolveDriver(input.DriverId, input.DriverName, po.VendorId),
            LevelBeforeLitres = input.LevelBefore, LevelAfterLitres = input.LevelAfter, DeliveredQtyLitres = input.LevelAfter - input.LevelBefore,
            SafetyCheckPassed = input.SafetyCheckPassed, Notes = input.Notes,
        };
        Db.TankRefills.Add(refill);
        await Db.SaveChangesAsync();
        await TanksController.RecordLevel(Db, Lookups, tank, input.LevelAfter, "refill", when, $"Refill {r.RequestNo}", refill.Id);
        await Advance(r, 6, $"Tank refilled: {refill.DeliveredQtyLitres:0.###} L delivered");
        // waiting for management approval
        await Advance(r, 7, "Waiting for management approval");
        await tx.CommitAsync();
        return Ok(await Get(id));
    }

    // ------------------------------------------------------------------ stage 7 → 8: approval

    [HttpPost("{id:long}/approve")]
    [RequirePermission("tanks.approve_refill")]
    public async Task<IActionResult> Approve(long id, [FromBody] ApproveInput input)
    {
        var (r, _) = await Load(id, expectedStage: 7);
        r.ApprovedBy = Me.UserId;
        r.ApprovedAt = DateTimeOffset.UtcNow;
        r.ApprovalNotes = input.Notes;
        r.CompletedAt = DateTimeOffset.UtcNow;
        await Advance(r, 8, input.Notes ?? "Approved and completed");
        return Ok(await Get(id));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var (r, stage) = await Load(id);
        if (stage >= 6) throw new BusinessRuleException("A refill that was already delivered cannot be cancelled.");
        var from = r.CurrentStageId;
        r.StatusId = await LkId(LookupTypes.RefillStatus, "cancelled");
        r.Notes = string.IsNullOrWhiteSpace(input.Reason) ? r.Notes : $"{r.Notes}\nCancelled: {input.Reason}".Trim();
        Db.RefillStageHistories.Add(new RefillStageHistory { RefillRequestId = r.Id, FromStageId = from, ToStageId = from ?? await LkId(LookupTypes.RefillStage, "0"), ChangedBy = Me.UserId, ChangedAt = DateTimeOffset.UtcNow, Remarks = $"Cancelled: {input.Reason}" });
        await Db.SaveChangesAsync();
        return Ok(new { r.Id, status = "cancelled", warning = stage >= 2 ? "A supplier payment was already recorded; handle the refund in vendor payments." : null });
    }

    // ------------------------------------------------------------------ helpers

    private async Task<(RefillRequest R, int Stage)> Load(long id, int? expectedStage = null)
    {
        await LoadLookups();
        var r = await Db.RefillRequests.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("RefillRequest", id);
        EnsureBranchAccess(r.BranchId);
        if (Code(r.StatusId) == "cancelled") throw new BusinessRuleException("This refill request is cancelled.");
        var stage = int.TryParse(Code(r.CurrentStageId), out var s) ? s : 0;
        if (expectedStage != null && stage != expectedStage)
            throw new BusinessRuleException($"This step needs the request to be at stage {expectedStage}; it is at stage {stage}.");
        return (r, stage);
    }

    private async Task Advance(RefillRequest r, int to, string remarks)
    {
        var from = r.CurrentStageId;
        r.CurrentStageId = await LkId(LookupTypes.RefillStage, to.ToString());
        r.StatusId = await LkId(LookupTypes.RefillStatus, StatusByStage[to]);
        await Db.SaveChangesAsync();
        await History(r, from, to, remarks);
        var buildingId = await Db.Tanks.Where(t => t.Id == r.TankId).Select(t => (long?)t.BuildingId).FirstOrDefaultAsync();
        Activity.Log("refill_request", r.Id, "status_change", $"{r.RequestNo}: {remarks}", buildingId, r.BranchId);
        await Db.SaveChangesAsync();
    }

    private async Task History(RefillRequest r, long? fromStageId, int to, string remarks)
    {
        Db.RefillStageHistories.Add(new RefillStageHistory
        {
            RefillRequestId = r.Id, FromStageId = fromStageId, ToStageId = await LkId(LookupTypes.RefillStage, to.ToString()),
            ChangedBy = Me.UserId, ChangedAt = DateTimeOffset.UtcNow, Remarks = remarks,
        });
        await Db.SaveChangesAsync();
    }

    private async Task<long?> ResolveVehicle(long? vehicleId, string? plate, long? vendorId)
    {
        if (vehicleId != null) return await Db.Vehicles.AnyAsync(v => v.Id == vehicleId) ? vehicleId : throw new NotFoundException("Vehicle", vehicleId);
        if (string.IsNullOrWhiteSpace(plate)) return null;
        plate = plate.Trim();
        var v = await Db.Vehicles.FirstOrDefaultAsync(x => x.PlateNo == plate);
        if (v == null) { v = new Vehicle { PlateNo = plate, VendorId = vendorId, IsActive = true }; Db.Vehicles.Add(v); await Db.SaveChangesAsync(); }
        return v.Id;
    }

    private async Task<long?> ResolveDriver(long? driverId, string? name, long? vendorId)
    {
        if (driverId != null) return await Db.Drivers.AnyAsync(d => d.Id == driverId) ? driverId : throw new NotFoundException("Driver", driverId);
        if (string.IsNullOrWhiteSpace(name)) return null;
        name = name.Trim();
        var d = await Db.Drivers.FirstOrDefaultAsync(x => x.NameAr == name || x.NameEn == name);
        if (d == null) { d = new Driver { NameAr = name, NameEn = name, VendorId = vendorId, IsActive = true }; Db.Drivers.Add(d); await Db.SaveChangesAsync(); }
        return d.Id;
    }

    /// <summary>Builds RefillRequestItem DTOs with stageData, loading each stage table once per page.</summary>
    private async Task<List<object>> BuildDtos(IReadOnlyList<RefillRequest> list)
    {
        var ids = list.Select(r => r.Id).ToList();
        var tankIds = list.Select(r => r.TankId).Distinct().ToList();
        var tanks = await Db.Tanks.AsNoTracking().Where(t => tankIds.Contains(t.Id))
            .Select(t => new { t.Id, t.TankNo, t.CapacityLitres, t.CurrentLevelLitres, t.BuildingId, BCode = t.Building!.Code, t.Building.NameEn, t.Building.NameAr, Gas = t.GasType!.Code })
            .ToDictionaryAsync(t => t.Id);
        var userIds = list.Select(r => r.RequestedBy).Concat(list.Select(r => r.ApprovedBy)).Where(x => x != null).Select(x => x!.Value).Distinct().ToList();
        var users = await Db.AppUsers.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => Loc(u.NameEn, u.NameAr));
        var pos = await Db.RefillPos.AsNoTracking().Where(p => ids.Contains(p.RefillRequestId))
            .Select(p => new { p.RefillRequestId, p.PoNumber, p.PoDate, p.QtyLitres, p.UnitPrice, p.TotalAmount, p.VendorId, p.Vendor!.NameEn, p.Vendor.NameAr, p.Vendor.Phone })
            .ToDictionaryAsync(p => p.RefillRequestId);
        var pays = await (from s in Db.RefillSupplierPayments.AsNoTracking()
                          join vp in Db.VendorPayments on s.VendorPaymentId equals vp.Id
                          where ids.Contains(s.RefillRequestId)
                          select new { s.RefillRequestId, vp.Id, vp.PaymentNo, vp.Amount, vp.PaymentDate, vp.PaymentMethodId, vp.ReferenceNo })
                         .ToDictionaryAsync(p => p.RefillRequestId);
        var cols = await Db.RefillCollections.AsNoTracking().Where(c => ids.Contains(c.RefillRequestId))
            .Select(c => new { c.RefillRequestId, c.VehicleId, Plate = c.Vehicle!.PlateNo, c.DriverId, DriverEn = c.Driver!.NameEn, DriverAr = c.Driver.NameAr, c.CollectionDate, c.MeterBefore, c.MeterAfter, c.CollectedQtyLitres })
            .ToDictionaryAsync(c => c.RefillRequestId);
        var bills = await (from p in Db.RefillPurchaseInvoices.AsNoTracking()
                           join b in Db.VendorBills on p.VendorBillId equals b.Id
                           where ids.Contains(p.RefillRequestId)
                           select new { p.RefillRequestId, b.Id, b.BillNo, b.BillDate, b.Subtotal, b.VatAmount, b.TotalAmount, b.StatusId })
                          .ToDictionaryAsync(b => b.RefillRequestId);
        var schedules = await Db.RefillSchedules.AsNoTracking().Where(s => ids.Contains(s.RefillRequestId)).ToDictionaryAsync(s => s.RefillRequestId);
        var refills = await Db.TankRefills.AsNoTracking().Where(t => ids.Contains(t.RefillRequestId))
            .Select(t => new { t.RefillRequestId, t.Id, t.RefillDatetime, t.LevelBeforeLitres, t.LevelAfterLitres, t.DeliveredQtyLitres, t.SafetyCheckPassed, t.Notes,
                               Plate = t.Vehicle!.PlateNo, DriverEn = t.Driver!.NameEn, DriverAr = t.Driver.NameAr })
            .ToDictionaryAsync(t => t.RefillRequestId);

        return list.Select(r =>
        {
            var t = tanks.GetValueOrDefault(r.TankId);
            var stage = int.TryParse(Code(r.CurrentStageId), out var s) ? s : 0;
            var cap = t?.CapacityLitres ?? 0;
            var level = t?.CurrentLevelLitres ?? 0;
            pos.TryGetValue(r.Id, out var po); pays.TryGetValue(r.Id, out var pay); cols.TryGetValue(r.Id, out var col);
            bills.TryGetValue(r.Id, out var bill); schedules.TryGetValue(r.Id, out var sch); refills.TryGetValue(r.Id, out var rf);
            return (object)new
            {
                r.Id, r.RequestNo, stage, status = Code(r.StatusId), statusLabel = Label(r.StatusId), priority = Code(r.PriorityId),
                r.RequestDate, requestedById = r.RequestedBy, requestedBy = r.RequestedBy == null ? null : users.GetValueOrDefault(r.RequestedBy.Value),
                tankId = r.TankId, tank = t?.TankNo, tankLabel = t == null ? null : $"{t.TankNo} – {t.Gas} ({t.CapacityLitres:0} L)",
                buildingId = t?.BuildingId, buildingCode = t?.BCode, building = t == null ? null : Loc(t.NameEn, t.NameAr),
                requestedQty = r.RequestedQtyLitres, quantityUnit = "L", levelAtRequest = r.LevelAtRequestLitres,
                currentLevel = level, capacity = cap, levelPct = cap > 0 ? Math.Round(level / cap * 100, 1) : 0, r.Notes,
                completedDate = r.CompletedAt, deliveredQty = rf?.DeliveredQtyLitres, newTankLevel = rf?.LevelAfterLitres,
                newLevelPct = rf != null && cap > 0 ? Math.Round((rf.LevelAfterLitres ?? 0) / cap * 100, 1) : (decimal?)null,
                stageData = new
                {
                    po = po == null ? null : new { supplierId = po.VendorId, supplier = Loc(po.NameEn, po.NameAr), po.PoNumber, po.PoDate, poQty = po.QtyLitres, po.UnitPrice, po.TotalAmount, supplierContact = po.Phone },
                    payment = pay == null ? null : new { vendorPaymentId = pay.Id, pay.PaymentNo, pay.Amount, date = pay.PaymentDate, method = Code(pay.PaymentMethodId), reference = pay.ReferenceNo },
                    collection = col == null ? null : new { col.VehicleId, tankerPlate = col.Plate, col.DriverId, driverName = Loc(col.DriverEn, col.DriverAr), col.CollectionDate, col.MeterBefore, col.MeterAfter, collectedQty = col.CollectedQtyLitres },
                    invoice = bill == null ? null : new { vendorBillId = bill.Id, invoiceNumber = bill.BillNo, invoiceDate = bill.BillDate, amount = bill.TotalAmount, bill.Subtotal, bill.VatAmount, status = Code(bill.StatusId) },
                    schedule = sch == null ? null : new { sch.DeliveryDate, deliveryTime = sch.TimeSlot, sch.CivilDefenseConfirmed, sch.CivilDefenseRef },
                    refill = rf == null ? null : new { tankRefillId = rf.Id, refillDate = rf.RefillDatetime, refillTime = rf.RefillDatetime.ToString("HH:mm"), tankMeterBefore = rf.LevelBeforeLitres, tankMeterAfter = rf.LevelAfterLitres,
                                                      deliveredQty = rf.DeliveredQtyLitres, rf.SafetyCheckPassed, driverName = Loc(rf.DriverEn, rf.DriverAr), tankerPlate = rf.Plate, rf.Notes },
                    approval = r.ApprovedAt == null ? null : new { approvedBy = r.ApprovedBy == null ? null : users.GetValueOrDefault(r.ApprovedBy.Value), approvalDate = r.ApprovedAt, notes = r.ApprovalNotes },
                }
            };
        }).ToList();
    }
}
