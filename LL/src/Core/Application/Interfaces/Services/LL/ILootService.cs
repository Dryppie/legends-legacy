using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;

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
    /// Generate Idle-Combat Loot based on defeated enemies
    /// </summary>
    /// <param name="enemyCharacters"></param>
    /// <returns></returns>
    Task<List<InventoryItem>> GenerateIdleCombatLootAsync(
        List<Entity> enemyCharacters,
        Dictionary<ItemType, double> multipliers,
        CancellationToken cancellationToken);
    int GenerateCinderLoot(Dictionary<Guid, int> creatureKills, Dictionary<Guid, int> baseCinderValues, double dropChance = 0.2);
}
