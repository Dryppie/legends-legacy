using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures;

public sealed record CreatureBuildProfileDiagnostic(
    Guid CreatureId,
    string CreatureName,
    string SourceMonsterId,
    bool EssenceDefinitionResolved,
    string? EssenceDefinitionId,
    int AreaDifficultyTier,
    CreatureArchetype Archetype,
    DamageProfile DamageProfile,
    DefenseProfile DefenseProfile,
    int CombatRating,
    IReadOnlyDictionary<AttributeType, float> FinalAttributes);
