using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures;

public class CreatureRoleProfile
{
    public CreatureArchetype Role { get; init; }

    // Simple scalar knobs
    public float HealthMultiplier { get; init; } = 1.0f;
    public float DamageMultiplier { get; init; } = 1.0f;
    public float DefenseMultiplier { get; init; } = 1.0f;
    public float SpeedMultiplier { get; init; } = 1.0f;

    // Optional: per-attribute overrides if you need fine control later
    public IReadOnlyDictionary<AttributeType, float> AttributeMultipliers { get; init; }
        = new Dictionary<AttributeType, float>();
}
