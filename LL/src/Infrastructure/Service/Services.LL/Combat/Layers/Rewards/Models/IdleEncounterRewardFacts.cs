using Domain.Models.Combat;
using Domain.Models.Entities.Creatures;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record IdleEncounterRewardFacts(
    Guid EncounterId,
    int Sequence,
    DateTimeOffset StartedAt,
    BattleOutcome Outcome,
    IReadOnlyList<Guid> HostileSourceEntityIds,
    IReadOnlyList<Creature> HostileCreatures,
    CombatResult CombatResult)
{
    public IReadOnlyList<Services.LL.Combat.Layers.Resolution.Models.CombatStyleExperienceAward> CombatStyleExperience { get; init; } = [];
    public bool IsVictory => Outcome == BattleOutcome.Victory;
}
