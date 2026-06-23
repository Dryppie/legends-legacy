using Domain.Models.CharacterActions;
using Domain.Models.CharacterActions.CharacterActionDetails;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record IdleCombatOrchestrationRequest(
    CharacterAction CharacterAction,
    DateTimeOffset Now)
    : CombatOrchestrationRequest(CombatMode.Idle)
{
    public Guid CharacterId => CharacterAction.CharacterId;
    public DateTimeOffset NextEncounterAt => CharacterAction.UpdatedAt;

    public CombatActionDetails ActionDetails =>
        CharacterAction.ActionDetails as CombatActionDetails
        ?? throw new InvalidOperationException(
            "CharacterAction.ActionDetails must be CombatActionDetails for idle combat.");
}
