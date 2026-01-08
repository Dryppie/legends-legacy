using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures.Templates;

public static class Archetypes
{
    public static readonly ArchetypeProfile Tank = new()
    {
        Archetype = CreatureArchetype.Tank,
        HealthMultiplier = 1.6f,
        DamageMultiplier = 0.6f,
        DefenseMultiplier = 1.4f,
        SpeedMultiplier = 0.8f
    };

    public static readonly ArchetypeProfile Bruiser = new()
    {
        Archetype = CreatureArchetype.Bruiser,
        HealthMultiplier = 1.2f,
        DamageMultiplier = 1.2f,
        DefenseMultiplier = 1.1f,
        SpeedMultiplier = 1.0f
    };

    public static readonly ArchetypeProfile DPS = new()
    {
        Archetype = CreatureArchetype.DPS,
        HealthMultiplier = 0.7f,
        DamageMultiplier = 1.5f,
        DefenseMultiplier = 0.8f,
        SpeedMultiplier = 1.1f
    };

    public static readonly ArchetypeProfile Support = new()
    {
        Archetype = CreatureArchetype.Support,
        HealthMultiplier = 1.1f,
        DamageMultiplier = 0.9f,
        DefenseMultiplier = 1.1f,
        SpeedMultiplier = 1.0f
    };

    public static readonly ArchetypeProfile Hazard = new()
    {
        Archetype = CreatureArchetype.Hazard,
        HealthMultiplier = 1.0f,
        DamageMultiplier = 1.0f,
        DefenseMultiplier = 1.0f,
        SpeedMultiplier = 1.0f
    };

    public static readonly ArchetypeProfile Balanced = new()
    {
        Archetype = CreatureArchetype.Balanced,
        HealthMultiplier = 1.0f,
        DamageMultiplier = 1.0f,
        DefenseMultiplier = 1.0f,
        SpeedMultiplier = 1.0f
    };

    public static ArchetypeProfile Get(CreatureArchetype type) => type switch
    {
        CreatureArchetype.Tank => Tank,
        CreatureArchetype.Bruiser => Bruiser,
        CreatureArchetype.DPS => DPS,
        CreatureArchetype.Support => Support,
        CreatureArchetype.Hazard => Hazard,
        CreatureArchetype.Balanced => Balanced,
        _ => Balanced
    };
}
