using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Professions.Crafting;

public class CharacterRecipeUnlockConfiguration : IEntityTypeConfiguration<CharacterRecipeUnlock>
{
    public void Configure(EntityTypeBuilder<CharacterRecipeUnlock> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BlueprintId).HasMaxLength(128);
        builder.HasIndex(x => new { x.CharacterId, x.BlueprintId }).IsUnique();
    }
}
