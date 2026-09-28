namespace Domain.Models.Attributes;
public enum AttributeType
{
    Power = 0,
    MaxHealth = 1,
    Armor = 2,
    Resistance = 3,
    CritChance = 4,
    CritDamage = 5,
    ArmorPenetration = 6,
    MagicPenetration = 7,

    DodgeChance = 8,
    BlockChance = 9,
    DamageReduction = 10,

    HealingPowerPercent = 11,
    HealthRegeneration = 12,
    LifeSteal = 13,

    Cooldown = 14,
    StatusResistance = 15,
    CrowdControlResistance = 16,

    Threat = 17,

    // Value 18 was retired with the dedicated summon attributes.
    AttackSpeed = 19,

    // Persisted identifiers are never reused when units or consumers change.
    AbilityHaste = 20,
    Tenacity = 21,
    Restoration = 22,

    // Finite normalized defense retained through combat. Armor/Resistance on
    // items remain raw ratings; their character-facing values remain percentages.
    ArmorRating = 23,
    ResistanceRating = 24
}
