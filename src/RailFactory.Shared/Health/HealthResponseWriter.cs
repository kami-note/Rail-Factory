using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RailFactory.Shared.Health;

/// <summary>
/// Writes health check results in the standard RNF-07 format (Phase 0 cross-cutting).
/// </summary>
public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var checks = report.Entries.ToDictionary(
            e => e.Key,
            e => e.Value.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy");
        var response = new HealthResponse
        {
            Status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy",
            Checks = checks,
            Timestamp = DateTimeOffset.UtcNow
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
