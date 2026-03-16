using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace RailFactory.Shared.Logging;

/// <summary>
/// Ensures every request has a correlation id (from header or new) and pushes it to logging scope (Phase 0 cross-cutting).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdConstants.HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;

        using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}
