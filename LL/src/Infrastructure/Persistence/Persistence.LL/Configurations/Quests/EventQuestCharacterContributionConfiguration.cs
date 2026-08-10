using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestCharacterContributionConfiguration : IEntityTypeConfiguration<EventQuestCharacterContribution>
{
    public void Configure(EntityTypeBuilder<EventQuestCharacterContribution> builder)
    {
        builder.HasKey(x => new { x.EventQuestId, x.CharacterId });
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.HasOne(x => x.EventQuest).WithMany(x => x.Contributions)
            .HasForeignKey(x => x.EventQuestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.EventQuestId, x.TotalAmount });
    }
}
