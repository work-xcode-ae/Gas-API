using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ---------- Database ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddDbContext<AppDbContext>(o =>
    // No EnableRetryOnFailure: the services use explicit transactions (BeginTransactionAsync),
    // which the retrying execution strategy does not allow.
    o.UseSqlServer(cfg.GetConnectionString("SedraPetrol")));

// ---------- Application services ----------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<NumberingService>();
builder.Services.AddScoped<PartyService>();
builder.Services.AddScoped<ActivityLogger>();
builder.Services.AddScoped<PostingService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<OwnerBillingService>();
builder.Services.AddScoped<TaxService>();
builder.Services.AddScoped<BalanceQueries>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<MeterOps>();
builder.Services.AddScoped<AlertsQuery>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddHostedService<DailyJobsService>();
builder.Services.AddPremisesModule();   // Restaurants & Villas (additive module)
builder.Services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
builder.Services.Configure<StorageOptions>(cfg.GetSection("Storage"));

// ---------- Auth ----------
var jwt = cfg.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters (appsettings.json or environment variable Jwt__Key).");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(2),
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
    };
    // a revoked session (logout, logout-all, password change, user disabled, theft detection) is refused immediately
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            var sid = ctx.Principal?.FindFirst("sid")?.Value;
            if (sid == null || !Guid.TryParse(sid, out var family)) return;   // tokens issued before sessions existed
            if (await SessionCheck.IsRevokedAsync(ctx.HttpContext.RequestServices, family)) ctx.Fail("Session revoked.");
        },
    };
});
builder.Services.AddAuthorization();

// ---------- MVC ----------
builder.Services.AddControllers(o => o.Filters.Add<PermissionFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddCors(o => o.AddPolicy("portal", p => p
    .WithOrigins(cfg.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
    .AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition")));

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sedra Petrol API", Version = "v1", Description = "Buildings, gas supply, meters, tanks/refill, AMC, billing and accounting." });
    c.CustomSchemaIds(t => t.FullName!.Replace("SedraPetrol.Api.", "").Replace("+", "."));
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        Description = "Paste the token returned by POST /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
    var xml = Path.Combine(AppContext.BaseDirectory, "SedraPetrol.Api.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

var app = builder.Build();

// ---------- Database: apply EF migrations (creates the database on first run), then seed ----------
if (cfg.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
if (cfg.GetValue<bool>("Seed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
    // Restaurants & Villas lookups (insert-only); a failure here must never stop the existing system
    try { await scope.ServiceProvider.GetRequiredService<PremisesSeeder>().SeedAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Restaurants & Villas seed failed."); }
    // every person / company has its own ledger account
    try { await scope.ServiceProvider.GetRequiredService<PartyService>().EnsureAllAccountsAsync(); }
    catch (Exception ex) { app.Logger.LogWarning(ex, "Party accounts were not created."); }
}

app.UseMiddleware<ExceptionMiddleware>();
if (app.Environment.IsDevelopment() || cfg.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.DocumentTitle = "Sedra Petrol API"; c.DisplayRequestDuration(); });
}
app.UseCors("portal");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow })).AllowAnonymous();

// demo buildings / tenants / invoices for presentations (Seed:DemoData = true); runs once, in the background, after the API is up
if (cfg.GetValue<bool>("Seed:DemoData"))
    app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(() => DemoDataSeeder.RunAsync(app)));

app.Run();

public partial class Program { }
