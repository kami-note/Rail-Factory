namespace RailFactory.IAM.Domain.Outbox;

/// <summary>
/// Outbox row for reliable event publishing (doc 03 §10). Written in same transaction as aggregate; worker publishes to RabbitMQ then sets Published = true.
/// </summary>
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    /// <summary>Idempotency key for consumers.</summary>
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public bool Published { get; set; }
    public DateTime CreatedAtUtc { get; init; }
}
