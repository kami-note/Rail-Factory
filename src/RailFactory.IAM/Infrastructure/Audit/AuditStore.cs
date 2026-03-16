using RailFactory.IAM.Domain.Audit;
using RailFactory.IAM.Infrastructure.Persistence;
using RailFactory.IAM.Ports.Audit;

namespace RailFactory.IAM.Infrastructure.Audit;

/// <summary>
/// EF Core implementation of IAuditStore (RF-IA-05).
/// </summary>
public sealed class AuditStore : IAuditStore
{
    private readonly IamDbContext _db;

    public AuditStore(IamDbContext db)
    {
        _db = db;
    }

    /// <summary>Adds the audit entry to the current unit of work. Call IIdentityUnitOfWork.SaveChangesAsync() to persist.</summary>
    public Task AppendAsync(UserAuditEntry entry, CancellationToken cancellationToken = default)
    {
        _db.UserAuditEntries.Add(entry);
        return Task.CompletedTask;
    }
}
