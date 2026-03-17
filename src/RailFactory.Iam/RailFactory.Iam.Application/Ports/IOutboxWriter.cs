namespace RailFactory.Iam.Application.Ports;

/// <summary>
/// Port for writing outbox messages in the same transaction as domain changes.
/// Ensures at-least-once delivery when a relay publishes to the message broker.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Enqueues a message to be published. Must be committed in the same transaction as domain persistence.
    /// </summary>
    /// <param name="messageType">Event type name (e.g. "UserCreated").</param>
    /// <param name="payload">Serialized payload (e.g. JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(string messageType, string payload, CancellationToken cancellationToken = default);
}
