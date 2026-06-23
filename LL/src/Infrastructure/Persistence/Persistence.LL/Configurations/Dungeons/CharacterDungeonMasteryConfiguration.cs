using Domain.Models.Dungeons.Mastery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Dungeons;

public sealed class CharacterDungeonMasteryConfiguration : IEntityTypeConfiguration<CharacterDungeonMastery>
{
    public void Configure(EntityTypeBuilder<CharacterDungeonMastery> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.DungeonDefinitionId });

        builder.Property(x => x.DungeonDefinitionId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Experience)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired();

        builder.HasIndex(x => x.DungeonDefinitionId);
    }
}
