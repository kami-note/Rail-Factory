using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Application.Ports;
using RailFactory.Iam.Domain.Entities;
using RailFactory.Iam.Infrastructure.Persistence;

namespace RailFactory.Iam.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IamDbContext _dbContext;

    public UserRepository(IamDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

    public void Add(User user) => _dbContext.Users.Add(user);
}
