using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Receipt vouchers (سند قبض) and payment vouchers (سند صرف) in one place:
///   • customer receipts  → table payment          (created through POST /api/payments)
///   • supplier payments  → table vendor_payment   (created through POST /api/vendor-payments)
///   • any other account  → table voucher          (created here, posted Dr/Cr cash ↔ the chosen account)
/// </summary>
[Route("api/vouchers")]
[PermissionModule("payments")]
public class VouchersController : ApiControllerBase
{
    private PostingService PostingSvc => HttpContext.RequestServices.GetRequiredService<PostingService>();
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    public record VoucherInput(long? BranchId, string VoucherType, DateOnly? Date, long AccountId, string PartyName, decimal Amount, string? Method,
        string? Reference, string? ChequeNo, DateOnly? ChequeDate, string? BankName, long? BuildingId, string? Description);
    public record CancelInput(string? Reason);

    /// <summary>One list of all vouchers. type = receipt | payment; party = customer | supplier | account.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string type, [FromQuery] string? party, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? search, [FromQuery] bool includeCancelled = true, CancellationToken ct = default)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        var s = search?.Trim();
        var dead = await Billing.DeadPaymentStatusIdsAsync();
        bool Branch(long b) => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(b);
        var rows = new List<VoucherRow>();

        if (type == "receipt" && party is null or "customer")
        {
            var q = Db.Payments.AsNoTracking().Where(p => p.PaymentDate >= f && p.PaymentDate <= t);
            if (!string.IsNullOrEmpty(s)) q = q.Where(p => p.ReceiptNo.Contains(s) || (p.ReferenceNo ?? "").Contains(s) || (p.Tenant!.Party!.NameAr ?? "").Contains(s) || (p.Tenant.Party!.NameEn ?? "").Contains(s));
            var list = await q.Select(p => new { p.Id, p.BranchId, p.ReceiptNo, p.PaymentDate, p.Amount, p.PaymentMethodId, p.ReferenceNo, p.StatusId, p.Notes, p.JournalEntryId,
                p.TenantId, p.Tenant!.Party!.NameAr, p.Tenant.Party!.NameEn, Jn = p.JournalEntry!.JournalNo }).ToListAsync(ct);
            rows.AddRange(list.Where(p => Branch(p.BranchId)).Select(p => new VoucherRow("customer", p.Id, p.ReceiptNo, "receipt", p.PaymentDate, p.TenantId, Loc(p.NameEn, p.NameAr) ?? "",
                null, p.Amount, Code(p.PaymentMethodId), p.ReferenceNo, p.StatusId != null && dead.Contains(p.StatusId.Value) ? "cancelled" : Code(p.StatusId) ?? "completed",
                Label(p.StatusId), p.Notes, p.Jn)));
        }
        if (type == "payment" && party is null or "supplier")
        {
            var q = Db.VendorPayments.AsNoTracking().Where(p => p.PaymentDate >= f && p.PaymentDate <= t);
            if (!string.IsNullOrEmpty(s)) q = q.Where(p => p.PaymentNo.Contains(s) || (p.ReferenceNo ?? "").Contains(s) || (p.Vendor!.Party!.NameAr ?? "").Contains(s) || (p.Vendor.Party!.NameEn ?? "").Contains(s));
            var list = await q.Select(p => new { p.Id, p.BranchId, p.PaymentNo, p.PaymentDate, p.Amount, p.PaymentMethodId, p.ReferenceNo, p.StatusId, p.Notes,
                p.VendorId, p.Vendor!.Party!.NameAr, p.Vendor.Party!.NameEn, Jn = p.JournalEntry!.JournalNo }).ToListAsync(ct);
            rows.AddRange(list.Where(p => Branch(p.BranchId)).Select(p => new VoucherRow("supplier", p.Id, p.PaymentNo, "payment", p.PaymentDate, p.VendorId, Loc(p.NameEn, p.NameAr) ?? "",
                null, p.Amount, Code(p.PaymentMethodId), p.ReferenceNo, p.StatusId != null && dead.Contains(p.StatusId.Value) ? "cancelled" : Code(p.StatusId) ?? "completed",
                Label(p.StatusId), p.Notes, p.Jn)));
        }
        if (party is null or "account")
        {
            var q = Db.Vouchers.AsNoTracking().Where(v => v.VoucherType == type && v.VoucherDate >= f && v.VoucherDate <= t);
            if (!string.IsNullOrEmpty(s)) q = q.Where(v => v.VoucherNo.Contains(s) || v.PartyName.Contains(s) || (v.ReferenceNo ?? "").Contains(s) || (v.Description ?? "").Contains(s));
            var list = await q.Select(v => new { V = v, v.Account!.AccountCode, v.Account.NameAr, v.Account.NameEn, Jn = v.JournalEntry!.JournalNo }).ToListAsync(ct);
            rows.AddRange(list.Where(x => Branch(x.V.BranchId)).Select(x => new VoucherRow("account", x.V.Id, x.V.VoucherNo, x.V.VoucherType, x.V.VoucherDate, x.V.AccountId, x.V.PartyName,
                $"{x.AccountCode} {Loc(x.NameEn, x.NameAr)}", x.V.Amount, Code(x.V.PaymentMethodId), x.V.ReferenceNo, x.V.Status, x.V.Status == "cancelled" ? "ملغى" : "مرحّل",
                x.V.Description, x.Jn)));
        }
        var result = rows.Where(r => includeCancelled || r.Status != "cancelled").OrderByDescending(r => r.Date).ThenByDescending(r => r.No).ToList();
        var live = result.Where(r => r.Status != "cancelled").ToList();
        return Ok(new
        {
            items = result,
            total = live.Sum(r => r.Amount),
            count = live.Count,
            byParty = live.GroupBy(r => r.Party).Select(g => new { party = g.Key, count = g.Count(), amount = g.Sum(r => r.Amount) }),
            byMethod = live.GroupBy(r => r.Method ?? "other").Select(g => new { method = g.Key, count = g.Count(), amount = g.Sum(r => r.Amount) }),
        });
    }

    public record VoucherRow(string Party, long Id, string No, string Type, DateOnly Date, long? PartyId, string PartyName, string? Account,
        decimal Amount, string? Method, string? Reference, string? Status, string? StatusLabel, string? Description, string? JournalNo);

    /// <summary>One voucher for printing: party = customer | supplier | account.</summary>
    [HttpGet("{party}/{id:long}")]
    public async Task<IActionResult> Get(string party, long id, CancellationToken ct)
    {
        await LoadLookups();
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        if (party == "customer")
        {
            var p = await Db.Payments.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { P = x, x.Tenant!.Party!.NameAr, x.Tenant.Party!.NameEn, TenantCode = x.Tenant.Party!.PartyCode, Jn = x.JournalEntry!.JournalNo, By = x.ReceivedByUser!.NameAr ?? x.ReceivedByUser.NameEn })
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Payment", id);
            EnsureBranchAccess(p.P.BranchId);
            var alloc = await Db.PaymentAllocations.AsNoTracking().Where(a => a.PaymentId == id)
                .Select(a => new { a.InvoiceId, no = a.Invoice!.InvoiceNo, a.Amount }).ToListAsync(ct);
            return Ok(new
            {
                party, id, no = p.P.ReceiptNo, type = "receipt", date = p.P.PaymentDate, partyName = Loc(p.NameEn, p.NameAr), partyCode = p.TenantCode,
                account = "ذمم العملاء", amount = p.P.Amount, method = Code(p.P.PaymentMethodId), reference = p.P.ReferenceNo, status = Code(p.P.StatusId),
                description = p.P.Notes, journalNo = p.Jn, by = p.By, allocations = alloc.Select(a => new { a.InvoiceId, a.no, a.Amount }), company,
            });
        }
        if (party == "supplier")
        {
            var p = await Db.VendorPayments.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { P = x, x.Vendor!.Party!.NameAr, x.Vendor.Party!.NameEn, Code = x.Vendor.Party!.PartyCode, Jn = x.JournalEntry!.JournalNo, By = x.PaidByUser!.NameAr ?? x.PaidByUser.NameEn })
                .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("VendorPayment", id);
            EnsureBranchAccess(p.P.BranchId);
            var alloc = await Db.VendorPaymentAllocations.AsNoTracking().Where(a => a.VendorPaymentId == id)
                .Select(a => new { a.VendorBillId, no = a.VendorBill!.BillNo, a.Amount }).ToListAsync(ct);
            return Ok(new
            {
                party, id, no = p.P.PaymentNo, type = "payment", date = p.P.PaymentDate, partyName = Loc(p.NameEn, p.NameAr), partyCode = p.Code,
                account = "ذمم الموردين", amount = p.P.Amount, method = Code(p.P.PaymentMethodId), reference = p.P.ReferenceNo, status = Code(p.P.StatusId),
                description = p.P.Notes, journalNo = p.Jn, by = p.By, allocations = alloc.Select(a => new { invoiceId = a.VendorBillId, a.no, a.Amount }), company,
            });
        }
        var v = await Db.Vouchers.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { V = x, x.Account!.AccountCode, x.Account.NameAr, x.Account.NameEn, Jn = x.JournalEntry!.JournalNo, Bld = x.Building!.NameAr ?? x.Building.NameEn })
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Voucher", id);
        EnsureBranchAccess(v.V.BranchId);
        var by = v.V.CreatedBy == null ? null : await Db.AppUsers.Where(u => u.Id == v.V.CreatedBy).Select(u => u.NameAr ?? u.NameEn).FirstOrDefaultAsync(ct);
        return Ok(new
        {
            party = "account", id, no = v.V.VoucherNo, type = v.V.VoucherType, date = v.V.VoucherDate, partyName = v.V.PartyName, partyCode = (string?)null,
            account = $"{v.AccountCode} {Loc(v.NameEn, v.NameAr)}", amount = v.V.Amount, method = Code(v.V.PaymentMethodId), reference = v.V.ReferenceNo,
            chequeNo = v.V.ChequeNo, chequeDate = v.V.ChequeDate, bankName = v.V.BankName, building = v.Bld, status = v.V.Status, cancelReason = v.V.CancelReason,
            description = v.V.Description, journalNo = v.Jn, by, allocations = Array.Empty<object>(), company,
        });
    }

    /// <summary>Postable accounts for the voucher «account» picker.</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery] string? search, CancellationToken ct)
    {
        await LoadLookups();
        var q = Db.Accounts.AsNoTracking().Where(a => a.IsActive && a.IsPostable && !a.IsGroup);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(a => a.AccountCode.Contains(search) || (a.NameAr ?? "").Contains(search) || (a.NameEn ?? "").Contains(search));
        var rows = await q.OrderBy(a => a.AccountCode).Take(500).Select(a => new { a.Id, a.AccountCode, a.NameAr, a.NameEn, a.AccountTypeId }).ToListAsync(ct);
        return Ok(rows.Select(a => new { a.Id, a.AccountCode, name = Loc(a.NameEn, a.NameAr), type = Code(a.AccountTypeId), typeLabel = Label(a.AccountTypeId) }));
    }

    /// <summary>Create a receipt / payment voucher against a ledger account and post it (Dr cash ↔ Cr account, or the reverse).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VoucherInput input)
    {
        if (input.VoucherType is not ("receipt" or "payment")) throw new AppException("Voucher type must be receipt or payment.");
        if (input.Amount <= 0) throw new AppException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(input.PartyName)) throw new AppException(input.VoucherType == "receipt" ? "Enter who the money was received from." : "Enter who the money was paid to.");
        var date = input.Date ?? Today;
        if (date > Today) throw new AppException("The voucher date cannot be in the future.");
        var branchId = RequireBranch(input.BranchId);
        var acc = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == input.AccountId) ?? throw new NotFoundException("Account", input.AccountId);
        if (acc.IsGroup || !acc.IsPostable || !acc.IsActive) throw new AppException($"Account {acc.AccountCode} cannot be posted to.");
        var methodId = await Lk(LookupTypes.PaymentMethod, null, input.Method ?? "cash", field: "method");
        if (input.Method == "cheque" && string.IsNullOrWhiteSpace(input.ChequeNo)) throw new AppException("Enter the cheque number.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        var v = new Voucher
        {
            BranchId = branchId, VoucherType = input.VoucherType, VoucherDate = date, AccountId = acc.Id, PartyName = input.PartyName.Trim(), Amount = input.Amount,
            PaymentMethodId = methodId, ReferenceNo = input.Reference, ChequeNo = input.ChequeNo, ChequeDate = input.ChequeDate, BankName = input.BankName,
            BuildingId = input.BuildingId, Description = input.Description, Status = "posted",
            VoucherNo = await Numbers.NextAsync(input.VoucherType == "receipt" ? NumberPrefixes.ReceiptVoucher : NumberPrefixes.PaymentVoucher, date: date),
        };
        Db.Vouchers.Add(v);
        await Db.SaveChangesAsync();
        var je = await PostingSvc.PostVoucherAsync(v);
        v.JournalEntryId = je.Id;
        Activity.Log("voucher", v.Id, "create", $"{(v.VoucherType == "receipt" ? "Receipt" : "Payment")} voucher {v.VoucherNo}: {v.Amount:0.##} {(v.VoucherType == "receipt" ? "from" : "to")} {v.PartyName}",
            v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { v.Id, v.VoucherNo, journalNo = je.JournalNo });
    }

    /// <summary>Cancel a general voucher: its journal entry is reversed.</summary>
    [HttpPost("account/{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInput input)
    {
        var v = await Db.Vouchers.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Voucher", id);
        EnsureBranchAccess(v.BranchId);
        if (v.Status == "cancelled") throw new BusinessRuleException("The voucher is already cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the cancellation reason.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (v.JournalEntryId != null) await PostingSvc.ReverseAsync(v.JournalEntryId.Value, Today, $"Voucher {v.VoucherNo} cancelled: {input.Reason}");
        v.Status = "cancelled"; v.CancelReason = input.Reason.Trim();
        Activity.Log("voucher", v.Id, "cancel", $"Voucher {v.VoucherNo} cancelled: {input.Reason}", v.BuildingId, v.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }
}

/// <summary>Statement of account (كشف حساب) for a customer, a supplier or any ledger account, with opening and running balance.</summary>
[Route("api/statements")]
[PermissionModule("payments")]
public class StatementsController : ApiControllerBase
{
    private BillingService Billing => HttpContext.RequestServices.GetRequiredService<BillingService>();

    private record Line(DateOnly Date, string Type, long? Id, string Reference, string? Description, decimal Debit, decimal Credit, int Order);

    /// <summary>partyType = customer | supplier | account; from / to default to the current year.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string partyType, [FromQuery] long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        await LoadLookups();
        var f = from ?? new DateOnly(Today.Year, 1, 1);
        var t = to ?? Today;
        if (t < f) throw new AppException("The end date must be after the start date.");
        List<Line> all;
        object partyInfo;
        string nature; // debit = balance shown as Dr − Cr (customers, assets, expenses); credit = Cr − Dr (suppliers, liabilities, income)

        if (partyType == "customer")
        {
            var c = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Tenant", id);
            var deadInv = await Billing.InvoiceStatusIdsAsync("draft", "cancelled");
            var deadPay = await Billing.DeadPaymentStatusIdsAsync();
            var inv = await Db.Invoices.AsNoTracking().Where(i => i.TenantId == id && (i.StatusId == null || !deadInv.Contains(i.StatusId.Value)))
                .Select(i => new { i.Id, i.InvoiceNo, i.IssueDate, i.InvoiceTypeId, i.TotalAmount, i.BillingPeriodStart, i.BillingPeriodEnd, Unit = i.Contract!.Unit!.UnitNo }).ToListAsync(ct);
            var pay = await Db.Payments.AsNoTracking().Where(p => p.TenantId == id && (p.StatusId == null || !deadPay.Contains(p.StatusId.Value)))
                .Select(p => new { p.Id, p.ReceiptNo, p.PaymentDate, p.Amount, p.PaymentMethodId, p.ReferenceNo }).ToListAsync(ct);
            all = inv.Select(i => new Line(i.IssueDate, "invoice", i.Id, i.InvoiceNo,
                    $"فاتورة {Label(i.InvoiceTypeId)}{(i.Unit != null ? $" — وحدة {i.Unit}" : "")}{(i.BillingPeriodStart != null ? $" ({i.BillingPeriodStart:yyyy-MM-dd} → {i.BillingPeriodEnd:yyyy-MM-dd})" : "")}",
                    i.TotalAmount ?? 0, 0, 0))
                .Concat(pay.Select(p => new Line(p.PaymentDate, "receipt", p.Id, p.ReceiptNo, $"سند قبض — {Label(p.PaymentMethodId)}{(p.ReferenceNo != null ? $" ({p.ReferenceNo})" : "")}", 0, p.Amount, 1)))
                .ToList();
            // credit notes (credit) and refunds of customer credit (debit), posted only
            var cns = await Db.CreditNotes.AsNoTracking().Where(x => x.TenantId == id && x.Status == "posted")
                .Select(x => new { x.Id, x.CreditNoteNo, x.IssueDate, x.TotalAmount, x.Reason, InvoiceNo = x.Invoice!.InvoiceNo }).ToListAsync(ct);
            var refunds = await Db.CustomerRefunds.AsNoTracking().Where(x => x.TenantId == id && x.Status == "posted")
                .Select(x => new { x.Id, x.RefundNo, x.RefundDate, x.Amount, x.PaymentMethodId, x.ReferenceNo, x.Reason }).ToListAsync(ct);
            all.AddRange(cns.Select(x => new Line(x.IssueDate, "credit_note", x.Id, x.CreditNoteNo, $"إشعار دائن على الفاتورة {x.InvoiceNo} — {x.Reason}", 0, x.TotalAmount, 2)));
            all.AddRange(refunds.Select(x => new Line(x.RefundDate, "refund", x.Id, x.RefundNo,
                $"استرداد للعميل — {Label(x.PaymentMethodId)}{(x.ReferenceNo != null ? $" ({x.ReferenceNo})" : "")} — {x.Reason}", x.Amount, 0, 3)));
            partyInfo = new { id, code = c.Party!.PartyCode, name = Loc(c.Party!.NameEn, c.Party!.NameAr), phone = c.Party!.Phone, email = c.Party!.Email, trn = (string?)null };
            nature = "debit";
        }
        else if (partyType == "supplier")
        {
            var v = await Db.Vendors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Vendor", id);
            var deadBill = new List<long>();
            foreach (var code in new[] { "draft", "cancelled" }) if (await Lookups.IdOrNullAsync(LookupTypes.BillStatus, code) is long b) deadBill.Add(b);
            var deadPay = await Billing.DeadPaymentStatusIdsAsync();
            var bills = await Db.VendorBills.AsNoTracking().Where(b => b.VendorId == id && (b.StatusId == null || !deadBill.Contains(b.StatusId.Value)))
                .Select(b => new { b.Id, b.BillNo, b.BillDate, b.TotalAmount, b.Notes }).ToListAsync(ct);
            var pays = await Db.VendorPayments.AsNoTracking().Where(p => p.VendorId == id && (p.StatusId == null || !deadPay.Contains(p.StatusId.Value)))
                .Select(p => new { p.Id, p.PaymentNo, p.PaymentDate, p.Amount, p.PaymentMethodId, p.ReferenceNo }).ToListAsync(ct);
            all = bills.Select(b => new Line(b.BillDate, "bill", b.Id, b.BillNo, $"فاتورة مورد{(string.IsNullOrWhiteSpace(b.Notes) ? "" : $" — {b.Notes}")}", 0, b.TotalAmount ?? 0, 0))
                .Concat(pays.Select(p => new Line(p.PaymentDate, "payment", p.Id, p.PaymentNo, $"سند صرف — {Label(p.PaymentMethodId)}{(p.ReferenceNo != null ? $" ({p.ReferenceNo})" : "")}", p.Amount, 0, 1)))
                .ToList();
            partyInfo = new { id, code = v.Party!.PartyCode, name = Loc(v.Party!.NameEn, v.Party!.NameAr), phone = v.Party!.Phone, email = v.Party!.Email, trn = v.Party!.TaxNumber };
            nature = "credit";
        }
        else if (partyType == "account")
        {
            var a = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Account", id);
            var draft = await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, "draft");
            var lines = await Db.JournalLines.AsNoTracking().Where(l => l.AccountId == id && l.JournalEntry!.StatusId != draft)
                .Select(l => new { l.JournalEntryId, l.JournalEntry!.JournalNo, l.JournalEntry.EntryDate, l.JournalEntry.SourceType, Desc = l.Description ?? l.JournalEntry.Description,
                                   Debit = l.Debit ?? 0, Credit = l.Credit ?? 0, l.Id }).ToListAsync(ct);
            all = lines.Select(l => new Line(l.EntryDate, l.SourceType ?? "journal", l.JournalEntryId, l.JournalNo, l.Desc, l.Debit, l.Credit, (int)(l.Id % 1000000))).ToList();
            var type = Code(a.AccountTypeId) ?? "";
            nature = type is "liability" or "equity" or "revenue" or "income" ? "credit" : "debit";
            partyInfo = new { id, code = a.AccountCode, name = Loc(a.NameEn, a.NameAr), phone = (string?)null, email = (string?)null, trn = (string?)null, type, typeLabel = Label(a.AccountTypeId) };
        }
        else throw new AppException("partyType must be customer, supplier or account.");

        decimal Net(Line l) => nature == "debit" ? l.Debit - l.Credit : l.Credit - l.Debit;
        var opening = all.Where(l => l.Date < f).Sum(Net);
        var running = opening;
        var period = all.Where(l => l.Date >= f && l.Date <= t).OrderBy(l => l.Date).ThenBy(l => l.Order).ThenBy(l => l.Reference).ToList();
        var rows = period.Select(l => { running += Net(l); return new { l.Date, l.Type, l.Id, reference = l.Reference, description = l.Description, debit = l.Debit, credit = l.Credit, balance = running }; }).ToList();
        var company = await Db.Companies.AsNoTracking().Select(c => new { c.NameEn, c.NameAr, c.Trn, c.Address, c.Phone, c.Email, c.LogoPath }).FirstOrDefaultAsync(ct);
        return Ok(new
        {
            partyType, party = partyInfo, nature, from = f, to = t, opening, closing = running,
            totalDebit = rows.Sum(r => r.debit), totalCredit = rows.Sum(r => r.credit), lines = rows, company,
        });
    }

    /// <summary>Parties for the statement picker.</summary>
    [HttpGet("parties")]
    public async Task<IActionResult> Parties([FromQuery] string partyType, [FromQuery] string? search, CancellationToken ct)
    {
        var s = search?.Trim();
        if (partyType == "customer")
            return Ok((await Db.Tenants.AsNoTracking().Where(x => string.IsNullOrEmpty(s) || (x.Party!.NameAr ?? "").Contains(s) || (x.Party!.NameEn ?? "").Contains(s) || (x.Party!.PartyCode ?? "").Contains(s) || (x.Party!.Phone ?? "").Contains(s))
                .OrderBy(x => x.Party!.NameAr).Take(50).Select(x => new { x.Id, code = x.Party!.PartyCode, x.Party!.NameAr, x.Party!.NameEn }).ToListAsync(ct))
                .Select(x => new { x.Id, x.code, name = Loc(x.NameEn, x.NameAr) }));
        if (partyType == "supplier")
            return Ok((await Db.Vendors.AsNoTracking().Where(x => string.IsNullOrEmpty(s) || (x.Party!.NameAr ?? "").Contains(s) || (x.Party!.NameEn ?? "").Contains(s) || (x.Party!.PartyCode ?? "").Contains(s))
                .OrderBy(x => x.Party!.NameAr).Take(50).Select(x => new { x.Id, code = x.Party!.PartyCode, x.Party!.NameAr, x.Party!.NameEn }).ToListAsync(ct))
                .Select(x => new { x.Id, x.code, name = Loc(x.NameEn, x.NameAr) }));
        return Ok((await Db.Accounts.AsNoTracking().Where(x => x.IsActive && !x.IsGroup && (string.IsNullOrEmpty(s) || x.AccountCode.Contains(s) || (x.NameAr ?? "").Contains(s) || (x.NameEn ?? "").Contains(s)))
            .OrderBy(x => x.AccountCode).Take(100).Select(x => new { x.Id, code = x.AccountCode, x.NameAr, x.NameEn }).ToListAsync(ct))
            .Select(x => new { x.Id, x.code, name = Loc(x.NameEn, x.NameAr) }));
    }
}
