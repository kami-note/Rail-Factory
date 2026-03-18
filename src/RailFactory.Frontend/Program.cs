using RailFactory.Frontend.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using RailFactory.Frontend.Security;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IAuthService, IamAuthService>();
builder.Services.AddScoped<IAuthSession, CookieAuthSession>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};

var trustAllForwardedHeaders = builder.Configuration.GetValue<bool>("RAILFACTORY_TRUST_ALL_FORWARDED_HEADERS");
if (trustAllForwardedHeaders)
{
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
else
{
    var trustedProxies = builder.Configuration["RAILFACTORY_TRUSTED_PROXIES"];
    if (!string.IsNullOrWhiteSpace(trustedProxies))
    {
        foreach (var entry in trustedProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPAddress.TryParse(entry, out var ip))
                forwardedHeadersOptions.KnownProxies.Add(ip);
        }
    }

    var trustedNetworks = builder.Configuration["RAILFACTORY_TRUSTED_NETWORKS"];
    if (!string.IsNullOrWhiteSpace(trustedNetworks))
    {
        foreach (var entry in trustedNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            forwardedHeadersOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(entry));
        }
    }
}
app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapPost("/logout", async (HttpContext context) =>
{
    var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

    try
    {
        await antiforgery.ValidateRequestAsync(context).ConfigureAwait(false);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid antiforgery token.");
    }

    try
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        context.Response.Redirect("/login");
        return Results.Empty;
    }
    catch
    {
        return Results.Problem("Logout failed.", statusCode: StatusCodes.Status500InternalServerError);
    }
}).RequireAuthorization().WithOrder(-1);

app.MapGet("/auth/callback", async (HttpContext context, IAuthService auth) =>
{
    // Handle IAM redirect in a plain HTTP request so we can set cookies (Blazor Server circuits can't).
    var error = context.Request.Query["error"].ToString();
    if (!string.IsNullOrWhiteSpace(error))
    {
        context.Response.Redirect("/login?error=" + Uri.EscapeDataString(error));
        return;
    }

    var code = context.Request.Query["code"].ToString();
    if (string.IsNullOrWhiteSpace(code))
    {
        context.Response.Redirect("/login?error=" + Uri.EscapeDataString("missing_code"));
        return;
    }

    var state = context.Request.Query["state"].ToString();
    var stateOk = await auth.ValidateStateAsync(context, state, context.RequestAborted).ConfigureAwait(false);
    if (!stateOk)
    {
        context.Response.Redirect("/login?error=" + Uri.EscapeDataString("invalid_state"));
        return;
    }

    var result = await auth.ExchangeGoogleAuthCodeAsync(code, context.RequestAborted).ConfigureAwait(false);
    if (!result.Success || result.Principal is null)
    {
        context.Response.Redirect("/login?error=" + Uri.EscapeDataString(result.Error ?? "sign_in_failed"));
        return;
    }

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        result.Principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        }).ConfigureAwait(false);

    var returnUrl = context.Request.Query["ReturnUrl"].ToString();
    if (!IsLocalUrl(returnUrl))
        returnUrl = "/";
    context.Response.Redirect(returnUrl);
}).AllowAnonymous().WithOrder(-1);

static bool IsLocalUrl(string? url)
{
    if (string.IsNullOrWhiteSpace(url))
        return false;

    if (url[0] != '/')
        return false;

    // "/" is local; "//" and "/\" are not.
    if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
        return false;

    return true;
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
