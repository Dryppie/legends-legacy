using Domain.Models.Regions.Areas;

namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record IdleCombatRewardFacts(
    Guid CharacterId,
    DateTimeOffset From,
    DateTimeOffset RequestedTo,
    DateTimeOffset ProcessedUntil,
    TimeSpan ProcessedDuration,
    Area Area,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<IdleEncounterRewardFacts> Encounters)
{
    public IdleEncounterRewardFacts? LastEncounter =>
        Encounters.Count == 0 ? null : Encounters[^1];
}