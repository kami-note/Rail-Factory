using Microsoft.AspNetCore.Http;
using RailFactory.Shared.Tenant;

namespace RailFactory.TenantResolution;

/// <summary>
/// Reads the current tenant id from the request header (e.g. X-Tenant-Id).
/// Use for development or when JWT is not yet available; Phase 1 will add JWT-based provider.
/// </summary>
public sealed class HeaderCurrentTenantIdProvider : ICurrentTenantIdProvider
{
    public const string DefaultHeaderName = "X-Tenant-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _headerName;

    public HeaderCurrentTenantIdProvider(IHttpContextAccessor httpContextAccessor, string headerName = DefaultHeaderName)
    {
        _httpContextAccessor = httpContextAccessor;
        _headerName = headerName;
    }

    public Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = _httpContextAccessor.HttpContext?.Request.Headers[_headerName].FirstOrDefault();
        return Task.FromResult(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
    }
}
