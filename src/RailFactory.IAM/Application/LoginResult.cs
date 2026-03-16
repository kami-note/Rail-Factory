using RailFactory.IAM.Domain;

namespace RailFactory.IAM.Application;

/// <summary>
/// Result of login/link: user and their tenant-role assignments for JWT issuance.
/// </summary>
public sealed class LoginResult
{
    public required User User { get; init; }
    public required IReadOnlyList<UserTenantRole> TenantRoles { get; init; }
}
