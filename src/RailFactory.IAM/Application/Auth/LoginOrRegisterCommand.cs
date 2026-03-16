namespace RailFactory.IAM.Application.Auth;

/// <summary>
/// After OAuth2 callback: link external identity to user (create if first login) and return login result for JWT.
/// </summary>
public sealed class LoginOrRegisterCommand
{
    public required string ExternalId { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
}
