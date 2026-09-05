namespace Domain.Models.Items.Equipments.Progression;

/// <summary>
/// Immutable balance inputs. A changed configuration requires a new version;
/// historical versions must remain resolvable by the eventual catalog provider.
/// Tier scaling and stat exchange rates deliberately remain on v17 together.
/// </summary>
public sealed class EquipmentBalance
{
    public const int ModelVersion = 2;
    public const int MaximumRank = 5;
    public const double MinimumSupportedBasicAttackIntervalMultiplier = 0.75d;
    public const int StatUnitVersion = EquipmentStatBudgetCatalog.BalanceVersion;

    public EquipmentBalance(
        int version,
        double baseTierBudget = 100d,
        double styleBudgetShare = 0.15d,
        double rankBudgetIncrement = 0.04d)
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
    }

    public int Version { get; }
    public double BaseTierBudget { get; }
    public double StyleBudgetShare { get; }
    public double RankBudgetIncrement { get; }

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
