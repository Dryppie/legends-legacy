using Domain.Models.Entities;
using Domain.Models.Inventories;
using Domain.Models.Items;

namespace Application.Interfaces.Services.LL;
public interface ILootService
{
    /// <summary>
    /// Generate soulstone loot
    /// </summary>
    /// <param name="seconds"></param>
    /// <param name="dropRate"></param>
    /// <param name="doubleChance"></param>
    /// <returns></returns>
    int GenerateSoulstoneLoot(int seconds, double dropRate, double doubleChance);

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
