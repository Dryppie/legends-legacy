using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record DungeonEncounterRewardFacts(
    Guid EncounterId,
    BattleOutcome Outcome,
    IReadOnlyList<Guid> HostileSourceEntityIds,
    IReadOnlyList<Creature> HostileCreatures,
    CombatResult CombatResult)
{
    public bool IsVictory => Outcome == BattleOutcome.Victory;
}