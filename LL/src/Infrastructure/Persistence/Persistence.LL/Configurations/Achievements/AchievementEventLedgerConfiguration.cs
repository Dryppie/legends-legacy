using Domain.Models.Achievements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Achievements;

public sealed class AchievementEventLedgerConfiguration : IEntityTypeConfiguration<AchievementEventLedger>
{
    public void Configure(EntityTypeBuilder<AchievementEventLedger> builder)
    {
        builder.ToTable("AchievementEventLedgers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(160).IsRequired();

        builder.HasIndex(x => x.OutboxMessageId).IsUnique();
        builder.HasIndex(x => new { x.CharacterId, x.ProcessedAt });
    }
}
