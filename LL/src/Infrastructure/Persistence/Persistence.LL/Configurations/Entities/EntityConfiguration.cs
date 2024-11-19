using Domain.Models;
using Domain.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Entities;
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder
        .HasMany(e => e.EquippedEssences)
        .WithMany(e => e.Entities)
        .UsingEntity<Dictionary<string, object>>(
            "EntityEssence",
            j => j.HasOne<Essence>().WithMany().HasForeignKey("EssenceId"),
            j => j.HasOne<Entity>().WithMany().HasForeignKey("EntityId")
        );
    }
}