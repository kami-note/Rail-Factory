using Microsoft.AspNetCore.Http;
using RailFactory.Shared.Jwt;
using RailFactory.Shared.Tenant;

namespace RailFactory.TenantResolution;

/// <summary>
/// Reads the current tenant id from the validated JWT (claim "tenant_id"). Use in production (Phase 1); doc 13.
/// Register when using JWT Bearer auth so the principal is set before resolution.
/// </summary>
public sealed class JwtCurrentTenantIdProvider : ICurrentTenantIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentTenantIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return Task.FromResult<string?>(null);

        var tenantId = user.FindFirst(JwtClaimNames.TenantId)?.Value;
        if (!string.IsNullOrWhiteSpace(tenantId))
            return Task.FromResult<string?>(tenantId.Trim());

        var tenantIds = user.FindAll(JwtClaimNames.TenantIds).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        return Task.FromResult(tenantIds.Count > 0 ? tenantIds[0] : null);
    }
}
