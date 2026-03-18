using System.Security.Claims;

namespace RailFactory.Frontend.Security;

public interface IAuthSession
{
    Task SignInAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task SignOutAsync(CancellationToken cancellationToken);
}

