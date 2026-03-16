namespace RailFactory.IAM.Ports.Persistence;

/// <summary>
/// Unit of work for IAM identity DB. Persists all pending changes (users, audit, outbox) in a single transaction.
/// </summary>
public interface IIdentityUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
