using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;
using Domain.Models.Snapshots;

namespace Domain.Models.Professions.Crafting.V2;

/// <summary>
/// Deterministic, idempotent conversion for persisted crafted equipment.
/// V15 is first converted to represented-budget-equivalent v16 values. V16
/// direct-percentage candidates are then frozen at the effective value they
/// provided at the item's own tier and persisted as intrinsic v17 percentages.
/// </summary>
public static class EquipmentStatModelMigrator
{
    private static readonly IReadOnlyDictionary<AttributeType, LegacyCostDefinition> LegacyCosts =
        new Dictionary<AttributeType, LegacyCostDefinition>
        {
            [AttributeType.Power] = Tiered((1, 24d), (5, 12d), (10, 18d)),
            [AttributeType.MaxHealth] = Fixed(0.2d),
            [AttributeType.Armor] = Tiered((1, 0.68d), (5, 1.87d), (10, 4.12d)),
            [AttributeType.Resistance] = Tiered((1, 0.68d), (5, 1.87d), (10, 4.12d)),
            [AttributeType.CritChance] = Fixed(4d),
            [AttributeType.CritDamage] = Tiered((1, 2d), (5, 2.25d), (10, 2.5d)),
            [AttributeType.ArmorPenetration] = Fixed(3d),
            [AttributeType.MagicPenetration] = Fixed(3d),
            [AttributeType.DodgeChance] = Fixed(5d),
            [AttributeType.BlockChance] = Fixed(5d),
            [AttributeType.DamageReduction] = Fixed(6d),
            [AttributeType.HealingPowerPercent] = Fixed(3d),
            [AttributeType.HealthRegeneration] = Tiered((1, 1.5d), (5, 1.5d), (10, 2.1d)),
            [AttributeType.LifeSteal] = Fixed(6d),
            [AttributeType.Cooldown] = Fixed(6d),
            [AttributeType.StatusResistance] = Tiered((1, 2d), (5, 2d), (10, 2.2d)),
            [AttributeType.CrowdControlResistance] = Fixed(2d),
            [AttributeType.AttackSpeed] = Fixed(2.8d)
        };

    private static readonly IReadOnlyDictionary<AttributeType, V16RatingDefinition> V16Ratings =
        new Dictionary<AttributeType, V16RatingDefinition>
        {
            [AttributeType.CritChance] = Rating(100f, 100d),
            [AttributeType.CritDamage] = Rating(300f, 300d),
            [AttributeType.ArmorPenetration] = Rating(60f, 60d),
            [AttributeType.MagicPenetration] = Rating(60f, 60d),
            [AttributeType.DodgeChance] = Rating(50f, 50d),
            [AttributeType.BlockChance] = Rating(50f, 50d),
            [AttributeType.DamageReduction] = Rating(40f, 40d),
            [AttributeType.HealingPowerPercent] = Rating(300f, 300d),
            [AttributeType.LifeSteal] = Rating(50f, 50d),
            [AttributeType.Cooldown] = new(100f, 160d, UsesCooldownRate: true),
            [AttributeType.StatusResistance] = Rating(80f, 20d),
            [AttributeType.CrowdControlResistance] = Rating(80f, 20d),
            [AttributeType.AttackSpeed] = Rating(300f, 300d)
        };

    private static readonly IReadOnlyDictionary<AttributeType, double> V16Costs =
        new Dictionary<AttributeType, double>
        {
            [AttributeType.Power] = 24d,
            [AttributeType.MaxHealth] = 0.2d,
            [AttributeType.Armor] = 0.68d,
            [AttributeType.Resistance] = 0.68d,
            [AttributeType.CritChance] = 4d,
            [AttributeType.CritDamage] = 2d,
            [AttributeType.ArmorPenetration] = 3d,
            [AttributeType.MagicPenetration] = 3d,
            [AttributeType.DodgeChance] = 5d,
            [AttributeType.BlockChance] = 5d,
            [AttributeType.DamageReduction] = 6d,
            [AttributeType.HealingPowerPercent] = 3d,
            [AttributeType.HealthRegeneration] = 1.5d,
            [AttributeType.LifeSteal] = 6d,
            [AttributeType.Cooldown] = 6d,
            [AttributeType.StatusResistance] = 2d,
            [AttributeType.CrowdControlResistance] = 2d,
            [AttributeType.AttackSpeed] = 2.8d
        };

    public static bool RequiresMigration(EquipmentInstance equipment) =>
        equipment.UsesRecipeStatBudget
        && equipment.StatModelVersion < EquipmentStatBudgetCatalog.BalanceVersion;

    public static bool MigrateToCurrent(EquipmentInstance equipment)
    {
        if (!RequiresMigration(equipment))
            return false;

        if (equipment.StatModelVersion < EquipmentStatBudgetCatalog.PreviousBalanceVersion)
            MigrateV15ToV16(equipment);

        if (equipment.StatModelVersion == EquipmentStatBudgetCatalog.PreviousBalanceVersion)
            MigrateV16ToV17(equipment);

        return true;
    }

