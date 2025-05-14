using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Recipes;
public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.HasKey(m => new { m.RecipeId, m.ItemId });
        //builder.HasOne(m => m.Recipe)
        //    .WithMany(r => r.Materials)
        //    .HasForeignKey(m => m.RecipeId)
        //    .OnDelete(DeleteBehavior.Restrict);
    }
}