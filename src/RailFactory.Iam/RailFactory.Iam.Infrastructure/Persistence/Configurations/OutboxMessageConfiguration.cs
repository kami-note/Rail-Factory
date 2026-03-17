using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RailFactory.Iam.Infrastructure.Persistence.Outbox;

namespace RailFactory.Iam.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.MessageType).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Payload).IsRequired();
        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.Processed).IsRequired();
        builder.HasIndex(e => new { e.Processed, e.CreatedAtUtc });
    }
}
