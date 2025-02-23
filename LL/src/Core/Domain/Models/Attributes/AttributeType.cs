namespace Domain.Models.Attributes;
public enum AttributeType
{
    // Primary Stats - 500 is the expected top value of most attribute types, if one maxes out
    Constitution, // 1 = 8 MaxHealth, 10 = 2 HP Regen, 10 = 1 CrowdControlResistance
    Endurance, // 1 = 2 MaxHealth, 10 = 2 PhysicalDefense, 10 = 1 CritDamageReduction
    Willpower, // 10 = 2 MP Regen, 10 = 2 MagicalDefense, 10 = 1 CritDamageReduction,
    Strength, // 10 = 1% CritDamage, 1 = 1 Block
    FightingSpirit, // 10 = 1 HP Regen, 10 = 1 CrowdControlResistance, 4 = 1 Parry
    Dexterity, // 10 = 1% Accuracy, 10 = 1% CritChance, 4 = Parry 
    Agility, // 10 = 1% Dodge, 25 = 1 BasicAttackSpeed, ???
    Intelligence, // 1 = 2 MaxMana, 10 = CritDamage = 1%, ???
    Wisdom, // 1 = 3 MaxMana, 10 = 1 MP Regen, ???
    Instinct, // 2 = 1 Parry, 10 = 1% Accuracy, 10 = 1% Dodge
    Perception, // 10 = 1% CritChance, 10 = 1% CritDamage, 15 = 1% ArmorPenetration Increase chance of spotting Hidden Rooms in Dungeons and Raids
    Luck, // 10 = 1% CritChance, 10 = 0.1% MultiStrike, 10 = 0.1% MultiCast, Increase chance for Special Encounters in Dungeons and Raids
    // Combat Stats
    MaxHealth, // 5k HP maxed out from CON and END
    Health,
    HealthRegeneration, // 150 Regen every 5 Tick maxed out from CON and FIG
    MaxMana, // 2.5k MP maxed out from INT and WIS
    Mana,
    ManaRegeneration, // 150 Regen every 5 Tick maxed out from WIL and WIS
    BasicAttackSpeed, // +20 maxed out from AGI - Equals 50 increments per tick, resulting in 6 ticks instead of 10 to perform an attack
    Power,  // Do I want to keep this and simply display it visually for a play? Perhaps it's simply the SUM of all Primary Attributes
            // Main-hand and Off-hand increase a base stat - An ability should not do more than 2k damage to an un-armored entity
            //               (Tanks) - Shields
            //              STR (Warriors) - Sword, Hammer, Axe, Mace,
            //              DEX (Rogues) - Dagger, Claws, Gloves, Bows, Crossbows,
            //              INT (Mages),
            //              WIS (Supports),
    PhysicalDefense, // 100 maxed out from END - Primarily gotten through Essences and Equipment
    MagicalDefense, // 100 maxed out from WIL - Primarily gotten through Essences and Equipment
    FlatDamageReduction,
    DamageReduction,
    CritChance,
    CritDamage,
    CritDamageReduction, // Reduces damage dealt by a crit
    Threat,
    CrowdControlResistance, // CC
    Accuracy,
    Dodge,
    Block,
    Parry,
    Barrier, // Can't be more than 2x Health
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