namespace RailFactory.Iam.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox table entity for transactional outbox pattern. Same transaction as domain changes.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
