using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatEncounterResultFactory : ICombatEncounterResultFactory
{
    public CombatEncounterResolutionResult Create(
        CombatEncounterRuntime runtime,
        CombatResult combatResult)
    {
        var friendlyPostState = combatResult.PlayerTeam;
        var hostilePostState = combatResult.EnemyTeam;

        return new CombatEncounterResolutionResult(
            EncounterId: runtime.Plan.EncounterId,
            Mode: runtime.Plan.Mode,
            Sequence: runtime.Plan.Sequence,
            StartedAt: runtime.Plan.StartsAt,
            Outcome: combatResult.Outcome,
            CombatResult: combatResult,
            FriendlyPostState: friendlyPostState,
            HostilePostState: hostilePostState)
        {
            ContentType = runtime.Plan.ContentType
        };
    }
}
