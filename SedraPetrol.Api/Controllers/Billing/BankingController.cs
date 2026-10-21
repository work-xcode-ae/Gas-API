using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Cash &amp; bank management (BRD §15): bank / cash accounts, transfers, payment-method → account mappings,
/// statement import, matching and bank reconciliation. Balances always come from the general ledger.
/// </summary>
[Route("api/banking")]
[PermissionModule("accounting")]
public class BankingController : ApiControllerBase
{
    private const string TransferPrefix = "BT";
    private static readonly string[] AccountTypes = { "cash", "bank", "petty_cash" };

    public record AccountInput(long? BranchId, string NameAr, string? NameEn, string AccountType, string? BankName, string? AccountNo, string? Iban,
        long? CurrencyId, long GlAccountId, decimal? OpeningBalance, DateOnly? OpeningDate, bool IsActive = true, string? Notes = null,
        string? SwiftCode = null, bool ShowOnInvoice = false);
    public record OpeningEntryInput(long EquityAccountId, DateOnly? Date);
    public record MappingInput(string Method, long? ReceiptAccountId, long? PaymentAccountId);
    public record TransferInput(long? BranchId, DateOnly? Date, long FromAccountId, long ToAccountId, decimal Amount, decimal? Fees, long? FeesAccountId,
        string? Reference, string? Notes);
    public record CancelInput(string? Reason);
    public record ImportLine(DateOnly Date, string? Description, string? Reference, decimal Amount, decimal? Balance);
    public record ImportInput(List<ImportLine> Lines);
    public record MatchInput(long JournalLineId);
    public record CreateEntryInput(long AccountId, string? Description);
    public record RangeInput(DateOnly? From, DateOnly? To);
    public record ReconcileInput(DateOnly PeriodFrom, DateOnly PeriodTo, decimal StatementBalance, string? Notes);

    // ================================================================== helpers

    private async Task<List<long>> CountedJournalStatuses()
    {
        // posted + reversed entries count (a reversal is itself a posted entry); drafts never do
        var list = new List<long>();
        foreach (var c in new[] { "posted", "reversed" })
            if (await Lookups.IdOrNullAsync(LookupTypes.JournalStatus, c) is long id) list.Add(id);
        return list;
    }

    private IQueryable<JournalLine> BookLines(long glAccountId, List<long> counted) =>
        Db.JournalLines.AsNoTracking().Where(l => l.AccountId == glAccountId && l.JournalEntry!.StatusId != null && counted.Contains(l.JournalEntry.StatusId.Value));

    private async Task<decimal> GlBalance(long glAccountId, DateOnly? upTo = null)
    {
        var counted = await CountedJournalStatuses();
        var q = BookLines(glAccountId, counted);
        if (upTo != null) q = q.Where(l => l.JournalEntry!.EntryDate <= upTo);
        return await q.SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
    }

    private async Task<BankAccount> LoadAccount(long id)
    {
        var a = await Db.BankAccounts.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("BankAccount", id);
        EnsureBranchAccess(a.BranchId);
        return a;
    }

    private async Task<long> CompanyIdAsync(long? branchId)
    {
        if (branchId != null)
        {
            var c = await Db.Branches.Where(b => b.Id == branchId).Select(b => (long?)b.CompanyId).FirstOrDefaultAsync();
            if (c != null) return c.Value;
        }
        return Me.CompanyId ?? await Db.Companies.Select(c => (long?)c.Id).FirstOrDefaultAsync() ?? throw new AppException("No company exists.");
    }

