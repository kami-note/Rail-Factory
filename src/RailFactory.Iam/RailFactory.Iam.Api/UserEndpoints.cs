using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RailFactory.Iam.Application.Services;
using RailFactory.Iam.Domain.Exceptions;

namespace RailFactory.Iam.Api;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("/me", async (HttpContext context, IUserApplicationService userService, CancellationToken ct) =>
        {
            var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (string.IsNullOrEmpty(token))
                return Results.Unauthorized();
            
            var user = await userService.GetByGoogleIdTokenAsync(token, ct).ConfigureAwait(false);
            return user is null ? Results.Unauthorized() : Results.Json(user);
        });

        group.MapPut("/{id:guid}/profile", async (Guid id, HttpContext context, UpdateProfileRequest request, IUserApplicationService userService, CancellationToken ct) =>
        {
            var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (string.IsNullOrEmpty(token))
                return Results.Unauthorized();
            
            var caller = await userService.GetByGoogleIdTokenAsync(token, ct).ConfigureAwait(false);
            if (caller == null)
                return Results.Unauthorized();
            
            // IDOR check: users can only update their own profile
            if (caller.Id != id)
                return Results.Forbid();

            try
            {
                var user = await userService.UpdateProfileAsync(id, request.DisplayName, request.PictureUrl, ct).ConfigureAwait(false);
                return Results.Ok(user);
            }
            catch (UserNotFoundException)
            {
                return Results.NotFound();
            }
        });

        group.MapPost("/{id:guid}/expel", async (Guid id, IUserApplicationService userService, CancellationToken ct) =>
        {
            try
            {
                await userService.ExpelAsync(id, ct).ConfigureAwait(false);
                return Results.NoContent();
            }
            catch (UserNotFoundException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization("AdminOnly");
    }

    internal sealed record UpdateProfileRequest(string? DisplayName, string? PictureUrl);
}
