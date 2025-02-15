using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Inventories;
public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.HasKey(i => i.CharacterId);

        builder.HasMany(i => i.InventoryItems)
            .WithOne()
            .HasForeignKey(ii => ii.InventoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}