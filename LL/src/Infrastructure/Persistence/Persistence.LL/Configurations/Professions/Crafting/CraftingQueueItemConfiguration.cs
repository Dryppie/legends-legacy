using Domain.Models.Professions.Crafting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Professions.Crafting;
public class CraftingQueueItemConfiguration : IEntityTypeConfiguration<CraftingQueueItem>
{
    public void Configure(EntityTypeBuilder<CraftingQueueItem> builder)
    {
        builder
            .HasOne(x => x.EquipmentInstance)
            .WithMany()
            .HasForeignKey(x => x.EquipmentInstanceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}