    public static bool MigrateToCurrent(EquipmentSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.BaseRecipeId)
            || snapshot.StatModelVersion >= EquipmentStatBudgetCatalog.BalanceVersion)
        {
            return false;
        }

        var temporary = new EquipmentInstance
        {
            BaseRecipeId = snapshot.BaseRecipeId,
            Tier = snapshot.Tier,
            StatModelVersion = snapshot.StatModelVersion,
            InstanceModifiers = snapshot.InstanceModifiers
                .Select(modifier => new InstanceAttributeModifier(
                    modifier.AttributeType,
                    modifier.Amount,
                    modifier.ModifierType)
                {
                    RarityBonusAmount = modifier.RarityBonusAmount
                })
                .ToList()
        };
        MigrateToCurrent(temporary);

        var converted = temporary.InstanceModifiers.ToList();
        var existing = snapshot.InstanceModifiers.ToList();
        for (var index = 0; index < converted.Count; index++)
        {
            if (index < existing.Count)
            {
                existing[index].AttributeType = converted[index].AttributeType;
                existing[index].Amount = converted[index].Amount;
                existing[index].ModifierType = converted[index].ModifierType;
                existing[index].RarityBonusAmount = converted[index].RarityBonusAmount;
                continue;
            }

            snapshot.InstanceModifiers.Add(
                EquipmentAttributeModifierSnapshot.From(converted[index]));
        }

        foreach (var duplicate in existing.Skip(converted.Count))
            snapshot.InstanceModifiers.Remove(duplicate);
        snapshot.StatModelVersion = temporary.StatModelVersion;
        return true;
    }

    public static float ConvertV16RatingToDirectPercentage(
        AttributeType attribute,
        double rawRating,
        int itemTier)
    {
        if (!V16Ratings.TryGetValue(attribute, out var definition))
            throw new InvalidOperationException($"Attribute '{attribute}' was not a v16 percentage rating.");

        var normalizedRating = Math.Max(0d, rawRating)
            / EquipmentTierBudgetCurve.GetScale(Math.Max(EquipmentStatBudgetCatalog.MinimumTier, itemTier));
        if (normalizedRating <= 0d)
            return 0f;

        if (definition.UsesCooldownRate)
        {
            var rate = 1d + normalizedRating / definition.HalfCapNormalizedRating;
            return (float)(100d * (1d - 1d / rate));
        }

        var effective = definition.EffectiveCap
            * normalizedRating
            / (definition.HalfCapNormalizedRating + normalizedRating);
        return (float)Math.Clamp(effective, 0d, definition.EffectiveCap);
    }

    private static void MigrateV15ToV16(EquipmentInstance equipment)
    {

        foreach (var modifier in equipment.InstanceModifiers)
        {
            if (modifier.ModifierType != ModifierType.Flat
                || modifier.Amount <= 0
                || !LegacyCosts.TryGetValue(modifier.AttributeType, out var oldDefinition))
            {
                continue;
            }

            var representedBudget = modifier.Amount
                * Interpolate(oldDefinition, Math.Clamp(equipment.Tier, 1, 10));
            var newPoints = representedBudget
                / V16Costs[modifier.AttributeType];
            modifier.Amount = AttributeValueQuantizer.Quantize(
                modifier.AttributeType,
                (float)newPoints);
        }

        equipment.StatModelVersion = EquipmentStatBudgetCatalog.PreviousBalanceVersion;
    }

    private static void MigrateV16ToV17(EquipmentInstance equipment)
    {
        foreach (var attribute in V16Ratings.Keys.Order())
        {
            var candidates = equipment.InstanceModifiers
                .Where(modifier =>
                    modifier.AttributeType == attribute
                    && modifier.ModifierType == ModifierType.Flat)
                .ToList();
            if (candidates.Count == 0)
                continue;

            var effective = ConvertV16RatingToDirectPercentage(
                attribute,
                candidates.Sum(modifier => Math.Max(0f, modifier.Amount)),
                equipment.Tier);
            candidates[0].Amount = AttributeValueQuantizer.Quantize(attribute, effective);
            foreach (var duplicate in candidates.Skip(1))
                equipment.InstanceModifiers.Remove(duplicate);
        }

        equipment.StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion;
    }

    private static double Interpolate(LegacyCostDefinition definition, int tier)
    {
        var anchors = definition.Anchors;
        var upperIndex = 0;
        while (upperIndex < anchors.Count && anchors[upperIndex].Tier < tier)
            upperIndex++;

        if (upperIndex == 0)
            return anchors[0].CostPerPoint;
        if (upperIndex >= anchors.Count)
            return anchors[^1].CostPerPoint;

        var lower = anchors[upperIndex - 1];
        var upper = anchors[upperIndex];
        var progress = (tier - lower.Tier) / (double)(upper.Tier - lower.Tier);
        return lower.CostPerPoint + ((upper.CostPerPoint - lower.CostPerPoint) * progress);
    }

    private static LegacyCostDefinition Fixed(double costPerPoint) =>
        Tiered((1, costPerPoint), (10, costPerPoint));

    private static LegacyCostDefinition Tiered(params (int Tier, double CostPerPoint)[] anchors) =>
        new(anchors
            .OrderBy(anchor => anchor.Tier)
            .Select(anchor => new EquipmentStatCostAnchor(anchor.Tier, anchor.CostPerPoint))
            .ToArray());

    private sealed record LegacyCostDefinition(IReadOnlyList<EquipmentStatCostAnchor> Anchors);

    private static V16RatingDefinition Rating(float effectiveCap, double halfCapNormalizedRating) =>
        new(effectiveCap, halfCapNormalizedRating, UsesCooldownRate: false);

    private sealed record V16RatingDefinition(
        float EffectiveCap,
        double HalfCapNormalizedRating,
        bool UsesCooldownRate);
}
