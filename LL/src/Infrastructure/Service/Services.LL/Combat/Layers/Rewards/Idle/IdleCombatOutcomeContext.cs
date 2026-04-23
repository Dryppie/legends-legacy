using Domain.Models.Regions.Areas;
using Services.LL.Combat.Layers.Orchestration.Idle;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Combat.Layers.Rewards.Idle;

public sealed record IdleCombatOutcomeContext(
    IdleCombatOrchestrationRequest OrchestrationRequest,
    CombatOrchestrationResult OrchestrationResult,
    IdleCombatOrchestrationDetails Details)
{
    public Guid CharacterId => OrchestrationRequest.CharacterId;

    public Area Area => OrchestrationRequest.ActionDetails.Area;

    public IReadOnlyList<Guid> PlayerEntityIds => OrchestrationRequest.ActionDetails.CharacterTeam;

    public IReadOnlyList<CombatEncounterRecord> Encounters => OrchestrationResult.Encounters;

    public CombatEncounterRecord? LastEncounter => OrchestrationResult.LastEncounter;
}