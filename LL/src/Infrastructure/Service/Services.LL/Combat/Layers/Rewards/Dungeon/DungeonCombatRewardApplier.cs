using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

internal class DungeonCombatRewardApplier : IDungeonCombatRewardApplier
{
    private readonly IDungeonPendingRewardWriter _pendingRewardWriter;

    public DungeonCombatRewardApplier(IDungeonPendingRewardWriter pendingRewardWriter)
    {
        _pendingRewardWriter = pendingRewardWriter;
    }

    public async Task ApplyAsync(
        DungeonCombatRewardFacts facts,
        DungeonCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken)
    {
        if (outcome.TotalExperience <= 0 &&
            outcome.TotalCinders <= 0 &&
            outcome.TotalSoulstones <= 0 &&
            outcome.TotalLoot.Count <= 0)
        {
            return;
        }

        await _pendingRewardWriter.AddAsync(
            facts,
            outcome,
            cancellationToken);
    }
}
