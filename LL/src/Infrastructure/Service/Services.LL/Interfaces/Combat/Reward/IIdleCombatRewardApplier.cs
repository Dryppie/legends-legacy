using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward;

public interface IIdleCombatRewardApplier
{
    Task ApplyAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken);
}