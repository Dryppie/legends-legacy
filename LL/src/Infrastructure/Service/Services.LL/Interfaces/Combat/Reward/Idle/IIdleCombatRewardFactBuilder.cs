using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleCombatRewardFactBuilder
{
    Task<IdleCombatRewardFacts> BuildAsync(
        IdleCombatOutcomeContext context,
        CancellationToken cancellationToken);
}