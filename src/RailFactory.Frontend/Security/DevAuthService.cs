using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace RailFactory.Frontend.Security;

public sealed class DevAuthService : IAuthService
{
    private static readonly ConcurrentDictionary<string, UserRecord> Users = new(StringComparer.OrdinalIgnoreCase);
    private static readonly PasswordHasher<UserRecord> Hasher = new();

    public Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = NormalizeEmail(request.Email);
        if (email is null) return Task.FromResult(AuthResult.Fail("Invalid email."));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return Task.FromResult(AuthResult.Fail("Password must be at least 6 characters."));

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim();

        var record = new UserRecord(email, displayName, "");
        record = record with { PasswordHash = Hasher.HashPassword(record, request.Password) };

        if (!Users.TryAdd(email, record))
            return Task.FromResult(AuthResult.Fail("User already exists."));

        return Task.FromResult(AuthResult.Ok(ToPrincipal(record)));
    }

    public Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = NormalizeEmail(request.Email);
        if (email is null) return Task.FromResult(AuthResult.Fail("Invalid email or password."));

        if (!Users.TryGetValue(email, out var record))
            return Task.FromResult(AuthResult.Fail("Invalid email or password."));

        var verify = Hasher.VerifyHashedPassword(record, record.PasswordHash, request.Password);
        if (verify is PasswordVerificationResult.Failed)
            return Task.FromResult(AuthResult.Fail("Invalid email or password."));

        return Task.FromResult(AuthResult.Ok(ToPrincipal(record)));
    }

    private static ClaimsPrincipal ToPrincipal(UserRecord record)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, record.Email),
            new(ClaimTypes.Name, record.DisplayName),
            new(ClaimTypes.Email, record.Email),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static string? NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var trimmed = email.Trim();
        if (!trimmed.Contains('@')) return null;
        return trimmed.ToLowerInvariant();
    }

    private sealed record UserRecord(string Email, string DisplayName, string PasswordHash);
}

