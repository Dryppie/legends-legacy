using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Characters;
using Domain.Models.Entities.Creatures;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;

namespace Services.LL.Combat.Layers.Resolution;

public sealed class CombatantFactory : ICombatantFactory
{
    private readonly ICombatSetupService _combatSetupService;

    public CombatantFactory(ICombatSetupService combatSetupService)
    {
        _combatSetupService = combatSetupService;
    }

    public CombatEntity Create(
        CombatParticipantSlot slot,
        Entity sourceEntity,
        CombatEncounterSourceContext sourceContext)
    {
        var combatant = sourceEntity switch
        {
            Character character => CreateCharacterCombatant(character),
            Creature creature => CreateCreatureCombatant(creature, sourceContext),
            _ => throw new NotSupportedException(
                $"Entity type '{sourceEntity.GetType().Name}' is not supported for combat resolution.")
        };

        AssignRuntimeIdentity(combatant, slot);
        return combatant;
    }

    private CombatEntity CreateCharacterCombatant(Character character)
    {
        return _combatSetupService
            .CreatePlayerCombatEntities([character])
            .Single();
    }

    private CombatEntity CreateCreatureCombatant(
        Creature creature,
        CombatEncounterSourceContext sourceContext)
    {
        return sourceContext switch
        {
            IdleEncounterSourceContext idleContext =>
                _combatSetupService
                    .CreateCreatureCombatEntities([creature], idleContext.Area)
                    .Single(),

            // Add concrete environment-backed implementations as these modes arrive.
            DungeonEncounterSourceContext =>
                throw new NotSupportedException(
                    "Dungeon room combatant creation requires dungeon room environment data in the source context."),

            RaidEncounterSourceContext =>
                _combatSetupService
                    .CreateCreatureCombatEntities([creature], new Domain.Models.Regions.Areas.Area { DifficultyTier = 1 })
                    .Single(),

            PvpEncounterSourceContext =>
                throw new InvalidOperationException(
                    "PvP hostile participants should resolve from character entities, not creatures."),

            _ => throw new NotSupportedException(
                $"Source context '{sourceContext.GetType().Name}' is not supported.")
        };
    }

    private static void AssignRuntimeIdentity(
        CombatEntity combatant,
        CombatParticipantSlot slot)
    {
        // Assumption: CombatEntity has a mutable runtime Id or similar field.
        // If it does not, add one. Do not keep relying on AppendPrefixToId hacks.
        combatant.Id = slot.SlotId;
        combatant.OriginalId = slot.SourceEntityId;
    }
}
