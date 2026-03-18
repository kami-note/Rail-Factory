using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RailFactory.Frontend.Options;

/// <summary>
/// Centralizes Gateway URL resolution and HttpClient registration for Frontend → Gateway.
/// </summary>
public static class GatewayServiceCollectionExtensions
{
    /// <summary>Named HttpClient used for Frontend → Gateway (OAuth proxy and IAM exchange).</summary>
    public const string GatewayHttpClientName = "Gateway";

    /// <summary>
    /// Adds <see cref="GatewayOptions"/> bound from configuration and registers the named
    /// HttpClient "Gateway" with no auto-redirect (for OAuth proxy Location handling).
    /// </summary>
    public static IServiceCollection AddGatewayHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var baseUrl = GatewayOptions.GetBaseUrl(configuration);

        services.Configure<GatewayOptions>(opts => opts.BaseUrl = baseUrl);

        services.AddHttpClient(GatewayHttpClientName, (_, client) =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false // so we can forward Cookie header in OAuth callback proxy
        });

        return services;
    }
}
