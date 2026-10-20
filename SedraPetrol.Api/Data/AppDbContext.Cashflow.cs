using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Cash-flow forecast tables (BRD §16): manually entered expected receipts / payments.</summary>
public partial class AppDbContext
{
    public DbSet<CashflowItem> CashflowItems => Set<CashflowItem>();
}
