using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Essences;

public sealed class CharacterCreatureArchiveEntryConfiguration : IEntityTypeConfiguration<CharacterCreatureArchiveEntry>
{
    public void Configure(EntityTypeBuilder<CharacterCreatureArchiveEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatureDefinitionId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatureName).HasMaxLength(128).IsRequired();
        // Keep the existing storage names so saved focus targets and cooldown history need no migration.
        builder.Property(x => x.IsCreatureFocus).HasColumnName("IsEssenceFocus");
        builder.Property(x => x.CreatureFocusSetAtUtc).HasColumnName("EssenceFocusSetAtUtc");
        builder.Property(x => x.CreatureFocusTotalDurationSeconds).HasColumnName("EssenceFocusTotalDurationSeconds");
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.IsCreatureFocus })
            .HasDatabaseName("IX_CharacterCreatureArchiveEntries_CharacterId_IsEssenceFocus");
        builder.HasIndex(x => new { x.CharacterId, x.CreatureDefinitionId }).IsUnique();
    }
}
