using Domain.Models.Items;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Seeds;
public static class SeedData
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        var swordId = Guid.NewGuid();
        var shieldId = Guid.NewGuid();
        var potionId = Guid.NewGuid();

        modelBuilder.Entity<Item>().HasData(
            new Item { Id = swordId, Name = "Sword" },
            new Item { Id = shieldId, Name = "Shield" },
            new Item { Id = potionId, Name = "Potion" }
        );

        // Seed LootTable
        var lootTableId = Guid.NewGuid();
        modelBuilder.Entity<LootTable>().HasData(new LootTable
        {
            Id = lootTableId,
        });

        // Seed relationships between LootTable and Items
        modelBuilder.Entity<LootTable>()
            .HasMany(lt => lt.Entries)
            .WithMany(i => i.LootTables)
            .UsingEntity(j => j.HasData(
                new { LootTablesId = lootTableId, ItemsId = swordId },
                new { LootTablesId = lootTableId, ItemsId = shieldId },
                new { LootTablesId = lootTableId, ItemsId = potionId }
            ));
    }
}