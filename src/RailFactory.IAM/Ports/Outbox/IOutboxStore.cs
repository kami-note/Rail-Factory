using RailFactory.IAM.Domain.Outbox;

namespace RailFactory.IAM.Ports.Outbox;

/// <summary>
/// Port for appending outbox messages in the same transaction as domain writes (doc 03 §10). Does not commit; caller uses IIdentityUnitOfWork.SaveChangesAsync().
/// </summary>
public interface IOutboxStore
{
    /// <summary>Adds the message to the current unit of work. Call IIdentityUnitOfWork.SaveChangesAsync() to persist.</summary>
    void Append(OutboxMessage message);
}
