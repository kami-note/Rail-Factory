using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using RailFactory.Shared.Health;
using RailFactory.Shared.Logging;
using RailFactory.TenantResolution;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().Enrich.WithProperty(CorrelationIdConstants.LogPropertyName, ""));
builder.Services.AddHealthChecks();
builder.Services.AddTenantResolutionFromHeaderAndConfig();

var app = builder.Build();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.MapGet("/", () => Results.Ok(new { service = "RailFactory.SupplyChain", status = "running" }));
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });
app.Run();
