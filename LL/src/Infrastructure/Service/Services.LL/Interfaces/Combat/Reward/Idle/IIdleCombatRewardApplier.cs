using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleCombatRewardApplier
{
    Task ApplyAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken);

    Task ApplyProgressionAsync(
        IdleCombatRewardFacts facts,
        IdleCombatCalculatedOutcome outcome,
        CancellationToken cancellationToken) =>
        ApplyAsync(facts, outcome, cancellationToken);

    Task ApplySettlementAsync(
        IReadOnlyList<IdleCombatSettlementBatch> batches,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
