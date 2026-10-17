using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Annual maintenance contracts (AddAmcContractModal) with scope, terms, payment schedule and generated visits.</summary>
[Route("api/amc-contracts")]
[PermissionModule("amc")]
public class AmcContractsController : ApiControllerBase
{
    public record TermInput(string? Type, string Text);
    public record ScheduleInput(string? Label, DateOnly DueDate, decimal Amount, string? Notes);
    public record AmcInput(long BuildingId, long VendorId, long? OwnerPartyId, DateOnly StartDate, DateOnly EndDate, decimal? AnnualValue,
        string? VisitFrequency, short? VisitsTotal, short? ResponseTimeHours, bool EmergencyIncluded, string? Status, string? Notes,
        List<string>? Scope, List<string>? ExcludedScope, List<TermInput>? Terms, List<string>? CustomTerms, List<ScheduleInput>? PaymentSchedule,
        bool GenerateVisits = true, bool AutoPaymentSchedule = true);
    public record BillInput(string BillNo, DateOnly? BillDate, decimal? VatRate);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? vendorId, [FromQuery] string? status, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.AmcContracts.AsNoTracking().Where(a => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(a.BranchId));
        if (buildingId != null) query = query.Where(a => a.BuildingId == buildingId);
        if (vendorId != null) query = query.Where(a => a.VendorId == vendorId);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.AmcStatus, status); query = query.Where(a => a.StatusId == s); }
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(a => a.ContractNo.Contains(q.Search) || (a.Vendor!.NameAr ?? "").Contains(q.Search) || (a.Vendor.NameEn ?? "").Contains(q.Search));
        var page = await query.OrderByDescending(a => a.StartDate).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        await LoadLookups();
        var a = await Db.AmcContracts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("AmcContract", id);
        EnsureBranchAccess(a.BranchId);
        return Ok((await ToDtos(new[] { a }))[0]);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AmcInput input)
    {
        var b = await Db.Buildings.FirstOrDefaultAsync(x => x.Id == input.BuildingId) ?? throw new NotFoundException("Building", input.BuildingId);
        EnsureBranchAccess(b.BranchId);
        Validate(input);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var a = new AmcContract { BranchId = b.BranchId, BuildingId = b.Id, ContractNo = await Numbers.NextAsync(NumberPrefixes.Amc, date: input.StartDate), IsActive = true };
        await Apply(a, input);
        Db.AmcContracts.Add(a);
        await Db.SaveChangesAsync();
        await ReplaceChildren(a, input, replaceSchedule: true);
        if (input.GenerateVisits) await GenerateVisits(a);
        Activity.Log("amc_contract", a.Id, "create", $"AMC {a.ContractNo} created", a.BuildingId, a.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = a.Id }, new { a.Id, a.ContractNo });
    }

    /// <summary>Update the contract. Scope / terms are replaced when sent; only un-billed schedule lines are replaced.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] AmcInput input)
    {
        var a = await Db.AmcContracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("AmcContract", id);
        EnsureBranchAccess(a.BranchId);
        Validate(input);
        await using var tx = await Db.Database.BeginTransactionAsync();
        await Apply(a, input);
        await ReplaceChildren(a, input, replaceSchedule: input.PaymentSchedule != null);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> SetStatus(long id, [FromBody] BuildingsController.StatusInput input)
    {
        var a = await Db.AmcContracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("AmcContract", id);
        a.StatusId = await Lk(LookupTypes.AmcStatus, null, input.Status, true, "status");
        a.IsActive = input.Status is "active" or "pending";
        if (input.Status == "terminated")
        {
            var scheduled = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, "scheduled");
            var cancelled = await LkId(LookupTypes.VisitStatus, "cancelled");
            await Db.MaintenanceVisits.Where(v => v.AmcContractId == id && v.StatusId == scheduled && v.ScheduledDate >= Today).ForEachAsync(v => v.StatusId = cancelled);
        }
        Activity.Log("amc_contract", a.Id, "status_change", $"AMC {a.ContractNo} → {input.Status}", a.BuildingId, a.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var a = await Db.AmcContracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("AmcContract", id);
        if (await Db.AmcPaymentSchedules.AnyAsync(s => s.ContractId == id && s.VendorBillId != null)) throw new BusinessRuleException("The contract has billed instalments.");
        var completed = await Lookups.IdOrNullAsync(LookupTypes.VisitStatus, "completed");
        if (await Db.MaintenanceVisits.AnyAsync(v => v.AmcContractId == id && v.StatusId == completed)) throw new BusinessRuleException("The contract has completed visits; terminate it instead.");
        Db.AmcContracts.Remove(a);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Turn a payment-schedule instalment into a vendor bill (A/P).</summary>
    [HttpPost("{id:long}/payment-schedule/{scheduleId:long}/bill")]
    [RequirePermission("payables.manage")]
    public async Task<IActionResult> BillInstalment(long id, long scheduleId, [FromBody] BillInput input)
    {
        var a = await Db.AmcContracts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("AmcContract", id);
        var s = await Db.AmcPaymentSchedules.FirstOrDefaultAsync(x => x.Id == scheduleId && x.ContractId == id) ?? throw new NotFoundException("AmcPaymentSchedule", scheduleId);
        if (s.VendorBillId != null) throw new BusinessRuleException("This instalment is already billed.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var bill = await HttpContext.RequestServices.GetRequiredService<BillingService>().CreateVendorBillAsync(a.BranchId, a.VendorId, input.BillNo.Trim(),
            input.BillDate ?? Today, s.DueDate, "amc_contract", a.Id,
            new[] { new BillingService.BillLineInput("maintenance", null, $"AMC {a.ContractNo} – {s.Label}", 1, "each", s.Amount, input.VatRate, null) }, null, a.CurrencyId);
        s.VendorBillId = bill.Id;
        s.StatusId = await LkId(LookupTypes.AmcPaymentStatus, "billed");
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { bill.Id, bill.TotalAmount });
    }

    // ------------------------------------------------------------------ helpers

    private static void Validate(AmcInput i)
    {
        if (i.EndDate <= i.StartDate) throw new AppException("End date must be after the start date.");
        if (i.PaymentSchedule != null && i.AnnualValue != null && i.PaymentSchedule.Count > 0 && i.PaymentSchedule.Sum(p => p.Amount) != i.AnnualValue)
            throw new AppException($"Payment schedule total ({i.PaymentSchedule.Sum(p => p.Amount)}) must equal the contract value ({i.AnnualValue}).");
    }

    private async Task Apply(AmcContract a, AmcInput i)
    {
        if (!await Db.Vendors.AnyAsync(v => v.Id == i.VendorId)) throw new NotFoundException("Vendor", i.VendorId);
        a.VendorId = i.VendorId; a.OwnerPartyId = i.OwnerPartyId ?? await Db.BuildingOwners.Where(o => o.BuildingId == a.BuildingId && o.IsPrimary).Select(o => (long?)o.PartyId).FirstOrDefaultAsync();
        a.StartDate = i.StartDate; a.EndDate = i.EndDate; a.AnnualValue = i.AnnualValue;
        a.CurrencyId ??= await Db.Currencies.Where(c => c.Code == "AED").Select(c => (long?)c.Id).FirstOrDefaultAsync();
        a.VisitFrequencyId = await Lk(LookupTypes.VisitFrequency, null, i.VisitFrequency ?? "quarterly", field: "visitFrequency");
        a.VisitsTotal = i.VisitsTotal ?? (short)VisitDates(i.StartDate, i.EndDate, i.VisitFrequency ?? "quarterly").Count;
        a.ResponseTimeHours = i.ResponseTimeHours; a.EmergencyIncluded = i.EmergencyIncluded; a.Notes = i.Notes;
        var status = i.Status ?? (i.StartDate > Today ? "pending" : "active");
        a.StatusId = await Lk(LookupTypes.AmcStatus, null, status, field: "status");
        a.IsActive = status is "active" or "pending";
    }

    private async Task ReplaceChildren(AmcContract a, AmcInput i, bool replaceSchedule)
    {
        if (i.Scope != null || i.ExcludedScope != null)
        {
            Db.AmcScopeItems.RemoveRange(await Db.AmcScopeItems.Where(s => s.ContractId == a.Id).ToListAsync());
            var inc = await LkId(LookupTypes.ScopeItemType, "included");
            var exc = await LkId(LookupTypes.ScopeItemType, "excluded");
            foreach (var s in (i.Scope ?? new()).Where(x => !string.IsNullOrWhiteSpace(x))) Db.AmcScopeItems.Add(new AmcScopeItem { ContractId = a.Id, ItemTypeId = inc, Description = s.Trim() });
            foreach (var s in (i.ExcludedScope ?? new()).Where(x => !string.IsNullOrWhiteSpace(x))) Db.AmcScopeItems.Add(new AmcScopeItem { ContractId = a.Id, ItemTypeId = exc, Description = s.Trim() });
        }
        if (i.Terms != null || i.CustomTerms != null)
        {
            Db.AmcTerms.RemoveRange(await Db.AmcTerms.Where(s => s.ContractId == a.Id).ToListAsync());
            foreach (var t in (i.Terms ?? new()).Where(x => !string.IsNullOrWhiteSpace(x.Text)))
                Db.AmcTerms.Add(new AmcTerm { ContractId = a.Id, TermTypeId = await Lk(LookupTypes.AmcTermType, null, t.Type ?? "general", field: "term type"), Description = t.Text.Trim() });
            foreach (var t in (i.CustomTerms ?? new()).Where(x => !string.IsNullOrWhiteSpace(x)))
                Db.AmcTerms.Add(new AmcTerm { ContractId = a.Id, TermTypeId = await LkId(LookupTypes.AmcTermType, "other"), Description = t.Trim(), IsCustom = true });
        }
        if (replaceSchedule)
        {
            Db.AmcPaymentSchedules.RemoveRange(await Db.AmcPaymentSchedules.Where(s => s.ContractId == a.Id && s.VendorBillId == null).ToListAsync());
            var pending = await LkId(LookupTypes.AmcPaymentStatus, "pending");
            var lines = i.PaymentSchedule ?? new();
            if (lines.Count == 0 && i.AutoPaymentSchedule && (a.AnnualValue ?? 0) > 0)
            {
                // default: one instalment per quarter across the contract period
                var dates = VisitDates(a.StartDate, a.EndDate, "quarterly");
                var each = Math.Round(a.AnnualValue!.Value / dates.Count, 2);
                lines = dates.Select((d, n) => new ScheduleInput($"Instalment {n + 1}", d, n == dates.Count - 1 ? a.AnnualValue.Value - each * (dates.Count - 1) : each, null)).ToList();
            }
            foreach (var s in lines) Db.AmcPaymentSchedules.Add(new AmcPaymentSchedule { ContractId = a.Id, Label = s.Label, DueDate = s.DueDate, Amount = s.Amount, StatusId = pending, Notes = s.Notes });
        }
        await Db.SaveChangesAsync();
    }

    internal static List<DateOnly> VisitDates(DateOnly start, DateOnly end, string frequency)
    {
        var months = frequency switch { "monthly" => 1, "quarterly" => 3, "semi_annual" => 6, "annual" => 12, _ => 3 };
        var list = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddMonths(months)) list.Add(d);
        return list.Count == 0 ? new List<DateOnly> { start } : list;
    }

    private async Task GenerateVisits(AmcContract a)
    {
        var freq = await Lookups.CodeAsync(a.VisitFrequencyId) ?? "quarterly";
        var scheduled = await LkId(LookupTypes.VisitStatus, "scheduled");
        var preventive = await LkId(LookupTypes.VisitType, "preventive");
        var template = await Db.ChecklistTemplates.Where(t => t.VisitTypeId == preventive && t.IsActive).Select(t => (long?)t.Id).FirstOrDefaultAsync();
        var n = 0;
        foreach (var d in VisitDates(a.StartDate, a.EndDate, freq).Take(a.VisitsTotal ?? 100))
        {
            n++;
            var v = new MaintenanceVisit
            {
                BranchId = a.BranchId, VisitNo = await Numbers.NextAsync(NumberPrefixes.Visit, date: d), AmcContractId = a.Id, BuildingId = a.BuildingId,
                VisitTypeId = preventive, PeriodLabel = freq == "quarterly" ? $"Q{(d.Month - 1) / 3 + 1} {d.Year}" : $"{d:MMM yyyy}",
                ScheduledDate = d, StatusId = scheduled,
                VendorContactId = await Db.VendorContacts.Where(c => c.VendorId == a.VendorId && c.IsDefault).Select(c => (long?)c.Id).FirstOrDefaultAsync(),
            };
            Db.MaintenanceVisits.Add(v);
            await Db.SaveChangesAsync();
            if (template != null) await MaintenanceVisitsController.CopyChecklist(Db, v.Id, template.Value);
        }
    }

    private async Task<List<object>> ToDtos(IReadOnlyList<AmcContract> list)
    {
        var ids = list.Select(a => a.Id).ToList();
        var vendorIds = list.Select(a => a.VendorId).Distinct().ToList();
        var vendors = await Db.Vendors.AsNoTracking().Where(v => vendorIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);
        var contacts = await Db.VendorContacts.AsNoTracking().Where(c => vendorIds.Contains(c.VendorId)).OrderByDescending(c => c.IsDefault).ToListAsync();
        var partyIds = list.Where(a => a.OwnerPartyId != null).Select(a => a.OwnerPartyId!.Value).Distinct().ToList();
        var parties = await Db.Parties.AsNoTracking().Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var partyContacts = await Db.PartyContacts.AsNoTracking().Where(c => partyIds.Contains(c.PartyId)).ToListAsync();
        var scope = await Db.AmcScopeItems.AsNoTracking().Where(s => ids.Contains(s.ContractId)).ToListAsync();
        var terms = await Db.AmcTerms.AsNoTracking().Where(s => ids.Contains(s.ContractId)).ToListAsync();
        var schedule = await Db.AmcPaymentSchedules.AsNoTracking().Where(s => ids.Contains(s.ContractId)).OrderBy(s => s.DueDate).ToListAsync();
        var visits = await Db.MaintenanceVisits.AsNoTracking().Where(v => ids.Contains(v.AmcContractId)).OrderBy(v => v.ScheduledDate).ToListAsync();
        var buildings = await Db.Buildings.AsNoTracking().Where(b => list.Select(a => a.BuildingId).Contains(b.Id)).ToDictionaryAsync(b => b.Id);
        var billing = HttpContext.RequestServices.GetRequiredService<BillingService>();
        var paidBills = await billing.PaidByBillAsync(schedule.Where(s => s.VendorBillId != null).Select(s => s.VendorBillId!.Value));
        var bills = await Db.VendorBills.AsNoTracking().Where(b => schedule.Select(s => s.VendorBillId).Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.TotalAmount ?? 0);
        var emailType = await Lookups.IdOrNullAsync(LookupTypes.ContactType, "email");

        return list.Select(a =>
        {
            var v = vendors.GetValueOrDefault(a.VendorId);
            var vc = contacts.FirstOrDefault(c => c.VendorId == a.VendorId);
            var p = a.OwnerPartyId == null ? null : parties.GetValueOrDefault(a.OwnerPartyId.Value);
            var pcs = partyContacts.Where(c => c.PartyId == a.OwnerPartyId).ToList();
            var mv = visits.Where(x => x.AmcContractId == a.Id).ToList();
            var done = mv.Where(x => Code(x.StatusId) == "completed").OrderBy(x => x.VisitDate ?? x.ScheduledDate).ToList();
            var next = mv.Where(x => Code(x.StatusId) is "scheduled" or "in_progress" && x.ScheduledDate >= Today).OrderBy(x => x.ScheduledDate).FirstOrDefault();
            var b = buildings.GetValueOrDefault(a.BuildingId);
            return (object)new
            {
                a.Id, a.ContractNo, a.BuildingId, buildingCode = b?.Code, building = Loc(b?.NameEn, b?.NameAr),
                vendorId = a.VendorId, vendor = Loc(v?.NameEn, v?.NameAr), vendorContact = vc == null ? v?.Phone : $"{Loc(vc.NameEn, vc.NameAr)} {vc.Phone}".Trim(),
                vendorEmail = vc?.Email ?? v?.Email, ownerPartyId = a.OwnerPartyId, ownerName = Loc(p?.NameEn, p?.NameAr),
                ownerPhone = pcs.FirstOrDefault(c => c.ContactTypeId != emailType)?.Value, ownerEmail = pcs.FirstOrDefault(c => c.ContactTypeId == emailType)?.Value,
                a.StartDate, a.EndDate, a.AnnualValue, currency = "AED", status = Code(a.StatusId), statusLabel = Label(a.StatusId),
                scope = scope.Where(s => s.ContractId == a.Id && Code(s.ItemTypeId) == "included").Select(s => s.Description),
                excludedScope = scope.Where(s => s.ContractId == a.Id && Code(s.ItemTypeId) == "excluded").Select(s => s.Description),
                visitFrequency = Code(a.VisitFrequencyId), visitsTotal = a.VisitsTotal, visitSchedule = mv.Select(x => x.ScheduledDate),
                visits = mv.Select(x => new { x.Id, x.VisitNo, x.ScheduledDate, x.VisitDate, x.PeriodLabel, status = Code(x.StatusId) }),
                paymentSchedule = schedule.Where(s => s.ContractId == a.Id).Select(s => new
                {
                    s.Id, s.DueDate, s.Amount, s.Label, s.VendorBillId,
                    status = s.VendorBillId != null && paidBills.GetValueOrDefault(s.VendorBillId.Value) >= bills.GetValueOrDefault(s.VendorBillId.Value) && bills.GetValueOrDefault(s.VendorBillId.Value) > 0 ? "paid" : Code(s.StatusId) ?? "pending",
                }),
                terms = terms.Where(t => t.ContractId == a.Id && !t.IsCustom).Select(t => t.Description),
                customTerms = terms.Where(t => t.ContractId == a.Id && t.IsCustom).Select(t => t.Description),
                visitsDone = done.Count, lastVisit = done.LastOrDefault()?.VisitDate ?? done.LastOrDefault()?.ScheduledDate, lastVisitReport = done.LastOrDefault()?.ReportSummary,
                nextVisit = next?.ScheduledDate, responseTime = a.ResponseTimeHours == null ? null : $"{a.ResponseTimeHours}h", responseTimeHours = a.ResponseTimeHours,
                a.EmergencyIncluded, a.Notes,
            };
        }).ToList();
    }
}

[Route("api/amc-scope-items")]
[PermissionModule("amc")]
public class AmcScopeItemsController : CrudController<AmcScopeItem> { }

[Route("api/amc-terms")]
[PermissionModule("amc")]
public class AmcTermsController : CrudController<AmcTerm> { }

[Route("api/amc-payment-schedules")]
[PermissionModule("amc")]
public class AmcPaymentSchedulesController : CrudController<AmcPaymentSchedule> { }
