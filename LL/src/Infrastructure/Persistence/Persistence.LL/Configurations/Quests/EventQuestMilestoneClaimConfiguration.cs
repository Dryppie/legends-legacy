using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestMilestoneClaimConfiguration : IEntityTypeConfiguration<EventQuestMilestoneClaim>
{
    public void Configure(EntityTypeBuilder<EventQuestMilestoneClaim> builder)
    {
        builder.HasKey(x => new { x.EventQuestId, x.CharacterId, x.MilestoneKey });
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.Property(x => x.MilestoneKey).HasMaxLength(100);
        builder.HasOne(x => x.EventQuest).WithMany(x => x.MilestoneClaims)
            .HasForeignKey(x => x.EventQuestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.EventQuestId, x.CharacterId });
    }
}
