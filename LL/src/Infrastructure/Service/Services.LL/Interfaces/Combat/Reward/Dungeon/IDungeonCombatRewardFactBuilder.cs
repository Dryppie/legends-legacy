using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonCombatRewardFactBuilder
{
    Task<DungeonCombatRewardFacts> BuildAsync(
        DungeonCombatOutcomeContext context,
        CancellationToken cancellationToken);
}