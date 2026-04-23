using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonCombatRewardCalculator
{
    Task<DungeonCombatCalculatedOutcome> CalculateAsync(
        DungeonCombatRewardFacts facts,
        CancellationToken cancellationToken);
}