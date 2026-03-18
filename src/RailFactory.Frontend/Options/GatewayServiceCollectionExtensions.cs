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

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) || string.IsNullOrWhiteSpace(baseUri.Scheme) || string.IsNullOrWhiteSpace(baseUri.Host))
        {
            throw new InvalidOperationException(
                $"Gateway base URL is invalid. Expected an absolute URI (e.g. 'http://gateway:port'). Got '{baseUrl}'. " +
                "Check Aspire service discovery env vars for 'services:gateway:http:0'/'services:gateway:https:0' (or 'services__gateway__http__0'/'services__gateway__https__0').");
        }

        var normalizedBaseUrl = baseUri.ToString();

        services.Configure<GatewayOptions>(opts => opts.BaseUrl = normalizedBaseUrl);

        services.AddHttpClient(GatewayHttpClientName, (_, client) =>
        {
            client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false // so we can forward Cookie header in OAuth callback proxy
        });

        return services;
    }
}
