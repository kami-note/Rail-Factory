using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using RailFactory.Iam.Api;
using RailFactory.Iam.Application;
using RailFactory.Iam.Infrastructure;
using RailFactory.Iam.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("redis");

// Forwarded Headers Configuration
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Loopback);
    var gatewayIp = builder.Configuration["Gateway:IpAddress"];
    if (!string.IsNullOrEmpty(gatewayIp) && IPAddress.TryParse(gatewayIp, out var address))
    {
        options.KnownProxies.Add(address);
    }
});

builder.Services.AddHttpClient();

// Use Redis for distributed cache (OAuth state + auth exchange codes)
var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Connection string 'redis' is not configured.");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});

// Hexagonal Architecture: Register Application and Infrastructure layers
builder.Services.AddIamApplication();
builder.Services.AddIamInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<IamDbContext>("iamdb", tags: ["ready"])
    .AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Rail Factory IAM API",
        Version = "v1",
        Description = "Identity & Access Management microservice."
    });
});

builder.Services.AddHostedService<MigrationHostedService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "IAM API v1"));
}

// Migrations run in background under a Redis distributed lock (see MigrationHostedService); startup and health probes are not blocked.

// Auth Middlewares
app.UseAuthentication();
app.UseAuthorization();

// Register Endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();
