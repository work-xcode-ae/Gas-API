using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SedraPetrol.Api.Data;

/// <summary>
/// Used only by the EF tools (dotnet ef migrations add / database update) so they don't have to start the web host.
/// Reads the connection string from appsettings(.Development).json or the ConnectionStrings__SedraPetrol variable.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var cfg = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var cs = cfg.GetConnectionString("SedraPetrol")
                 ?? "Server=localhost;Database=SedraPetrol;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options;
        return new AppDbContext(options);
    }
}
