namespace RailFactory.IAM.Ports;

/// <summary>
/// Port for issuing JWTs with tenant id and roles so downstream services can scope without calling IAM on every request.
/// </summary>
public interface IJwtIssuer
{
    /// <summary>
    /// Issues a JWT for the authenticated user with the given tenant and roles.
    /// For Matrix Admin, caller may pass multiple tenant ids or a special claim; for single-tenant users, one tenant id.
    /// </summary>
    string IssueToken(JwtIssueRequest request);
}

public sealed class JwtIssueRequest
{
    public required string Subject { get; init; } // user id
    public required string Email { get; init; }
    public string? Name { get; init; }
    /// <summary>Primary or selected tenant id for this request (single-tenant users).</summary>
    public string? TenantId { get; init; }
    /// <summary>All tenant ids the user can access (e.g. for Matrix Admin).</summary>
    public IReadOnlyList<string>? TenantIds { get; init; }
    public required IReadOnlyList<string> Roles { get; init; } // role names, e.g. "BranchAdmin", "MatrixAdmin"
    public TimeSpan? ValidFor { get; init; }
}
