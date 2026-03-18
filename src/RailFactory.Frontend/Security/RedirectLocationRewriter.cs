using System;

namespace RailFactory.Frontend.Security;

/// <summary>
/// Centralizes redirect Location rewrite rules for OAuth proxying.
/// If Location is absolute and targets a non-gateway host, preserve it (don't force same-origin).
/// </summary>
public static class RedirectLocationRewriter
{
    /// <summary>
    /// Returns a rewritten absolute Location value or <c>null</c> if no rewrite should occur.
    /// </summary>
    public static string? RewriteLocationIfNeeded(
        string requestScheme,
        string publicHost,
        string? gatewayHost,
        Uri location)
    {
        if (string.IsNullOrWhiteSpace(requestScheme))
            throw new ArgumentException("requestScheme must be non-empty.", nameof(requestScheme));
        if (string.IsNullOrWhiteSpace(publicHost))
            throw new ArgumentException("publicHost must be non-empty.", nameof(publicHost));
        if (location is null)
            throw new ArgumentNullException(nameof(location));

        // Relative => safe to rewrite to the current origin.
        if (!location.IsAbsoluteUri)
        {
            // Uri.PathAndQuery throws for relative URIs; use OriginalString instead.
            var pathAndQuery = location.OriginalString;
            if (!pathAndQuery.StartsWith('/'))
                pathAndQuery = "/" + pathAndQuery;
            return $"{requestScheme}://{publicHost}{pathAndQuery}";
        }

        // Absolute => rewrite only when the redirect is pointing at the gateway/internal host.
        // This prevents breaking external redirects (e.g. Google or other 3rd parties).
        if (!string.IsNullOrWhiteSpace(gatewayHost) &&
            string.Equals(location.Host, gatewayHost, StringComparison.OrdinalIgnoreCase))
        {
            return $"{requestScheme}://{publicHost}{location.PathAndQuery}";
        }

        return null;
    }
}

