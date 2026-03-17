using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RailFactory.Iam.Application.Services;
using RailFactory.Iam.Infrastructure.Google;
using RailFactory.Iam.Application.Ports;

namespace RailFactory.Iam.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        // Redirect flow: GET /auth/google -> Google -> GET /auth/google/callback
        group.MapGet("/google", (IGoogleAuthProvider googleAuth, IOptions<GoogleAuthOptions> googleOptions, IMemoryCache cache) =>
        {
            var opts = googleOptions.Value;
            if (string.IsNullOrEmpty(opts.ClientId) || string.IsNullOrEmpty(opts.RedirectUri))
                return Results.BadRequest("Google OAuth is not configured (ClientId, RedirectUri).");

            var state = Guid.NewGuid().ToString("N");
            cache.Set("oauth_state:" + state, true, TimeSpan.FromMinutes(5));

            var url = googleAuth.BuildAuthorizationUrl(opts.RedirectUri, state);
            return Results.Redirect(url);
        });

        group.MapGet("/google/callback", async (
            string? code,
            string? state,
            IGoogleAuthProvider googleAuth,
            IUserApplicationService userService,
            IOptions<GoogleAuthOptions> googleOptions,
            IMemoryCache cache,
            CancellationToken ct) =>
        {
            var opts = googleOptions.Value;
            if (string.IsNullOrEmpty(opts.RedirectUri) || string.IsNullOrEmpty(opts.FrontendRedirectUri))
                return Results.BadRequest("Google OAuth redirect URIs are not configured.");

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return Results.Redirect(opts.FrontendRedirectUri + "?error=missing_code_or_state");

            var stateKey = "oauth_state:" + state;
            if (cache.Get(stateKey) is null)
                return Results.Redirect(opts.FrontendRedirectUri + "?error=invalid_state");
            cache.Remove(stateKey);

            var idToken = await googleAuth.ExchangeCodeForIdTokenAsync(code, opts.RedirectUri, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(idToken))
                return Results.Redirect(opts.FrontendRedirectUri + "?error=token_exchange_failed");

            try
            {
                await userService.RegisterOrUpdateFromGoogleAsync(idToken, ct).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Redirect(opts.FrontendRedirectUri + "?error=unauthorized");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("expelled"))
            {
                return Results.Redirect(opts.FrontendRedirectUri + "?error=expelled");
            }

            var fragment = "id_token=" + Uri.EscapeDataString(idToken);
            return Results.Redirect(opts.FrontendRedirectUri + "#" + fragment);
        });

        app.MapPost("/api/auth/google", async ([FromBody] GoogleLoginRequest request, IUserApplicationService userService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdToken))
                return Results.BadRequest("IdToken is required.");
            try
            {
                var user = await userService.RegisterOrUpdateFromGoogleAsync(request.IdToken, ct).ConfigureAwait(false);
                return Results.Ok(user);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("expelled"))
            {
                return Results.Json(new { error = "Account has been expelled." }, statusCode: 403);
            }
        });
    }

    internal sealed record GoogleLoginRequest(string? IdToken);
}
