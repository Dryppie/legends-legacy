using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestRewardClaimConfiguration : IEntityTypeConfiguration<EventQuestRewardClaim>
{
    public void Configure(EntityTypeBuilder<EventQuestRewardClaim> builder)
    {
        builder.HasKey(x => new { x.EventQuestId, x.CharacterId });
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.HasOne(x => x.EventQuest).WithMany(x => x.RewardClaims)
            .HasForeignKey(x => x.EventQuestId).OnDelete(DeleteBehavior.Cascade);
    }
}
