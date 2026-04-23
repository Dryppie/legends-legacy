using Services.LL.Combat.Layers.Orchestration.Dungeon;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Rewards.Dungeon;

public sealed record DungeonCombatOutcomeContext(
    DungeonCombatOrchestrationRequest OrchestrationRequest,
    CombatOrchestrationResult OrchestrationResult,
    DungeonCombatOrchestrationDetails Details)
{
    public Guid DungeonRunId => Details.DungeonRunId;

    public Guid CharacterId => OrchestrationRequest.CharacterId;

    public IReadOnlyList<Guid> PlayerEntityIds => [OrchestrationRequest.CharacterId];

    public IReadOnlyList<CombatEncounterRecord> Encounters => OrchestrationResult.Encounters;

    public CombatEncounterRecord? LastEncounter => OrchestrationResult.LastEncounter;
}