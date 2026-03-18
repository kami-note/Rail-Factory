using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RailFactory.Iam.Application.Services;
using RailFactory.Iam.Infrastructure.Google;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Domain.Exceptions;

namespace RailFactory.Iam.Api;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        // Redirect flow: GET /auth/google -> Google -> GET /auth/google/callback
        group.MapGet("/google", async (IGoogleAuthProvider googleAuth, IOptions<GoogleAuthOptions> googleOptions, IOAuthStateStore stateStore) =>
        {
            var opts = googleOptions.Value;
            if (string.IsNullOrEmpty(opts.ClientId) || string.IsNullOrEmpty(opts.RedirectUri))
                return Results.BadRequest("Google OAuth is not configured (ClientId, RedirectUri).");

            var state = Guid.NewGuid().ToString("N");
            var nonce = Guid.NewGuid().ToString("N");
            await stateStore.SaveStateAsync(state, nonce, TimeSpan.FromMinutes(5));

            var url = googleAuth.BuildAuthorizationUrl(opts.RedirectUri, state);
            return Results.Redirect(url);
        });

        group.MapGet("/google/callback", async (
            string? code,
            string? state,
            HttpContext context,
            IGoogleAuthProvider googleAuth,
            IUserApplicationService userService,
            IOptions<GoogleAuthOptions> googleOptions,
            IOAuthStateStore stateStore,
            IAuthCodeStore authCodeStore,
            CancellationToken ct) =>
        {
            var opts = googleOptions.Value;
            if (string.IsNullOrEmpty(opts.RedirectUri))
                return Results.BadRequest("Google OAuth redirect URIs are not configured.");

            var frontendRedirectUri = ResolveFrontendRedirectUri(opts.FrontendRedirectUri, context);

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return Results.Redirect(frontendRedirectUri + "?error=missing_code_or_state");

            var storedNonce = await stateStore.GetAndRemoveStateAsync(state, ct);
            if (string.IsNullOrEmpty(storedNonce))
                return Results.Redirect(frontendRedirectUri + "?error=invalid_state");

            var idToken = await googleAuth.ExchangeCodeForIdTokenAsync(code, opts.RedirectUri, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(idToken))
                return Results.Redirect(frontendRedirectUri + "?error=token_exchange_failed");

            try
            {
                await userService.RegisterOrUpdateFromGoogleAsync(idToken, ct).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Redirect(frontendRedirectUri + "?error=unauthorized");
            }
            catch (UserExpelledException)
            {
                return Results.Redirect(frontendRedirectUri + "?error=expelled");
            }

            // Professional flow: do not send tokens to the browser. Generate one-time code and let frontend exchange server-to-server.
            var exchangeCode = Guid.NewGuid().ToString("N");
            await authCodeStore.StoreAsync(exchangeCode, idToken, TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);
            return Results.Redirect(frontendRedirectUri + "?code=" + Uri.EscapeDataString(exchangeCode));
        });

        group.MapPost("/google", async ([FromBody] GoogleLoginRequest request, IUserApplicationService userService, CancellationToken ct) =>
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
            catch (UserExpelledException)
            {
                return Results.Json(new { error = "Account has been expelled." }, statusCode: 403);
            }
        });

        group.MapPost("/exchange", async ([FromBody] ExchangeRequest request, IAuthCodeStore authCodeStore, IUserApplicationService userService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return Results.BadRequest("Code is required.");

            var idToken = await authCodeStore.ConsumeAsync(request.Code.Trim(), ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(idToken))
                return Results.Unauthorized();

            var user = await userService.GetByGoogleIdTokenAsync(idToken, ct).ConfigureAwait(false);
            return user is null ? Results.Unauthorized() : Results.Ok(new ExchangeResponse(user, idToken));
        });
    }

    internal sealed record GoogleLoginRequest(string? IdToken);
    internal sealed record ExchangeRequest(string? Code);
    internal sealed record ExchangeResponse(object User, string IdToken);

    private static string ResolveFrontendRedirectUri(string configured, HttpContext context)
    {
        var fallback = $"{context.Request.Scheme}://{context.Request.Host}/auth/callback";

        if (string.IsNullOrWhiteSpace(configured))
            return fallback;

        // Guard rail: misconfiguration that causes redirect loops via gateway route.
        // If someone points FrontendRedirectUri to the IAM callback, redirect to the frontend callback on same host instead.
        if (configured.Contains("/auth/google/callback", StringComparison.OrdinalIgnoreCase))
            return fallback;

        return configured;
    }
}
