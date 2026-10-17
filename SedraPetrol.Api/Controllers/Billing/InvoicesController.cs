using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Tenant invoices (A/R): list, detail, manual invoices, consumption billing run, issue, cancel, overdue.</summary>
[Route("api/invoices")]
[PermissionModule("invoices")]
public class InvoicesController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record ItemInput(string? ItemType, long? ItemTypeId, string? Description, decimal Quantity, string? Uom, decimal UnitPrice, decimal? VatRate);
    public record CreateInput(long TenantId, long? ContractId, long? BuildingId, string? Type, DateOnly? IssueDate, DateOnly? DueDate,
        DateOnly? PeriodStart, DateOnly? PeriodEnd, string? Notes, List<ItemInput> Items, bool Issue = true);
    public record CancelInput(string? Reason);
    public record RunInput(long? BuildingId, long? BranchId, List<long>? ContractIds, DateOnly PeriodStart, DateOnly PeriodEnd, bool Issue = true);

    /// <summary>Invoices. Filters: tenantId, buildingId, contractId, status, type, from, to (issue date), overdueOnly, openOnly.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PageQuery q, [FromQuery] long? tenantId, [FromQuery] long? buildingId, [FromQuery] long? contractId,
        [FromQuery] long? branchId, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] bool overdueOnly = false, [FromQuery] bool openOnly = false, CancellationToken ct = default)
    {
        await LoadLookups();
        var query = Filter(tenantId, buildingId, contractId, branchId, from, to);
        if (!string.IsNullOrEmpty(status)) { var s = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, status); query = query.Where(i => i.StatusId == s); }
        if (!string.IsNullOrEmpty(type)) { var s = await Lookups.IdOrNullAsync(LookupTypes.InvoiceType, type); query = query.Where(i => i.InvoiceTypeId == s); }
        var open = await Billing.InvoiceStatusIdsAsync("pending", "partial", "overdue");
        if (openOnly || overdueOnly) query = query.Where(i => i.StatusId != null && open.Contains(i.StatusId.Value));
        if (overdueOnly) query = query.Where(i => i.DueDate < Today);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(i => i.InvoiceNo.Contains(q.Search) || (i.Tenant!.NameAr ?? "").Contains(q.Search) || (i.Tenant.NameEn ?? "").Contains(q.Search));
        query = q.Sort?.ToLowerInvariant() switch
        {
            "duedate" => q.Desc ? query.OrderByDescending(i => i.DueDate) : query.OrderBy(i => i.DueDate),
            "amount" => q.Desc ? query.OrderByDescending(i => i.TotalAmount) : query.OrderBy(i => i.TotalAmount),
            _ => q.Desc ? query.OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.Id) : query.OrderBy(i => i.IssueDate).ThenBy(i => i.Id),
        };
        var page = await Project(query).ToPagedAsync(q, ct);
        return Ok(new PagedResult<object>(await ToDtos(page.Items), page.Total, page.Page, page.PageSize));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken ct)
    {
        await LoadLookups();
        var row = await Project(Db.Invoices.AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Invoice", id);
        EnsureBranchAccess(row.I.BranchId);
        var items = await Db.InvoiceItems.AsNoTracking().Where(i => i.InvoiceId == id).OrderBy(i => i.Id).ToListAsync(ct);
        var payments = await Db.PaymentAllocations.AsNoTracking().Where(a => a.InvoiceId == id)
            .Select(a => new { a.Id, a.PaymentId, a.Payment!.ReceiptNo, a.Payment.PaymentDate, a.Amount, a.Payment.PaymentMethodId, a.Payment.StatusId, a.Payment.ReferenceNo }).ToListAsync(ct);
        var journal = row.I.JournalEntryId == null ? null : await Db.JournalEntries.AsNoTracking().Where(j => j.Id == row.I.JournalEntryId).Select(j => new { j.Id, j.JournalNo }).FirstOrDefaultAsync(ct);
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        return Ok(new
        {
            invoice = (await ToDtos(new[] { row }))[0],
            items = items.Select(i => new { i.Id, type = Code(i.ItemTypeId), typeLabel = Label(i.ItemTypeId), i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate, i.Amount,
                                            vat = BillingService.R2((i.Amount ?? 0) * (i.VatRate ?? 0) / 100), i.MeterReadingId }),
            payments = payments.Select(p => new { p.Id, p.PaymentId, p.ReceiptNo, date = p.PaymentDate, p.Amount, method = Code(p.PaymentMethodId), status = Code(p.StatusId), reference = p.ReferenceNo }),
            journal,
            company,
        });
    }

    /// <summary>Create a manual invoice (NewInvoiceModal). issue=false keeps it as a draft.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInput input)
    {
        var tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == input.TenantId) ?? throw new NotFoundException("Tenant", input.TenantId);
        EnsureBranchAccess(tenant.BranchId);
        var buildingId = input.BuildingId;
        if (input.ContractId != null)
        {
            var c = await Db.SupplyContracts.Include(x => x.Unit).FirstOrDefaultAsync(x => x.Id == input.ContractId && x.TenantId == tenant.Id)
                    ?? throw new AppException("The contract does not belong to this tenant.");
            buildingId ??= c.Unit!.BuildingId;
        }
        if (input.Issue && !Me.HasPermission("invoices.post")) throw new AppException("Missing permission: invoices.post", 403);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var inv = await Billing.CreateInvoiceAsync(new InvoiceDraft(tenant.BranchId, tenant.Id, buildingId, input.ContractId, input.Type, null,
            input.IssueDate ?? Today, input.DueDate, input.PeriodStart, input.PeriodEnd, null, input.Notes,
            input.Items.Select(i => new InvoiceLineInput(i.ItemType, i.ItemTypeId, i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate)).ToList(), input.Issue));
        Activity.Log("invoice", inv.Id, "create", $"Invoice {inv.InvoiceNo} ({inv.TotalAmount:0.##})", buildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = inv.Id }, new { inv.Id, inv.InvoiceNo, inv.TotalAmount });
    }

    /// <summary>Replace the lines of a draft invoice.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateDraft(long id, [FromBody] CreateInput input)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be edited.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        var old = await Db.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync();
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        Db.InvoiceItems.RemoveRange(old);
        Db.Invoices.Remove(inv);
        await Db.SaveChangesAsync();
        var created = await Billing.CreateInvoiceAsync(new InvoiceDraft(inv.BranchId, input.TenantId, input.BuildingId ?? inv.BuildingId, input.ContractId ?? inv.ContractId, input.Type, null,
            input.IssueDate ?? inv.IssueDate, input.DueDate, input.PeriodStart, input.PeriodEnd, inv.CurrencyId, input.Notes,
            input.Items.Select(i => new InvoiceLineInput(i.ItemType, i.ItemTypeId, i.Description, i.Quantity, i.Uom, i.UnitPrice, i.VatRate)).ToList(), false));
        await tx.CommitAsync();
        return Ok(new { created.Id, created.InvoiceNo, replacedId = id });
    }

    /// <summary>Issue a draft invoice (creates the journal entry).</summary>
    [HttpPost("{id:long}/issue")]
    [RequirePermission("invoices.post")]
    public async Task<IActionResult> Issue(long id)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be issued.");
        await Billing.IssueAsync(inv);
        return NoContent();
    }

    /// <summary>Cancel an invoice: reverses its journal entry and releases its meter readings. Not allowed when payments are applied.</summary>
    [HttpPost("{id:long}/cancel")]
    [RequirePermission("invoices.cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) == "cancelled") throw new BusinessRuleException("The invoice is already cancelled.");
        var paid = (await Billing.PaidByInvoiceAsync(new[] { id })).GetValueOrDefault(id);
        if (paid > 0) throw new BusinessRuleException("Payments are applied to this invoice. Cancel or re-allocate the payments first.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (inv.JournalEntryId != null) await Posting.ReverseAsync(inv.JournalEntryId.Value, Today, $"Invoice {inv.InvoiceNo} cancelled");
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        inv.StatusId = await LkId(LookupTypes.InvoiceStatus, "cancelled");
        inv.Notes = $"{inv.Notes}\nCancelled: {input.Reason}".Trim();
        Activity.Log("invoice", inv.Id, "status_change", $"Invoice {inv.InvoiceNo} cancelled: {input.Reason}", inv.BuildingId, inv.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteDraft(long id)
    {
        var inv = await Load(id);
        if (await Lookups.CodeAsync(inv.StatusId) != "draft") throw new BusinessRuleException("Only draft invoices can be deleted; cancel issued invoices instead.");
        await Db.MeterReadings.Where(r => r.InvoiceId == id).ForEachAsync(r => r.InvoiceId = null);
        Db.InvoiceItems.RemoveRange(await Db.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync());
        Db.Invoices.Remove(inv);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Billing run: creates gas-consumption invoices for every active contract of a building / branch (or the given contracts)
    /// from the unbilled readings and monthly fees. Each contract is processed independently.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] RunInput input)
    {
        if (input.PeriodEnd < input.PeriodStart) throw new AppException("Period end must be after the start.");
        var contracts = Db.SupplyContracts.Where(c => c.IsActive);
        if (input.ContractIds is { Count: > 0 }) contracts = contracts.Where(c => input.ContractIds.Contains(c.Id));
        if (input.BuildingId != null) contracts = contracts.Where(c => c.Unit!.BuildingId == input.BuildingId);
        if (input.BranchId != null) contracts = contracts.Where(c => c.BranchId == input.BranchId);
        if (!Me.IsAdmin && Me.BranchIds.Count > 0) contracts = contracts.Where(c => Me.BranchIds.Contains(c.BranchId));
        var list = await contracts.Select(c => new { c.Id, c.ContractNo }).ToListAsync();
        var results = new List<RunResult>();
        foreach (var c in list)
        {
            await using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                var inv = await Billing.GenerateConsumptionInvoiceAsync(c.Id, input.PeriodStart, input.PeriodEnd, input.Issue);
                await tx.CommitAsync();
                results.Add(new RunResult(c.Id, c.ContractNo, inv != null, inv?.Id, inv?.InvoiceNo, inv?.TotalAmount, null));
            }
            catch (AppException ex)
            {
                await tx.RollbackAsync();
                Db.ChangeTracker.Clear();
                results.Add(new RunResult(c.Id, c.ContractNo, false, null, null, null, ex.Message));
            }
        }
        return Ok(new { contracts = list.Count, created = results.Count(r => r.Created), results });
    }

    public record RunResult(long ContractId, string ContractNo, bool Created, long? InvoiceId, string? InvoiceNo, decimal? Amount, string? Error);

    /// <summary>Totals by status and by month (charts on the invoices page / dashboard).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] long? buildingId, [FromQuery] long? branchId)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        var query = Filter(null, buildingId, null, branchId, f, t);
        var byStatus = await query.GroupBy(i => i.StatusId).Select(g => new { g.Key, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount ?? 0) }).ToListAsync();
        var cancelled = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "cancelled");
        var draft = await Lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "draft");
        var byMonth = await query.Where(i => i.StatusId != cancelled && i.StatusId != draft)
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count(), Amount = g.Sum(i => i.TotalAmount ?? 0), Vat = g.Sum(i => i.VatAmount ?? 0) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
        return Ok(new
        {
            from = f, to = t,
            byStatus = byStatus.Select(s => new { status = Code(s.Key), label = Label(s.Key), s.Count, s.Amount }),
            byMonth = byMonth.Select(m => new { month = $"{m.Year}-{m.Month:00}", m.Count, m.Amount, m.Vat }),
            total = byStatus.Where(s => s.Key != cancelled && s.Key != draft).Sum(s => s.Amount),
        });
    }

    /// <summary>Overdue invoices with days overdue and aging bucket (Overdue page). groupBy=tenant returns one row per tenant.</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue([FromQuery] PageQuery q, [FromQuery] long? buildingId, [FromQuery] long? branchId, [FromQuery] string? groupBy)
    {
        await LoadLookups();
        var open = await Billing.InvoiceStatusIdsAsync("pending", "partial", "overdue");
        var query = Filter(null, buildingId, null, branchId, null, null).Where(i => i.StatusId != null && open.Contains(i.StatusId.Value) && i.DueDate < Today);
        var rows = await Project(query.OrderBy(i => i.DueDate)).Take(5000).ToListAsync();
        var dtos = await ToDtos(rows);
        var list = rows.Zip(dtos, (r, d) => new { r, d, balance = d.Balance, days = Today.DayNumber - r.I.DueDate.DayNumber })
            .Where(x => x.balance > 0).ToList();
        static string Bucket(int days) => days <= 30 ? "0-30" : days <= 60 ? "31-60" : days <= 90 ? "61-90" : "90+";

        var aging = list.GroupBy(x => Bucket(x.days)).Select(g => new { bucket = g.Key, count = g.Count(), amount = g.Sum(x => x.balance) }).OrderBy(x => x.bucket).ToList();
        if (groupBy == "tenant")
        {
            var tenants = list.GroupBy(x => x.r.I.TenantId).Select(g => new
            {
                tenantId = g.Key, tenant = Loc(g.First().r.TenantEn, g.First().r.TenantAr), phone = g.First().r.TenantPhone,
                building = Loc(g.First().r.BEn, g.First().r.BAr), invoices = g.Count(), amount = g.Sum(x => x.balance), maxDays = g.Max(x => x.days),
                oldestDueDate = g.Min(x => x.r.I.DueDate), bucket = Bucket(g.Max(x => x.days)),
            }).OrderByDescending(x => x.amount).ToList();
            return Ok(new { total = tenants.Sum(t => t.amount), tenants = tenants.Count, aging, items = Page(tenants, q) });
        }
        var items = list.Select(x => new { invoice = x.d, daysOverdue = x.days, bucket = Bucket(x.days) }).ToList();
        return Ok(new { total = list.Sum(x => x.balance), tenants = list.Select(x => x.r.I.TenantId).Distinct().Count(), aging, items = Page(items, q) });
    }

    private static PagedResult<T> Page<T>(List<T> all, PageQuery q)
    {
        var size = q.PageSize <= 0 ? Math.Max(all.Count, 1) : Math.Clamp(q.PageSize, 1, 500);
        var page = Math.Max(1, q.Page);
        return new PagedResult<T>(all.Skip((page - 1) * size).Take(size).ToList(), all.Count, page, size);
    }

    // ------------------------------------------------------------------ helpers

    private async Task<Invoice> Load(long id)
    {
        var inv = await Db.Invoices.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Invoice", id);
        EnsureBranchAccess(inv.BranchId);
        return inv;
    }

    private IQueryable<Invoice> Filter(long? tenantId, long? buildingId, long? contractId, long? branchId, DateOnly? from, DateOnly? to)
    {
        var query = Db.Invoices.AsNoTracking().Where(i => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(i.BranchId));
        if (tenantId != null) query = query.Where(i => i.TenantId == tenantId);
        if (buildingId != null) query = query.Where(i => i.BuildingId == buildingId);
        if (contractId != null) query = query.Where(i => i.ContractId == contractId);
        if (branchId != null) query = query.Where(i => i.BranchId == branchId);
        if (from != null) query = query.Where(i => i.IssueDate >= from);
        if (to != null) query = query.Where(i => i.IssueDate <= to);
        return query;
    }

    private class Row
    {
        public Invoice I { get; init; } = null!;
        public string? TenantEn { get; init; }
        public string? TenantAr { get; init; }
        public string? TenantPhone { get; init; }
        public string? BCode { get; init; }
        public string? BEn { get; init; }
        public string? BAr { get; init; }
        public string? ContractNo { get; init; }
        public string? UnitNo { get; init; }
    }

    private static IQueryable<Row> Project(IQueryable<Invoice> q) => q.Select(i => new Row
    {
        I = i, TenantEn = i.Tenant!.NameEn, TenantAr = i.Tenant.NameAr, TenantPhone = i.Tenant.Phone, BCode = i.Building!.Code, BEn = i.Building.NameEn, BAr = i.Building.NameAr,
        ContractNo = i.Contract!.ContractNo, UnitNo = i.Contract.Unit!.UnitNo,
    });

    public record InvoiceDto(long Id, string No, string InvoiceNo, long TenantId, string? Customer, long? BuildingId, string? BuildingCode, string? Building,
        long? ContractId, string? ContractNo, string? Unit, DateOnly Date, DateOnly DueDate, string? Period, DateOnly? BillingPeriodStart, DateOnly? BillingPeriodEnd,
        string? Type, string? TypeLabel, decimal Subtotal, decimal Vat, decimal Amount, decimal Paid, decimal Balance, string? Status, string? StatusLabel,
        DateOnly? PaidDate, string? Notes, long? JournalEntryId);

    private async Task<List<InvoiceDto>> ToDtos(IReadOnlyList<Row> rows)
    {
        var ids = rows.Select(r => r.I.Id).ToList();
        var paid = await Billing.PaidByInvoiceAsync(ids);
        var lastPay = await Db.PaymentAllocations.AsNoTracking().Where(a => ids.Contains(a.InvoiceId))
            .GroupBy(a => a.InvoiceId).Select(g => new { g.Key, Date = g.Max(a => a.Payment!.PaymentDate) }).ToDictionaryAsync(x => x.Key, x => x.Date);
        return rows.Select(r =>
        {
            var p = paid.GetValueOrDefault(r.I.Id);
            var total = r.I.TotalAmount ?? 0;
            var status = Code(r.I.StatusId);
            if (status is "pending" or "partial" && r.I.DueDate < Today && p < total) status = "overdue";
            return new InvoiceDto(r.I.Id, r.I.InvoiceNo, r.I.InvoiceNo, r.I.TenantId, Loc(r.TenantEn, r.TenantAr),
                r.I.BuildingId, r.BCode, Loc(r.BEn, r.BAr), r.I.ContractId, r.ContractNo, r.UnitNo,
                r.I.IssueDate, r.I.DueDate, r.I.BillingPeriodStart == null ? null : $"{r.I.BillingPeriodStart:yyyy-MM-dd} – {r.I.BillingPeriodEnd:yyyy-MM-dd}",
                r.I.BillingPeriodStart, r.I.BillingPeriodEnd, Code(r.I.InvoiceTypeId), Label(r.I.InvoiceTypeId),
                r.I.Subtotal ?? 0, r.I.VatAmount ?? 0, total, p, status == "cancelled" ? 0 : total - p,
                status, Label(r.I.StatusId), status == "paid" ? lastPay.GetValueOrDefault(r.I.Id) : null, r.I.Notes, r.I.JournalEntryId);
        }).ToList();
    }
}
