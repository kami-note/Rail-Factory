using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;

namespace RailFactory.Frontend.Security;

public sealed class IamAuthService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IDistributedCache cache) : IAuthService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        => Task.FromResult(AuthResult.Fail("Registration is handled by Google sign-in in IAM."));

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => Task.FromResult(AuthResult.Fail("Password login is not supported. Use Google sign-in."));

    public async Task<AuthResult> ExchangeGoogleAuthCodeAsync(string code, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return AuthResult.Fail("Missing code.");

            var client = CreateIamClient();

            using var content = new StringContent(JsonSerializer.Serialize(new { code }, Json), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("/auth/exchange", content, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return AuthResult.Fail("Invalid or expired login code.");
            if (!response.IsSuccessStatusCode)
                return AuthResult.Fail($"IAM exchange failed ({(int)response.StatusCode}).");

            var exchange = await response.Content.ReadFromJsonAsync<ExchangeResponse>(Json, cancellationToken).ConfigureAwait(false);
            if (exchange?.User is null || string.IsNullOrWhiteSpace(exchange.IdToken))
                return AuthResult.Fail("Invalid IAM exchange response.");
            if (string.IsNullOrWhiteSpace(exchange.User.Email))
                return AuthResult.Fail("Invalid IAM exchange response (missing email).");

            var sessionId = Guid.NewGuid().ToString("N");
            await cache.SetStringAsync($"iam_session:{sessionId}", exchange.IdToken, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
            }, cancellationToken).ConfigureAwait(false);

            var principal = ToPrincipal(exchange.User, sessionId);
            return AuthResult.Ok(principal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return AuthResult.Fail($"IAM exchange error: {ex.Message}");
        }
    }

    private HttpClient CreateIamClient()
    {
        var baseUrl =
            // Aspire service discovery (normalized)
            configuration["services:identity-access-management:https:0"]
            ?? configuration["services:identity-access-management:http:0"]
            // Aspire service discovery (raw env var key lookup)
            ?? configuration["services__identity-access-management__https__0"]
            ?? configuration["services__identity-access-management__http__0"]
            // Aspire reference aliases
            ?? configuration["IDENTITY_ACCESS_MANAGEMENT_HTTPS"]
            ?? configuration["IDENTITY_ACCESS_MANAGEMENT_HTTP"]
            ?? throw new InvalidOperationException("IAM base URL not configured (missing Aspire service discovery env vars).");

        var client = httpClientFactory.CreateClient(nameof(IamAuthService));
        client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        return client;
    }

    private static ClaimsPrincipal ToPrincipal(UserDto me, string sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, me.Id.ToString("D")),
            new(ClaimTypes.Email, me.Email),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(me.DisplayName) ? me.Email : me.DisplayName!),
            new("iam:session_id", sessionId),
        };

        if (!string.IsNullOrWhiteSpace(me.PictureUrl))
            claims.Add(new Claim("iam:picture_url", me.PictureUrl));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private sealed class ExchangeResponse
    {
        public UserDto? User { get; set; }
        public string IdToken { get; set; } = "";
    }

    private sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? PictureUrl { get; set; }
    }
}

