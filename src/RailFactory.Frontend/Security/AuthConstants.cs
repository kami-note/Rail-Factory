namespace RailFactory.Frontend.Security;

/// <summary>
/// Centralized auth-related paths and error keys for the Frontend (login, OAuth proxy, Gateway paths).
/// </summary>
public static class AuthConstants
{
    /// <summary>Path for the login page and redirect base for errors.</summary>
    public const string LoginPath = "/login";

    /// <summary>Path for logout (POST).</summary>
    public const string LogoutPath = "/logout";

    /// <summary>Path where the Frontend receives the OAuth callback (code + state) from IAM.</summary>
    public const string CallbackPath = "/auth/callback";

    /// <summary>Path proxied to Gateway to start Google sign-in.</summary>
    public const string GoogleStartPath = "/auth/google";

    /// <summary>Path proxied to Gateway for Google OAuth callback.</summary>
    public const string GoogleCallbackPath = "/auth/google/callback";

    /// <summary>Gateway path for IAM auth code exchange (Frontend calls this after callback).</summary>
    public const string IamExchangePath = "/auth/iam/exchange";

    /// <summary>Cookie name used by IAM for frontend callback state (must match IAM).</summary>
    public const string CallbackStateCookieName = "rf_login_state";

    /// <summary>Error key when sign-in is unavailable (Gateway/network error).</summary>
    public const string ErrorSignInUnavailable = "sign_in_unavailable";

    /// <summary>Error key when state validation fails.</summary>
    public const string ErrorInvalidState = "invalid_state";

    /// <summary>Error key when code is missing.</summary>
    public const string ErrorMissingCode = "missing_code";

    /// <summary>Error key when exchange or sign-in fails.</summary>
    public const string ErrorSignInFailed = "sign_in_failed";

    /// <summary>Query string key for error on login page.</summary>
    public const string ErrorQueryKey = "error";
}
