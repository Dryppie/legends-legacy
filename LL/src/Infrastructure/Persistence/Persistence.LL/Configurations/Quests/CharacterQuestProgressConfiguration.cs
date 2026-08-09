using Domain.Models.Quests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Quests;

public sealed class CharacterQuestProgressConfiguration : IEntityTypeConfiguration<CharacterQuestProgress>
{
    public void Configure(EntityTypeBuilder<CharacterQuestProgress> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.QuestId });
        builder.Property(x => x.QuestId).HasMaxLength(160).IsRequired();
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.CharacterId, x.Status });
        builder.HasIndex(x => new { x.CharacterId, x.IsPinned });
    }
}
