using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using RailFactory.Frontend.Components;
using RailFactory.Frontend.Options;
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
        options.LoginPath = AuthConstants.LoginPath;
        options.LogoutPath = AuthConstants.LogoutPath;
        options.AccessDeniedPath = AuthConstants.LoginPath;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IAuthService, IamAuthService>();
builder.Services.AddScoped<IAuthSession, CookieAuthSession>();
builder.Services.AddScoped<GoogleAuthProxyHandler>();

builder.Services.AddGatewayHttpClient(builder.Configuration);

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

app.MapAuthEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
