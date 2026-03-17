using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RailFactory.Iam.Application.Services;

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

        group.MapPut("/{id:guid}/profile", async (Guid id, UpdateProfileRequest request, IUserApplicationService userService, CancellationToken ct) =>
        {
            try
            {
                var user = await userService.UpdateProfileAsync(id, request.DisplayName, request.PictureUrl, ct).ConfigureAwait(false);
                return Results.Ok(user);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
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
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return Results.NotFound();
            }
        });
    }

    internal sealed record UpdateProfileRequest(string? DisplayName, string? PictureUrl);
}
