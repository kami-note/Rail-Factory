using RailFactory.IAM.Domain.Outbox;
using RailFactory.IAM.Infrastructure.Persistence;
using RailFactory.IAM.Ports.Outbox;

namespace RailFactory.IAM.Infrastructure.Outbox;

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
