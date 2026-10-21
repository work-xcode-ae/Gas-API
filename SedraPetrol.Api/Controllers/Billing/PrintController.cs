using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Everything a printed document needs in one call (the portal lays it out like the company's paper forms):
/// the gas invoice, the gas supply contract and the annual maintenance contract.
/// </summary>
[Route("api/print")]
[PermissionModule("invoices")]
public class PrintController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    private async Task<object?> CompanyAsync()
    {
        var c = await Db.Companies.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (c == null) return null;
        return new
        {
            c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.Website, c.LogoPath, c.StampPath, c.LetterheadPath,
            c.PoBox, c.Fax, c.CustomerCarePhone, c.CashContactName, c.CashContactMobile, c.InvoicePaymentNote,
            c.SignatoryNameEn, c.SignatoryNameAr, c.SignatoryTitleEn, c.SignatoryTitleAr,
        };
    }

    // ═════════════════════════ gas invoice ═════════════════════════

    [HttpGet("invoice/{id:long}")]
    [RequirePermission("invoices.view")]
    public async Task<IActionResult> Invoice(long id, CancellationToken ct)
    {
        await LoadLookups();
        var i = await Db.Invoices.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.BranchId, x.InvoiceNo, x.IssueDate, x.DueDate, x.BillingPeriodStart, x.BillingPeriodEnd, x.Subtotal, x.VatAmount, x.TotalAmount,
            x.StatusId, x.InvoiceTypeId, x.Notes, x.TenantId, x.ContractId,
            CustEn = x.Tenant!.Party!.NameEn, CustAr = x.Tenant.Party!.NameAr, CustCode = x.Tenant.Party!.PartyCode, x.Tenant.Party!.Phone, x.Tenant.Party!.Email,
            CustTrn = x.Tenant.Party!.TaxNumber, CustPoBox = x.Tenant.Party!.PoBox, CustAddress = x.Tenant.Party!.Address,
            CustCityEn = x.Tenant.Party!.City!.NameEn, CustCityAr = x.Tenant.Party!.City!.NameAr,
            ContractNo = x.Contract!.ContractNo, UnitNo = x.Contract.Unit!.UnitNo,
            BCode = x.Building != null ? x.Building.Code : x.Contract.Building!.Code,
            BEn = x.Building != null ? x.Building.NameEn : x.Contract.Building!.NameEn,
            BAr = x.Building != null ? x.Building.NameAr : x.Contract.Building!.NameAr,
            BCityEn = x.Building != null ? x.Building.City!.NameEn : x.Contract.Building!.City!.NameEn,
            BCityAr = x.Building != null ? x.Building.City!.NameAr : x.Contract.Building!.City!.NameAr,
        }).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Invoice", id);
        EnsureBranchAccess(i.BranchId);

        var items = await Db.InvoiceItems.AsNoTracking().Where(x => x.InvoiceId == id).OrderBy(x => x.Id)
            .Select(x => new { x.ItemTypeId, x.Description, x.Quantity, x.Uom, x.UnitPrice, x.VatRate, x.Amount }).ToListAsync(ct);

        // meter readings billed on this invoice → previous / current reading
        var readings = await Db.MeterReadings.AsNoTracking().Where(r => r.InvoiceId == id).OrderBy(r => r.ReadingDate)
            .Select(r => new { r.MeterId, r.ReadingDate, r.ReadingValue, r.Consumption, MeterNo = r.Meter!.MeterNo }).ToListAsync(ct);
        object? reading = null;
        string? meterNo = null;
        if (readings.Count > 0)
        {
            var first = readings[0];
            var last = readings[^1];
            meterNo = last.MeterNo;
            var prev = await Db.MeterReadings.AsNoTracking()
                .Where(r => r.MeterId == first.MeterId && r.ReadingDate < first.ReadingDate && (r.InvoiceId == null || r.InvoiceId != id))
                .OrderByDescending(r => r.ReadingDate).Select(r => new { r.ReadingDate, r.ReadingValue }).FirstOrDefaultAsync(ct);
            var consumption = readings.Sum(r => r.Consumption ?? 0);
            reading = new
            {
                currentDate = DateOnly.FromDateTime(last.ReadingDate.LocalDateTime), current = last.ReadingValue,
                previousDate = prev != null ? DateOnly.FromDateTime(prev.ReadingDate.LocalDateTime) : i.BillingPeriodStart,
                previous = prev?.ReadingValue ?? (first.ReadingValue - (first.Consumption ?? 0)),
                consumption,
            };
        }
        else if (i.ContractId != null)
        {
            meterNo = await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId == i.ContractId).OrderByDescending(r => r.ReadingDate)
                .Select(r => r.Meter!.MeterNo).FirstOrDefaultAsync(ct);
        }

        // previous balance of the customer when this invoice was issued (other invoices − payments − credit notes + refunds)
        var frozen = await Billing.InvoiceStatusIdsAsync("draft", "cancelled");
        var dead = await Billing.DeadPaymentStatusIdsAsync();
        var prevInvoices = await Db.Invoices.AsNoTracking()
            .Where(x => x.TenantId == i.TenantId && x.Id != id && (x.StatusId == null || !frozen.Contains(x.StatusId.Value))
                        && (x.IssueDate < i.IssueDate || (x.IssueDate == i.IssueDate && x.Id < id)))
            .SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0;
        var paidBefore = await Db.Payments.AsNoTracking()
            .Where(p => p.TenantId == i.TenantId && p.PaymentDate <= i.IssueDate && (p.StatusId == null || !dead.Contains(p.StatusId.Value)))
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        var paidOnThis = await Db.PaymentAllocations.AsNoTracking()
            .Where(a => a.InvoiceId == id && a.Payment!.PaymentDate <= i.IssueDate && (a.Payment.StatusId == null || !dead.Contains(a.Payment.StatusId.Value)))
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0;
        var creditBefore = await Db.CreditNotes.AsNoTracking()
            .Where(c => c.TenantId == i.TenantId && c.Status == "posted" && c.IssueDate <= i.IssueDate && c.InvoiceId != id)
            .SumAsync(c => (decimal?)c.TotalAmount, ct) ?? 0;
        var refundsBefore = await Db.CustomerRefunds.AsNoTracking()
            .Where(r => r.TenantId == i.TenantId && r.Status == "posted" && r.RefundDate <= i.IssueDate)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
        var previousBalance = BillingService.R2(prevInvoices - (paidBefore - paidOnThis) - creditBefore + refundsBefore);

        var lastPayment = await Db.Payments.AsNoTracking()
            .Where(p => p.TenantId == i.TenantId && p.PaymentDate <= i.IssueDate && (p.StatusId == null || !dead.Contains(p.StatusId.Value)))
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Select(p => new { p.ReceiptNo, date = p.PaymentDate, p.Amount }).FirstOrDefaultAsync(ct);

        var bank = await Db.BankAccounts.AsNoTracking().Where(b => b.IsActive && b.AccountType == "bank" && (b.ShowOnInvoice || b.Iban != null))
            .OrderByDescending(b => b.ShowOnInvoice).ThenBy(b => b.Id)
            .Select(b => new { b.BankName, b.NameEn, b.NameAr, b.AccountNo, b.Iban, b.SwiftCode }).FirstOrDefaultAsync(ct);

        return Ok(new
        {
            invoice = new
            {
                i.Id, i.InvoiceNo, i.IssueDate, i.DueDate, periodStart = i.BillingPeriodStart, periodEnd = i.BillingPeriodEnd,
                subtotal = i.Subtotal ?? 0, vat = i.VatAmount ?? 0, total = i.TotalAmount ?? 0,
                type = Code(i.InvoiceTypeId), typeLabel = Label(i.InvoiceTypeId), status = Code(i.StatusId), i.Notes,
                previousBalance, totalDue = BillingService.R2(previousBalance + (i.TotalAmount ?? 0)),
            },
            customer = new
            {
                id = i.TenantId, code = i.CustCode, nameEn = i.CustEn, nameAr = i.CustAr, phone = i.Phone, email = i.Email, trn = i.CustTrn,
                address = i.CustAddress ?? i.BCityEn ?? i.CustCityEn, addressAr = i.BCityAr ?? i.CustCityAr, poBox = i.CustPoBox,
            },
            contractNo = i.ContractNo,
            building = new { code = i.BCode, nameEn = i.BEn, nameAr = i.BAr, cityEn = i.BCityEn, cityAr = i.BCityAr },
            unitNo = i.UnitNo,
            meterNo,
            reading,
            items = items.Select(x => new { type = Code(x.ItemTypeId), typeLabel = Label(x.ItemTypeId), x.Description, quantity = x.Quantity, x.Uom, unitPrice = x.UnitPrice, x.VatRate, amount = x.Amount ?? 0 }),
            lastPayment,
            bank,
            company = await CompanyAsync(),
        });
    }

    // ═════════════════════════ gas supply contract ═════════════════════════

    [HttpGet("supply-contract/{id:long}")]
    [RequirePermission("contracts.view")]
    public async Task<IActionResult> SupplyContract(long id, CancellationToken ct)
    {
        await LoadLookups();
        var c = await Db.SupplyContracts.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.BranchId, x.ContractNo, x.StartDate, x.EndDate, x.AutoRenew, x.RenewalPeriodMonths, x.StatusId, x.DepositAmount, x.Terms, x.CreatedAt,
            x.TenantId, TEn = x.Tenant!.Party!.NameEn, TAr = x.Tenant.Party!.NameAr, TCode = x.Tenant.Party!.PartyCode, TPhone = x.Tenant.Party!.Phone, TEmail = x.Tenant.Party!.Email,
            TPoBox = x.Tenant.Party!.PoBox, TEid = x.Tenant.Party!.EmiratesId, TCityEn = x.Tenant.Party!.City!.NameEn, TCityAr = x.Tenant.Party!.City!.NameAr,
            BCode = x.Building!.Code, BEn = x.Building.NameEn, BAr = x.Building.NameAr, BCityEn = x.Building.City!.NameEn, BCityAr = x.Building.City!.NameAr,
            UnitNo = x.Unit!.UnitNo, x.Unit.FloorNo,
            x.ServiceTypeId, x.UsageTypeId, x.BillByEmail, x.BillByPost, x.BillingCycleMonths,
        }).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("SupplyContract", id);
        EnsureBranchAccess(c.BranchId);

        var phones = await Db.PartyContacts.AsNoTracking().Where(p => p.PartyId == c.TenantId)
            .Select(p => new { p.ContactTypeId, p.Value }).ToListAsync(ct);
        var fees = await Db.ContractFees.AsNoTracking().Where(f => f.ContractId == id && f.IsActive).OrderBy(f => f.EffectiveFrom).ToListAsync(ct);
        var meterNo = await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId == id).OrderBy(r => r.ReadingDate).Select(r => r.Meter!.MeterNo).FirstOrDefaultAsync(ct);
        var opening = await Db.MeterReadings.AsNoTracking().Where(r => r.ContractId == id).OrderBy(r => r.ReadingDate)
            .Select(r => new { r.ReadingDate, r.ReadingValue }).FirstOrDefaultAsync(ct);

        return Ok(new
        {
            contract = new
            {
                c.Id, c.ContractNo, c.StartDate, c.EndDate, c.AutoRenew, renewalMonths = c.RenewalPeriodMonths, status = Code(c.StatusId), statusLabel = Label(c.StatusId),
                deposit = c.DepositAmount ?? 0, notes = c.Terms, createdAt = c.CreatedAt,
                serviceType = Code(c.ServiceTypeId), usageType = Code(c.UsageTypeId), billByEmail = c.BillByEmail, billByPost = c.BillByPost,
                billingCycleMonths = c.BillingCycleMonths,
                openingReading = opening?.ReadingValue, openingDate = opening == null ? (DateOnly?)null : DateOnly.FromDateTime(opening.ReadingDate.LocalDateTime),
            },
            tenant = new
            {
                id = c.TenantId, code = c.TCode, nameEn = c.TEn, nameAr = c.TAr, phone = c.TPhone, email = c.TEmail, poBox = c.TPoBox, emiratesId = c.TEid,
                cityEn = c.TCityEn, cityAr = c.TCityAr,
                contacts = phones.Select(p => new { type = Code(p.ContactTypeId), p.Value }),
            },
            building = new { code = c.BCode, nameEn = c.BEn, nameAr = c.BAr, cityEn = c.BCityEn, cityAr = c.BCityAr },
            unitNo = c.UnitNo, floor = c.FloorNo, meterNo,
            fees = fees.Select(f => new { type = Code(f.FeeTypeId), typeLabel = Label(f.FeeTypeId), cycle = Code(f.CycleId), f.Amount, f.EffectiveFrom }),
            terms = await TermsAsync("supply", id, ct),
            serviceTypes = await OptionsAsync(LookupTypes.ContractServiceType),
            usageTypes = await OptionsAsync(LookupTypes.ContractUsageType),
            company = await CompanyAsync(),
        });
    }

    // ═════════════════════════ annual maintenance contract ═════════════════════════

    [HttpGet("amc-contract/{id:long}")]
    [RequirePermission("amc.view")]
    public async Task<IActionResult> AmcContract(long id, CancellationToken ct)
    {
        await LoadLookups();
        var a = await Db.AmcContracts.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.BranchId, x.ContractNo, x.StartDate, x.EndDate, x.AutoRenew, x.StatusId, x.AnnualValue, x.VisitFrequencyId, x.VisitsTotal,
            x.ResponseTimeHours, x.EmergencyIncluded, x.Notes, x.CreatedAt,
            OEn = x.OwnerParty!.NameEn, OAr = x.OwnerParty.NameAr, OPhone = x.OwnerParty.Phone, OEmail = x.OwnerParty.Email, OPoBox = x.OwnerParty.PoBox,
            OCityEn = x.OwnerParty.City!.NameEn, OCityAr = x.OwnerParty.City!.NameAr, x.OwnerPartyId,
            VEn = x.Vendor!.Party!.NameEn, VAr = x.Vendor.Party!.NameAr, VPhone = x.Vendor.Party!.Phone, VEmail = x.Vendor.Party!.Email,
            BCode = x.Building!.Code, BEn = x.Building.NameEn, BAr = x.Building.NameAr, BStreet = x.Building.Street, BZone = x.Building.Zone,
            BCityEn = x.Building.City!.NameEn, BCityAr = x.Building.City!.NameAr,
        }).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("AmcContract", id);
        EnsureBranchAccess(a.BranchId);

        var ownerFax = a.OwnerPartyId == null ? null : await Db.PartyContacts.AsNoTracking()
            .Where(p => p.PartyId == a.OwnerPartyId).Select(p => new { p.ContactTypeId, p.Value }).ToListAsync(ct);
        var included = await LkId(LookupTypes.ScopeItemType, "included");
        var scope = await Db.AmcScopeItems.AsNoTracking().Where(s => s.ContractId == id).OrderBy(s => s.Id).Select(s => new { s.ItemTypeId, s.Description }).ToListAsync(ct);
        var schedule = await Db.AmcPaymentSchedules.AsNoTracking().Where(s => s.ContractId == id).OrderBy(s => s.DueDate)
            .Select(s => new { s.Label, s.DueDate, s.Amount }).ToListAsync(ct);

        return Ok(new
        {
            contract = new
            {
                a.Id, a.ContractNo, a.StartDate, a.EndDate, a.AutoRenew, status = Code(a.StatusId), statusLabel = Label(a.StatusId), annualValue = a.AnnualValue ?? 0,
                visitFrequency = Code(a.VisitFrequencyId), visitFrequencyLabel = Label(a.VisitFrequencyId), a.VisitsTotal, a.ResponseTimeHours, a.EmergencyIncluded,
                a.Notes, createdAt = a.CreatedAt,
            },
            owner = new
            {
                nameEn = a.OEn, nameAr = a.OAr, phone = a.OPhone, email = a.OEmail, poBox = a.OPoBox, cityEn = a.OCityEn, cityAr = a.OCityAr,
                contacts = (ownerFax ?? new()).Select(p => new { type = Code(p.ContactTypeId), p.Value }),
            },
            provider = new { nameEn = a.VEn, nameAr = a.VAr, phone = a.VPhone, email = a.VEmail },
            building = new { code = a.BCode, nameEn = a.BEn, nameAr = a.BAr, street = a.BStreet, zone = a.BZone, cityEn = a.BCityEn, cityAr = a.BCityAr },
            scope = scope.Where(s => s.ItemTypeId == included).Select(s => s.Description),
            excluded = scope.Where(s => s.ItemTypeId != included).Select(s => s.Description),
            schedule,
            terms = await TermsAsync("amc", id, ct),
            company = await CompanyAsync(),
        });
    }

    /// <summary>Active values of a lookup list (for the ☐ / ☑ boxes of the printed form).</summary>
    private async Task<List<object>> OptionsAsync(string type) =>
        (await Lookups.AllAsync()).Values.Where(v => v.TypeCode == type && v.IsActive).OrderBy(v => v.SortOrder).ThenBy(v => v.Id)
            .Select(v => (object)new { code = v.Code, nameEn = v.NameEn, nameAr = v.NameAr }).ToList();

    /// <summary>The contract's terms in Arabic and English (English from the term, or from its template when the text was not changed).</summary>
    private async Task<List<object>> TermsAsync(string type, long contractId, CancellationToken ct)
    {
        var rows = await Db.ContractTerms.AsNoTracking().Where(t => t.ContractType == type && t.ContractId == contractId).OrderBy(t => t.SortOrder).ThenBy(t => t.Id)
            .Select(t => new { t.Category, t.TextAr, t.TextEn, t.IsCustom, SrcAr = t.Source != null ? t.Source.TextAr : null, SrcEn = t.Source != null ? t.Source.TextEn : null })
            .ToListAsync(ct);
        return rows.Select(t => (object)new
        {
            category = t.Category,
            ar = t.TextAr,
            en = t.TextEn ?? (t.SrcEn != null && t.SrcAr != null && t.SrcAr.Trim() == t.TextAr.Trim() ? t.SrcEn : null),
            custom = t.IsCustom,
        }).ToList();
    }
}
