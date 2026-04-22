using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatEncounterRuntimeFactory : ICombatEncounterRuntimeFactory
{
    private readonly ICombatantFactory _combatantFactory;
    private readonly ICombatSetupService _combatSetupService;

    public CombatEncounterRuntimeFactory(
        ICombatantFactory combatantFactory,
        ICombatSetupService combatSetupService)
    {
        _combatantFactory = combatantFactory;
        _combatSetupService = combatSetupService;
    }

    public async Task<CombatEncounterRuntime> CreateAsync(
        CombatEncounterPlan encounterPlan,
        LoadedEncounterEntities loadedEntities,
        CancellationToken cancellationToken)
    {
        var friendlyParticipants = new List<CombatRuntimeParticipant>();
        var hostileParticipants = new List<CombatRuntimeParticipant>();

        foreach (var slot in encounterPlan.Participants)
        {
            var sourceEntity = loadedEntities.SourceEntitiesById[slot.SourceEntityId];
            var combatant = _combatantFactory.Create(slot, sourceEntity, encounterPlan.SourceContext);

            var runtimeParticipant = new CombatRuntimeParticipant(
                slot,
                sourceEntity,
                combatant);

            if (slot.Side == CombatSide.Friendly)
            {
                friendlyParticipants.Add(runtimeParticipant);
            }
            else
            {
                hostileParticipants.Add(runtimeParticipant);
            }
        }

        await _combatSetupService.PrepareEntitiesForCombat(
            [.. friendlyParticipants.Select(x => x.Combatant), .. hostileParticipants.Select(x => x.Combatant)]);

        return new CombatEncounterRuntime(
            encounterPlan,
            friendlyParticipants,
            hostileParticipants);
    }
}