using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Domain.Models.Essences;

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
            [.. friendlyParticipants.Select(x => x.Combatant), .. hostileParticipants.Select(x => x.Combatant)],
            MapEssenceActivity(encounterPlan.Mode));

        return new CombatEncounterRuntime(
            encounterPlan,
            friendlyParticipants,
            hostileParticipants);
    }

    private static EssenceCombatActivity MapEssenceActivity(CombatMode mode) => mode switch
    {
        CombatMode.Dungeon => EssenceCombatActivity.Dungeon,
        CombatMode.Raid => EssenceCombatActivity.Raid,
        CombatMode.Pvp => EssenceCombatActivity.Arena,
        CombatMode.RegionBoss => EssenceCombatActivity.RegionBoss,
        _ => EssenceCombatActivity.IdleCombat
    };
}
