using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentBudgetAllocator
{
    private const double Epsilon = 0.000001d;

    public static EquipmentBudgetAllocation Allocate(
        int tier,
        double budget,
        IReadOnlyDictionary<AttributeType, double> weights,
        IReadOnlyDictionary<AttributeType, double>? currentPoints = null,
        bool roundToWholePoints = true)
    {
        var targetBudget = Math.Max(0d, budget);
        var normalizedWeights = weights
            .Where(x => x.Value > 0 && EquipmentStatBudgetCatalog.IsKnown(x.Key))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);
        if (targetBudget <= Epsilon || normalizedWeights.Count == 0)
            return EquipmentBudgetAllocation.Empty(targetBudget);

        var allocatedBudget = normalizedWeights.Keys.ToDictionary(attribute => attribute, _ => 0d);
        var remainingCapacityBudget = normalizedWeights.Keys.ToDictionary(
            attribute => attribute,
            attribute =>
            {
                var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
                var current = Math.Clamp(currentPoints?.GetValueOrDefault(attribute) ?? 0d, 0d, rule.PerItemHardCap);
                return Math.Max(0d, rule.PerItemHardCap - current) * rule.CostPerPoint;
            });
        var active = normalizedWeights.Keys
            .Where(attribute => remainingCapacityBudget[attribute] > Epsilon)
            .ToList();
        var cappedAttributes = normalizedWeights.Keys
            .Where(attribute => remainingCapacityBudget[attribute] <= Epsilon)
            .ToHashSet();
        var remainingBudget = targetBudget;

        while (remainingBudget > Epsilon && active.Count > 0)
        {
            var activeWeight = active.Sum(attribute => normalizedWeights[attribute]);
            if (activeWeight <= Epsilon)
                break;

            var newlyCapped = active
                .Where(attribute =>
                    remainingBudget * normalizedWeights[attribute] / activeWeight
                    >= remainingCapacityBudget[attribute] - Epsilon)
                .ToList();
            if (newlyCapped.Count == 0)
            {
                foreach (var attribute in active)
                {
                    var share = remainingBudget * normalizedWeights[attribute] / activeWeight;
                    allocatedBudget[attribute] += share;
                }

                remainingBudget = 0;
                break;
            }

            foreach (var attribute in newlyCapped)
            {
                var capacity = remainingCapacityBudget[attribute];
                allocatedBudget[attribute] += capacity;
                remainingBudget = Math.Max(0d, remainingBudget - capacity);
                cappedAttributes.Add(attribute);
                active.Remove(attribute);
            }
        }

        var addedPoints = new Dictionary<AttributeType, double>();
        foreach (var attribute in normalizedWeights.Keys)
        {
            var rule = EquipmentStatBudgetCatalog.Get(attribute, tier);
            var current = Math.Clamp(currentPoints?.GetValueOrDefault(attribute) ?? 0d, 0d, rule.PerItemHardCap);
            var points = allocatedBudget[attribute] / rule.CostPerPoint;
            if (roundToWholePoints && points > Epsilon)
                points = Math.Max(1d, Math.Round(points, MidpointRounding.AwayFromZero));
            points = Math.Clamp(points, 0d, rule.PerItemHardCap - current);
            if (points > Epsilon)
                addedPoints[attribute] = points;
        }

        var spentBudget = addedPoints.Sum(x =>
            x.Value * EquipmentStatBudgetCatalog.Get(x.Key, tier).CostPerPoint);
        return new EquipmentBudgetAllocation(
            targetBudget,
            spentBudget,
            Math.Max(0d, targetBudget - spentBudget),
            addedPoints,
            cappedAttributes.Order().ToList());
    }
}

public sealed record EquipmentBudgetAllocation(
    double TargetBudget,
    double SpentBudget,
    double UnspentBudget,
    IReadOnlyDictionary<AttributeType, double> AddedPoints,
    IReadOnlyList<AttributeType> CappedAttributes)
{
    public static EquipmentBudgetAllocation Empty(double targetBudget) =>
        new(targetBudget, 0d, Math.Max(0d, targetBudget), new Dictionary<AttributeType, double>(), []);
}
