using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences.Definitions;
using Domain.Models.Snapshots;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatPlan(
    Guid DungeonRunId,
    Guid CharacterId,
    CharacterSnapshot CharacterSnapshot,
    int DungeonTier,
    int DungeonRegion,
    IReadOnlyList<Guid> PlayerEntityIds,
    IReadOnlyList<Guid> EnemySourceEntityIds,
    IReadOnlyList<AttributeModifierBase> RunAttributeModifiers,
    IReadOnlyList<EssenceAbilityModifierDefinition> RunAbilityModifiers,
    IReadOnlyList<AttributeModifierBase> EnemyAttributeModifiers,
    float? EnemyStrengthMultiplier);
