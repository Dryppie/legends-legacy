using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonPendingRewardWriter
{
    Task AddAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken);

    Task AddLootAsync(
        Guid dungeonRunId,
        IReadOnlyList<InventoryItem> loot,
        string source,
        CancellationToken cancellationToken);
}
