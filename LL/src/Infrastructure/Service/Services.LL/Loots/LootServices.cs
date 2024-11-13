using Application.Interfaces.Services.LL;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;

namespace Services.LL.Loots;
public class LootServices : ILootService
{
    public List<InventoryItem> GenerateGatheringLootAsync(LootTable lootTable, CancellationToken cancellationToken)
    {
        return GetRandomLoot(lootTable);
    }

    // TODO: Redo Loot Generation
    public List<InventoryItem> GetRandomLoot(LootTable lootTable, bool allowZeroDrops = false)
    {
        var selectedLoot = new List<InventoryItem>();
        //var random = new Random();

        //int numberOfItems = GetNumberOfItemsToDrop(allowZeroDrops);

        //if (numberOfItems == 0)
        //    return selectedLoot;

        //// TODO: Make a CONST / Appsettings for drop %
        //var totalWeight = 10000;
        //var itemsCopy = new List<Item>(lootTable.Entries);

        //for (int i = 0; i < numberOfItems; i++)
        //{
        //    int roll = random.Next(0, totalWeight);
        //    float cumulativeWeight = 0;

        //    foreach (var item in itemsCopy)
        //    {
        //        cumulativeWeight += totalWeight / lootTable.Entries.Count;
        //        if (roll < cumulativeWeight)
        //        {
        //            var inventoryItem = ConvertItemIntoInventoryItem(item);
        //            selectedLoot.Add(inventoryItem);
        //            //totalWeight /= i; // Reduce total weight
        //            itemsCopy.Remove(item); // Ensure item is not selected again
        //            break;
        //        }
        //    }
        //}

        return selectedLoot;
    }

    private int GetNumberOfItemsToDrop(bool allowZeroDrops)
    {
        var random = new Random();

        // Define weights: 0 drops -> 1
        //                 1 drop  -> 6
        //                 2 drops -> 1
        //                 3 drops -> 2
        var weights = allowZeroDrops ? new int[] { 5, 85, 9, 1 } : new int[] { 0, 90, 9, 1 };

        var totalWeight = weights.Sum();
        var roll = random.Next(0, totalWeight);
        var cumulativeWeight = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulativeWeight += weights[i];
            if (roll < cumulativeWeight)
            {
                return i;
            }
        }

        return 1; // Default to 1 item if something goes wrong
    }

    private InventoryItem ConvertItemIntoInventoryItem(Item item)
    {
        return new InventoryItem()
        {
            ItemId = item.Id,
            Quantity = 1
        };
    }
}