using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailFactory.Iam.Application;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Application.Services;
using RailFactory.Iam.Infrastructure;
using RailFactory.Iam.Infrastructure.Google;
using RailFactory.Iam.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddIamApplication();
builder.Services.AddIamInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<IamDbContext>("iamdb", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Rail Factory IAM API",
        Version = "v1",
        Description = "Identity & Access Management microservice."
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "IAM API v1"));
}

// Apply pending migrations on startup so __EFMigrationsHistory and schema exist before health checks run.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    const int maxRetries = 5;
    for (var i = 0; i < maxRetries; i++)
    {
        try
        {
            await db.Database.MigrateAsync().ConfigureAwait(false);
            break;
        }
        catch (Exception ex)
        {
            if (i == maxRetries - 1)
                throw;
            logger.LogWarning(ex, "Migration attempt {Attempt}/{Max} failed, retrying in 2s...", i + 1, maxRetries);
            await Task.Delay(2000).ConfigureAwait(false);
        }
    }
}

app.MapGet("/api/users/me", async (HttpContext context, IUserApplicationService userService, CancellationToken ct) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();
    var user = await userService.GetByGoogleIdTokenAsync(token, ct).ConfigureAwait(false);
    return user is null ? Results.Unauthorized() : Results.Json(user);
});

app.MapPost("/api/auth/google", async (GoogleLoginRequest request, IUserApplicationService userService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.IdToken))
        return Results.BadRequest("IdToken is required.");
    try
    {
        var user = await userService.RegisterOrUpdateFromGoogleAsync(request.IdToken, ct).ConfigureAwait(false);
        return Results.Ok(user);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("expelled"))
    {
        return Results.Json(new { error = "Account has been expelled." }, statusCode: 403);
    }
});

// Redirect flow: GET /auth/google -> Google -> GET /auth/google/callback
app.MapGet("/auth/google", (HttpContext context, IGoogleAuthProvider googleAuth, IOptions<GoogleAuthOptions> googleOptions, IMemoryCache cache) =>
{
    var opts = googleOptions.Value;
    if (string.IsNullOrEmpty(opts.ClientId) || string.IsNullOrEmpty(opts.RedirectUri))
        return Results.BadRequest("Google OAuth is not configured (ClientId, RedirectUri).");

    var state = Guid.NewGuid().ToString("N");
    cache.Set("oauth_state:" + state, true, TimeSpan.FromMinutes(5));

    var url = googleAuth.BuildAuthorizationUrl(opts.RedirectUri, state);
    return Results.Redirect(url);
});

app.MapGet("/auth/google/callback", async (
    HttpContext context,
    string? code,
    string? state,
    IGoogleAuthProvider googleAuth,
    IUserApplicationService userService,
    IOptions<GoogleAuthOptions> googleOptions,
    IMemoryCache cache,
    CancellationToken ct) =>
{
    var opts = googleOptions.Value;
    if (string.IsNullOrEmpty(opts.RedirectUri) || string.IsNullOrEmpty(opts.FrontendRedirectUri))
        return Results.BadRequest("Google OAuth redirect URIs are not configured.");

    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        return Results.Redirect(opts.FrontendRedirectUri + "?error=missing_code_or_state");

    var stateKey = "oauth_state:" + state;
    if (cache.Get(stateKey) is null)
        return Results.Redirect(opts.FrontendRedirectUri + "?error=invalid_state");
    cache.Remove(stateKey);

    var idToken = await googleAuth.ExchangeCodeForIdTokenAsync(code, opts.RedirectUri, ct).ConfigureAwait(false);
    if (string.IsNullOrEmpty(idToken))
        return Results.Redirect(opts.FrontendRedirectUri + "?error=token_exchange_failed");

    try
    {
        await userService.RegisterOrUpdateFromGoogleAsync(idToken, ct).ConfigureAwait(false);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Redirect(opts.FrontendRedirectUri + "?error=unauthorized");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("expelled"))
    {
        return Results.Redirect(opts.FrontendRedirectUri + "?error=expelled");
    }

    var fragment = "id_token=" + Uri.EscapeDataString(idToken);
    return Results.Redirect(opts.FrontendRedirectUri + "#" + fragment);
});

app.MapPut("/api/users/{id:guid}/profile", async (Guid id, UpdateProfileRequest request, IUserApplicationService userService, CancellationToken ct) =>
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

app.MapPost("/api/users/{id:guid}/expel", async (Guid id, IUserApplicationService userService, CancellationToken ct) =>
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

app.Run();

// Request DTOs (API layer)
internal sealed record GoogleLoginRequest(string? IdToken);
internal sealed record UpdateProfileRequest(string? DisplayName, string? PictureUrl);
