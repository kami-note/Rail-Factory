using RailFactory.IAM.Domain;

namespace RailFactory.IAM.Ports;

/// <summary>
/// Port for user persistence. IAM uses a single identity DB (not per-tenant) for users and user-tenant-role.
/// Use Add/Update + IIdentityUnitOfWork.SaveChangesAsync() for create/update in one transaction with audit and outbox.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserTenantRole>> GetTenantRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Adds user to the current unit of work. Call IIdentityUnitOfWork.SaveChangesAsync() to persist.</summary>
    void Add(User user);
    /// <summary>Attaches updated user to the current unit of work. Call IIdentityUnitOfWork.SaveChangesAsync() to persist.</summary>
    void Update(User user);
    Task<UserTenantRole?> AddTenantRoleAsync(UserTenantRole utr, CancellationToken cancellationToken = default);
    Task<bool> RemoveTenantRoleAsync(Guid userTenantRoleId, CancellationToken cancellationToken = default);
}
