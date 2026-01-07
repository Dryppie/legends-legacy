using Domain.Models.Attributes;
using Domain.Models.Entities.Creatures.Templates.Enums;

namespace Domain.Models.Entities.Creatures;

public static class CreatureRoles
{
    public static readonly CreatureRoleProfile Tank = new()
    {
        Role = CreatureArchetype.Tank,
        HealthMultiplier = 1.6f,
        DamageMultiplier = 0.8f,
        DefenseMultiplier = 1.4f,
        SpeedMultiplier = 0.9f,
        AttributeMultipliers = new Dictionary<AttributeType, float>
        {
            { AttributeType.Threat, 1.5f },
            { AttributeType.CritChance, 0.7f },
        }
    };

    public static readonly CreatureRoleProfile DPS = new()
    {
        Role = CreatureArchetype.DPS,
        HealthMultiplier = 0.7f,
        DamageMultiplier = 1.5f,
        DefenseMultiplier = 0.8f,
        SpeedMultiplier = 1.1f,
        AttributeMultipliers = new Dictionary<AttributeType, float>
        {
            { AttributeType.CritChance, 1.3f },
            { AttributeType.CritDamage, 1.2f },
            { AttributeType.MultiStrike, 1.3f },
            { AttributeType.MultiCast,  1.3f },
        }
    };

    public static readonly CreatureRoleProfile Support = new()
    {
        Role = CreatureArchetype.Support,
        HealthMultiplier = 1.1f,
        DamageMultiplier = 0.9f,
        DefenseMultiplier = 1.1f,
        SpeedMultiplier = 1.0f,
        AttributeMultipliers = new Dictionary<AttributeType, float>
        {
            { AttributeType.MaxMana,          1.4f },
            { AttributeType.ManaRegeneration, 1.5f },
            { AttributeType.CooldownReduction,1.2f },
        }
    };

    public static readonly CreatureRoleProfile Balanced = new()
    {
        Role = CreatureArchetype.Balanced,
        HealthMultiplier = 1.0f,
        DamageMultiplier = 1.0f,
        DefenseMultiplier = 1.0f,
        SpeedMultiplier = 1.0f,
        AttributeMultipliers = new Dictionary<AttributeType, float>
        {
            { AttributeType.FireResistance,  1.2f },
            { AttributeType.WaterResistance, 1.2f },
            { AttributeType.EarthResistance, 1.2f },
            { AttributeType.AirResistance,   1.2f },
        }
    };

    public static CreatureRoleProfile Get(CreatureArchetype role) => role switch
    {
        CreatureArchetype.Tank => Tank,
        CreatureArchetype.DPS => DPS,
        CreatureArchetype.Support => Support,
        CreatureArchetype.Balanced => Balanced,
        _ => Balanced
    };
}
