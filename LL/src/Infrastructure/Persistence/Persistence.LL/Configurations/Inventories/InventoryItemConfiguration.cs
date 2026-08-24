using Domain.Models.Inventories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.LL.Configurations.Inventories;
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasKey(ii => new { ii.InventoryId, ii.ItemInstanceId });
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_InventoryItems_Quantity_Positive",
            "\"Quantity\" > 0"));

        // Supports "does this character have anything unseen" without scanning the inventory.
        builder.HasIndex(ii => new { ii.InventoryId, ii.SeenAtUtc })
            .HasFilter("\"SeenAtUtc\" IS NULL");
    }
}
