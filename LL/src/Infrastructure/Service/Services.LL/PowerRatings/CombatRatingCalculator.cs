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
/// attributes, and active Essence attributes. Essence abilities are deliberately
/// outside this version's contract.
/// </summary>
public static class CombatRatingCalculator
{
    public const int DefinitionVersion = 10;
    public const int ReferenceWeightTier = EquipmentStatBudgetCatalog.MinimumTier;

    private static readonly IReadOnlySet<AttributeType> OffenseAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.Power,
            AttributeType.Precision,
            AttributeType.WeaponDamage,
            AttributeType.CritChance,
            AttributeType.CritDamage,
            AttributeType.ArmorPenetration,
            AttributeType.MagicPenetration,
            AttributeType.Cooldown,
            AttributeType.SummonPower,
            AttributeType.AttackSpeed
        };

    private static readonly IReadOnlySet<AttributeType> PhysicalDurabilityAttributes =
        new HashSet<AttributeType>
        {
            AttributeType.Fortitude,
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
            AttributeType.Fortitude,
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
            AttributeType.Spirit,
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
            equipmentModifiers,
            includePrimaryContributions: false);
    }

    public static IReadOnlyDictionary<AttributeType, float> RemovePrimaryContributions(
        IReadOnlyDictionary<AttributeType, float> projectedAttributes)
    {
        var direct = projectedAttributes.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var contribution in AttributeCombatRules.PrimaryContributions)
        {
            var derived = direct.GetValueOrDefault(contribution.DerivedAttribute);
            var primary = direct.GetValueOrDefault(contribution.PrimaryAttribute);
            direct[contribution.DerivedAttribute] = Math.Max(
                0,
                derived - primary * contribution.ContributionPerPoint);
        }

        return direct;
    }

    public static CombatRatingBreakdown Calculate(
        IEnumerable<Domain.Models.Attributes.EntityAttribute> baseAttributes,
        IEnumerable<EquipmentInstance> equipment,
        IEnumerable<CombatRatingModifierSource>? additionalAttributeSources = null)
    {
        var sources = CreateBaseSources(baseAttributes);
        foreach (var item in equipment.DistinctBy(item => item.Id))
            AddModifierSource(sources, item.AttributeModifiers, item.Tier);

        foreach (var source in additionalAttributeSources ?? [])
            AddModifierSource(sources, source.Modifiers, source.Tier);

        return CreateBreakdown(ApplyCaps(sources));
    }

    public static CombatRatingBreakdown CalculateCanonical(
        IReadOnlyDictionary<AttributeType, float> directBaseAttributes,
        IReadOnlyDictionary<AttributeType, double> equipmentPoints,
        int equipmentTier)
    {
        var sources = directBaseAttributes.ToDictionary(
            entry => entry.Key,
            entry => CreateSource(entry.Key, entry.Value, ReferenceWeightTier));
        AddPointSource(sources, equipmentPoints, equipmentTier);
        return CreateBreakdown(ApplyCaps(sources));
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

    private static Dictionary<AttributeType, RatingSource> CreateBaseSources(
        IEnumerable<Domain.Models.Attributes.EntityAttribute> baseAttributes)
    {
        var values = baseAttributes
            .GroupBy(attribute => attribute.AttributeType)
            .ToDictionary(group => group.Key, group => group.Sum(attribute => attribute.Value));
        return EquipmentStatBudgetCatalog.Attributes.ToDictionary(
            attribute => attribute,
            attribute => CreateSource(
                attribute,
                values.GetValueOrDefault(attribute),
                ReferenceWeightTier));
    }

    private static void AddModifierSource(
        IDictionary<AttributeType, RatingSource> target,
        IEnumerable<AttributeModifierBase> modifiers,
        int tier)
    {
        var points = modifiers
            .Where(modifier =>
                modifier.Amount > 0
                && EquipmentStatBudgetCatalog.IsKnown(modifier.AttributeType))
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(group => group.Key, group => group.Sum(modifier => (double)modifier.Amount));
        AddPointSource(target, points, tier);
    }

    private static void AddPointSource(
        IDictionary<AttributeType, RatingSource> target,
        IReadOnlyDictionary<AttributeType, double> points,
        int tier)
    {
        foreach (var (attribute, amount) in points.Where(entry =>
                     entry.Value > 0
                     && EquipmentStatBudgetCatalog.IsKnown(entry.Key)))
        {
            var addition = CreateSource(attribute, amount, tier);
            if (!target.TryGetValue(attribute, out var current))
                current = new RatingSource(0, 0);
            target[attribute] = new RatingSource(
                current.Points + addition.Points,
                current.Budget + addition.Budget);
        }
    }

    private static RatingSource CreateSource(
        AttributeType attribute,
        double points,
        int weightTier)
    {
        var usefulPoints = Math.Max(0, points);
        return new RatingSource(
            usefulPoints,
            usefulPoints
            * EquipmentStatBudgetCatalog.Get(attribute, weightTier).CostPerPoint);
    }

    private static IReadOnlyDictionary<AttributeType, double> ApplyCaps(
        IReadOnlyDictionary<AttributeType, RatingSource> sources)
    {
        return sources.ToDictionary(
            entry => entry.Key,
            entry =>
            {
                if (entry.Value.Points <= 0)
                    return 0;
                if (!AttributeCatalog.TryGetEffectiveCharacterCap(
                        entry.Key,
                        EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier,
                        out var cap))
                {
                    return entry.Value.Budget;
                }

                return entry.Value.Budget
                       * Math.Min(1d, cap / entry.Value.Points);
            });
    }

    private static double Sum(
        IReadOnlyDictionary<AttributeType, double> weighted,
        IReadOnlySet<AttributeType> attributes) =>
        attributes.Sum(attribute => weighted.GetValueOrDefault(attribute));

    private static int Round(double value) =>
        Math.Max(0, (int)Math.Round(value, MidpointRounding.AwayFromZero));

    private readonly record struct RatingSource(double Points, double Budget);
}
