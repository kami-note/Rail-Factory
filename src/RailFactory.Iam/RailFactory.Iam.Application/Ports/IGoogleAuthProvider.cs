using RailFactory.Iam.Application.DTOs;

namespace RailFactory.Iam.Application.Ports;

/// <summary>
/// Port for obtaining user information from Google OAuth (ID token validation and code exchange).
/// </summary>
public interface IGoogleAuthProvider
{
    /// <summary>
    /// Validates the token and returns the user's claims from Google.
    /// </summary>
    Task<GoogleUserInfo?> GetUserInfoAsync(string idToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authorization code for an ID token (and optionally access token) from Google.
    /// </summary>
    /// <param name="code">The authorization code from the callback query.</param>
    /// <param name="redirectUri">Must match the redirect_uri used in the authorization request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID token string, or null if exchange failed.</returns>
    Task<string?> ExchangeCodeForIdTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the Google authorization URL for the redirect flow (GET /auth/google).
    /// </summary>
    /// <param name="redirectUri">Callback URL (must match Google Console and be used in callback).</param>
    /// <param name="state">CSRF state value to verify in callback.</param>
    string BuildAuthorizationUrl(string redirectUri, string state);
}
