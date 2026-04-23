namespace Services.LL.Combat.Layers.Rewards.Models;

public sealed record DungeonCombatRewardFacts(
    Guid CharacterId,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<DungeonEncounterRewardFacts> Encounters)
{
    public DungeonEncounterRewardFacts? LastEncounter =>
        Encounters.Count == 0 ? null : Encounters[^1];
}