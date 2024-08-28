using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;

namespace Application.Interfaces.Services.LL;
public interface ILootService
{
    /// <summary>
    /// Generate Gathering Loot based on a lootTable
    /// </summary>
    /// <param name="lootTable"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public List<InventoryItem> GenerateGatheringLoot(LootTable lootTable, CancellationToken cancellationToken);

}