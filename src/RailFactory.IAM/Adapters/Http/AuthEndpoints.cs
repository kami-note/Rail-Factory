using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using RailFactory.IAM.Application.Auth;
using RailFactory.IAM.Domain.User;
using RailFactory.IAM.Infrastructure.Auth;
using RailFactory.IAM.Ports.Auth;
using RailFactory.IAM.Ports.Persistence;

namespace RailFactory.IAM.Adapters.Http;

/// <summary>
/// OAuth2 Google challenge and callback; issues JWT with tenant and roles (RF-IA-01, RF-IA-02).
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/google", (IConfiguration configuration) =>
        {
            if (string.IsNullOrWhiteSpace(configuration["Google:ClientId"]))
                return Results.Json(new { error = "Google OAuth is not configured. Set Google:ClientId and Google:ClientSecret in appsettings or environment." }, statusCode: 503);
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/auth/google/callback" },
                [GoogleDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        app.MapGet("/auth/google/callback", async (
            HttpContext context,
            [FromServices] LoginOrRegisterHandler loginHandler,
            [FromServices] IJwtIssuer jwtIssuer,
            [FromServices] IUserRepository userRepository,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(configuration["Google:ClientId"]))
                return Results.Json(new { error = "Google OAuth is not configured." }, statusCode: 503);
            var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme).ConfigureAwait(false);
            if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var externalId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? result.Principal.FindFirstValue("sub") ?? "";
            var email = result.Principal.FindFirstValue(ClaimTypes.Email)
                ?? result.Principal.FindFirstValue("email") ?? "";
            var name = result.Principal.FindFirstValue(ClaimTypes.Name)
                ?? result.Principal.FindFirstValue("name");

            var command = new LoginOrRegisterCommand { ExternalId = externalId, Email = email?.Trim() ?? "", Name = name?.Trim() };
            var validationErrors = LoginOrRegisterCommandValidator.Validate(command);
            if (validationErrors.Count > 0)
                return Results.Json(new { error = "Invalid login data", details = validationErrors }, statusCode: 400);

            var loginResult = await loginHandler.HandleAsync(command, cancellationToken).ConfigureAwait(false);
            if (loginResult is null)
                return Results.Problem("Login or registration failed.", statusCode: 500);

            // If user has no tenant, assign default tenant (dev convenience; admin can manage via RF-IA-06)
            var tenantRoles = loginResult.TenantRoles;
            if (tenantRoles.Count == 0)
            {
                var defaultTenantId = configuration["IAM:DefaultTenantIdForNewUsers"]?.Trim();
                if (!string.IsNullOrEmpty(defaultTenantId))
                {
                    var utr = new UserTenantRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = loginResult.User.Id,
                        TenantId = defaultTenantId,
                        Role = Role.Operator,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await userRepository.AddTenantRoleAsync(utr, cancellationToken).ConfigureAwait(false);
                    tenantRoles = [utr];
                }
            }
            var roles = tenantRoles.Select(r => r.Role.ToString()).Distinct().ToList();
            string? tenantId = null;
            IReadOnlyList<string>? tenantIds = null;
            if (tenantRoles.Count == 1)
                tenantId = tenantRoles[0].TenantId;
            else if (tenantRoles.Count > 1)
                tenantIds = tenantRoles.Select(r => r.TenantId).Distinct().ToList();

            var token = jwtIssuer.IssueToken(new JwtIssueRequest
            {
                Subject = loginResult.User.Id.ToString(),
                Email = loginResult.User.Email,
                Name = loginResult.User.DisplayName,
                TenantId = tenantId,
                TenantIds = tenantIds,
                Roles = roles
            });

            await context.SignOutAsync(GoogleDefaults.AuthenticationScheme).ConfigureAwait(false);

            return Results.Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in = 3600,
                tenant_id = tenantId ?? (tenantIds?.Count > 0 ? tenantIds[0] : null),
                tenant_ids = tenantIds,
                roles
            });
        }).AllowAnonymous();

        return app;
    }
}
