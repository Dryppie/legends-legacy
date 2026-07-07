using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Outbox;

public sealed class GameEventOutboxMessageConfiguration : IEntityTypeConfiguration<GameEventOutboxMessage>
{
    public void Configure(EntityTypeBuilder<GameEventOutboxMessage> builder)
    {
        builder.ToTable("GameEventOutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(160);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(300);

        builder.HasIndex(x => new { x.AvailableAt, x.CreatedAt });
        builder.HasIndex(x => new { x.CharacterId, x.CreatedAt });
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");

        builder.HasMany(x => x.Deliveries)
            .WithOne(x => x.Message)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
