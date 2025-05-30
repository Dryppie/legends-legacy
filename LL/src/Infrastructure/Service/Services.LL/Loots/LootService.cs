using Application.Interfaces.Services.LL;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.EssenceItems;
using Domain.Models.LootTables;

namespace Services.LL.Loots;
public class LootService : ILootService
{
    private static readonly Random RandomGenerator = new();

    public int GenerateSoulstoneLoot(int seconds, double dropRate, double doubleChance)
    {
        double baseChance = 0.000278; // every 1 hour
                                      // 1/21600 - 0.0000463 // every 6 hour
                                      // 1/43200 - 0.0000232 // every 12 hour
        double effectiveRate = baseChance * (1 + (dropRate / 100.0));
        double expectedDrops = seconds * effectiveRate;

        int earned = SamplePoisson(expectedDrops);
        if (earned < 1) return 0;

        var rng = Random.Shared;
        if (earned > 0 && rng.NextDouble() <= doubleChance)
            earned *= 2;

        return earned;
    }

    private static int SamplePoisson(double lambda)
    {
        var rng = Random.Shared;
        int k = 0;
        double p = 1.0;
        double L = Math.Exp(-lambda);

        while (p > L)
        {
            k++;
            p *= rng.NextDouble();
        }

        return k - 1;
    }

    public List<InventoryItem> GenerateGatheringLootAsync(LootTable lootTable, CancellationToken cancellationToken)
    {
        return GetRandomLoot(lootTable);
    }

    public List<InventoryItem> GenerateIdleCombatLootAsync(List<Entity> entities)
    {
        var totalLoot = new List<InventoryItem>();
        foreach (var entity in entities.OfType<Creature>())
        {
            totalLoot.AddRange(GetRandomLoot(entity.LootTable));
        }
        return totalLoot;
    }

    // TODO: Redo Loot Generation
    public List<InventoryItem> GetRandomLoot(LootTable lootTable, int numberOfRolls = 1)
    {
        var generatedLoot = new List<InventoryItem>();

        for (int i = 0; i < numberOfRolls; i++)
        {

            var selectedEntry = GetRandomEntryBasedOnWeight([.. lootTable.Entries]);

            if (selectedEntry is LootTableItem lootTableItem)
            {
                generatedLoot.Add(ConvertItemIntoInventoryItem(lootTableItem.Item));
            }
            else if (selectedEntry is LootTable table)
            {
                generatedLoot.AddRange(GetRandomLoot(table, 1));
            }
        }

        return generatedLoot;
    }

    private LootTableEntry? GetRandomEntryBasedOnWeight(List<LootTableEntry> entries)
    {
        double totalWeight = entries.Sum(e => e.Weight);
        double randomValue = RandomGenerator.NextSingle() * 100;
        double cumulativeWeight = 0.0;

        foreach (var entry in entries)
        {
            cumulativeWeight += entry.Weight;
            if (randomValue <= cumulativeWeight)
            {
                return entry;
            }
        }

        return null;
    }

    private InventoryItem ConvertItemIntoInventoryItem(ItemBase item)
    {
        var itemInstance = item.ItemType switch
        {
            ItemType.Equipment => new EquipmentInstance() { Id = Guid.NewGuid(), ItemBaseId = item.Id, ItemBase = item },
            ItemType.Essence => new EssenceItemInstance() { Id = Guid.NewGuid(), ItemBaseId = item.Id, ItemBase = item },
            _ => new ItemInstance() { Id = Guid.NewGuid(), ItemBaseId = item.Id, ItemBase = item },
        };
        return new InventoryItem()
        {
            ItemInstanceId = itemInstance.Id,
            Quantity = 1,
            ItemInstance = itemInstance,
        };
    }
}