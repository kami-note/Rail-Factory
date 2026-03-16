using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RailFactory.Shared.Tenant;

namespace RailFactory.TenantResolution;

/// <summary>
/// Registers tenant resolution services so the app can resolve tenant id and connection string per request.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds tenant resolution using header for tenant id (X-Tenant-Id) and configuration for connection strings.
    /// Call from the service's Program.cs after AddConfiguration. Use only in development (doc 13).
    /// </summary>
    public static IServiceCollection AddTenantResolutionFromHeaderAndConfig(this IServiceCollection services, string tenantIdHeaderName = HeaderCurrentTenantIdProvider.DefaultHeaderName)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantIdProvider>(sp => new HeaderCurrentTenantIdProvider(sp.GetRequiredService<IHttpContextAccessor>(), tenantIdHeaderName));
        services.AddSingleton<ITenantConnectionProvider, ConfigurationTenantConnectionProvider>();
        services.AddScoped<ITenantResolver, DefaultTenantResolver>();
        return services;
    }

    /// <summary>
    /// Adds tenant resolution using JWT claims (tenant_id / tenant_ids). Use in production with JWT Bearer auth (Phase 1).
    /// </summary>
    public static IServiceCollection AddTenantResolutionFromJwtAndConfig(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantIdProvider, JwtCurrentTenantIdProvider>();
        services.AddSingleton<ITenantConnectionProvider, ConfigurationTenantConnectionProvider>();
        services.AddScoped<ITenantResolver, DefaultTenantResolver>();
        return services;
    }
}
