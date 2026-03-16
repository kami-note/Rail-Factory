namespace RailFactory.Shared.Messaging.Events;

/// <summary>
/// Published when a user is first registered (IAM). Use event_id for idempotent consumption.
/// </summary>
public sealed class UserCreated
{
    public Guid EventId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? ExternalId { get; init; }
}
