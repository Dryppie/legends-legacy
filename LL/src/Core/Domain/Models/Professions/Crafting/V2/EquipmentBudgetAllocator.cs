using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public static class EquipmentBudgetAllocator
{
    private const double Epsilon = 0.000001d;

    public static EquipmentConstrainedBudgetAllocation AllocateDesignConstrained(
        int tier,
        double baseBudget,
        EquipmentCraftingDesign design,
        IReadOnlyList<EquipmentLinearBudgetConstraint> constraints,
        IReadOnlyDictionary<AttributeType, double>? baseOverflowWeights = null,
        IReadOnlyDictionary<AttributeType, double>? currentPoints = null,
        double perItemCapMultiplier = 1d)
    {
        var baseAllocation = AllocateConstrained(
            tier,
            baseBudget,
            design.InitialStatProfile,
            constraints,
            baseOverflowWeights,
            currentPoints,
            perItemCapMultiplier: perItemCapMultiplier);
        var bonusBudget = Math.Max(0d, baseBudget)
            * Math.Max(0d, design.BlueprintBonusBudgetMultiplier);
        if (bonusBudget <= Epsilon || design.BlueprintBonusStatProfile.Count == 0)
            return baseAllocation;

        var pointsBeforeBonus = (currentPoints
                ?? new Dictionary<AttributeType, double>())
            .Concat(baseAllocation.AddedPoints)
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value));
        var bonusAllocation = AllocateConstrained(
            tier,
            bonusBudget,
            design.BlueprintBonusStatProfile,
            constraints,
            EquipmentConstraintProfile.GetOverflowWeights(design),
            currentPoints: pointsBeforeBonus,
            perItemCapMultiplier:
                perItemCapMultiplier
                * EquipmentConstraintProfile.BlueprintBonusCapMultiplier);
        var combinedPoints = baseAllocation.AddedPoints
            .Concat(bonusAllocation.AddedPoints)
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value));

        return new EquipmentConstrainedBudgetAllocation(
            baseAllocation.TargetBudget + bonusAllocation.TargetBudget,
            baseAllocation.SpentBudget + bonusAllocation.SpentBudget,
            baseAllocation.UnspentBudget + bonusAllocation.UnspentBudget,
            combinedPoints,
            baseAllocation.CappedAttributes
                .Concat(bonusAllocation.CappedAttributes)
                .Distinct()
                .Order()
                .ToList(),
            baseAllocation.BindingCombatCaps
                .Concat(bonusAllocation.BindingCombatCaps)
                .Distinct()
                .Order()
                .ToList());
    }

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

    public static EquipmentConstrainedBudgetAllocation AllocateConstrained(
        int tier,
        double budget,
        IReadOnlyDictionary<AttributeType, double> weights,
        IReadOnlyList<EquipmentLinearBudgetConstraint> constraints,
        IReadOnlyDictionary<AttributeType, double>? overflowWeights = null,
        IReadOnlyDictionary<AttributeType, double>? currentPoints = null,
        double perItemCapMultiplier = 1d)
    {
        var targetBudget = Math.Max(0d, budget);
        var normalizedWeights = NormalizeWeights(weights);
        if (targetBudget <= Epsilon || normalizedWeights.Count == 0)
            return EquipmentConstrainedBudgetAllocation.Empty(targetBudget);

        var normalizedOverflowWeights = NormalizeWeights(
            overflowWeights ?? new Dictionary<AttributeType, double>());
        var existingPoints = (currentPoints ?? new Dictionary<AttributeType, double>())
            .Where(x => x.Value > 0 && EquipmentStatBudgetCatalog.IsKnown(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);
        var points = normalizedWeights.Keys
            .Union(normalizedOverflowWeights.Keys)
            .ToDictionary(attribute => attribute, _ => 0d);
        var activeWeights = new Dictionary<AttributeType, double>(normalizedWeights);
        var permanentlyBlocked = new HashSet<AttributeType>();
        var bindingCombatCaps = new HashSet<AttributeType>();
        var cappedAttributes = new HashSet<AttributeType>();
        var remainingBudget = targetBudget;

        for (var iteration = 0;
             iteration < (normalizedWeights.Count + normalizedOverflowWeights.Count) * 2
             && remainingBudget > Epsilon;
             iteration++)
        {
            if (activeWeights.Count == 0)
            {
                foreach (var (attribute, weight) in normalizedOverflowWeights.Where(x =>
                             !permanentlyBlocked.Contains(x.Key)
                             && GetTotalPoints(x.Key) < GetPerItemCap(x.Key) - Epsilon))
                {
                    activeWeights[attribute] = weight;
                }
            }

            var totalWeight = activeWeights.Values.Sum();
            if (totalWeight <= Epsilon)
                break;

            var proposedPoints = activeWeights.ToDictionary(
                entry => entry.Key,
                entry =>
                    remainingBudget
                    * entry.Value
                    / totalWeight
                    / EquipmentStatBudgetCatalog.Get(entry.Key, tier).CostPerPoint);
            var scale = 1d;

            foreach (var (attribute, proposedPointDelta) in proposedPoints)
            {
                if (proposedPointDelta <= Epsilon)
                    continue;

                scale = Math.Min(
                    scale,
                    Math.Max(0d, GetPerItemCap(attribute) - GetTotalPoints(attribute))
                    / proposedPointDelta);
            }

            foreach (var constraint in constraints)
            {
                var currentContribution =
                    existingPoints.Sum(entry =>
                        entry.Value
                        * GetDirectContribution(
                            entry.Key,
                            constraint.EffectiveAttribute))
                    + points.Sum(entry =>
                        entry.Value
                        * GetDirectContribution(
                            entry.Key,
                            constraint.EffectiveAttribute));
                var proposedContribution = proposedPoints.Sum(entry =>
                    entry.Value
                    * GetDirectContribution(
                        entry.Key,
                        constraint.EffectiveAttribute));
                if (proposedContribution <= Epsilon)
                    continue;

                scale = Math.Min(
                    scale,
                    Math.Max(0d, constraint.MaximumAddedValue - currentContribution)
                    / proposedContribution);
            }

            scale = Math.Clamp(scale, 0d, 1d);
            var spentThisIteration = 0d;
            foreach (var (attribute, proposedPointDelta) in proposedPoints)
            {
                var pointIncrement = proposedPointDelta * scale;
                points[attribute] += pointIncrement;
                spentThisIteration +=
                    pointIncrement
                    * EquipmentStatBudgetCatalog.Get(attribute, tier).CostPerPoint;
            }

            remainingBudget = Math.Max(0d, remainingBudget - spentThisIteration);
            if (scale >= 1d - Epsilon)
                break;

            var blockedAttributes = new HashSet<AttributeType>();
            foreach (var attribute in activeWeights.Keys)
            {
                if (GetTotalPoints(attribute) >= GetPerItemCap(attribute) - Epsilon)
                {
                    blockedAttributes.Add(attribute);
                    cappedAttributes.Add(attribute);
                }
            }

            foreach (var constraint in constraints)
            {
                var contribution =
                    existingPoints.Sum(entry =>
                        entry.Value
                        * GetDirectContribution(
                            entry.Key,
                            constraint.EffectiveAttribute))
                    + points.Sum(entry =>
                        entry.Value
                        * GetDirectContribution(
                            entry.Key,
                            constraint.EffectiveAttribute));
                if (contribution < constraint.MaximumAddedValue - Epsilon)
                    continue;

                bindingCombatCaps.Add(constraint.EffectiveAttribute);
                foreach (var attribute in activeWeights.Keys.Where(attribute =>
                             GetDirectContribution(
                                 attribute,
                                 constraint.EffectiveAttribute) > 0))
                {
                    blockedAttributes.Add(attribute);
                }
            }

            if (blockedAttributes.Count == 0)
                break;

            foreach (var attribute in blockedAttributes)
            {
                activeWeights.Remove(attribute);
                permanentlyBlocked.Add(attribute);
            }
        }

        var addedPoints = points
            .Where(x => x.Value > Epsilon)
            .ToDictionary(x => x.Key, x => x.Value);
        var spentBudget = addedPoints.Sum(x =>
            x.Value * EquipmentStatBudgetCatalog.Get(x.Key, tier).CostPerPoint);
        return new EquipmentConstrainedBudgetAllocation(
            targetBudget,
            spentBudget,
            Math.Max(0d, targetBudget - spentBudget),
            addedPoints,
            cappedAttributes.Order().ToList(),
            bindingCombatCaps.Order().ToList());

        double GetTotalPoints(AttributeType attribute) =>
            existingPoints.GetValueOrDefault(attribute)
            + points.GetValueOrDefault(attribute);

        double GetPerItemCap(AttributeType attribute) =>
            EquipmentStatBudgetCatalog.Get(attribute, tier).PerItemHardCap
            * Math.Max(1d, perItemCapMultiplier);
    }

    private static Dictionary<AttributeType, double> NormalizeWeights(
        IReadOnlyDictionary<AttributeType, double> weights) =>
        weights
            .Where(x => x.Value > 0 && EquipmentStatBudgetCatalog.IsKnown(x.Key))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value);

    private static double GetDirectContribution(
        AttributeType source,
        AttributeType target) =>
        source == target ? 1d : 0d;
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

public sealed record EquipmentConstrainedBudgetAllocation(
    double TargetBudget,
    double SpentBudget,
    double UnspentBudget,
    IReadOnlyDictionary<AttributeType, double> AddedPoints,
    IReadOnlyList<AttributeType> CappedAttributes,
    IReadOnlyList<AttributeType> BindingCombatCaps)
{
    public static EquipmentConstrainedBudgetAllocation Empty(double targetBudget) =>
        new(
            targetBudget,
            0d,
            Math.Max(0d, targetBudget),
            new Dictionary<AttributeType, double>(),
            [],
            []);
}
