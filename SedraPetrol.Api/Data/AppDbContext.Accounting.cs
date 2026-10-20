using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Accounting tables added for BRD phase 1 (settings, journal approvals).</summary>
public partial class AppDbContext
{
    public DbSet<AccountingSetting> AccountingSettings => Set<AccountingSetting>();
    public DbSet<JournalApproval> JournalApprovals => Set<JournalApproval>();
}
