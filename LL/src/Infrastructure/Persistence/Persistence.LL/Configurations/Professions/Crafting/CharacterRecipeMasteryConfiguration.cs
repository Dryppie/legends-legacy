using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Professions.Crafting;

public class CharacterRecipeMasteryConfiguration : IEntityTypeConfiguration<CharacterRecipeMastery>
{
    public void Configure(EntityTypeBuilder<CharacterRecipeMastery> builder)
    {
        builder.HasKey(x => new { x.CharacterId, x.RecipeId });
        builder.Property(x => x.RecipeId).HasMaxLength(128);
    }
}
