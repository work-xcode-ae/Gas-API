using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Refresh tokens (sessions).</summary>
public partial class AppDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}
