using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Background housekeeping every few hours: overdue invoices, expired / auto-renewed contracts,
/// expired AMC contracts and Civil Defense certificates, and low tank status.
/// </summary>
public class DailyJobsService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<DailyJobsService> _log;

    public DailyJobsService(IServiceScopeFactory scopes, ILogger<DailyJobsService> log)
    {
        _scopes = scopes; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await RunOnceAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Daily jobs failed (will retry).");
            }
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    public static async Task RunOnceAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        if (!await db.Database.CanConnectAsync(ct)) return;
        var lookups = sp.GetRequiredService<LookupService>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 1. Overdue invoices
        var overdue = await lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "overdue");
        var pending = await lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "pending");
        var partial = await lookups.IdOrNullAsync(LookupTypes.InvoiceStatus, "partial");
        if (overdue != null)
            await db.Invoices.Where(i => i.DueDate < today && (i.StatusId == pending || i.StatusId == partial))
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.StatusId, overdue).SetProperty(i => i.UpdatedAt, DateTimeOffset.UtcNow), ct);

        // 2. Supply contracts: auto-renew or expire
        var active = await lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        var expired = await lookups.IdOrNullAsync(LookupTypes.ContractStatus, "expired");
        var ending = await db.SupplyContracts.Where(c => c.StatusId == active && c.EndDate != null && c.EndDate < today).ToListAsync(ct);
        foreach (var c in ending)
        {
            if (c.AutoRenew && (c.RenewalPeriodMonths ?? 0) > 0)
                while (c.EndDate < today) c.EndDate = c.EndDate!.Value.AddMonths(c.RenewalPeriodMonths!.Value);
            else { c.StatusId = expired; c.IsActive = false; }
        }

        // 3. AMC contracts
        var amcActive = await lookups.IdOrNullAsync(LookupTypes.AmcStatus, "active");
        var amcExpired = await lookups.IdOrNullAsync(LookupTypes.AmcStatus, "expired");
        foreach (var a in await db.AmcContracts.Where(a => a.StatusId == amcActive && a.EndDate < today).ToListAsync(ct))
            a.StatusId = amcExpired;

        // 4. Civil Defense certificates
        var cdValid = await lookups.IdOrNullAsync(LookupTypes.CdStatus, "valid");
        var cdExpired = await lookups.IdOrNullAsync(LookupTypes.CdStatus, "expired");
        foreach (var cert in await db.CdCertificates.Where(c => c.StatusId == cdValid && c.ExpiryDate < today).ToListAsync(ct))
            cert.StatusId = cdExpired;

        // 5. Tank status from level
        var ok = await lookups.IdOrNullAsync(LookupTypes.TankStatus, "ok");
        var low = await lookups.IdOrNullAsync(LookupTypes.TankStatus, "low");
        foreach (var t in await db.Tanks.Where(t => (t.StatusId == ok || t.StatusId == low) && t.CapacityLitres > 0).ToListAsync(ct))
        {
            var pct = (t.CurrentLevelLitres ?? 0) / t.CapacityLitres * 100;
            t.StatusId = pct < TankRules.LowLevelPercent ? low : ok;
        }
        await db.SaveChangesAsync(ct);

        // 6. Owner billing: AMC instalments that reached their due date are invoiced and posted automatically
        try
        {
            var n = await sp.GetRequiredService<OwnerBillingService>().IssueDueAmcAsync(null, sp.GetRequiredService<ILogger<DailyJobsService>>());
            if (n > 0) sp.GetRequiredService<ILogger<DailyJobsService>>().LogInformation("Daily jobs: {Count} AMC instalment invoices issued.", n);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sp.GetRequiredService<ILogger<DailyJobsService>>().LogWarning(ex, "AMC auto-invoicing failed (will retry).");
        }
    }
}

public static class TankRules
{
    /// <summary>Below this percentage a tank is flagged "low" and shows in refill alerts.</summary>
    public const decimal LowLevelPercent = 25m;
}
