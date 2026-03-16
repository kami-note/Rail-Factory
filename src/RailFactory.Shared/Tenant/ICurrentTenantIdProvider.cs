namespace RailFactory.Shared.Tenant;

/// <summary>
/// Provides the current request's tenant id (e.g. from JWT or header).
/// Implementations are host-specific (ASP.NET Core: header, JWT in Phase 1).
/// </summary>
public interface ICurrentTenantIdProvider
{
    /// <summary>
    /// Gets the tenant id for the current request, or null if not set (e.g. unauthenticated).
    /// </summary>
    Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default);
}
