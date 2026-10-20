using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Tenants (gas customers): list with balances, profile drawer, statement.</summary>
[Route("api/tenants")]
[PermissionModule("tenants")]
public class TenantsController : ApiControllerBase
{
    private BalanceQueries Balances => HttpContext.RequestServices.GetRequiredService<BalanceQueries>();

    public record TenantInput(long? BranchId, string? NameAr, string? NameEn, string? Email, string? Phone, string? EmiratesId, string? Nationality,
        long? NationalityId, DateOnly? Dob, long? CityId, string? Address, long? UnitId, ContractService.ContractInput? Contract);

    public record ContactInput(string? Phone, string? Email, string? Address);

    /// <summary>Tenants list. Filters: buildingId, branchId, paymentStatus (paid|pending|overdue), active.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? branchId,
        [FromQuery] string? paymentStatus, [FromQuery] bool? active, CancellationToken ct)
    {
        await LoadLookups();
        var query = Db.Tenants.AsNoTracking().Where(t => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(t.BranchId));
        if (branchId != null) query = query.Where(t => t.BranchId == branchId);
        if (active != null) query = query.Where(t => t.IsActive == active);
        if (buildingId != null) query = query.Where(t => Db.SupplyContracts.Any(c => c.TenantId == t.Id && c.IsActive && c.Unit!.BuildingId == buildingId));
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(t => t.Party!.PartyCode.Contains(s) || (t.Party!.NameAr ?? "").Contains(s) || (t.Party!.NameEn ?? "").Contains(s) || (t.Party!.Phone ?? "").Contains(s)
                                     || (t.Party!.EmiratesId ?? "").Contains(s) || (t.Party!.Email ?? "").Contains(s));
        }
        var rows = query.OrderByDescending(t => t.Id).Select(t => new
        {
            T = t,
            C = Db.SupplyContracts.Where(c => c.TenantId == t.Id && c.IsActive).OrderByDescending(c => c.StartDate)
                .Select(c => new CRow
                {
                    Id = c.Id, ContractNo = c.ContractNo, StartDate = c.StartDate, EndDate = c.EndDate, DepositPaid = c.DepositPaid, UnitId = c.UnitId!.Value,
                    UnitNo = c.Unit!.UnitNo, FloorNo = c.Unit.FloorNo, BuildingId = c.Unit.BuildingId, BCode = c.Unit.Building!.Code,
                    BEn = c.Unit.Building.NameEn, BAr = c.Unit.Building.NameAr,
                    MeterNo = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => a.Meter!.MeterNo).FirstOrDefault(),
                    MeterId = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => (long?)a.MeterId).FirstOrDefault(),
                })
                .FirstOrDefault(),
        });

        // paymentStatus filter needs balances → filter in memory on the (bounded) result
        if (!string.IsNullOrEmpty(paymentStatus))
        {
            var all = await rows.Take(5000).ToListAsync(ct);
            var bal = await Balances.ByTenantAsync(all.Select(r => r.T.Id));
            var filtered = all.Where(r => (bal.GetValueOrDefault(r.T.Id)?.PaymentStatus ?? "paid") == paymentStatus).ToList();
            var size = q.PageSize <= 0 ? filtered.Count : Math.Clamp(q.PageSize, 1, 500);
            var pageItems = filtered.Skip((Math.Max(1, q.Page) - 1) * size).Take(size).ToList();
            return Ok(new PagedResult<object>(pageItems.Select(r => Dto(r.T, r.C, bal.GetValueOrDefault(r.T.Id))).ToList(), filtered.Count, q.Page, size));
        }

        var page = await rows.ToPagedAsync(q, ct);
        var balances = await Balances.ByTenantAsync(page.Items.Select(r => r.T.Id));
        return Ok(new PagedResult<object>(page.Items.Select(r => Dto(r.T, r.C, balances.GetValueOrDefault(r.T.Id))).ToList(), page.Total, page.Page, page.PageSize));

        object Dto(Tenant t, CRow? c, BalanceQueries.Balance? b) => new
        {
            t.Id, code = t.Party!.PartyCode, name = Loc(t.Party!.NameEn, t.Party!.NameAr), t.Party!.NameEn, t.Party!.NameAr, avatar = Initials(t.Party!.NameAr ?? t.Party!.NameEn), t.Party!.Phone, t.Party!.Email, t.Party!.EmiratesId,
            nationality = Label(t.Party!.NationalityId), t.IsActive, t.BranchId,
            contractId = c?.Id, contractNo = c?.ContractNo, contractStart = c?.StartDate, contractEnd = c?.EndDate,
            depositPaid = c?.DepositPaid, unitId = c?.UnitId, unit = c?.UnitNo, floor = c?.FloorNo,
            buildingId = c?.BuildingId, buildingCode = c?.BCode, building = c == null ? null : Loc(c.BEn, c.BAr),
            meterId = c?.MeterId, meterNo = c?.MeterNo,
            invoiced = b?.Invoiced ?? 0, paid = b?.Paid ?? 0, balance = b?.Outstanding ?? 0, overdue = b?.Overdue ?? 0, paymentStatus = b?.PaymentStatus ?? "paid",
        };
    }

    public record ActiveInput(bool IsActive, string? Reason);

    /// <summary>KPIs of the customers page: counts, outstanding and overdue totals (branch-scoped).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] long? branchId, CancellationToken ct)
    {
        var query = Db.Tenants.AsNoTracking().Where(t => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(t.BranchId));
        if (branchId != null) query = query.Where(t => t.BranchId == branchId);
        var ids = await query.Select(t => t.Id).ToListAsync(ct);
        var active = await query.CountAsync(t => t.IsActive, ct);
        var withContract = await query.CountAsync(t => Db.SupplyContracts.Any(c => c.TenantId == t.Id && c.IsActive), ct);
        var bal = await Balances.ByTenantAsync(ids);
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);
        var newThisMonth = await query.CountAsync(t => t.CreatedAt >= new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero), ct);
        return Ok(new
        {
            total = ids.Count, active, inactive = ids.Count - active, withContract, newThisMonth,
            withBalance = bal.Values.Count(b => b.Outstanding > 0), outstanding = bal.Values.Sum(b => b.Outstanding),
            overdueCount = bal.Values.Count(b => b.Overdue > 0), overdue = bal.Values.Sum(b => b.Overdue),
        });
    }

    /// <summary>Deactivate / re-activate a customer (kept with its history). A customer with an active contract cannot be deactivated.</summary>
    [HttpPatch("{id:long}/active")]
    public async Task<IActionResult> SetActive(long id, [FromBody] ActiveInput input)
    {
        var t = await Db.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tenant", id);
        EnsureBranchAccess(t.BranchId);
        if (!input.IsActive && await Db.SupplyContracts.AnyAsync(c => c.TenantId == id && c.IsActive))
            throw new BusinessRuleException("The customer has an active supply contract. Terminate it first.");
        t.IsActive = input.IsActive;
        Activity.Log("tenant", t.Id, "status_change", $"Customer {t.Party!.PartyCode} {(input.IsActive ? "re-activated" : "deactivated")}{(string.IsNullOrWhiteSpace(input.Reason) ? "" : ": " + input.Reason)}", null, t.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Tenant profile (TenantProfileDrawer).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var t = await Db.Tenants.AsNoTracking().Include(x => x.Party!.City).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Tenant", id);
        EnsureBranchAccess(t.BranchId);
        var contracts = await Db.SupplyContracts.AsNoTracking().Where(c => c.TenantId == id).OrderByDescending(c => c.IsActive).ThenByDescending(c => c.StartDate)
            .Select(c => new { c.Id, c.ContractNo, c.StatusId, c.IsActive, c.StartDate, c.EndDate, c.DepositAmount, c.DepositPaid, c.UnitId, c.Unit!.UnitNo, c.Unit.FloorNo, c.Unit.RentAmount,
                               c.Unit.BuildingId, BCode = c.Unit.Building!.Code, BEn = c.Unit.Building.NameEn, BAr = c.Unit.Building.NameAr,
                               MeterId = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => (long?)a.MeterId).FirstOrDefault(),
                               MeterNo = Db.MeterAssignments.Where(a => a.UnitId == c.UnitId && a.ToDate == null).Select(a => a.Meter!.MeterNo).FirstOrDefault() })
            .ToListAsync(ct);
        var bal = (await Balances.ByTenantAsync(new[] { id })).GetValueOrDefault(id) ?? new BalanceQueries.Balance(0, 0, 0);
        var credit = await Balances.TenantCreditAsync(id);
        var current = contracts.FirstOrDefault(c => c.IsActive) ?? contracts.FirstOrDefault();
        var lastReading = current == null ? null : await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId == current.Id)
            .OrderByDescending(r => r.ReadingDate).Select(r => new { r.ReadingDate, r.ReadingValue, r.Consumption }).FirstOrDefaultAsync(ct);
        var unbilledM3 = current == null ? 0 : await Db.MeterReadings.Where(r => r.ContractId == current.Id && r.InvoiceId == null).SumAsync(r => r.Consumption ?? 0, ct);

        return Ok(new
        {
            t.Id, code = t.Party!.PartyCode, name = Loc(t.Party!.NameEn, t.Party!.NameAr), t.Party!.NameEn, t.Party!.NameAr, avatar = Initials(t.Party!.NameAr ?? t.Party!.NameEn),
            t.Party!.Phone, t.Party!.Email, t.Party!.EmiratesId, nationality = Label(t.Party!.NationalityId), nationalityCode = Code(t.Party!.NationalityId), t.Party!.NationalityId,
            dob = t.Party!.DateOfBirth, t.Party!.CityId, city = Loc(t.Party!.City?.NameEn, t.Party!.City?.NameAr), t.Party!.Address, t.IsActive, t.BranchId, t.CreatedAt,
            unitId = current?.UnitId, unit = current?.UnitNo, floor = current?.FloorNo, rent = current?.RentAmount,
            buildingId = current?.BuildingId, buildingCode = current?.BCode, building = current == null ? null : Loc(current.BEn, current.BAr),
            meterId = current?.MeterId, meterNo = current?.MeterNo,
            contractId = current?.Id, contractNo = current?.ContractNo, contractStart = current?.StartDate, contractEnd = current?.EndDate, depositPaid = current?.DepositPaid,
            paymentStatus = bal.PaymentStatus, invoiced = bal.Invoiced, paid = bal.Paid, balance = bal.Outstanding, overdue = bal.Overdue, credit,
            gasBalance = unbilledM3, lastReading,
            contracts = contracts.Select(c => new { c.Id, c.ContractNo, status = Code(c.StatusId), c.IsActive, c.StartDate, c.EndDate, c.UnitNo, building = Loc(c.BEn, c.BAr), c.MeterNo, c.DepositAmount, c.DepositPaid }),
        });
    }

    /// <summary>
    /// Create a tenant (AddTenantModal). With "contract" a supply contract is created in the same transaction;
    /// with only "unitId" a draft contract links the tenant to the unit (activate it from the contract screen).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TenantInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NameAr) && string.IsNullOrWhiteSpace(input.NameEn)) throw new AppException("Tenant name is required.");
        long branchId;
        if (input.UnitId != null)
            branchId = await Db.Units.Where(u => u.Id == input.UnitId).Select(u => (long?)u.Building!.BranchId).FirstOrDefaultAsync() ?? throw new NotFoundException("Unit", input.UnitId);
        else branchId = RequireBranch(input.BranchId);
        EnsureBranchAccess(branchId);
        await CheckEmiratesId(input.EmiratesId, null);

        await using var tx = await Db.Database.BeginTransactionAsync();
        // the same person may already be in the system (owner, supplier, employee …): reuse the party, add the customer role
        var eid = string.IsNullOrWhiteSpace(input.EmiratesId) ? null : input.EmiratesId.Trim();
        var party = eid == null ? null : await Db.Parties.FirstOrDefaultAsync(p => p.EmiratesId == eid);
        party ??= await Parties.NewAsync(branchId, new PartyService.PartyInput { NameAr = input.NameAr, NameEn = input.NameEn });
        var t = new Tenant { Party = party, BranchId = branchId, IsActive = true };
        await Apply(t, input);
        Db.Tenants.Add(t);
        await Db.SaveChangesAsync();
        await Parties.EnsureAccountAsync(t.Id);

        long? contractId = null;
        if (input.Contract != null || input.UnitId != null)
        {
            var ci = input.Contract ?? new ContractService.ContractInput { UnitId = input.UnitId, StartDate = Today, Status = "draft" };
            ci = ci with { TenantId = t.Id, UnitId = ci.UnitId ?? input.UnitId };
            contractId = (await HttpContext.RequestServices.GetRequiredService<ContractService>().CreateAsync(ci)).Id;
        }
        Activity.Log("tenant", t.Id, "create", $"Tenant {t.Party!.PartyCode} created", null, t.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { t.Id, code = t.Party!.PartyCode, contractId });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TenantInput input)
    {
        var t = await Db.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tenant", id);
        EnsureBranchAccess(t.BranchId);
        await CheckEmiratesId(input.EmiratesId, id);
        await Apply(t, input);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Quick edit of phone / email / address (EditContactModal).</summary>
    [HttpPatch("{id:long}/contact")]
    public async Task<IActionResult> UpdateContact(long id, [FromBody] ContactInput input)
    {
        var t = await Db.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tenant", id);
        EnsureBranchAccess(t.BranchId);
        if (input.Phone != null) t.Party!.Phone = input.Phone.Trim();
        if (input.Email != null) t.Party!.Email = input.Email.Trim();
        if (input.Address != null) t.Party!.Address = input.Address;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await Db.Tenants.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tenant", id);
        if (await Db.SupplyContracts.AnyAsync(c => c.TenantId == id && c.IsActive)) throw new BusinessRuleException("The tenant has an active supply contract.");
        var bal = (await Balances.ByTenantAsync(new[] { id })).GetValueOrDefault(id);
        if (bal != null && bal.Outstanding != 0) throw new BusinessRuleException($"The tenant still has an outstanding balance of {bal.Outstanding:0.##}.");
        Db.Tenants.Remove(t);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Account statement: invoices (debit) and payments (credit) with a running balance.</summary>
    [HttpGet("{id:long}/statement")]
    public async Task<IActionResult> Statement(long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        await LoadLookups();
        var t = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Tenant", id);
        var (deadInv, deadPay) = await Balances.DeadStatusesAsync();
        var inv = await Db.Invoices.AsNoTracking().Where(i => i.TenantId == id && !deadInv.Contains(i.StatusId))
            .Select(i => new { Date = i.IssueDate, Ref = i.InvoiceNo, i.Id, Type = "invoice", i.InvoiceTypeId, Debit = i.TotalAmount ?? 0, Credit = 0m, i.DueDate }).ToListAsync();
        var pay = await Db.Payments.AsNoTracking().Where(p => p.TenantId == id && !deadPay.Contains(p.StatusId))
            .Select(p => new { Date = p.PaymentDate, Ref = p.ReceiptNo, p.Id, Type = "payment", InvoiceTypeId = p.PaymentMethodId, Debit = 0m, Credit = p.Amount, DueDate = (DateOnly?)null }).ToListAsync();
        var lines = inv.Select(i => new { i.Date, i.Ref, i.Id, i.Type, i.InvoiceTypeId, i.Debit, i.Credit, DueDate = (DateOnly?)i.DueDate })
            .Concat(pay.Select(p => new { p.Date, p.Ref, p.Id, p.Type, p.InvoiceTypeId, p.Debit, p.Credit, p.DueDate }))
            .OrderBy(l => l.Date).ThenBy(l => l.Type == "payment").ToList();
        var opening = lines.Where(l => from != null && l.Date < from).Sum(l => l.Debit - l.Credit);
        var running = opening;
        var rows = lines.Where(l => (from == null || l.Date >= from) && (to == null || l.Date <= to)).Select(l =>
        {
            running += l.Debit - l.Credit;
            return new { l.Date, l.Type, l.Id, reference = l.Ref, description = Label(l.InvoiceTypeId), l.Debit, l.Credit, balance = running, l.DueDate };
        }).ToList();
        return Ok(new { tenantId = id, tenant = Loc(t.Party!.NameEn, t.Party!.NameAr), from, to, opening, closing = running,
                        totalDebit = rows.Sum(r => r.Debit), totalCredit = rows.Sum(r => r.Credit), lines = rows });
    }

    // ------------------------------------------------------------------ helpers

    private class CRow
    {
        public long Id { get; init; }
        public string ContractNo { get; init; } = "";
        public DateOnly StartDate { get; init; }
        public DateOnly? EndDate { get; init; }
        public bool DepositPaid { get; init; }
        public long UnitId { get; init; }
        public string? UnitNo { get; init; }
        public int? FloorNo { get; init; }
        public long BuildingId { get; init; }
        public string? BCode { get; init; }
        public string? BEn { get; init; }
        public string? BAr { get; init; }
        public long? MeterId { get; init; }
        public string? MeterNo { get; init; }
    }

    private PartyService Parties => HttpContext.RequestServices.GetRequiredService<PartyService>();

    private async Task Apply(Tenant t, TenantInput i)
    {
        await Parties.ApplyAsync(t.Party!, new PartyService.PartyInput
        {
            NameAr = i.NameAr, NameEn = i.NameEn, Email = i.Email, Phone = i.Phone, EmiratesId = i.EmiratesId,
            NationalityId = await Lk(LookupTypes.Nationality, i.NationalityId, i.Nationality, field: "nationality"),
            DateOfBirth = i.Dob, CityId = i.CityId, Address = i.Address,
        }, onlyGiven: false);
    }

    /// <summary>One customer per person: the Emirates ID may belong to an owner / supplier / employee, but not to another customer.</summary>
    private async Task CheckEmiratesId(string? eid, long? exceptId)
    {
        if (string.IsNullOrWhiteSpace(eid)) return;
        var code = await Db.Tenants.Where(t => t.Party!.EmiratesId == eid.Trim() && t.Id != exceptId).Select(t => t.Party!.PartyCode).FirstOrDefaultAsync();
        if (code != null) throw new BusinessRuleException($"A customer with Emirates ID {eid} already exists ({code}).");
    }

    internal static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(p => p[0])).ToUpperInvariant();
    }
}
