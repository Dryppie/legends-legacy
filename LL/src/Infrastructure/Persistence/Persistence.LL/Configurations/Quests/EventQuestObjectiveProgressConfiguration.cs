using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestObjectiveProgressConfiguration : IEntityTypeConfiguration<EventQuestObjectiveProgress>
{
    public void Configure(EntityTypeBuilder<EventQuestObjectiveProgress> builder)
    {
        builder.HasKey(x => new { x.EventQuestId, x.ObjectiveKey });
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.Property(x => x.ObjectiveKey).HasMaxLength(100);
        builder.HasOne(x => x.EventQuest).WithMany(x => x.Objectives)
            .HasForeignKey(x => x.EventQuestId).OnDelete(DeleteBehavior.Cascade);
    }
}
