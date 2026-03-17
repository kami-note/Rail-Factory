namespace RailFactory.Iam.Application.DTOs;

/// <summary>
/// User information returned by the Google OAuth provider.
/// </summary>
public sealed class GoogleUserInfo
{
    public string Sub { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Picture { get; init; }
}
