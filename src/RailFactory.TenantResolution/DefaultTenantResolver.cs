using RailFactory.Shared.Tenant;

namespace RailFactory.TenantResolution;

/// <summary>
/// Resolves tenant context by combining current tenant id (e.g. from header/JWT) and connection string from registry/config.
/// </summary>
public sealed class DefaultTenantResolver : ITenantResolver
{
    private readonly ICurrentTenantIdProvider _tenantIdProvider;
    private readonly ITenantConnectionProvider _connectionProvider;

    public DefaultTenantResolver(
        ICurrentTenantIdProvider tenantIdProvider,
        ITenantConnectionProvider connectionProvider)
    {
        _tenantIdProvider = tenantIdProvider;
        _connectionProvider = connectionProvider;
    }

    public async Task<TenantContext?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = await _tenantIdProvider.GetCurrentTenantIdAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(tenantId))
            return null;

        var connectionString = await _connectionProvider.GetConnectionStringAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new TenantContext { TenantId = tenantId.Trim(), ConnectionString = connectionString };
    }
}
