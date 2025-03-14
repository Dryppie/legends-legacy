using Domain.Models.Entities;
using Domain.Models.Essences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Entities;
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        //builder
        //.HasMany(e => e.EquippedEssences)
        //.WithMany(e => e.Entities)
        //.UsingEntity<Dictionary<string, object>>(
        //    "EntityEssence",
        //    j => j.HasOne<Essence>().WithMany().HasForeignKey("EssenceId").OnDelete(DeleteBehavior.Cascade),
        //    j => j.HasOne<Entity>().WithMany().HasForeignKey("EntityId").OnDelete(DeleteBehavior.Restrict)
        //);
    }
}