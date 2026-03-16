namespace RailFactory.Shared.Tenant;

/// <summary>
/// Resolved tenant context for the current request. Used by all services to scope queries and operations.
/// </summary>
public sealed class TenantContext
{
    public required string TenantId { get; init; }
    public string? ConnectionString { get; init; }
}
