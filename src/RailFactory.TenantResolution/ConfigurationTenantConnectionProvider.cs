using Microsoft.Extensions.Configuration;
using RailFactory.Shared.Tenant;

namespace RailFactory.TenantResolution;

/// <summary>
/// Resolves tenant connection strings from configuration (appsettings or environment).
/// Section "Tenants" with array of { "Id": "...", "ConnectionString": "..." } or
/// ConnectionStrings:Tenant_{id} for each tenant.
/// </summary>
public sealed class ConfigurationTenantConnectionProvider : ITenantConnectionProvider
{
    private const string TenantsSection = "Tenants";
    private const string ConnectionStringsTenantPrefix = "Tenant_";

    private readonly IConfiguration _configuration;

    public ConfigurationTenantConnectionProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<string?> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1) ConnectionStrings:Tenant_{id} (e.g. Tenant_default)
        var key = ConnectionStringsTenantPrefix + tenantId;
        var fromConnStrings = _configuration.GetConnectionString(key);
        if (!string.IsNullOrWhiteSpace(fromConnStrings))
            return Task.FromResult<string?>(fromConnStrings);

        // 2) Section "Tenants": array of { Id, ConnectionString }
        var tenants = _configuration.GetSection(TenantsSection).Get<List<TenantEntry>>();
        var entry = tenants?.FirstOrDefault(t => string.Equals(t.Id, tenantId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entry?.ConnectionString);
    }

    private sealed class TenantEntry
    {
        public string Id { get; set; } = "";
        public string ConnectionString { get; set; } = "";
    }
}
