using System.Text;
using ATMCashForecasting.Application;
using ATMCashForecasting.Application.Common.Interfaces;
using ATMCashForecasting.Infrastructure;
using ATMCashForecasting.Infrastructure.Identity;
using ATMCashForecasting.Web.Startup;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (Module: structured logging across the whole request pipeline) ----------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console());

// ---------- MVC + Web API (single host, per the pragmatic ASP.NET Core Clean Architecture pattern) ----------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ---------- Swagger / OpenAPI ----------
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ATM Cash Forecasting & Replenishment Management API",
        Version = "v1",
        Description = "REST APIs for ATM master data, transaction ingestion, forecasting, replenishment, dashboards, alerts and reporting."
    });

    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter the JWT access token returned from /api/auth/login."
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ---------- Authentication: cookie scheme for the MVC UI (from Identity), JWT bearer for the REST API ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session timeout (security requirement).
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("ApiUser", policy => policy
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ---------- Middleware pipeline ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Dashboard}/{id?}");
app.MapRazorPages();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ATMCashForecasting.Infrastructure.Persistence.ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        db.Database.EnsureCreated();
    }
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

// Nightly forecast run at 02:00 UTC and an offline-ATM sweep every 15 minutes (Module 4/5/8 automation).
RecurringJob.AddOrUpdate<ATMCashForecasting.Infrastructure.BackgroundJobs.ForecastJob>(
    "nightly-forecast-run", job => job.RunNightlyForecastAsync(), "0 2 * * *");
RecurringJob.AddOrUpdate<ATMCashForecasting.Infrastructure.BackgroundJobs.AlertScanJob>(
    "atm-offline-scan", job => job.ScanForOfflineAtmsAsync(), "*/15 * * * *");
RecurringJob.AddOrUpdate<ATMCashForecasting.Infrastructure.BackgroundJobs.AlertScanJob>(
    "notification-dispatch", job => job.DispatchPendingNotificationsAsync(), "*/5 * * * *");

app.Run();

/// <summary>Restricts the Hangfire dashboard to authenticated System Administrators only.</summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(ATMCashForecasting.Application.Common.Roles.SystemAdministrator);
    }
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
