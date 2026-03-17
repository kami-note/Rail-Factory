using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Infrastructure.Persistence;

namespace RailFactory.Iam.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IamDbContext _dbContext;

    public UnitOfWork(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
