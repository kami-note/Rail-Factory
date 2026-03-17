using RailFactory.Iam.Domain.Entities;

namespace RailFactory.Iam.Application.Ports;

/// <summary>
/// Port for persisting and retrieving the User aggregate.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    void Add(User user);
}