    private async Task ValidateGl(long glAccountId, long? exceptBankId)
    {
        var acc = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == glAccountId) ?? throw new NotFoundException("Account", glAccountId);
        if (acc.IsGroup || !acc.IsPostable || !acc.IsActive) throw new AppException($"Account {acc.AccountCode} is a group or inactive account; choose a postable account.");
        if (await Db.BankAccounts.AnyAsync(b => b.GlAccountId == glAccountId && b.Id != exceptBankId))
            throw new BusinessRuleException($"GL account {acc.AccountCode} is already linked to another bank / cash account.");
    }

    private async Task<DateOnly?> LockedUntil(long bankAccountId) =>
        await Db.BankReconciliations.Where(r => r.BankAccountId == bankAccountId && r.Status == "completed").MaxAsync(r => (DateOnly?)r.PeriodTo);

    private async Task<BankStatementLine> LoadLine(long id, bool mustBeOpen = true)
    {
        var l = await Db.BankStatementLines.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("BankStatementLine", id);
        await LoadAccount(l.BankAccountId);
        if (mustBeOpen && l.ReconciliationId != null) throw new BusinessRuleException("This line belongs to a completed reconciliation and is locked. Reopen the reconciliation first.");
        return l;
    }

    // ================================================================== accounts

    /// <summary>Bank / cash accounts with their GL balance, payment-method mappings and reconciliation state.</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery] bool includeInactive = true, CancellationToken ct = default)
    {
        await LoadLookups();
        var rows = await Db.BankAccounts.AsNoTracking().Where(a => includeInactive || a.IsActive)
            .Where(a => Me.IsAdmin || Me.BranchIds.Count == 0 || a.BranchId == null || Me.BranchIds.Contains(a.BranchId.Value))
            .OrderBy(a => a.AccountType).ThenBy(a => a.NameAr)
            .Select(a => new { A = a, a.GlAccount!.AccountCode, GlAr = a.GlAccount.NameAr, GlEn = a.GlAccount.NameEn, Cur = a.Currency!.Code,
                               BrAr = a.Branch!.NameAr, BrEn = a.Branch.NameEn })
            .ToListAsync(ct);
        var counted = await CountedJournalStatuses();
        var glIds = rows.Select(r => r.A.GlAccountId).ToList();
        var balances = await Db.JournalLines.AsNoTracking()
            .Where(l => glIds.Contains(l.AccountId) && l.JournalEntry!.StatusId != null && counted.Contains(l.JournalEntry.StatusId.Value))
            .GroupBy(l => l.AccountId).Select(g => new { g.Key, Bal = g.Sum(l => (l.Debit ?? 0) - (l.Credit ?? 0)), Last = g.Max(l => l.JournalEntry!.EntryDate) })
            .ToDictionaryAsync(x => x.Key, ct);
        var maps = await Db.AccountMappings.AsNoTracking()
            .Where(m => m.IsActive && (m.SourceType == MappingSources.Payment || m.SourceType == MappingSources.VendorPayment))
            .Select(m => new { m.SourceType, m.SourceKeyId, m.DebitAccountId, m.CreditAccountId, m.BranchId }).ToListAsync(ct);
        var ids = rows.Select(r => r.A.Id).ToList();
        var unmatched = await Db.BankStatementLines.AsNoTracking().Where(l => ids.Contains(l.BankAccountId) && l.Status == "unmatched")
            .GroupBy(l => l.BankAccountId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var lastRec = await Db.BankReconciliations.AsNoTracking().Where(r => ids.Contains(r.BankAccountId) && r.Status == "completed")
            .GroupBy(r => r.BankAccountId).Select(g => new { g.Key, To = g.Max(r => r.PeriodTo) }).ToDictionaryAsync(x => x.Key, x => (DateOnly?)x.To, ct);

        return Ok(rows.Select(r =>
        {
            var b = balances.GetValueOrDefault(r.A.GlAccountId);
            var receiptMethods = maps.Where(m => m.SourceType == MappingSources.Payment && m.DebitAccountId == r.A.GlAccountId).Select(m => Code(m.SourceKeyId) ?? "default").Distinct();
            var paymentMethods = maps.Where(m => m.SourceType == MappingSources.VendorPayment && m.CreditAccountId == r.A.GlAccountId).Select(m => Code(m.SourceKeyId) ?? "default").Distinct();
            return new
            {
                r.A.Id, r.A.BranchId, branch = Loc(r.BrEn, r.BrAr), r.A.NameAr, r.A.NameEn, name = Loc(r.A.NameEn, r.A.NameAr), r.A.AccountType, r.A.BankName, r.A.AccountNo,
                r.A.Iban, r.A.CurrencyId, currency = r.Cur ?? "AED", r.A.GlAccountId, glAccount = $"{r.AccountCode} {Loc(r.GlEn, r.GlAr)}", r.A.OpeningBalance, r.A.OpeningDate,
                r.A.OpeningJournalId, r.A.IsActive, r.A.Notes, r.A.SwiftCode, r.A.ShowOnInvoice, balance = b?.Bal ?? 0m, lastMovement = b?.Last,
                receiptMethods, paymentMethods, unmatchedLines = unmatched.GetValueOrDefault(r.A.Id), reconciledUntil = lastRec.GetValueOrDefault(r.A.Id),
            };
        }));
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] AccountInput input)
    {
        Validate(input);
        await ValidateGl(input.GlAccountId, null);
        EnsureBranchAccess(input.BranchId);
        var a = new BankAccount();
        Apply(a, input);
        Db.BankAccounts.Add(a);
        await Db.SaveChangesAsync();
        Activity.Log("bank_account", a.Id, "create", $"Bank / cash account «{a.NameAr}» created", null, a.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { a.Id });
    }

    [HttpPut("accounts/{id:long}")]
    public async Task<IActionResult> UpdateAccount(long id, [FromBody] AccountInput input)
    {
        var a = await LoadAccount(id);
        Validate(input);
        if (a.GlAccountId != input.GlAccountId)
        {
            if (await Db.BankStatementLines.AnyAsync(l => l.BankAccountId == id && l.JournalLineId != null) || await Db.BankTransfers.AnyAsync(t => t.FromAccountId == id || t.ToAccountId == id))
                throw new BusinessRuleException("The GL account cannot be changed: the account already has transfers or matched statement lines.");
            await ValidateGl(input.GlAccountId, id);
        }
        if (a.OpeningJournalId != null && (a.OpeningBalance != (input.OpeningBalance ?? 0) || a.OpeningDate != input.OpeningDate))
            throw new BusinessRuleException("The opening balance is already posted. Reverse its journal entry in Accounting before changing it.");
        EnsureBranchAccess(input.BranchId);
        Apply(a, input);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("accounts/{id:long}")]
    public async Task<IActionResult> DeleteAccount(long id)
    {
        var a = await LoadAccount(id);
        if (await Db.BankTransfers.AnyAsync(t => t.FromAccountId == id || t.ToAccountId == id) || await Db.BankStatementLines.AnyAsync(l => l.BankAccountId == id) || a.OpeningJournalId != null)
            throw new BusinessRuleException("The account has transfers, statement lines or an opening entry. Deactivate it instead.");
        Db.BankAccounts.Remove(a);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Post the opening balance once: Dr bank / Cr the chosen equity (opening balances) account, or the reverse for a negative balance.</summary>
    [HttpPost("accounts/{id:long}/opening-entry")]
    public async Task<IActionResult> OpeningEntry(long id, [FromBody] OpeningEntryInput input)
    {
        var a = await LoadAccount(id);
        if (a.OpeningJournalId != null) throw new BusinessRuleException("The opening balance is already posted.");
        if (a.OpeningBalance == 0) throw new AppException("The opening balance is zero.");
        if (input.EquityAccountId == a.GlAccountId) throw new AppException("Choose a different account for the other side of the opening entry.");
        var date = input.Date ?? a.OpeningDate ?? Today;
        var branchId = a.BranchId ?? RequireBranch(null);
        var amt = Math.Abs(a.OpeningBalance);
        var text = $"Opening balance — {a.NameAr}";
        await using var tx = await Db.Database.BeginTransactionAsync();
        var je = await Posting.CreateEntryAsync(branchId, date, text, "bank_opening", a.Id, a.OpeningBalance > 0
            ? new[] { new PostingService.Line(a.GlAccountId, amt, 0, text), new PostingService.Line(input.EquityAccountId, 0, amt, text) }
            : new[] { new PostingService.Line(input.EquityAccountId, amt, 0, text), new PostingService.Line(a.GlAccountId, 0, amt, text) }, true);
        a.OpeningJournalId = je.Id;
        a.OpeningDate ??= date;
        Activity.Log("bank_account", a.Id, "opening", $"Opening balance {a.OpeningBalance:0.##} posted for «{a.NameAr}» ({je.JournalNo})", null, a.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { je.Id, je.JournalNo });
    }

    private static void Validate(AccountInput i)
    {
        if (string.IsNullOrWhiteSpace(i.NameAr)) throw new AppException("Enter the account name.");
        if (!AccountTypes.Contains(i.AccountType)) throw new AppException("Account type must be cash, bank or petty_cash.");
        if (i.AccountType == "bank" && string.IsNullOrWhiteSpace(i.BankName)) throw new AppException("Enter the bank name.");
    }

    private static void Apply(BankAccount a, AccountInput i)
    {
        a.BranchId = i.BranchId; a.NameAr = i.NameAr.Trim(); a.NameEn = i.NameEn?.Trim(); a.AccountType = i.AccountType;
        a.BankName = i.BankName?.Trim(); a.AccountNo = i.AccountNo?.Trim(); a.Iban = i.Iban?.Replace(" ", "").ToUpperInvariant();
        a.CurrencyId = i.CurrencyId; a.GlAccountId = i.GlAccountId; a.OpeningBalance = i.OpeningBalance ?? 0; a.OpeningDate = i.OpeningDate;
        a.IsActive = i.IsActive; a.Notes = i.Notes;
        a.SwiftCode = string.IsNullOrWhiteSpace(i.SwiftCode) ? null : i.SwiftCode.Trim().ToUpperInvariant(); a.ShowOnInvoice = i.ShowOnInvoice;
    }

    /// <summary>Postable GL accounts for pickers (bank side, fees, equity, charges, interest).</summary>
    [HttpGet("gl-accounts")]
    public async Task<IActionResult> GlAccounts([FromQuery] string? search, CancellationToken ct)
    {
        await LoadLookups();
        var q = Db.Accounts.AsNoTracking().Where(a => a.IsActive && a.IsPostable && !a.IsGroup);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(a => a.AccountCode.Contains(search) || (a.NameAr ?? "").Contains(search) || (a.NameEn ?? "").Contains(search));
        var linked = await Db.BankAccounts.AsNoTracking().Select(b => new { b.GlAccountId, b.NameAr }).ToDictionaryAsync(b => b.GlAccountId, b => b.NameAr, ct);
        var rows = await q.OrderBy(a => a.AccountCode).Take(1000).Select(a => new { a.Id, a.AccountCode, a.NameAr, a.NameEn, a.AccountTypeId }).ToListAsync(ct);
        return Ok(rows.Select(a => new { a.Id, a.AccountCode, name = Loc(a.NameEn, a.NameAr), type = Code(a.AccountTypeId), typeLabel = Label(a.AccountTypeId),
                                         linkedBank = linked.GetValueOrDefault(a.Id) }));
    }

    // ================================================================== payment-method mappings

    /// <summary>Which GL account each payment method posts to: receipts (debit, «payment» mapping) and payments (credit, «vendor_payment» mapping).</summary>
    [HttpGet("method-mappings")]
    public async Task<IActionResult> MethodMappings([FromQuery] long? branchId, CancellationToken ct)
    {
        await LoadLookups();
        var companyId = await CompanyIdAsync(branchId);
        var methods = (await Lookups.ByTypeAsync(LookupTypes.PaymentMethod)).Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToList();
        var maps = await Db.AccountMappings.AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.IsActive && (m.SourceType == MappingSources.Payment || m.SourceType == MappingSources.VendorPayment))
            .Select(m => new { m.Id, m.SourceType, m.SourceKeyId, m.BranchId, m.DebitAccountId, m.CreditAccountId }).ToListAsync(ct);
        var accNames = await Db.Accounts.AsNoTracking().Select(a => new { a.Id, a.AccountCode, a.NameAr, a.NameEn }).ToDictionaryAsync(a => a.Id, ct);
        var banks = await Db.BankAccounts.AsNoTracking().Select(b => new { b.GlAccountId, b.Id, b.NameAr }).ToDictionaryAsync(b => b.GlAccountId, ct);
        object? Describe(long? acc) => acc == null ? null : new
        {
            accountId = acc, account = accNames.TryGetValue(acc.Value, out var n) ? $"{n.AccountCode} {Loc(n.NameEn, n.NameAr)}" : $"#{acc}",
            bankAccountId = banks.TryGetValue(acc.Value, out var b) ? b.Id : (long?)null, bankAccount = banks.TryGetValue(acc.Value, out var b2) ? b2.NameAr : null,
        };
        // same resolution as PostingService.MapAsync: branch+key › key › branch › generic
        long? Pick(string source, long methodId, bool debit)
        {
            var best = maps.Where(m => m.SourceType == source && (m.BranchId == null || m.BranchId == branchId) && (m.SourceKeyId == null || m.SourceKeyId == methodId))
                .OrderByDescending(m => (m.SourceKeyId != null ? 2 : 0) + (m.BranchId != null ? 1 : 0)).FirstOrDefault();
            return debit ? best?.DebitAccountId : best?.CreditAccountId;
        }
        return Ok(methods.Select(m => new
        {
            method = m.Code, label = Loc(m.NameEn, m.NameAr), methodId = m.Id,
            receipt = Describe(Pick(MappingSources.Payment, m.Id, true)),
            payment = Describe(Pick(MappingSources.VendorPayment, m.Id, false)),
        }));
    }

    /// <summary>Set the default receipt / payment account of one payment method (company-wide mapping row for that method).</summary>
    [HttpPut("method-mappings")]
    public async Task<IActionResult> SetMethodMapping([FromBody] MappingInput input)
    {
        var methodId = await LkId(LookupTypes.PaymentMethod, input.Method);
        var companyId = await CompanyIdAsync(null);
        foreach (var (source, acc, debit) in new[] { (MappingSources.Payment, input.ReceiptAccountId, true), (MappingSources.VendorPayment, input.PaymentAccountId, false) })
        {
            if (acc == null) continue;
            var a = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == acc) ?? throw new NotFoundException("Account", acc.Value);
            if (a.IsGroup || !a.IsPostable || !a.IsActive) throw new AppException($"Account {a.AccountCode} cannot be posted to.");
            var row = await Db.AccountMappings.FirstOrDefaultAsync(m => m.CompanyId == companyId && m.BranchId == null && m.SourceType == source && m.SourceKeyId == methodId);
            if (row == null)
            {
                row = new AccountMapping { CompanyId = companyId, SourceType = source, SourceKeyId = methodId, IsActive = true };
                Db.AccountMappings.Add(row);
            }
            if (debit) row.DebitAccountId = acc; else row.CreditAccountId = acc;
            row.IsActive = true;
        }
        Activity.Log("account_mapping", null, "update", $"Payment method «{input.Method}» mapped: receipts → {input.ReceiptAccountId?.ToString() ?? "unchanged"}, payments → {input.PaymentAccountId?.ToString() ?? "unchanged"}");
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // ================================================================== transfers

    [HttpGet("transfers")]
    public async Task<IActionResult> Transfers([FromQuery] long? accountId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool includeCancelled = true, CancellationToken ct = default)
    {
        var q = Db.BankTransfers.AsNoTracking().Where(t => Me.IsAdmin || Me.BranchIds.Count == 0 || Me.BranchIds.Contains(t.BranchId));
        if (accountId != null) q = q.Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId);
        if (from != null) q = q.Where(t => t.TransferDate >= from);
        if (to != null) q = q.Where(t => t.TransferDate <= to);
        if (!includeCancelled) q = q.Where(t => t.Status != "cancelled");
        var rows = await q.OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.Id)
            .Select(t => new { t.Id, t.TransferNo, t.TransferDate, t.FromAccountId, from = t.FromAccount!.NameAr, t.ToAccountId, to = t.ToAccount!.NameAr, t.Amount, t.Fees,
                               feesAccount = t.FeesAccount!.AccountCode + " " + (t.FeesAccount.NameAr ?? t.FeesAccount.NameEn), t.ReferenceNo, t.Notes, t.Status, t.CancelReason,
                               journalNo = t.JournalEntry!.JournalNo })
            .Take(1000).ToListAsync(ct);
        return Ok(rows);
    }

    /// <summary>Move money between two accounts: Dr to-account / Cr from-account (fees: Dr fees account / Cr from-account).</summary>
    [HttpPost("transfers")]
    public async Task<IActionResult> CreateTransfer([FromBody] TransferInput input)
    {
        if (input.FromAccountId == input.ToAccountId) throw new AppException("Choose two different accounts.");
        if (input.Amount <= 0) throw new AppException("The amount must be greater than zero.");
        var fees = input.Fees ?? 0;
        if (fees < 0) throw new AppException("Fees cannot be negative.");
        if (fees > 0 && input.FeesAccountId == null) throw new AppException("Choose the account for the bank fees.");
        var date = input.Date ?? Today;
        if (date > Today) throw new AppException("The transfer date cannot be in the future.");
        var fromAcc = await LoadAccount(input.FromAccountId);
        var toAcc = await LoadAccount(input.ToAccountId);
        if (!fromAcc.IsActive || !toAcc.IsActive) throw new BusinessRuleException("Both accounts must be active.");
        if (fromAcc.CurrencyId != null && toAcc.CurrencyId != null && fromAcc.CurrencyId != toAcc.CurrencyId)
            throw new BusinessRuleException("Transfers between different currencies are not supported yet.");
        var available = await GlBalance(fromAcc.GlAccountId);
        var branchId = input.BranchId ?? fromAcc.BranchId ?? RequireBranch(null);

        await using var tx = await Db.Database.BeginTransactionAsync();
        var t = new BankTransfer
        {
            BranchId = branchId, TransferNo = await Numbers.NextAsync(TransferPrefix, date: date), TransferDate = date, FromAccountId = fromAcc.Id, ToAccountId = toAcc.Id,
            Amount = input.Amount, Fees = fees, FeesAccountId = fees > 0 ? input.FeesAccountId : null, ReferenceNo = input.Reference, Notes = input.Notes, Status = "posted",
        };
        Db.BankTransfers.Add(t);
        await Db.SaveChangesAsync();
        var text = $"Transfer {t.TransferNo}: {fromAcc.NameAr} → {toAcc.NameAr}{(string.IsNullOrWhiteSpace(t.Notes) ? "" : $" — {t.Notes}")}";
        var lines = new List<PostingService.Line>
        {
            new(toAcc.GlAccountId, t.Amount, 0, text),
            new(fromAcc.GlAccountId, 0, t.Amount + fees, text),
        };
        if (fees > 0) lines.Add(new PostingService.Line(t.FeesAccountId!.Value, fees, 0, $"Bank fees — {t.TransferNo}"));
        var je = await Posting.CreateEntryAsync(branchId, date, text, "bank_transfer", t.Id, lines, true);
        t.JournalEntryId = je.Id;
        Activity.Log("bank_transfer", t.Id, "create", $"{text}: {t.Amount:0.##}{(fees > 0 ? $" + fees {fees:0.##}" : "")}", null, branchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { t.Id, t.TransferNo, je.JournalNo, warning = available < t.Amount + fees ? $"The source account balance ({available:0.##}) is lower than the transfer." : null });
    }

    [HttpPost("transfers/{id:long}/cancel")]
    public async Task<IActionResult> CancelTransfer(long id, [FromBody] CancelInput input)
    {
        var t = await Db.BankTransfers.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("BankTransfer", id);
        EnsureBranchAccess(t.BranchId);
        if (t.Status == "cancelled") throw new BusinessRuleException("The transfer is already cancelled.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the cancellation reason.");
        if (t.JournalEntryId != null && await Db.BankStatementLines.AnyAsync(l => l.JournalLine!.JournalEntryId == t.JournalEntryId))
            throw new BusinessRuleException("The transfer is matched to a bank statement line. Unmatch it first.");
        await using var tx = await Db.Database.BeginTransactionAsync();
        if (t.JournalEntryId != null) await Posting.ReverseAsync(t.JournalEntryId.Value, Today, $"Transfer {t.TransferNo} cancelled: {input.Reason}");
        t.Status = "cancelled"; t.CancelReason = input.Reason.Trim();
        Activity.Log("bank_transfer", t.Id, "cancel", $"Transfer {t.TransferNo} cancelled: {input.Reason}", null, t.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    // ================================================================== bank statement

    /// <summary>Import statement lines (already parsed and column-mapped by the client). Lines identical to existing ones are skipped.</summary>
    [HttpPost("accounts/{id:long}/statement/import")]
    public async Task<IActionResult> Import(long id, [FromBody] ImportInput input)
    {
        var a = await LoadAccount(id);
        if (input.Lines == null || input.Lines.Count == 0) throw new AppException("No lines to import.");
        if (input.Lines.Count > 5000) throw new AppException("Import at most 5000 lines at a time.");
        var locked = await LockedUntil(id);
        var from = input.Lines.Min(l => l.Date); var to = input.Lines.Max(l => l.Date);
        var existing = await Db.BankStatementLines.AsNoTracking().Where(l => l.BankAccountId == id && l.LineDate >= from && l.LineDate <= to)
            .Select(l => new { l.LineDate, l.Amount, l.Reference, l.Description }).ToListAsync();
        var keys = existing.Select(e => $"{e.LineDate:yyyyMMdd}|{e.Amount:0.00}|{e.Reference}|{e.Description}").ToHashSet();
        var batch = Guid.NewGuid();
        int added = 0, duplicates = 0, inLocked = 0, zero = 0;
        foreach (var l in input.Lines)
        {
            if (l.Amount == 0) { zero++; continue; }
            if (locked != null && l.Date <= locked) { inLocked++; continue; }
            var desc = l.Description?.Trim(); var reference = l.Reference?.Trim();
            if (desc?.Length > 500) desc = desc[..500];
            if (reference?.Length > 100) reference = reference[..100];
            var key = $"{l.Date:yyyyMMdd}|{Math.Round(l.Amount, 2):0.00}|{reference}|{desc}";
            if (!keys.Add(key)) { duplicates++; continue; }
            Db.BankStatementLines.Add(new BankStatementLine
            {
                BankAccountId = id, LineDate = l.Date, Description = desc, Reference = reference, Amount = Math.Round(l.Amount, 2),
                Balance = l.Balance == null ? null : Math.Round(l.Balance.Value, 2), Status = "unmatched", ImportBatch = batch,
            });
            added++;
        }
        Activity.Log("bank_account", a.Id, "statement_import", $"Statement imported for «{a.NameAr}»: {added} lines ({from:yyyy-MM-dd} → {to:yyyy-MM-dd}), {duplicates} duplicates skipped", null, a.BranchId);
        await Db.SaveChangesAsync();
        return Ok(new { added, duplicates, skippedLocked = inLocked, skippedZero = zero, batch });
    }

    [HttpGet("accounts/{id:long}/statement-lines")]
    public async Task<IActionResult> StatementLines(long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? status, CancellationToken ct)
    {
        await LoadAccount(id);
        var q = Db.BankStatementLines.AsNoTracking().Where(l => l.BankAccountId == id);
        if (from != null) q = q.Where(l => l.LineDate >= from);
        if (to != null) q = q.Where(l => l.LineDate <= to);
        if (!string.IsNullOrEmpty(status)) q = q.Where(l => l.Status == status);
        var rows = await q.OrderBy(l => l.LineDate).ThenBy(l => l.Id)
            .Select(l => new { l.Id, date = l.LineDate, l.Description, l.Reference, l.Amount, l.Balance, l.Status, l.JournalLineId, l.ReconciliationId, l.ImportBatch,
                               journalNo = l.JournalLine!.JournalEntry!.JournalNo, journalDate = (DateOnly?)l.JournalLine.JournalEntry.EntryDate,
                               journalText = l.JournalLine.Description ?? l.JournalLine.JournalEntry.Description })
            .Take(5000).ToListAsync(ct);
        return Ok(rows.Select(r => new { r.Id, r.date, r.Description, r.Reference, r.Amount, r.Balance, r.Status, r.JournalLineId, locked = r.ReconciliationId != null, r.ImportBatch,
                                         r.journalNo, r.journalDate, r.journalText }));
    }

    /// <summary>Book (GL) lines of the account in the period; amount signed like the statement (debit = +).</summary>
    [HttpGet("accounts/{id:long}/book-lines")]
    public async Task<IActionResult> BookLinesEndpoint(long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] bool onlyUnmatched = false, CancellationToken ct = default)
    {
        var a = await LoadAccount(id);
        var counted = await CountedJournalStatuses();
        var q = BookLines(a.GlAccountId, counted);
        if (from != null) q = q.Where(l => l.JournalEntry!.EntryDate >= from);
        if (to != null) q = q.Where(l => l.JournalEntry!.EntryDate <= to);
        var matched = Db.BankStatementLines.Where(s => s.BankAccountId == id && s.JournalLineId != null).Select(s => new { s.JournalLineId, s.Id });
        var rows = await q.OrderBy(l => l.JournalEntry!.EntryDate).ThenBy(l => l.Id)
            .Select(l => new
            {
                l.Id, l.JournalEntryId, l.JournalEntry!.JournalNo, date = l.JournalEntry.EntryDate, l.JournalEntry.SourceType, l.JournalEntry.SourceId,
                description = l.Description ?? l.JournalEntry.Description, amount = (l.Debit ?? 0) - (l.Credit ?? 0),
                statementLineId = matched.Where(m => m.JournalLineId == l.Id).Select(m => (long?)m.Id).FirstOrDefault(),
            })
            .Take(5000).ToListAsync(ct);
        return Ok(rows.Where(r => !onlyUnmatched || r.statementLineId == null));
    }

    /// <summary>Match candidates: same amount, date within ±3 days, reference / description similarity raises the score.</summary>
    [HttpGet("accounts/{id:long}/suggestions")]
    public async Task<IActionResult> Suggestions(long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var (lines, book) = await OpenItems(id, from, to, ct);
        return Ok(BuildSuggestions(lines, book).Select(s => new { statementLineId = s.Line.Id, candidates = s.Candidates.Select(c => new
        {
            journalLineId = c.Book.Id, c.Book.JournalNo, date = c.Book.Date, c.Book.Description, amount = c.Book.Amount, c.Score, c.Reason,
        }) }));
    }

    private record OpenLine(long Id, DateOnly Date, string? Description, string? Reference, decimal Amount);
    private record OpenBook(long Id, string JournalNo, DateOnly Date, string? Description, decimal Amount);
    private record Candidate(OpenBook Book, int Score, string Reason);
    private record Suggestion(OpenLine Line, List<Candidate> Candidates);

    private async Task<(List<OpenLine>, List<OpenBook>)> OpenItems(long id, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var a = await LoadAccount(id);
        var q = Db.BankStatementLines.AsNoTracking().Where(l => l.BankAccountId == id && l.Status == "unmatched");
        if (from != null) q = q.Where(l => l.LineDate >= from);
        if (to != null) q = q.Where(l => l.LineDate <= to);
        var lines = await q.Select(l => new OpenLine(l.Id, l.LineDate, l.Description, l.Reference, l.Amount)).ToListAsync(ct);
        if (lines.Count == 0) return (lines, new List<OpenBook>());
        var counted = await CountedJournalStatuses();
        var minD = lines.Min(l => l.Date).AddDays(-3); var maxD = lines.Max(l => l.Date).AddDays(3);
        var matchedIds = Db.BankStatementLines.Where(s => s.JournalLineId != null).Select(s => s.JournalLineId!.Value);
        var book = await BookLines(a.GlAccountId, counted).Where(l => l.JournalEntry!.EntryDate >= minD && l.JournalEntry.EntryDate <= maxD && !matchedIds.Contains(l.Id))
            .Select(l => new OpenBook(l.Id, l.JournalEntry!.JournalNo, l.JournalEntry.EntryDate, l.Description ?? l.JournalEntry.Description, (l.Debit ?? 0) - (l.Credit ?? 0)))
            .ToListAsync(ct);
        return (lines, book);
    }

    private static string Norm(string? s) => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static List<Suggestion> BuildSuggestions(List<OpenLine> lines, List<OpenBook> book)
    {
        var result = new List<Suggestion>();
        foreach (var l in lines)
        {
            var refN = Norm(l.Reference);
            var cands = book.Where(b => b.Amount == l.Amount && Math.Abs(b.Date.DayNumber - l.Date.DayNumber) <= 3).Select(b =>
            {
                var days = Math.Abs(b.Date.DayNumber - l.Date.DayNumber);
                var score = 60 - days * 10;
                var reasons = new List<string> { "same amount", days == 0 ? "same date" : $"{days} day(s) apart" };
                var bText = Norm(b.Description) + Norm(b.JournalNo);
                if (refN.Length >= 3 && bText.Contains(refN)) { score += 40; reasons.Add("reference found"); }
                return new Candidate(b, score, string.Join(", ", reasons));
            }).OrderByDescending(c => c.Score).Take(5).ToList();
            if (cands.Count > 0) result.Add(new Suggestion(l, cands));
        }
        return result;
    }

    /// <summary>Apply suggestions automatically where exactly one candidate fits (and it is not claimed by another line).</summary>
    [HttpPost("accounts/{id:long}/auto-match")]
    public async Task<IActionResult> AutoMatch(long id, [FromBody] RangeInput input)
    {
        var (lines, book) = await OpenItems(id, input.From, input.To, default);
        var suggestions = BuildSuggestions(lines, book);
        var claimed = suggestions.SelectMany(s => s.Candidates.Select(c => c.Book.Id)).GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();
        var locked = await LockedUntil(id);
        var matched = 0;
        foreach (var s in suggestions)
        {
            var best = s.Candidates.Where(c => !claimed.Contains(c.Book.Id)).ToList();
            if (best.Count != 1 && !(best.Count > 1 && best[0].Score >= 90 && best[1].Score < 90)) continue;
            if (locked != null && s.Line.Date <= locked) continue;
            var line = await Db.BankStatementLines.FirstAsync(x => x.Id == s.Line.Id);
            line.JournalLineId = best[0].Book.Id; line.Status = "matched"; line.MatchedAt = DateTimeOffset.UtcNow; line.MatchedBy = Me.UserId;
            claimed.Add(best[0].Book.Id);
            matched++;
        }
        await Db.SaveChangesAsync();
        return Ok(new { matched, remaining = lines.Count - matched });
    }

    [HttpPost("statement-lines/{id:long}/match")]
    public async Task<IActionResult> Match(long id, [FromBody] MatchInput input)
    {
        var l = await LoadLine(id);
        if (l.Status == "matched") throw new BusinessRuleException("The line is already matched. Unmatch it first.");
        var a = await LoadAccount(l.BankAccountId);
        var counted = await CountedJournalStatuses();
        var jl = await BookLines(a.GlAccountId, counted).Where(x => x.Id == input.JournalLineId)
            .Select(x => new { x.Id, amount = (x.Debit ?? 0) - (x.Credit ?? 0) }).FirstOrDefaultAsync()
            ?? throw new AppException("The book entry is not a posted line of this account.");
        if (jl.amount != l.Amount) throw new BusinessRuleException($"Amounts differ (bank {l.Amount:0.00} / book {jl.amount:0.00}). Record the difference with «create entry» on a separate line.");
        if (await Db.BankStatementLines.AnyAsync(x => x.JournalLineId == jl.Id && x.Id != id)) throw new BusinessRuleException("This book entry is already matched to another statement line.");
        l.JournalLineId = jl.Id; l.Status = "matched"; l.MatchedAt = DateTimeOffset.UtcNow; l.MatchedBy = Me.UserId;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("statement-lines/{id:long}/unmatch")]
    public async Task<IActionResult> Unmatch(long id)
    {
        var l = await LoadLine(id);
        l.JournalLineId = null; l.Status = "unmatched"; l.MatchedAt = null; l.MatchedBy = null;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("statement-lines/{id:long}/ignore")]
    public async Task<IActionResult> Ignore(long id)
    {
        var l = await LoadLine(id);
        if (l.Status == "matched") throw new BusinessRuleException("Unmatch the line first.");
        l.Status = "ignored";
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("statement-lines/{id:long}/unignore")]
    public async Task<IActionResult> Unignore(long id)
    {
        var l = await LoadLine(id);
        if (l.Status == "ignored") l.Status = "unmatched";
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("statement-lines/{id:long}")]
    public async Task<IActionResult> DeleteLine(long id)
    {
        var l = await LoadLine(id);
        if (l.Status == "matched") throw new BusinessRuleException("Unmatch the line before deleting it.");
        Db.BankStatementLines.Remove(l);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Record a bank-only item (charges, interest, direct debit) from a statement line: posts Dr/Cr the chosen account against the bank
    /// GL account on the line date and matches the line to the new bank-side journal line.
    /// </summary>
    [HttpPost("statement-lines/{id:long}/create-entry")]
    public async Task<IActionResult> CreateEntry(long id, [FromBody] CreateEntryInput input)
    {
        var l = await LoadLine(id);
        if (l.Status != "unmatched") throw new BusinessRuleException("Only an unmatched line can create an entry.");
        var a = await LoadAccount(l.BankAccountId);
        if (input.AccountId == a.GlAccountId) throw new AppException("Choose the other account (charges, interest income …), not the bank account itself.");
        var acc = await Db.Accounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == input.AccountId) ?? throw new NotFoundException("Account", input.AccountId);
        if (acc.IsGroup || !acc.IsPostable || !acc.IsActive) throw new AppException($"Account {acc.AccountCode} cannot be posted to.");
        var amt = Math.Abs(l.Amount);
        var text = string.IsNullOrWhiteSpace(input.Description) ? $"Bank statement — {l.Description ?? l.Reference}" : input.Description.Trim();
        var branchId = a.BranchId ?? RequireBranch(null);
        await using var tx = await Db.Database.BeginTransactionAsync();
        var je = await Posting.CreateEntryAsync(branchId, l.LineDate, text, "bank_statement", l.Id, l.Amount > 0
            ? new[] { new PostingService.Line(a.GlAccountId, amt, 0, text), new PostingService.Line(acc.Id, 0, amt, text) }
            : new[] { new PostingService.Line(acc.Id, amt, 0, text), new PostingService.Line(a.GlAccountId, 0, amt, text) }, true);
        var bankLine = await Db.JournalLines.Where(x => x.JournalEntryId == je.Id && x.AccountId == a.GlAccountId).Select(x => x.Id).FirstAsync();
        l.JournalLineId = bankLine; l.Status = "matched"; l.MatchedAt = DateTimeOffset.UtcNow; l.MatchedBy = Me.UserId;
        Activity.Log("bank_account", a.Id, "statement_entry", $"«{a.NameAr}»: {text} {l.Amount:0.##} posted ({je.JournalNo})", null, a.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { je.Id, je.JournalNo });
    }

    // ================================================================== reconciliation

    private async Task<object> Summary(BankAccount a, DateOnly from, DateOnly to, decimal? statementBalance)
    {
        var counted = await CountedJournalStatuses();
        var book = await GlBalance(a.GlAccountId, to);
        // statement items not in the books (unmatched bank lines up to «to», ignored ones excluded)
        var unrecorded = await Db.BankStatementLines.Where(l => l.BankAccountId == a.Id && l.Status == "unmatched" && l.LineDate <= to)
            .SumAsync(l => (decimal?)l.Amount) ?? 0;
        // book items not yet on the statement (unmatched GL lines up to «to»)
        var matchedIds = Db.BankStatementLines.Where(s => s.JournalLineId != null).Select(s => s.JournalLineId!.Value);
        var outstanding = await BookLines(a.GlAccountId, counted).Where(l => l.JournalEntry!.EntryDate <= to && !matchedIds.Contains(l.Id))
            .SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        // the statement's own closing balance: last printed balance on or before «to» unless given
        var lastPrinted = await Db.BankStatementLines.Where(l => l.BankAccountId == a.Id && l.LineDate <= to && l.Balance != null)
            .OrderByDescending(l => l.LineDate).ThenByDescending(l => l.Id).Select(l => l.Balance).FirstOrDefaultAsync();
        var stmt = statementBalance ?? lastPrinted;
        var expected = book - outstanding + unrecorded;
        var counts = await Db.BankStatementLines.Where(l => l.BankAccountId == a.Id && l.LineDate >= from && l.LineDate <= to)
            .GroupBy(l => l.Status).Select(g => new { g.Key, N = g.Count(), Sum = g.Sum(x => x.Amount) }).ToListAsync();
        return new
        {
            accountId = a.Id, from, to, bookBalance = book, outstandingBook = outstanding, unrecordedBank = unrecorded, expectedStatementBalance = expected,
            statementBalance = stmt, statementBalanceSource = statementBalance != null ? "entered" : lastPrinted != null ? "statement" : "missing",
            difference = stmt == null ? (decimal?)null : stmt.Value - expected,
            lines = counts.Select(c => new { status = c.Key, count = c.N, amount = c.Sum }),
            lockedUntil = await LockedUntil(a.Id),
        };
    }

    [HttpGet("accounts/{id:long}/reconciliation-summary")]
    public async Task<IActionResult> ReconciliationSummary(long id, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] decimal? statementBalance)
    {
        var a = await LoadAccount(id);
        return Ok(await Summary(a, from, to, statementBalance));
    }

    /// <summary>Complete the reconciliation of a period (difference must be 0). Statement lines of the period are locked.</summary>
    [HttpPost("accounts/{id:long}/reconciliations")]
    public async Task<IActionResult> Complete(long id, [FromBody] ReconcileInput input)
    {
        var a = await LoadAccount(id);
        if (input.PeriodTo < input.PeriodFrom) throw new AppException("The period end must be after its start.");
        if (input.PeriodTo > Today) throw new AppException("The period cannot end in the future.");
        var locked = await LockedUntil(id);
        if (locked != null && input.PeriodFrom <= locked) throw new BusinessRuleException($"The account is already reconciled until {locked:yyyy-MM-dd}. Start the period after that date.");
        var counted = await CountedJournalStatuses();
        var book = await GlBalance(a.GlAccountId, input.PeriodTo);
        var unrecorded = await Db.BankStatementLines.Where(l => l.BankAccountId == id && l.Status == "unmatched" && l.LineDate <= input.PeriodTo).SumAsync(l => (decimal?)l.Amount) ?? 0;
        var matchedIds = Db.BankStatementLines.Where(s => s.JournalLineId != null).Select(s => s.JournalLineId!.Value);
        var outstanding = await BookLines(a.GlAccountId, counted).Where(l => l.JournalEntry!.EntryDate <= input.PeriodTo && !matchedIds.Contains(l.Id))
            .SumAsync(l => (decimal?)((l.Debit ?? 0) - (l.Credit ?? 0))) ?? 0;
        var diff = input.StatementBalance - (book - outstanding + unrecorded);
        if (diff != 0) throw new BusinessRuleException($"The reconciliation does not balance (difference {diff:0.00}). Match, ignore or record the remaining items first.");

        await using var tx = await Db.Database.BeginTransactionAsync();
        var r = new BankReconciliation
        {
            BankAccountId = id, PeriodFrom = input.PeriodFrom, PeriodTo = input.PeriodTo, StatementBalance = input.StatementBalance, BookBalance = book,
            OutstandingBook = outstanding, UnrecordedBank = unrecorded, Difference = 0, Status = "completed", CompletedAt = DateTimeOffset.UtcNow, CompletedBy = Me.UserId, Notes = input.Notes,
        };
        Db.BankReconciliations.Add(r);
        await Db.SaveChangesAsync();
        // lock matched / ignored lines of the period; unmatched ones stay open and carry forward
        var toLock = await Db.BankStatementLines.Where(l => l.BankAccountId == id && l.LineDate <= input.PeriodTo && l.ReconciliationId == null && l.Status != "unmatched").ToListAsync();
        foreach (var l in toLock) l.ReconciliationId = r.Id;
        Activity.Log("bank_account", a.Id, "reconciled", $"«{a.NameAr}» reconciled {input.PeriodFrom:yyyy-MM-dd} → {input.PeriodTo:yyyy-MM-dd}, statement balance {input.StatementBalance:0.##}", null, a.BranchId);
        await Db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { r.Id, locked = toLock.Count });
    }

    [HttpGet("accounts/{id:long}/reconciliations")]
    public async Task<IActionResult> Reconciliations(long id)
    {
        await LoadAccount(id);
        var rows = await Db.BankReconciliations.AsNoTracking().Where(r => r.BankAccountId == id).OrderByDescending(r => r.PeriodTo)
            .Select(r => new { r.Id, r.PeriodFrom, r.PeriodTo, r.StatementBalance, r.BookBalance, r.OutstandingBook, r.UnrecordedBank, r.Difference, r.Status, r.CompletedAt, r.CompletedBy, r.Notes })
            .ToListAsync();
        var users = await Db.AppUsers.AsNoTracking().Select(u => new { u.Id, n = u.NameAr ?? u.NameEn ?? u.Email }).ToDictionaryAsync(u => u.Id, u => u.n);
        return Ok(rows.Select(r => new { r.Id, r.PeriodFrom, r.PeriodTo, r.StatementBalance, r.BookBalance, r.OutstandingBook, r.UnrecordedBank, r.Difference, r.Status, r.CompletedAt,
                                         completedBy = r.CompletedBy == null ? null : users.GetValueOrDefault(r.CompletedBy.Value), r.Notes }));
    }

    /// <summary>Reopen the latest completed reconciliation of the account (unlocks its lines).</summary>
    [HttpPost("reconciliations/{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, [FromBody] CancelInput input)
    {
        var r = await Db.BankReconciliations.FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("BankReconciliation", id);
        var a = await LoadAccount(r.BankAccountId);
        if (r.Status != "completed") throw new BusinessRuleException("The reconciliation is not completed.");
        if (await Db.BankReconciliations.AnyAsync(x => x.BankAccountId == r.BankAccountId && x.Status == "completed" && x.PeriodTo > r.PeriodTo))
            throw new BusinessRuleException("Reopen the later reconciliations first.");
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Enter the reason for reopening.");
        r.Status = "reopened"; r.Notes = $"{r.Notes}\nReopened: {input.Reason}".Trim();
        await Db.BankStatementLines.Where(l => l.ReconciliationId == id).ForEachAsync(l => l.ReconciliationId = null);
        Activity.Log("bank_account", a.Id, "reconciliation_reopened", $"«{a.NameAr}» reconciliation until {r.PeriodTo:yyyy-MM-dd} reopened: {input.Reason}", null, a.BranchId);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
