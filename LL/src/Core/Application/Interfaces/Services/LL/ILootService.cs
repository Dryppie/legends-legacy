using Domain.Models.Entities;
using Domain.Models.Inventories;
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
    public List<InventoryItem> GenerateGatheringLootAsync(LootTable lootTable, CancellationToken cancellationToken);

    /// <summary>
    /// Generate Idle-Combat Loot based on defeated enemies
    /// </summary>
    /// <param name="enemyCharacters"></param>
    /// <returns></returns>
    public List<InventoryItem> GenerateIdleCombatLootAsync(List<Entity> enemyCharacters);
}