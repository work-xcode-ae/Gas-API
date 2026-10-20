using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Links of audit rows to the records they belong to (full record history).</summary>
public partial class AppDbContext
{
    public DbSet<AuditTrailLink> AuditTrailLinks => Set<AuditTrailLink>();
}
