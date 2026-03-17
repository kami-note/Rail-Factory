using Microsoft.EntityFrameworkCore;
using RailFactory.Iam.Domain.Entities;
using RailFactory.Iam.Infrastructure.Persistence.Outbox;

namespace RailFactory.Iam.Infrastructure.Persistence;

public sealed class IamDbContext : DbContext
{
    public IamDbContext(DbContextOptions<IamDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IamDbContext).Assembly);
    }
}
