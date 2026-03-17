namespace RailFactory.Iam.Domain.Events;

/// <summary>
/// Marker for domain events that can be stored in the outbox and published.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
