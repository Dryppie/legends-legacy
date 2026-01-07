using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public sealed class ArchetypeProfile
{
    public CreatureArchetype Archetype { get; init; }
    public float HealthMultiplier { get; init; } = 1.0f;
    public float DamageMultiplier { get; init; } = 1.0f;
    public float DefenseMultiplier { get; init; } = 1.0f;
    public float SpeedMultiplier { get; init; } = 1.0f;
}
