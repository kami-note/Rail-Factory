namespace RailFactory.Shared.Tenant;

/// <summary>
/// Provides the PostgreSQL connection string for a given tenant. Used by the tenant resolver.
/// Implementations may read from configuration, a tenant registry database, or a cache.
/// </summary>
public interface ITenantConnectionProvider
{
    /// <summary>
    /// Gets the connection string for the tenant, or null if the tenant is not registered.
    /// </summary>
    Task<string?> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default);
}
