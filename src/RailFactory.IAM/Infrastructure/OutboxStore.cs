using RailFactory.IAM.Domain;
using RailFactory.IAM.Ports;

namespace RailFactory.IAM.Infrastructure;

/// <summary>
/// Appends outbox messages to the current DbContext. Persistence happens when IIdentityUnitOfWork.SaveChangesAsync() is called.
/// </summary>
public sealed class OutboxStore : IOutboxStore
{
    private readonly IamDbContext _db;

    public OutboxStore(IamDbContext db)
    {
        _db = db;
    }

    public void Append(OutboxMessage message)
    {
        _db.OutboxMessages.Add(message);
    }
}
