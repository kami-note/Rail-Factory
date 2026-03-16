namespace RailFactory.Shared.Jwt;

/// <summary>
/// JWT claim names for tenant and roles (doc 13). Used by IAM when issuing tokens and by downstream services when reading.
/// Roles use the standard ClaimTypes.Role for .NET authorization; RoleClaimType is documented here for consistency.
/// </summary>
public static class JwtClaimNames
{
    public const string TenantId = "tenant_id";
    public const string TenantIds = "tenant_ids";

    /// <summary>
    /// Role claim type. IAM issues roles with System.Security.Claims.ClaimTypes.Role so [Authorize(Roles = "...")] works.
    /// </summary>
    public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
}
