using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Tax codes and their validity periods.</summary>
public partial class AppDbContext
{
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
}
