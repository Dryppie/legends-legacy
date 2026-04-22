using Domain.Models.Combat;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatEncounterResultFactory : ICombatEncounterResultFactory
{
    private readonly ICombatSetupService _combatSetupService;

    public CombatEncounterResultFactory(ICombatSetupService combatSetupService)
    {
        _combatSetupService = combatSetupService;
    }

    public CombatEncounterResolutionResult Create(
        CombatEncounterRuntime runtime,
        CombatResult combatResult)
    {
        var friendlyPostState = _combatSetupService.CreateSimpleCombatEntities(
            [.. runtime.FriendlyParticipants.Select(x => x.Combatant)]);

        var hostilePostState = _combatSetupService.CreateSimpleCombatEntities(
            [.. runtime.HostileParticipants.Select(x => x.Combatant)]);

        // Keep backward compatibility for now.
        combatResult.PlayerTeam = friendlyPostState;
        combatResult.EnemyTeam = hostilePostState;

        return new CombatEncounterResolutionResult(
            EncounterId: runtime.Plan.EncounterId,
            Mode: runtime.Plan.Mode,
            Sequence: runtime.Plan.Sequence,
            StartedAt: runtime.Plan.StartsAt,
            Outcome: combatResult.Outcome,
            CombatResult: combatResult,
            FriendlyPostState: friendlyPostState,
            HostilePostState: hostilePostState);
    }
}