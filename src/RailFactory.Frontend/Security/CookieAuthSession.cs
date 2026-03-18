using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace RailFactory.Frontend.Security;

public sealed class CookieAuthSession(IHttpContextAccessor httpContextAccessor) : IAuthSession
{
    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");

    public Task SignInAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    public Task SignOutAsync(CancellationToken cancellationToken)
    {
        return HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}

