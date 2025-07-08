namespace Domain.Models.Attributes;
public enum AttributeType
{
    /* ===== VITALITY ===== */
    MaxHealth,
    Health,
    HealthRegeneration,
    MaxMana,
    Mana,
    ManaRegeneration,
    RecoveryRate,
    Barrier, // Can't be more than 2x Health

    /* ===== OFFENSE =====*/
    AttackPower,
    SpellPower,
    AttackSpeed,
    Accuracy,
    CritChance,
    CritDamage,
    MultiStrike, // Chance to use Physical Attack a 2nd time
    MultiCast, // Chance to use Magical Attack a 2nd time
    ArmorPenetration,
    ManaPenetration,

    /* ===== DEFENSE ===== */
    PhysicalDefense,
    MagicalDefense,
    DamageReduction,
    CritDamageReduction, // Reduces damage dealt by a crit
    CrowdControlResistance, // CC
    Dodge,
    Block,
    Parry,

    /* ===== CONTROL & UTILITY ===== */
    Threat,
    CooldownReduction,

    /* ===== Resistances ===== */
    FireResistance,
    WaterResistance,
    EarthResistance,
    AirResistance,
}