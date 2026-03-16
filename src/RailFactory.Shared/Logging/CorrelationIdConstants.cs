namespace RailFactory.Shared.Logging;

/// <summary>
/// Header and property names for request correlation. Use in middleware and structured logging.
/// </summary>
public static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";
}
