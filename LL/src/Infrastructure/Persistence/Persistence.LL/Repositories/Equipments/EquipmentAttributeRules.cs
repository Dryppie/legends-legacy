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
            [AttributeType.MaxHealth] = FlatScaling(min: 50, max: 500, scales: true),
            [AttributeType.HealthRegeneration] = FlatScaling(min: 1, max: 15, scales: true),
            [AttributeType.Barrier] = FlatScaling(min: 25, max: 250, scales: true),

            // ===== OFFENSE =====================================================
            [AttributeType.AttackPower] = FlatScaling(min: 5, max: 50, scales: true),
            [AttributeType.SpellPower] = FlatScaling(min: 5, max: 50, scales: true),
            [AttributeType.AttackSpeed] = PercentCapped(min: 1, max: 3, softCap: 20),
            [AttributeType.CritChance] = PercentCapped(min: 1, max: 2, softCap: 50),
            [AttributeType.CritDamage] = PercentScaling(min: 2, max: 10, scales: true),

            // … keep going for every enum value …
        };
    }

    // -------- convenience factories ----------------------------------------
    private static Rule FlatScaling(int min, int max, bool scales) =>
        new(min, max, ModifierType.Flat, scales, SoftCap: null);

    private static Rule PercentScaling(int min, int max, bool scales) =>
        new(min, max, ModifierType.Additive, scales, SoftCap: null);

    private static Rule PercentCapped(int min, int max, int softCap) =>
        new(min, max, ModifierType.Additive, ScalesWithItemLevel: false, softCap);
}

public sealed record Rule(
    int Min,                           // inclusive
    int Max,                           // inclusive
    ModifierType ModType,
    bool ScalesWithItemLevel,
    int? SoftCap                       // null ⇒ no cap
);
