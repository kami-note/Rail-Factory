using RailFactory.IAM.Ports.Persistence;

namespace RailFactory.IAM.Infrastructure.Persistence;

/// <summary>
/// Unit of work that commits all pending changes on IamDbContext in one transaction.
/// </summary>
public sealed class IdentityUnitOfWork : IIdentityUnitOfWork
{
    private readonly IamDbContext _db;

    public IdentityUnitOfWork(IamDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
