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

    // Values 17 and 18 were retired with the dedicated summon attributes.
    AttackSpeed = 19
}
