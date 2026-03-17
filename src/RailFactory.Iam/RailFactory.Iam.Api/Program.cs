using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Api;
using RailFactory.Iam.Application;
using RailFactory.Iam.Infrastructure;
using RailFactory.Iam.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Infrastructure Configuration
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddIamApplication();
builder.Services.AddIamInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<IamDbContext>("iamdb", tags: ["ready"]);

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

var app = builder.Build();

app.UseForwardedHeaders();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "IAM API v1"));
}

// Database Migrations
await ApplyMigrationsAsync(app);

// --- Register Endpoints ---
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();

async Task ApplyMigrationsAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Migration");
    const int maxRetries = 5;
    for (var i = 0; i < maxRetries; i++)
    {
        try
        {
            await db.Database.MigrateAsync().ConfigureAwait(false);
            break;
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1) throw;
            logger.LogWarning(ex, "Migration attempt {Attempt}/{Max} failed, retrying in 2s...", i + 1, maxRetries);
            await Task.Delay(2000).ConfigureAwait(false);
        }
    }
}
