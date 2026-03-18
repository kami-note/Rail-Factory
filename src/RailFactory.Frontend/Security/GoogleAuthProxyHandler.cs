using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using RailFactory.Frontend.Options;

namespace RailFactory.Frontend.Security;

/// <summary>
/// Proxies Google OAuth start and callback to the Gateway so the browser only talks to the Frontend (single ngrok tunnel).
/// Forwards Set-Cookie on start and Cookie on callback so IAM state validation works.
/// </summary>
public sealed class GoogleAuthProxyHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleAuthProxyHandler> _logger;

    public GoogleAuthProxyHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleAuthProxyHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Proxies GET /auth/google to Gateway. Forwards Set-Cookie so the browser gets rf_login_state for callback validation.
    /// </summary>
    public async Task<IResult> HandleGoogleStartAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(GatewayServiceCollectionExtensions.GatewayHttpClientName);
            using var response = await client.GetAsync(AuthConstants.GoogleStartPath, cancellationToken).ConfigureAwait(false);

            if (response.Headers.Location is not { } location)
            {
                _logger.LogWarning("Gateway {Path} returned status {StatusCode} without Location header.", AuthConstants.GoogleStartPath, response.StatusCode);
                return RedirectToLoginWithError(AuthConstants.ErrorSignInUnavailable);
            }

            ForwardSetCookieToResponse(response, context);
            return Results.Redirect(location.ToString(), permanent: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway request failed for {Path}.", AuthConstants.GoogleStartPath);
            return RedirectToLoginWithError(AuthConstants.ErrorSignInUnavailable);
        }
    }

    /// <summary>
    /// Proxies GET /auth/google/callback to Gateway. Forwards browser Cookie, rewrites redirect Location to request host.
    /// Returns null when the response was written directly to <paramref name="context"/>.
    /// </summary>
    public async Task<IResult?> HandleGoogleCallbackAsync(HttpContext context, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(GatewayServiceCollectionExtensions.GatewayHttpClientName);
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
            using var request = new HttpRequestMessage(HttpMethod.Get, AuthConstants.GoogleCallbackPath + query);
            if (context.Request.Headers.Cookie is { } cookieHeader && !string.IsNullOrEmpty(cookieHeader))
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader.ToString());

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await CopyProxyResponseAsync(context, response, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway request failed for {Path}.", AuthConstants.GoogleCallbackPath);
            return RedirectToLoginWithError(AuthConstants.ErrorSignInUnavailable);
        }
    }

    private static void ForwardSetCookieToResponse(HttpResponseMessage gatewayResponse, HttpContext context)
    {
        if (!gatewayResponse.Headers.TryGetValues("Set-Cookie", out var setCookies))
            return;
        foreach (var value in setCookies)
            context.Response.Headers.Append("Set-Cookie", value);
    }

    private static async Task CopyProxyResponseAsync(HttpContext context, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        CopyHeaders(context.Response.Headers, response.Headers);
        CopyHeaders(context.Response.Headers, response.Content.Headers);
        context.Response.StatusCode = (int)response.StatusCode;

        if (IsRedirect(response) && response.Headers.Location is { } location)
        {
            var pathAndQuery = location.PathAndQuery;
            if (!pathAndQuery.StartsWith('/'))
                pathAndQuery = "/" + pathAndQuery;
            context.Response.Headers["Location"] = $"{context.Request.Scheme}://{context.Request.Host}" + pathAndQuery;
        }

        await response.Content.CopyToAsync(context.Response.Body, cancellationToken).ConfigureAwait(false);
    }

    private static void CopyHeaders(IHeaderDictionary target, HttpHeaders source)
    {
        foreach (var header in source)
        {
            if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                continue;
            target[header.Key] = header.Value.ToArray();
        }
    }

    private static bool IsRedirect(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently or (HttpStatusCode)307 or (HttpStatusCode)308;

    private static IResult RedirectToLoginWithError(string error) =>
        Results.Redirect(AuthConstants.LoginPath + "?error=" + Uri.EscapeDataString(error), permanent: false);
}
