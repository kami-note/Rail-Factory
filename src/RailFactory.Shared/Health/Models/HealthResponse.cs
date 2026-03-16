namespace RailFactory.Shared.Health;

/// <summary>
/// Standard health response format (RNF-07). Each service exposes API, DB, and broker (if used) status.
/// </summary>
public sealed class HealthResponse
{
    public required string Status { get; init; }
    public required IReadOnlyDictionary<string, string> Checks { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
