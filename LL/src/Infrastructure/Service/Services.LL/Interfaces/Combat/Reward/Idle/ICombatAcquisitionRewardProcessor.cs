using Domain.Models.Inventories;
using Services.LL.Combat.Layers.Rewards.Models;

namespace Services.LL.Interfaces.Combat.Reward.Idle;
public sealed record CombatAcquisitionRewardOutcome(IReadOnlyList<InventoryItem> Equipment, IReadOnlyList<InventoryItem> Sigils)
{
    public static CombatAcquisitionRewardOutcome Empty { get; } = new([], []);
}
public interface ICombatAcquisitionRewardProcessor
{
    Task<CombatAcquisitionRewardOutcome> ProcessAsync(IdleCombatRewardFacts facts, CancellationToken ct);
}
