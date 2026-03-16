using MassTransit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using RailFactory.IAM.Adapters;
using RailFactory.IAM.Application;
using RailFactory.IAM.Infrastructure;
using RailFactory.IAM.Ports;
using RailFactory.IAM.Worker;
using RailFactory.Shared.Health;
using RailFactory.Shared.Logging;
using RailFactory.Shared.Tenant;
using RailFactory.TenantResolution;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty(CorrelationIdConstants.LogPropertyName, ""));

// Identity DB (single DB for IAM; not per-tenant)
var identityConn = builder.Configuration.GetConnectionString("Identity");
builder.Services.AddDbContext<IamDbContext>(o => o.UseNpgsql(identityConn));

var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitPort = (builder.Configuration["RabbitMQ:Port"]?.ToString()) ?? "5672";
var rabbitVHost = (builder.Configuration["RabbitMQ:VirtualHost"] ?? "/").TrimStart('/');
var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "railfactory";
var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "railfactory_secret";
var rabbitUri = $"amqp://{Uri.EscapeDataString(rabbitUser)}:{Uri.EscapeDataString(rabbitPass)}@{rabbitHost}:{rabbitPort}/{rabbitVHost}";

// AspNetCore.HealthChecks.RabbitMQ 9.x requires IConnection (RabbitMQ.Client 7); register singleton and reuse for health checks.
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory { Uri = new Uri(rabbitUri), AutomaticRecoveryEnabled = true };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API"))
    .AddNpgSql(identityConn!, name: "identity_db", tags: ["db"])
    .AddRabbitMQ(name: "rabbitmq", tags: ["broker"]);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditStore, AuditStore>();
builder.Services.AddScoped<IOutboxStore, OutboxStore>();
builder.Services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();
builder.Services.AddScoped<LoginOrRegisterHandler>();
builder.Services.Configure<JwtIssuerOptions>(builder.Configuration.GetSection(JwtIssuerOptions.SectionName));
builder.Services.AddSingleton<IJwtIssuer, JwtIssuer>();

// MassTransit + RabbitMQ (UserCreated/UserUpdated outbox publisher)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(rabbitUri);
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddHostedService<OutboxPublisherWorker>();

// Tenant resolution: header for dev (e.g. /tenant endpoint); IAM does not use tenant DB for identity
builder.Services.AddTenantResolutionFromHeaderAndConfig();

// Auth: Cookie for Google OAuth flow, then we issue JWT in callback. Google is optional (dev without ClientId).
var googleClientId = builder.Configuration["Google:ClientId"]?.Trim();
var hasGoogle = !string.IsNullOrEmpty(googleClientId);

var authBuilder = builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = hasGoogle ? GoogleDefaults.AuthenticationScheme : CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie();

if (hasGoogle)
{
    authBuilder.AddGoogle(o =>
    {
        o.ClientId = googleClientId!;
        o.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
    });
}

authBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
{
    o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? "")),
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization(o => o.AddPolicy("Jwt", p =>
{
    p.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
    p.RequireAuthenticatedUser();
}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "RailFactory.IAM", Version = "v1" }));
var app = builder.Build();

// When behind the gateway (e.g. /api/iam), set ASPNETCORE_PATHBASE so redirect URIs and links are correct
var pathBase = builder.Configuration["ASPNETCORE_PATHBASE"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE");
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase.TrimEnd('/'));

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RailFactory.Shared.Logging.CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

// Ensure IAM schema and tables exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}

app.MapGet("/", () => Results.Ok(new { service = "RailFactory.IAM", status = "running" }));

app.MapGet("/tenant", async (ITenantResolver resolver) =>
{
    var ctx = await resolver.ResolveAsync();
    if (ctx is null)
        return Results.Json(new { resolved = false, message = "Set X-Tenant-Id header to a registered tenant id" }, statusCode: 400);
    return Results.Json(new { resolved = true, tenantId = ctx.TenantId, hasConnection = !string.IsNullOrEmpty(ctx.ConnectionString) });
});

app.MapAuthEndpoints();

app.MapGet("/me", async (System.Security.Claims.ClaimsPrincipal user, [FromServices] IUserRepository userRepo) =>
{
    var sub = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
        return Results.Unauthorized();
    var u = await userRepo.GetByIdAsync(userId);
    if (u is null)
        return Results.NotFound();
    var roles = await userRepo.GetTenantRolesAsync(userId);
    return Results.Ok(new { u.Id, u.Email, u.DisplayName, tenantRoles = roles.Select(r => new { r.TenantId, r.Role }).ToList() });
}).RequireAuthorization("Jwt");

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = RailFactory.Shared.Health.HealthResponseWriter.WriteAsync
});

if (app.Environment.IsDevelopment())
    app.UseSwagger().UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "RailFactory.IAM v1"));
app.Run();
