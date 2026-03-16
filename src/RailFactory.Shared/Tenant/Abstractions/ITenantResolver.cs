namespace RailFactory.Shared.Tenant;

/// <summary>
/// Resolves tenant id from the current request (e.g. JWT claim or header) and provides tenant → PostgreSQL connection string.
/// One DB per tenant; each service uses one schema per tenant DB (see docs/03_Architecture.md).
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves tenant context for the current request. Returns null if tenant cannot be determined (e.g. unauthenticated).
    /// </summary>
    Task<TenantContext?> ResolveAsync(CancellationToken cancellationToken = default);
}
