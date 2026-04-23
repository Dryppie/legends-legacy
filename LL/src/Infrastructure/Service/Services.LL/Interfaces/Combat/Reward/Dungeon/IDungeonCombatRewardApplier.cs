using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonCombatRewardApplier
{
    Task ApplyAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken);
}