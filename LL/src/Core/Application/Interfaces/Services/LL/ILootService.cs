using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;

namespace Application.Interfaces.Services.LL;
public interface ILootService
{
    /// <summary>
    /// Generate base soulstone loot for elapsed time.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    int GenerateSoulstoneLoot(int seconds);

    /// <summary>
    /// Generate Gathering Loot based on a lootTable
    /// </summary>
    /// <param name="lootTable"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    List<InventoryItem> GenerateGatheringLootAsync(
        LootTable lootTable,
        CancellationToken cancellationToken,
        double rareEntryWeightBonusPercent = 0,
        int numberOfRolls = 1);

    /// <summary>
    /// Generate Idle-Combat Loot based on defeated enemies
    /// </summary>
    /// <param name="enemyCharacters"></param>
    /// <returns></returns>
    List<InventoryItem> GenerateIdleCombatLootAsync(List<Entity> enemyCharacters, Dictionary<ItemType, double> multipliers);
    List<InventoryItem> GenerateDungeonLoot(LootTable lootTable, Dictionary<ItemType, double>? multipliers = null);
    int GenerateCinderLoot(Dictionary<Guid, int> creatureKills, Dictionary<Guid, int> baseCinderValues, double dropChance = 0.2);
}
