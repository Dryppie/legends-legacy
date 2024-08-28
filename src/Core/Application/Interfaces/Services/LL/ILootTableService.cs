using Domain.Models.LootTables;

namespace Application.Interfaces.Services.LL;
public interface ILootTableService
{
    /// <summary>
    /// Get the Loot Table for Gathering
    /// </summary>
    /// <param name="lootTableId"></param>
    /// <returns></returns>
    Task<LootTable> GetLootTableByIdAsync(Guid lootTableId, CancellationToken cancellationToken);
    /// <summary>
    /// Get the Loot Table for a Monster
    /// </summary>
    /// <param name="monsterId"></param>
    /// <returns></returns>
    Task<LootTable> GetMonsterLootTableAsync(Guid monsterId, CancellationToken cancellationToken);
    /// <summary>
    /// Get the Loot Table for Gathering
    /// </summary>
    /// <param name="professionTaskId"></param>
    /// <returns></returns>
    Task<LootTable> GetProfessionTaskLootTableAsync(Guid professionTaskId, CancellationToken cancellationToken);
}