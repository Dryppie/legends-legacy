using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class QuestEventLedgerConfiguration : IEntityTypeConfiguration<QuestEventLedger>
{
    public void Configure(EntityTypeBuilder<QuestEventLedger> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(160).IsRequired();
        builder.HasIndex(x => x.OutboxMessageId).IsUnique();
        builder.HasIndex(x => new { x.CharacterId, x.ProcessedAt });
    }
}
