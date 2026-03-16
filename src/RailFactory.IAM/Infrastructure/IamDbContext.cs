using Microsoft.EntityFrameworkCore;
using RailFactory.IAM.Domain;

namespace RailFactory.IAM.Infrastructure;

/// <summary>
/// EF Core DbContext for IAM identity store (users, user_tenant_roles, audit).
/// Uses a single connection (ConnectionStrings:Identity) — not per-tenant; see docs/13_Tenant_Resolution_Security.md.
/// </summary>
public sealed class IamDbContext : DbContext
{
    public const string SchemaName = "iam_schema";

    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserTenantRole> UserTenantRoles => Set<UserTenantRole>();
    public DbSet<UserAuditEntry> UserAuditEntries => Set<UserAuditEntry>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.Property(x => x.ExternalId).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        });

        modelBuilder.Entity<UserTenantRole>(e =>
        {
            e.ToTable("user_tenant_roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).IsRequired().HasMaxLength(64);
            e.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        });

        modelBuilder.Entity<UserAuditEntry>(e =>
        {
            e.ToTable("user_audit_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).IsRequired().HasMaxLength(64);
            e.Property(x => x.PerformedByUserId).HasMaxLength(64);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => new { x.Published, x.CreatedAtUtc });
        });
    }
}
