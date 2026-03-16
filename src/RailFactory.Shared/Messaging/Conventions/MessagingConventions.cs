namespace RailFactory.Shared.Messaging;

/// <summary>
/// Exchange and queue naming conventions for RabbitMQ (MassTransit). One consistent convention across all services.
/// </summary>
public static class MessagingConventions
{
    public const string ExchangePrefix = "rail-factory";
    public const string QueuePrefix = "rail-factory";

    /// <summary>
    /// Format: rail-factory:{service-name}. Example: rail-factory:production.
    /// </summary>
    public static string ExchangeName(string serviceName) => $"{ExchangePrefix}:{serviceName}";

    /// <summary>
    /// Format: rail-factory:{service-name}:{queue}. Example: rail-factory:production:orders.
    /// </summary>
    public static string QueueName(string serviceName, string queue) => $"{QueuePrefix}:{serviceName}:{queue}";
}
