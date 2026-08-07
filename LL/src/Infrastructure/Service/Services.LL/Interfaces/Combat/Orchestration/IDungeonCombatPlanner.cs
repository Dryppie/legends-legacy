using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences.Definitions;
using Domain.Models.Snapshots;
using Services.LL.Combat.Layers.Orchestration.Models;

namespace Services.LL.Interfaces.Combat.Orchestration;

public interface IDungeonCombatPlanner
{
    DungeonCombatPlan CreatePlan(
        Guid dungeonRunId,
        Guid characterId,
        CharacterSnapshot characterSnapshot,
        int dungeonTier,
        IReadOnlyList<Guid> playerEntityIds,
        IReadOnlyList<Guid> enemySourceEntityIds,
        IReadOnlyList<AttributeModifierBase>? runAttributeModifiers = null,
        IReadOnlyList<EssenceAbilityModifierDefinition>? runAbilityModifiers = null,
        IReadOnlyList<AttributeModifierBase>? enemyAttributeModifiers = null,
        float? enemyStrengthMultiplier = null);

    CombatEncounterPlan CreateEncounterPlan(
        DungeonCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt);
}
