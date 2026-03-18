using System.Security.Claims;

namespace RailFactory.Frontend.Security;

public sealed record LoginRequest(string Email, string Password)
{
    public override string ToString() => $"LoginRequest {{ Email = {Email} }}";
}

public sealed record RegisterRequest(string Email, string Password, string DisplayName)
{
    public override string ToString() => $"RegisterRequest {{ Email = {Email}, DisplayName = {DisplayName} }}";
}

public sealed record AuthResult(bool Success, string? Error, ClaimsPrincipal? Principal)
{
    public static AuthResult Ok(ClaimsPrincipal principal) => new(true, null, principal);
    public static AuthResult Fail(string error) => new(false, error, null);
}

