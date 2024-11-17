using Domain.Models.Entities.Creatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Entities;
public class CreatureConfiguration : IEntityTypeConfiguration<Creature>
{
    public void Configure(EntityTypeBuilder<Creature> builder)
    {
        builder
            .HasOne(c => c.LootTable)
            .WithMany()
            .HasForeignKey(c => c.LootTableId);
    }
}