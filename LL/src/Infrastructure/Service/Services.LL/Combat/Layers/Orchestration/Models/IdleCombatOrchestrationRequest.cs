using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record IdleCombatOrchestrationRequest(
    CharacterAction CharacterAction,
    DateTimeOffset Now,
    bool CaptureFinalEncounterLog = true)
    : CombatOrchestrationRequest(CombatMode.Idle)
{
    public Guid CharacterId => CharacterAction.CharacterId;
    public DateTimeOffset NextEncounterAt => CharacterAction.NextResolutionAtUtc
        ?? throw new InvalidOperationException("Active idle combat requires a next-resolution boundary.");

    public CombatActionDetails ActionDetails =>
        CharacterAction.ActionDetails as CombatActionDetails
        ?? throw new InvalidOperationException(
            "CharacterAction.ActionDetails must be CombatActionDetails for idle combat.");
}
