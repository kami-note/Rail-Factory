using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RailFactory.Frontend.Security;

/// <summary>
/// Maps auth minimal API endpoints: logout, OAuth callback, and Google OAuth proxy (Frontend → Gateway).
/// Call after UseAuthorization(); use WithOrder(-1) so these run before fallback.
/// </summary>
public static class AuthEndpointExtensions
{
    /// <summary>
    /// Maps POST /logout, GET /auth/callback, GET /auth/google, GET /auth/google/callback.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(AuthConstants.LogoutPath, (Delegate)LogoutAsync).RequireAuthorization().WithOrder(-1);
        endpoints.MapGet(AuthConstants.CallbackPath, HandleOAuthCallbackAsync).AllowAnonymous().WithOrder(-1);
        endpoints.MapGet(AuthConstants.GoogleStartPath, (Delegate)((HttpContext context, GoogleAuthProxyHandler proxy, CancellationToken ct) =>
            proxy.HandleGoogleStartAsync(context, ct))).AllowAnonymous().WithOrder(-1);
        endpoints.MapGet(AuthConstants.GoogleCallbackPath, (Delegate)(async (HttpContext context, GoogleAuthProxyHandler proxy, CancellationToken ct) =>
        {
            var result = await proxy.HandleGoogleCallbackAsync(context, ct).ConfigureAwait(false);
            return result ?? Results.Empty;
        })).AllowAnonymous().WithOrder(-1);

        return endpoints;
    }

    private static async Task<IResult> LogoutAsync(HttpContext context)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Invalid antiforgery token.");
        }

        try
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            context.Response.Redirect(AuthConstants.LoginPath);
            return Results.Empty;
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Auth.Endpoints");
            logger.LogError(ex, "Logout failed.");
            return Results.Problem("Logout failed.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task HandleOAuthCallbackAsync(HttpContext context, IAuthService auth)
    {
        var error = context.Request.Query[AuthConstants.ErrorQueryKey].ToString();
        if (!string.IsNullOrWhiteSpace(error))
        {
            context.Response.Redirect(LoginUrl(error));
            return;
        }

        var code = context.Request.Query["code"].ToString();
        if (string.IsNullOrWhiteSpace(code))
        {
            context.Response.Redirect(LoginUrl(AuthConstants.ErrorMissingCode));
            return;
        }

        var state = context.Request.Query["state"].ToString();
        if (!await auth.ValidateStateAsync(context, state, context.RequestAborted).ConfigureAwait(false))
        {
            context.Response.Redirect(LoginUrl(AuthConstants.ErrorInvalidState));
            return;
        }

        var result = await auth.ExchangeGoogleAuthCodeAsync(code, context.RequestAborted).ConfigureAwait(false);
        if (!result.Success || result.Principal is null)
        {
            context.Response.Redirect(LoginUrl(result.Error ?? AuthConstants.ErrorSignInFailed));
            return;
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            result.Principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            }).ConfigureAwait(false);

        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        if (!IsLocalUrl(returnUrl))
            returnUrl = "/";
        context.Response.Redirect(returnUrl);
    }

    private static string LoginUrl(string error) =>
        AuthConstants.LoginPath + "?error=" + Uri.EscapeDataString(error);

    private static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url[0] != '/')
            return false;
        return url.Length <= 1 || (url[1] != '/' && url[1] != '\\');
    }
}
