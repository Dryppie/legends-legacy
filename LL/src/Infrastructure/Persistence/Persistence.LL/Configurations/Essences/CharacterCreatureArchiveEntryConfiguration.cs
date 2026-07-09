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
        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => new { x.CharacterId, x.CreatureDefinitionId }).IsUnique();
    }
}
