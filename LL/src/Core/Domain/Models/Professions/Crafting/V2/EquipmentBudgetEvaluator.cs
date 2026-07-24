using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentBudgetEvaluator
{
    public const int BalanceVersion = 1;

    public static double Evaluate(IEnumerable<AttributeModifierBase> modifiers) =>
        Math.Round(
            modifiers
                .Where(modifier => modifier.Amount > 0)
                .Sum(modifier =>
                    modifier.Amount
                    * EquipmentStatBudgetCatalog.Get(modifier.AttributeType).CostPerPoint),
            2,
            MidpointRounding.AwayFromZero);

    public static IReadOnlyDictionary<AttributeType, double> EvaluateByAttribute(
        IEnumerable<AttributeModifierBase> modifiers) =>
        modifiers
            .Where(modifier => modifier.Amount > 0)
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(
                group => group.Key,
                group => Math.Round(
                    group.Sum(modifier =>
                        modifier.Amount
                        * EquipmentStatBudgetCatalog.Get(modifier.AttributeType).CostPerPoint),
                    2,
                    MidpointRounding.AwayFromZero));
}
