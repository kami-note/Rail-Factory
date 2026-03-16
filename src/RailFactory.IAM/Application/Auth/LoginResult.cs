using RailFactory.IAM.Domain.User;

namespace RailFactory.IAM.Application.Auth;

/// <summary>
/// Result of login/link: user and their tenant-role assignments for JWT issuance.
/// </summary>
public sealed class LoginResult
{
    public required User User { get; init; }
    public required IReadOnlyList<UserTenantRole> TenantRoles { get; init; }
}
