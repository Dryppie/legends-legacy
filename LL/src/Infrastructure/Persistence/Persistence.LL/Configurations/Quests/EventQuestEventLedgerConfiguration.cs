using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestEventLedgerConfiguration : IEntityTypeConfiguration<EventQuestEventLedger>
{
    public void Configure(EntityTypeBuilder<EventQuestEventLedger> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.Property(x => x.ObjectiveKey).HasMaxLength(100);
        builder.Property(x => x.EventType).HasMaxLength(120);
        builder.HasIndex(x => new { x.EventQuestId, x.ObjectiveKey, x.OutboxMessageId }).IsUnique();
        builder.HasIndex(x => x.ProcessedAt);
    }
}
