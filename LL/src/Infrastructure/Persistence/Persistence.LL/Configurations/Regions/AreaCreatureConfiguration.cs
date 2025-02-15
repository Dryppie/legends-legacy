using Domain.Models.Entities.Creatures;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Regions;
public class AreaCreatureConfiguration : IEntityTypeConfiguration<AreaCreature>
{
    public void Configure(EntityTypeBuilder<AreaCreature> builder)
    {
        builder.HasKey(ai => new { ai.AreaId, ai.CreatureId });
        builder
            .HasOne<Creature>()
            .WithMany()
            .HasForeignKey(ac => ac.CreatureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
