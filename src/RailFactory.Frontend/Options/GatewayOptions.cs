namespace RailFactory.Frontend.Options;

/// <summary>
/// Centralized configuration for the Gateway base URL (Frontend → Gateway → microservices).
/// Resolves from Aspire service discovery keys and environment variables.
/// </summary>
public sealed class GatewayOptions
{
    public const string SectionName = "Gateway";

    /// <summary>
    /// Gateway base URL (e.g. https://gateway:port). Must be set by configuration.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Resolves the Gateway base URL from configuration.
    /// Throws <see cref="InvalidOperationException"/> if no value is configured.
    /// </summary>
    public static string GetBaseUrl(IConfiguration configuration)
    {
        var baseUrl =
            configuration["services:gateway:https:0"]
            ?? configuration["services:gateway:http:0"]
            ?? configuration["services__gateway__https__0"]
            ?? configuration["services__gateway__http__0"]
            ?? configuration["GATEWAY_HTTPS"]
            ?? configuration["GATEWAY_HTTP"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Gateway base URL not configured.");

        return baseUrl;
    }
}
