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
    o.UseSqlServer(cfg.GetConnectionString("SedraPetrol"), sql => sql.EnableRetryOnFailure(3)));

// ---------- Application services ----------
builder.Services.AddMemoryCache();
builder.Services.AddScoped<LookupService>();
builder.Services.AddScoped<NumberingService>();
builder.Services.AddScoped<ActivityLogger>();
builder.Services.AddScoped<PostingService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<BalanceQueries>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<MeterOps>();
builder.Services.AddScoped<AlertsQuery>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHostedService<DailyJobsService>();
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

// ---------- Seed (idempotent) ----------
if (cfg.GetValue<bool>("Seed:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();
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

app.Run();

public partial class Program { }
