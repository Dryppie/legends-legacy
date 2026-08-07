using Domain.Models.Attributes.Modifiers;
using Domain.Models.Essences.Definitions;
using Domain.Models.Snapshots;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatOrchestrationRequest(
    Guid DungeonRunId,
    Guid CharacterId,
    CharacterSnapshot CharacterSnapshot,
    int CurrentRoomIndex,
    int DungeonTier,
    IReadOnlyList<string> EnemyCreatureKeys,
    IReadOnlyList<AttributeModifierBase>? RunAttributeModifiers = null,
    IReadOnlyList<EssenceAbilityModifierDefinition>? RunAbilityModifiers = null,
    IReadOnlyList<AttributeModifierBase>? EnemyAttributeModifiers = null,
    float? EnemyStrengthMultiplier = null)
    : CombatOrchestrationRequest(CombatMode.Dungeon);
