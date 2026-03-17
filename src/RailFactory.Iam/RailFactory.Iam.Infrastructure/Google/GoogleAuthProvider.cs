using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailFactory.Iam.Application.DTOs;
using RailFactory.Iam.Application.Ports;

namespace RailFactory.Iam.Infrastructure.Google;

public sealed class GoogleAuthProvider : IGoogleAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private static readonly string[] Scopes = ["openid", "email", "profile"];

    private readonly IOptions<GoogleAuthOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleAuthProvider> _logger;

    public GoogleAuthProvider(
        IOptions<GoogleAuthOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleAuthProvider> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> GetUserInfoAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        var clientId = _options.Value.ClientId;
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogWarning("Google ClientId is not configured.");
            return null;
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            }).ConfigureAwait(false);

            return new GoogleUserInfo
            {
                Sub = payload.Subject,
                Email = payload.Email ?? string.Empty,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token.");
            return null;
        }
    }

    public async Task<string?> ExchangeCodeForIdTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (string.IsNullOrEmpty(opts.ClientId) || string.IsNullOrEmpty(opts.ClientSecret))
        {
            _logger.LogWarning("Google ClientId or ClientSecret is not configured.");
            return null;
        }

        var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = opts.ClientId,
            ["client_secret"] = opts.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        });

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(TokenEndpoint, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken).ConfigureAwait(false);
            return tokenResponse?.IdToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to exchange authorization code for tokens.");
            return null;
        }
    }

    /// <summary>
    /// Builds the Google authorization URL for the redirect flow. Caller must append state and use the same redirectUri in the callback.
    /// </summary>
    public string BuildAuthorizationUrl(string redirectUri, string state)
    {
        var opts = _options.Value;
        var scope = string.Join(" ", Scopes);
        var query = string.Join("&", new[]
        {
            $"client_id={Uri.EscapeDataString(opts.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            "response_type=code",
            $"scope={Uri.EscapeDataString(scope)}",
            $"state={Uri.EscapeDataString(state)}",
            "access_type=offline",
            "prompt=consent"
        });
        return $"{AuthorizationEndpoint}?{query}";
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
