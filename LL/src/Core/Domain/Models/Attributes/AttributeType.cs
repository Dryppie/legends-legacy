namespace Domain.Models.Attributes;
public enum AttributeType
{
    // Primary Stats
    Constitution,
    Endurance,
    Willpower,
    Strength,
    FightingSpirit,
    Dexterity,
    Agility,
    Intelligence,
    Wisdom,
    Perception,
    Luck,
    // Combat Stats
    Health,
    MaxHealth,
    HealthRegeneration,
    Mana,
    MaxMana,
    ManaRegeneration,
    BasicAttackSpeed,
    AttackPower,
    MagicPower,
    Defense,
    MagicDefense,
    Speed,
    CritChance,
    CritDamage,
    CritResistance,
    Threat,
    CrowdControlResistance,
    Accuracy,
    Dodge,
    Block,
    ManaShield, // Can't be more than 2x Health
    MultiStrike, // Chance to use Physical Attack a 2nd time
    MultiCast, // Chance to use Magical Attack a 2nd time
    CooldownReduction,
    ArmorPenetration,
    ManaPenetration,
    LifeSteal,
    // Resistances
    FireResistance,
    WaterResistance,
    EarthResistance,
    AirResistance,
    PoisonResistance,
}