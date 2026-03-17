namespace RailFactory.Iam.Infrastructure.Google;

/// <summary>
/// Configuration for Google OAuth (ID token validation and authorization code flow).
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Google";

    /// <summary>Google OAuth 2.0 Client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Google OAuth 2.0 Client Secret (required for code exchange in /auth/google/callback).</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Callback URL registered in Google Console (e.g. https://localhost:5001/auth/google/callback).</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Where to redirect the user after successful login (e.g. SPA URL). Id token is passed in fragment: #id_token=...</summary>
    public string FrontendRedirectUri { get; set; } = string.Empty;
}
