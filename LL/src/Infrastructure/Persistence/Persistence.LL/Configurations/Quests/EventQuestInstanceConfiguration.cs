using Domain.Models.Quests.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class EventQuestInstanceConfiguration : IEntityTypeConfiguration<EventQuestInstance>
{
    public void Configure(EntityTypeBuilder<EventQuestInstance> builder)
    {
        builder.HasKey(x => x.EventQuestId);
        builder.Property(x => x.EventQuestId).HasMaxLength(160);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.StartsAtUtc, x.EndsAtUtc });
    }
}
