using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Outbox;

public sealed class GameEventOutboxDeliveryConfiguration : IEntityTypeConfiguration<GameEventOutboxDelivery>
{
    public void Configure(EntityTypeBuilder<GameEventOutboxDelivery> builder)
    {
        builder.ToTable("GameEventOutboxDeliveries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Consumer).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(4000);

        builder.HasIndex(x => new { x.Status, x.AvailableAt, x.CreatedAt });
        builder.HasIndex(x => new { x.Consumer, x.Status, x.AvailableAt });
        builder.HasIndex(x => new { x.MessageId, x.Consumer }).IsUnique();
    }
}
