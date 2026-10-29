namespace SedraPetrol.Api.Services;

/// <summary>Registration of the Restaurants &amp; Villas module (one line in Program.cs).</summary>
public static class PremisesModule
{
    public static IServiceCollection AddPremisesModule(this IServiceCollection services)
    {
        services.AddScoped<PremisesSeeder>();
        services.AddScoped<PremisesBillingService>();
        services.AddHostedService<PremisesJobsService>();
        return services;
    }
}
