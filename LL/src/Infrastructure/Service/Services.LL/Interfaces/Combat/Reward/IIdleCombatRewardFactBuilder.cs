using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward;

public interface IIdleCombatRewardFactBuilder
{
    Task<IdleCombatRewardFacts> BuildAsync(
        CombatOutcomeRequest request,
        CancellationToken cancellationToken);
}