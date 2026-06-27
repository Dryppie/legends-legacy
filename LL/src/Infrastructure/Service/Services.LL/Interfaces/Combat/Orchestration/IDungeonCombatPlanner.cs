using Domain.Models.Attributes.Modifiers;
using Domain.Models.CombatStyles;
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
        IReadOnlyList<Guid> playerEntityIds,
        IReadOnlyList<Guid> enemySourceEntityIds,
        IReadOnlyList<AttributeModifierBase>? runAttributeModifiers = null,
        IReadOnlyList<EssenceAbilityModifierDefinition>? runAbilityModifiers = null,
        IReadOnlyList<AttributeModifierBase>? enemyAttributeModifiers = null,
        CombatStyleSnapshot? combatStyle = null);

    CombatEncounterPlan CreateEncounterPlan(
        DungeonCombatPlan plan,
        int sequence,
        DateTimeOffset startsAt);
}
