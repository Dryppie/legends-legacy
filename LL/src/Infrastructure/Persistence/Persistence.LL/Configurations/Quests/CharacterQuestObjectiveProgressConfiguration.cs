using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class CharacterQuestObjectiveProgressConfiguration
    : IEntityTypeConfiguration<CharacterQuestObjectiveProgress>
{
    public void Configure(EntityTypeBuilder<CharacterQuestObjectiveProgress> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.QuestId, x.ObjectiveKey });
        builder.Property(x => x.QuestId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ObjectiveKey).HasMaxLength(160).IsRequired();
        builder.HasOne(x => x.QuestProgress)
            .WithMany(x => x.Objectives)
            .HasForeignKey(x => new { x.CharacterId, x.QuestId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
