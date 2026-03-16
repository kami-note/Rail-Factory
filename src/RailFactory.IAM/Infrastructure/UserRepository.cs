using Microsoft.EntityFrameworkCore;
using RailFactory.IAM.Domain;
using RailFactory.IAM.Ports;

namespace RailFactory.IAM.Infrastructure;

/// <summary>
/// EF Core implementation of IUserRepository for the IAM identity database.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IamDbContext _db;

    public UserRepository(IamDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        return await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserTenantRole>> GetTenantRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserTenantRoles.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.TenantId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Add(User user)
    {
        _db.Users.Add(user);
    }

    public void Update(User user)
    {
        _db.Users.Update(user);
    }

    public async Task<UserTenantRole?> AddTenantRoleAsync(UserTenantRole utr, CancellationToken cancellationToken = default)
    {
        _db.UserTenantRoles.Add(utr);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return utr;
    }

    public async Task<bool> RemoveTenantRoleAsync(Guid userTenantRoleId, CancellationToken cancellationToken = default)
    {
        var row = await _db.UserTenantRoles.FindAsync([userTenantRoleId], cancellationToken).ConfigureAwait(false);
        if (row is null) return false;
        _db.UserTenantRoles.Remove(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
