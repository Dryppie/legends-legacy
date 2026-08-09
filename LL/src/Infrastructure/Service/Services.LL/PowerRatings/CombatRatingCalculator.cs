using Domain.Components.Attributes;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.PowerRatings;

public sealed record CombatRatingBreakdown(
    int Overall,
    int SingleTargetOffense,
    int MultiTargetOffense,
    int PhysicalDurability,
    int MagicalDurability,
    int Sustain,
    int ControlUtility);

public sealed record CombatRatingModifierSource(
    int Tier,
    IReadOnlyList<AttributeModifierBase> Modifiers);

/// <summary>
/// Calculates deterministic Combat Rating from base attributes, equipped item
/// attributes, plus explicitly supplied temporary attribute sources. Essence
/// abilities are deliberately outside this version's contract.
/// </summary>
public static class CombatRatingCalculator
{
    public const int DefinitionVersion = 14;
    public const int ReferenceWeightTier = EquipmentStatBudgetCatalog.MinimumTier;

    private static readonly IReadOnlySet<AttributeType> OffenseAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.Power,
            AttributeType.CritChance,
            AttributeType.CritDamage,
            AttributeType.ArmorPenetration,
            AttributeType.MagicPenetration,
            AttributeType.Cooldown,
            AttributeType.AttackSpeed
        };

    private static readonly IReadOnlySet<AttributeType> PhysicalDurabilityAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.MaxHealth,
            AttributeType.Armor,
            AttributeType.DodgeChance,
            AttributeType.BlockChance,
            AttributeType.DamageReduction,
            AttributeType.HealthRegeneration
        };

    private static readonly IReadOnlySet<AttributeType> MagicalDurabilityAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.MaxHealth,
            AttributeType.Resistance,
            AttributeType.DodgeChance,
            AttributeType.DamageReduction,
            AttributeType.HealthRegeneration,
            AttributeType.StatusResistance,
            AttributeType.CrowdControlResistance
        };

    private static readonly IReadOnlySet<AttributeType> SustainAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.MaxHealth,
            AttributeType.HealingPowerPercent,
            AttributeType.HealthRegeneration,
            AttributeType.LifeSteal,
            AttributeType.Cooldown
        };

    public static IReadOnlyDictionary<AttributeType, float> ProjectDirectAttributes(
        IEnumerable<Domain.Models.Attributes.EntityAttribute> baseAttributes,
        IEnumerable<AttributeModifierBase> equipmentModifiers)
    {
        var baseValues = baseAttributes
            .GroupBy(attribute => attribute.AttributeType)
            .ToDictionary(group => group.Key, group => group.Sum(attribute => attribute.Value));
        return AttributeCalculator.CalculateProjectedAttributes(
            baseValues,
            equipmentModifiers);
    }

    public static CombatRatingBreakdown Calculate(
        IEnumerable<Domain.Models.Attributes.EntityAttribute> baseAttributes,
        IEnumerable<EquipmentInstance> equipment,
        IEnumerable<CombatRatingModifierSource>? additionalAttributeSources = null)
    {
        var modifiers = equipment
            .DistinctBy(item => item.Id)
            .SelectMany(item => item.AttributeModifiers)
            .Concat((additionalAttributeSources ?? []).SelectMany(source => source.Modifiers));
        var projected = ProjectDirectAttributes(baseAttributes, modifiers);

        return CreateBreakdown(ValueProjectedAttributes(projected));
    }

    public static CombatRatingBreakdown CalculateCanonical(
        IReadOnlyDictionary<AttributeType, float> directBaseAttributes,
        IReadOnlyDictionary<AttributeType, double> equipmentPoints,
        int equipmentTier)
    {
        // Retain the tier parameter for callers that construct canonical rungs,
        // but do not let source metadata change the value of identical final stats.
        _ = equipmentTier;
        var projected = directBaseAttributes
            .Where(entry => EquipmentStatBudgetCatalog.IsKnown(entry.Key))
            .ToDictionary(
                entry => entry.Key,
                entry => (double)entry.Value);
        foreach (var (attribute, points) in equipmentPoints.Where(entry =>
                     entry.Value > 0
                     && EquipmentStatBudgetCatalog.IsKnown(entry.Key)))
        {
            projected[attribute] = projected.GetValueOrDefault(attribute) + points;
        }

        return CreateBreakdown(ValueProjectedAttributes(projected));
    }

    private static CombatRatingBreakdown CreateBreakdown(
        IReadOnlyDictionary<AttributeType, double> weighted)
    {
        var overall = Round(weighted.Values.Sum());
        var offense = Round(Sum(weighted, OffenseAttributes));
        return new CombatRatingBreakdown(
            overall,
            offense,
            offense,
            Round(Sum(weighted, PhysicalDurabilityAttributes)),
            Round(Sum(weighted, MagicalDurabilityAttributes)),
            Round(Sum(weighted, SustainAttributes)),
            0);
    }

    private static IReadOnlyDictionary<AttributeType, double> ValueProjectedAttributes(
        IReadOnlyDictionary<AttributeType, float> projected) =>
        ValueProjectedAttributes(projected.ToDictionary(
            entry => entry.Key,
            entry => (double)entry.Value));

    private static IReadOnlyDictionary<AttributeType, double> ValueProjectedAttributes(
        IReadOnlyDictionary<AttributeType, double> projected)
    {
        return projected
            .Where(entry => EquipmentStatBudgetCatalog.IsKnown(entry.Key))
            .ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    var usefulPoints = Math.Max(0, entry.Value);
                    if (usefulPoints <= 0)
                        return 0;
                    if (!AttributeCatalog.TryGetEffectiveCharacterCap(
                            entry.Key,
                            EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier,
                            out var cap))
                    {
                        cap = float.MaxValue;
                    }

                    return Math.Min(usefulPoints, cap)
                           * EquipmentStatBudgetCatalog
                               .Get(entry.Key, ReferenceWeightTier)
                               .CostPerPoint;
                });
    }

    private static double Sum(
        IReadOnlyDictionary<AttributeType, double> weighted,
        IReadOnlySet<AttributeType> attributes) =>
        attributes.Sum(attribute => weighted.GetValueOrDefault(attribute));

    private static int Round(double value) =>
        Math.Max(0, (int)Math.Round(value, MidpointRounding.AwayFromZero));
}
