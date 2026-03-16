namespace RailFactory.Shared.Messaging.Events;

/// <summary>
/// Published when a user's profile is updated (IAM). Use event_id for idempotent consumption.
/// </summary>
public sealed class UserUpdated
{
    public Guid EventId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = "";
    public string? DisplayName { get; init; }
    public string? ExternalId { get; init; }
}
