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
            // ===== PRIMARY =====================================================
            [AttributeType.Power] = Percent(min: 4, max: 15),
            [AttributeType.Fortitude] = Flat(min: 2, max: 10),
            [AttributeType.Precision] = Percent(min: 2, max: 15),
            [AttributeType.Spirit] = Percent(min: 5, max: 20),

            // ===== BASE AND DERIVED INPUTS ====================================
            [AttributeType.MaxHealth] = Percent(min: 4, max: 15),
            [AttributeType.WeaponDamage] = Percent(min: 4, max: 15),
            [AttributeType.Armor] = Percent(min: 2, max: 10),
            [AttributeType.Resistance] = Percent(min: 2, max: 10),
            [AttributeType.CritChance] = Percent(min: 1, max: 10),
            [AttributeType.CritDamage] = Percent(min: 10, max: 50),
            [AttributeType.ArmorPenetration] = Percent(min: 2, max: 8),
            [AttributeType.MagicPenetration] = Percent(min: 2, max: 8),

            // ===== DEFENSE =====================================================
            [AttributeType.DodgeChance] = Percent(min: 1, max: 5),
            [AttributeType.BlockChance] = Percent(min: 1, max: 5),
            [AttributeType.DamageReduction] = Percent(min: 2, max: 10),

            // ===== RECOVERY / UTILITY / STATUS / SUMMONS ======================
            [AttributeType.HealingPowerPercent] = Percent(min: 2, max: 10),
            [AttributeType.HealthRegeneration] = Percent(min: 4, max: 15),
            [AttributeType.LifeSteal] = Percent(min: 1, max: 5),
            [AttributeType.Cooldown] = Percent(min: 1, max: 5),
            [AttributeType.StatusResistance] = Percent(min: 2, max: 15),
            [AttributeType.CrowdControlResistance] = Percent(min: 2, max: 15),
            [AttributeType.SummonPower] = Percent(min: 4, max: 15),
            [AttributeType.SummonHealth] = Percent(min: 4, max: 15),
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
