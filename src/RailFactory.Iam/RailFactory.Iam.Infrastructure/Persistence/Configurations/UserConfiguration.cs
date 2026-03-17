using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RailFactory.Iam.Domain.Entities;

namespace RailFactory.Iam.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Email).HasMaxLength(512).IsRequired();
        builder.Property(e => e.ExternalId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(512);
        builder.Property(e => e.PictureUrl).HasMaxLength(2048);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.HasIndex(e => e.ExternalId).IsUnique();
        builder.HasIndex(e => e.Email);
    }
}
