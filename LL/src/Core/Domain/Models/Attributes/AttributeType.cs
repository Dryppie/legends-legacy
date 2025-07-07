namespace Domain.Models.Attributes;
public enum AttributeType
{
    /* ===== VITALITY ===== */
    MaxHealth, // 5k HP maxed out from CON and END
    Health,
    HealthRegeneration, // 150 Regen every 5 Tick maxed out from CON and FIG
    MaxMana, // 2.5k MP maxed out from INT and WIS
    Mana,
    ManaRegeneration, // 150 Regen every 5 Tick maxed out from WIL and WIS
    RecoveryRate,
    Barrier, // Can't be more than 2x Health

    /* ===== OFFENSE =====*/
    AttackPower,
    SpellPower,
    AttackSpeed, // +20 maxed out from AGI - Equals 50 increments per tick, resulting in 6 ticks instead of 10 to perform an attack
    Accuracy,
    CritChance,
    CritDamage,
    MultiStrike, // Chance to use Physical Attack a 2nd time
    MultiCast, // Chance to use Magical Attack a 2nd time
    ArmorPenetration,
    ManaPenetration,

    /* ===== DEFENSE ===== */
    PhysicalDefense, // 100 maxed out from END - Primarily gotten through Essences and Equipment
    MagicalDefense, // 100 maxed out from WIL - Primarily gotten through Essences and Equipment
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