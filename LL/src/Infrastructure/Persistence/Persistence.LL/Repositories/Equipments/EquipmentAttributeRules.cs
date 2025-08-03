using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Persistence.LL.Repositories.Equipments;
public static class EquipmentAttributeRules
{
    public static readonly IReadOnlyDictionary<AttributeType, Rule> Rules;

    static EquipmentAttributeRules()
    {
        Rules = new Dictionary<AttributeType, Rule>
        {
            // ===== VITALITY ====================================================
            [AttributeType.MaxHealth] = Percent(min: 4, max: 15),
            [AttributeType.MaxMana] = Percent(min: 5, max: 20),
            [AttributeType.HealthRegeneration] = Percent(min: 4, max: 15),

            // ===== OFFENSE =====================================================
            [AttributeType.AttackPower] = Percent(min: 4, max: 15),
            [AttributeType.SpellPower] = Percent(min: 4, max: 15),
            [AttributeType.CritChance] = Percent(min: 1, max: 10),
            [AttributeType.CritDamage] = Percent(min: 10, max: 50),
            [AttributeType.MultiStrike] = Percent(min: 2, max: 15),
            [AttributeType.MultiCast] = Percent(min: 2, max: 12),
            [AttributeType.ArmorPenetration] = Percent(min: 2, max: 8),
            [AttributeType.ManaPenetration] = Percent(min: 2, max: 8),

            // ===== DEFENSE =====================================================
            [AttributeType.PhysicalDefense] = Percent(min: 2, max: 10),
            [AttributeType.MagicalDefense] = Percent(min: 2, max: 10),
            [AttributeType.DamageReduction] = Percent(min: 2, max: 10),
            [AttributeType.CritDamageReduction] = Percent(min: 2, max: 8),
            [AttributeType.CrowdControlResistance] = Percent(min: 2, max: 15),
            [AttributeType.Parry] = Percent(min: 1, max: 3),

            // ===== CONTROL & UTILITY =====================================================
            [AttributeType.Threat] = Flat(min: 2, max: 10),
            [AttributeType.CooldownReduction] = Percent(min: 1, max: 5),

            // ===== RESISTANCES =====================================================
            [AttributeType.FireResistance] = Percent(min: 2, max: 10),
            [AttributeType.WaterResistance] = Percent(min: 2, max: 10),
            [AttributeType.EarthResistance] = Percent(min: 2, max: 10),
            [AttributeType.AirResistance] = Percent(min: 2, max: 10),
        };
    }

    // -------- convenience factories ----------------------------------------
    private static Rule Flat(int min, int max) =>
        new(min, max, ModifierType.Flat);

    private static Rule Percent(int min, int max) =>
        new(min, max, ModifierType.Additive);
}

public sealed record Rule(
    int Min,                           // inclusive
    int Max,                           // inclusive
    ModifierType ModType
);
