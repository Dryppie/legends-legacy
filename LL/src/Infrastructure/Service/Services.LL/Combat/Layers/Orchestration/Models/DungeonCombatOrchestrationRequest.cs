using Domain.Models.Attributes.Modifiers;
using Domain.Models.CombatStyles;
using Domain.Models.Essences.Definitions;
using Domain.Models.Snapshots;

namespace Services.LL.Combat.Layers.Orchestration.Models;

public sealed record DungeonCombatOrchestrationRequest(
    Guid DungeonRunId,
    Guid CharacterId,
    CharacterSnapshot CharacterSnapshot,
    int CurrentRoomIndex,
    IReadOnlyList<string> EnemyCreatureKeys,
    IReadOnlyList<AttributeModifierBase>? RunAttributeModifiers = null,
    IReadOnlyList<EssenceAbilityModifierDefinition>? RunAbilityModifiers = null,
    IReadOnlyList<AttributeModifierBase>? EnemyAttributeModifiers = null,
    CombatStyleSnapshot? CombatStyle = null)
    : CombatOrchestrationRequest(CombatMode.Dungeon);
