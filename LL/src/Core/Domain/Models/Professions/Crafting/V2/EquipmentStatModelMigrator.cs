using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items.Equipments;

namespace Domain.Models.Professions.Crafting.V2;

/// <summary>
/// Deterministic, idempotent conversion for persisted v15 crafted equipment.
/// The old item's budget per attribute is reconstructed at its original tier,
/// then repurchased with the v16 constant price.
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

    public static bool RequiresMigration(EquipmentInstance equipment) =>
        equipment.UsesRecipeStatBudget
        && equipment.StatModelVersion < EquipmentStatBudgetCatalog.BalanceVersion;

    public static bool MigrateToCurrent(EquipmentInstance equipment)
    {
        if (!RequiresMigration(equipment))
            return false;

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
                / EquipmentStatBudgetCatalog.Get(modifier.AttributeType).CostPerPoint;
            modifier.Amount = AttributeValueQuantizer.Quantize(
                modifier.AttributeType,
                (float)newPoints);
        }

        equipment.StatModelVersion = EquipmentStatBudgetCatalog.BalanceVersion;
        return true;
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
}
