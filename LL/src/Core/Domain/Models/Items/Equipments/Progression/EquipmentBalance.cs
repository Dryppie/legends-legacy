using Domain.Models.Professions.Crafting.V2;

namespace Domain.Models.Items.Equipments.Progression;

/// <summary>
/// Immutable balance inputs. A changed configuration requires a new version;
/// historical versions must remain resolvable by the eventual catalog provider.
/// Tier scaling and stat exchange rates deliberately remain on v17 together.
/// </summary>
public sealed class EquipmentBalance
{
    public const int ModelVersion = 1;
    public const int MaximumRank = 5;
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
        if (!Enum.IsDefined(type) || type == EquipmentType.Tool)
            throw new ArgumentOutOfRangeException(nameof(type));
        // Preserve current combined hand budgets: 2H = 1H + offhand = two 1H.
        var budget = BaseTierBudget * EquipmentTierBudgetCurve.GetScale(tier)
            * (type == EquipmentType.TwoHanded ? 2d : 1d);
        EquipmentValidation.PositiveFinite(budget);
        return budget;
    }
}
