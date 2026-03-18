using Microsoft.AspNetCore.Http;

namespace RailFactory.Frontend.Security;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<bool> ValidateStateAsync(HttpContext context, string? state, CancellationToken cancellationToken);
    Task<AuthResult> ExchangeGoogleAuthCodeAsync(string code, CancellationToken cancellationToken);
}

