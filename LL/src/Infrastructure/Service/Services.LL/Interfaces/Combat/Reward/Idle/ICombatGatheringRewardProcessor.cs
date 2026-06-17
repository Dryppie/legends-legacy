using Domain.Models.Combat;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface ICombatGatheringRewardProcessor
{
    Task<IReadOnlyList<GatheringRewardResult>> ProcessAsync(
        IdleCombatRewardFacts facts,
        CancellationToken cancellationToken);
}
