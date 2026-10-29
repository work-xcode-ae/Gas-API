using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;

namespace SedraPetrol.Api.Services;

/// <summary>
/// Restaurants &amp; Villas housekeeping (separate from <see cref="DailyJobsService"/>, which is not changed):
/// delivery contracts that passed their end date are auto-renewed (when set) or expired. Every 6 hours.
/// Tank low / ok status and refill alerts of villa / restaurant tanks are already handled by DailyJobsService and AlertsQuery,
/// which work on every tank.
/// </summary>
public class PremisesJobsService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PremisesJobsService> _log;

    public PremisesJobsService(IServiceScopeFactory scopes, ILogger<PremisesJobsService> log) { _scopes = scopes; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await RunOnceAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Restaurants & Villas jobs failed (will retry).");
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
        var active = await lookups.IdOrNullAsync(LookupTypes.ContractStatus, "active");
        var expired = await lookups.IdOrNullAsync(LookupTypes.ContractStatus, "expired");
        foreach (var c in await db.DeliveryContracts.Where(c => c.StatusId == active && c.EndDate != null && c.EndDate < today).ToListAsync(ct))
        {
            if (c.AutoRenew && (c.RenewalPeriodMonths ?? 0) > 0)
                while (c.EndDate < today) c.EndDate = c.EndDate!.Value.AddMonths(c.RenewalPeriodMonths!.Value);
            else { c.StatusId = expired; c.IsActive = false; }
        }
        await db.SaveChangesAsync(ct);
    }
}
