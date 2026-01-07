using Domain.Models.Attributes;

namespace Domain.Models.Entities.Creatures;

public static class MonsterBaseStats
{
    // Tier-1-ish baseline. Adjust until combat "feels" right.
    public const float BaseMaxHealth = 100f;
    public const float BaseHealth = 100f;
    public const float BaseHealthRegen = 0f;

    public const float BaseMaxMana = 100f;
    public const float BaseMana = 100f;
    public const float BaseManaRegen = 0f;
    public const float BaseRecoveryRate = 1f;
    public const float BaseBarrier = 0f;

    public const float BaseAttackPower = 10f;
    public const float BaseSpellPower = 10f;
    public const float BaseAttackSpeed = 1.0f; // your internal unit
    public const float BaseAccuracy = 0.90f;
    public const float BaseCritChance = 0.05f;
    public const float BaseCritDamage = 1.5f;
    public const float BaseMultiStrike = 0.0f;
    public const float BaseMultiCast = 0.0f;
    public const float BaseArmorPenetration = 0.0f;
    public const float BaseManaPenetration = 0.0f;

    public const float BasePhysicalDefense = 5f;
    public const float BaseMagicalDefense = 5f;
    public const float BaseDamageReduction = 0.0f;
    public const float BaseCritDamageRed = 0.0f;
    public const float BaseCcResistance = 0.0f;
    public const float BaseDodge = 0.0f;
    public const float BaseBlock = 0.0f;
    public const float BaseParry = 0.0f;

    public const float BaseThreat = 1.0f;
    public const float BaseCdr = 0.0f;

    public const float BaseFireRes = 0.0f;
    public const float BaseWaterRes = 0.0f;
    public const float BaseEarthRes = 0.0f;
    public const float BaseAirRes = 0.0f;

    public static readonly IReadOnlyDictionary<AttributeType, float> Baseline =
        new Dictionary<AttributeType, float>
        {
                { AttributeType.MaxHealth,          BaseMaxHealth },
                { AttributeType.Health,             BaseHealth },
                { AttributeType.HealthRegeneration, BaseHealthRegen },
                { AttributeType.MaxMana,            BaseMaxMana },
                { AttributeType.Mana,               BaseMana },
                { AttributeType.ManaRegeneration,   BaseManaRegen },
                { AttributeType.RecoveryRate,       BaseRecoveryRate },
                { AttributeType.Barrier,            BaseBarrier },

                { AttributeType.AttackPower,        BaseAttackPower },
                { AttributeType.SpellPower,         BaseSpellPower },
                { AttributeType.AttackSpeed,        BaseAttackSpeed },
                { AttributeType.Accuracy,           BaseAccuracy },
                { AttributeType.CritChance,         BaseCritChance },
                { AttributeType.CritDamage,         BaseCritDamage },
                { AttributeType.MultiStrike,        BaseMultiStrike },
                { AttributeType.MultiCast,          BaseMultiCast },
                { AttributeType.ArmorPenetration,   BaseArmorPenetration },
                { AttributeType.ManaPenetration,    BaseManaPenetration },

                { AttributeType.PhysicalDefense,    BasePhysicalDefense },
                { AttributeType.MagicalDefense,     BaseMagicalDefense },
                { AttributeType.DamageReduction,    BaseDamageReduction },
                { AttributeType.CritDamageReduction,BaseCritDamageRed },
                { AttributeType.CrowdControlResistance, BaseCcResistance },
                { AttributeType.Dodge,              BaseDodge },
                { AttributeType.Block,              BaseBlock },
                { AttributeType.Parry,              BaseParry },

                { AttributeType.Threat,             BaseThreat },
                { AttributeType.CooldownReduction,  BaseCdr },

                { AttributeType.FireResistance,     BaseFireRes },
                { AttributeType.WaterResistance,    BaseWaterRes },
                { AttributeType.EarthResistance,    BaseEarthRes },
                { AttributeType.AirResistance,      BaseAirRes },
        };
}