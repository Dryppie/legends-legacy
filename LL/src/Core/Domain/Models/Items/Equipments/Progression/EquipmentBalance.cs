using System.Collections.Frozen;
using Domain.Models.Attributes;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>
/// Immutable balance inputs. A changed configuration requires a new version;
/// historical versions remain resolvable by the catalog provider.
/// </summary>
public sealed class EquipmentBalance
{
    public const int ModelVersion = 3;
    public const int SpecializationBalanceVersion = 2;
    public const int MaximumRank = 5;
    public const double MinimumSupportedBasicAttackIntervalMultiplier = 0.75d;
    public const int StatUnitVersion = EquipmentStatBudgetCatalog.BalanceVersion;

    public EquipmentBalance(
        int version,
        double baseTierBudget = 100d,
        double styleBudgetShare = 0.15d,
        double rankBudgetIncrement = 0.04d,
        EquipmentBalanceSettings? settings = null)
    {
        if (version < 1)
            throw new ArgumentOutOfRangeException(nameof(version));
        EquipmentValidation.PositiveFinite(baseTierBudget);
        EquipmentValidation.PositiveFinite(rankBudgetIncrement);
        if (!double.IsFinite(styleBudgetShare) || styleBudgetShare <= 0 || styleBudgetShare >= 1)
            throw new ArgumentOutOfRangeException(nameof(styleBudgetShare));
        Version = version;
        BaseTierBudget = baseTierBudget;
        StyleBudgetShare = styleBudgetShare;
        RankBudgetIncrement = rankBudgetIncrement;
        AttributeVersion = settings?.AttributeVersion ?? (version == 1 ? 17 : 18);
        AttributeRules.ValidateVersion(AttributeVersion);
        if ((version == 1) != (AttributeVersion == AttributeRules.LegacyVersion))
            throw new ArgumentException("Equipment release 1 requires combat 17; later releases require combat 18.");
        CoreShare = settings?.CoreShare ?? .7d;
        IdentityShare = settings?.IdentityShare ?? .1d;
        if (!double.IsFinite(CoreShare) || CoreShare <= 0 || CoreShare >= 1
            || !double.IsFinite(IdentityShare) || IdentityShare < 0 || (UsesSpecializations && IdentityShare > StyleBudgetShare))
            throw new ArgumentException("Allocation shares must leave positive core and specialization budgets and a nonnegative style budget.");
        var costs = settings?.AttributeCosts ?? new Dictionary<AttributeType, double>();
        foreach (var (attribute, cost) in costs)
        {
            if (!EquipmentStatBudgetCatalog.IsKnown(attribute)) throw new ArgumentException($"Unknown equipment attribute '{attribute}'.");
            EquipmentValidation.PositiveFinite(cost);
        }
        AttributeCosts = costs.ToFrozenDictionary();
    }

    public int Version { get; }
    public int AttributeVersion { get; }
    public bool UsesSpecializations => Version >= SpecializationBalanceVersion;
    public double BaseTierBudget { get; }
    public double StyleBudgetShare { get; }
    public double RankBudgetIncrement { get; }
    public double CoreShare { get; }
    public double SpecializationShare => Math.Round(1d - CoreShare, 12);
    public double IdentityShare { get; }
    public IReadOnlyDictionary<AttributeType, double> AttributeCosts { get; }

    public double GetMaterializedCostPerPoint(AttributeType attribute, int tier)
    {
        var defaultCost = EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(attribute, tier, AttributeVersion);
        return AttributeCosts.TryGetValue(attribute, out var cost)
            ? defaultCost * cost / EquipmentStatBudgetCatalog.GetForVersion(attribute, AttributeVersion).CostPerPoint
            : defaultCost;
    }

    public double GetBaselineBudget(int tier, EquipmentType type)
    {
        if (tier < 1 || tier > EquipmentTierBudgetCurve.MaximumSupportedTier)
            throw new ArgumentOutOfRangeException(nameof(tier));
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));
        // Preserve current combined hand budgets: 2H = 1H + offhand = two 1H.
        var budget = BaseTierBudget * EquipmentTierBudgetCurve.GetScale(tier)
            * (type == EquipmentType.TwoHanded ? 2d : 1d);
        EquipmentValidation.PositiveFinite(budget);
        return budget;
    }

    public static double GetRarityMultiplier(EquipmentRarity rarity) => rarity switch
    {
        EquipmentRarity.Common => 1d,
        EquipmentRarity.Uncommon => 1.1d,
        EquipmentRarity.Rare => 1.3d,
        EquipmentRarity.Epic => 1.6d,
        EquipmentRarity.Unique => 2d,
        EquipmentRarity.Legendary => 2.5d,
        EquipmentRarity.Legacy => 3d,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };

    public static double GetQualityMultiplier(ItemQuality quality) => quality switch
    {
        ItemQuality.Crude => 0.90d,
        ItemQuality.Standard => 1d,
        ItemQuality.Fine => 1.12d,
        ItemQuality.Exceptional => 1.26d,
        ItemQuality.Masterpiece => 1.42d,
        _ => throw new ArgumentOutOfRangeException(nameof(quality))
    };
}
