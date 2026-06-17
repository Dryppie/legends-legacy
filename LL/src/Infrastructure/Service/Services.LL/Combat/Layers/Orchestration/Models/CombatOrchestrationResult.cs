using Domain.Models.Entities;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record CombatOrchestrationResult(
    Guid SessionId,
    CombatMode Mode,
    IReadOnlyList<CombatEncounterRecord> Encounters,
    ICombatOrchestrationDetails Details,
    IReadOnlyDictionary<Guid, Entity>? SourceEntitiesById = null)
{
    public bool HasAnyCombat => Encounters.Count > 0;

    public int EncounterCount => Encounters.Count;

    public CombatEncounterRecord? LastEncounter =>
        Encounters.Count == 0 ? null : Encounters[^1];
}
