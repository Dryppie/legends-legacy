using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;
using Services.LL.Extensions;

namespace Services.LL.Professions.Craftings;

public sealed class TemperingMechanicsService : ITemperingMechanicsService
{
    private const int XpPerRarity = 10;
    private readonly CraftingBalanceOptions _options;

    public TemperingMechanicsService(IOptions<CraftingBalanceOptions>? options = null)
    {
        _options = options?.Value ?? new CraftingBalanceOptions();
    }

    public TemperingAttemptResult ApplyTemperingAttempt(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng,
        double negativeOutcomeReductionBps = 0)
    {
        if ((equipment.Potential ?? 0) < TemperingConstants.PotentialCost)
            throw new InvalidOperationException("Equipment does not have enough Potential.");

        // Tempering is the explicit write boundary that upgrades legacy crafted
        // items. Read-only combat continues to interpret v15 items unchanged.
        EquipmentStatModelMigrator.MigrateToCurrent(equipment);
        QuantizeInstanceModifiers(equipment);

        var previousPotential = equipment.Potential ?? 0;
        var previousItemXp = equipment.ItemXp;
        var wasMasterpiece = equipment.IsMasterpiece;
        var wasLevelingItem = equipment.IsLevelingItem;
        var previousRarity = equipment.Rarity;
        var previousQuality = equipment.Quality;
        var outcome = RollOutcome(previousRarity, rng, negativeOutcomeReductionBps);
        var qualityIncreased = false;
        var rarityUpgraded = false;
        Domain.Models.Attributes.AttributeType? improvedStat = null;
        float? previousValue = null;
        float? newValue = null;

        switch (outcome)
        {
            case TemperingOutcome.Critical:
                qualityIncreased = HandleCriticalOutcome(equipment, previousQuality, rng);
                break;

            case TemperingOutcome.Positive:
                equipment.ItemXp++;
                var rarityResult = ApplyRarityProgress(equipment, profile, rng);
                rarityUpgraded = rarityResult.Upgraded;
                improvedStat = rarityResult.Improvement?.Stat;
                previousValue = rarityResult.Improvement?.PreviousValue;
                newValue = rarityResult.Improvement?.NewValue;
                break;

            case TemperingOutcome.Negative:
                HandleNegativeOutcome(equipment, rng);
                break;

            case TemperingOutcome.Neutral:
            default:
                break;
        }

        equipment.Potential -= TemperingConstants.PotentialCost;
        return new TemperingAttemptResult(
            equipment,
            outcome,
            TemperingConstants.PotentialCost,
            previousRarity,
            equipment.Rarity,
            rarityUpgraded,
            qualityIncreased,
            qualityIncreased ? previousQuality : null,
            qualityIncreased ? equipment.Quality : null,
            improvedStat,
            previousValue,
            newValue,
            previousPotential,
            equipment.Potential ?? 0,
            previousItemXp,
            equipment.ItemXp,
            !wasMasterpiece && equipment.IsMasterpiece,
            !wasLevelingItem && equipment.IsLevelingItem);
    }

    private static void HandleNegativeOutcome(EquipmentInstance equipment, Random rng)
    {
        if (rng.NextDouble() < 0.8)
        {
            if (equipment.Potential > 0)
                equipment.Potential--;
        }
        else if (equipment.ItemXp > 0)
        {
            equipment.ItemXp--;
        }
    }

    private DirectedImprovement? TryApplyDirectedImprovement(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng)
    {
        var currentByStat = equipment.InstanceModifiers
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(group => group.Key, group => group.Sum(modifier => modifier.Amount));
        var allCurrentPoints = GetCurrentEquipmentPoints(equipment);
        var slotWeight = _options.GetSlotBudgetWeight(equipment.EquipmentBase.EquipmentType);
        var constraints = CreateItemConstraints(equipment, slotWeight);
        var perItemCapMultiplier =
            EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeight)
            * EquipmentConstraintProfile.RarityImprovementCapMultiplier;
        var budgetByStat = currentByStat.ToDictionary(
            pair => pair.Key,
            pair => Math.Max(0d, pair.Value)
                    * EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(
                        pair.Key,
                        equipment.Tier));
        var totalBudget = Math.Max(1d, budgetByStat.Values.Sum());
        var candidates = CreateCandidates(profile.Stats);
        if (candidates.Count == 0)
        {
            var fallbackStats = EquipmentConstraintProfile
                .GetRarityOverflowWeights(
                    equipment.EquipmentBase.EquipmentType,
                    profile)
                .Where(entry => profile.Stats.All(stat => stat.Stat != entry.Key))
                .Select(entry => new TemperingStatWeightDefinition
                {
                    Stat = entry.Key,
                    Weight = entry.Value,
                    Category = TemperingStatCategory.Secondary,
                    CanIntroduce = true,
                    CanIncrease = true,
                    MaxBudgetShare = 1d,
                    MinimumTier = 1
                })
                .ToList();
            candidates = CreateCandidates(fallbackStats);
        }

        if (candidates.Count == 0)
            return null;

        var selected = PickWeighted(candidates, candidate => candidate.EffectiveWeight, rng);
        var previous = currentByStat.GetValueOrDefault(selected.Definition.Stat);
        var rollBudget = TemperingConstants.GetDirectedImprovementBudget(equipment.Tier)
            * slotWeight
            * _options.GetQualityStatMultiplier(equipment.Quality);
        var materializedCost = EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(
            selected.Definition.Stat,
            equipment.Tier);
        var rawIncrease = rollBudget / materializedCost;
        float increase = EquipmentStatBudgetCatalog.IsDirectPercentage(selected.Definition.Stat)
            ? (float)AttributeValueQuantizer.Quantize(selected.Definition.Stat, rawIncrease)
            : (float)Math.Max(1d, Math.Round(rawIncrease, MidpointRounding.AwayFromZero));
        increase = Math.Min(
            increase,
            (float)EquipmentConstraintProfile.GetMaximumAdditionalPoints(
                selected.Definition.Stat,
                equipment.Tier,
                allCurrentPoints,
                constraints,
                perItemCapMultiplier));
        if (increase <= 0)
            return null;

        var existingModifier = equipment.InstanceModifiers
            .FirstOrDefault(modifier => modifier.AttributeType == selected.Definition.Stat);
        if (existingModifier == null)
        {
            equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
                selected.Definition.Stat,
                AttributeValueQuantizer.Quantize(selected.Definition.Stat, increase)));
        }
        else
        {
            existingModifier.Amount = AttributeValueQuantizer.Quantize(
                selected.Definition.Stat,
                existingModifier.Amount + increase);
        }

        var updated = equipment.InstanceModifiers
            .Where(modifier => modifier.AttributeType == selected.Definition.Stat)
            .Sum(modifier => modifier.Amount);
        return new DirectedImprovement(selected.Definition.Stat, previous, updated);

        List<WeightedCandidate> CreateCandidates(
            IReadOnlyList<TemperingStatWeightDefinition> definitions)
        {
            var totalWeight = definitions.Sum(stat => Math.Max(0d, stat.Weight));
            if (totalWeight <= 0)
                return [];

            return definitions
                .Select(stat =>
                {
                    var exists = currentByStat.ContainsKey(stat.Stat);
                    var currentBudget = budgetByStat.GetValueOrDefault(stat.Stat);
                    var currentShare = currentBudget / totalBudget;
                    var targetShare = stat.Weight / totalWeight;
                    var cap = stat.MaxBudgetShare ?? 1d;
                    var maximumIncrease =
                        EquipmentConstraintProfile.GetMaximumAdditionalPoints(
                            stat.Stat,
                            equipment.Tier,
                            allCurrentPoints,
                            constraints,
                            perItemCapMultiplier);
                    var currentValue = (double)currentByStat.GetValueOrDefault(stat.Stat);
                    var quantizedCurrent = AttributeValueQuantizer.Quantize(
                        stat.Stat,
                        currentValue);
                    var quantizedMaximum = AttributeValueQuantizer.Quantize(
                        stat.Stat,
                        currentValue + maximumIncrease);

                    if ((!exists && !stat.CanIntroduce) ||
                        (exists && !stat.CanIncrease) ||
                        stat.MinimumTier > equipment.Tier ||
                        maximumIncrease <= 0.000001d ||
                        quantizedMaximum <= quantizedCurrent ||
                        currentShare >= cap)
                    {
                        return null;
                    }

                    var deficitMultiplier =
                        1d + (Math.Max(targetShare - currentShare, 0d) * 4d);
                    var continuationMultiplier = exists ? 1.15d : 1d;
                    var categoryMultiplier =
                        stat.Category == TemperingStatCategory.Primary ? 1.25d : 1d;
                    var capMultiplier = Math.Max(0.05d, 1d - (currentShare / cap));
                    var effectiveWeight =
                        stat.Weight
                        * deficitMultiplier
                        * continuationMultiplier
                        * categoryMultiplier
                        * capMultiplier;
                    return new WeightedCandidate(stat, effectiveWeight);
                })
                .Where(candidate =>
                    candidate is not null && candidate.EffectiveWeight > 0)
                .Select(candidate => candidate!)
                .ToList();
        }
    }

    private bool HandleCriticalOutcome(
        EquipmentInstance equipment,
        ItemQuality previousQuality,
        Random rng)
    {
        if (rng.NextDouble() < Math.Clamp(_options.CriticalLevelingItemChance, 0d, 1d))
        {
            equipment.IsLevelingItem = true;
            equipment.IsMasterpiece = false;
            return false;
        }

        var nextQuality = GetNextQuality(previousQuality);
        if (nextQuality == null)
            return false;

        equipment.Quality = nextQuality.Value;
        ApplyQualityStatMultiplierChange(equipment, previousQuality, nextQuality.Value);
        return true;
    }

    private TemperingOutcome RollOutcome(Rarity rarity, Random rng, double negativeOutcomeReductionBps)
    {
        var rarityIndex = (int)rarity;
        var criticalChance = Math.Clamp(
            _options.CriticalChanceBase + (_options.CriticalChancePerRarityStep * rarityIndex),
            0d,
            1d);
        var negativeChance = (0.05d + (0.05d * rarityIndex))
            .ReduceChanceByPercentagePointBps(negativeOutcomeReductionBps);
        var positiveChance = PositiveChance(rarity);
        var roll = rng.NextDouble();

        if (roll < criticalChance)
            return TemperingOutcome.Critical;
        roll -= criticalChance;

        if (roll < positiveChance)
            return TemperingOutcome.Positive;
        roll -= positiveChance;

        return roll < negativeChance
            ? TemperingOutcome.Negative
            : TemperingOutcome.Neutral;
    }

    private static double PositiveChance(Rarity rarity) =>
        rarity switch
        {
            Rarity.Common => 0.06d,
            Rarity.Uncommon => 0.03d,
            Rarity.Rare => 0.015d,
            Rarity.Epic => 0.005d,
            Rarity.Unique => 0.001d,
            _ => 0d
        };

    private RarityProgressResult ApplyRarityProgress(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng)
    {
        var upgraded = false;
        DirectedImprovement? improvement = null;

        while (equipment.ItemXp >= XpPerRarity && equipment.Rarity < Rarity.Legacy)
        {
            equipment.ItemXp -= XpPerRarity;
            equipment.Rarity++;
            improvement = TryApplyDirectedImprovement(equipment, profile, rng) ?? improvement;
            upgraded = true;
        }

        return new RarityProgressResult(upgraded, improvement);
    }

    private void ApplyQualityStatMultiplierChange(
        EquipmentInstance equipment,
        ItemQuality previousQuality,
        ItemQuality newQuality)
    {
        var previousMultiplier = _options.GetQualityStatMultiplier(previousQuality);
        var newMultiplier = _options.GetQualityStatMultiplier(newQuality);
        if (previousMultiplier <= 0 || newMultiplier <= 0)
            return;

        var ratio = newMultiplier / previousMultiplier;
        if (ratio <= 1d || equipment.InstanceModifiers.Count == 0)
            return;

        var currentPoints = equipment.InstanceModifiers
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(group => group.Key, group => (double)group.Sum(modifier => modifier.Amount));
        var currentBudgetWeights = currentPoints.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                    * EquipmentStatBudgetCatalog.GetMaterializedCostPerPoint(
                        pair.Key,
                        equipment.Tier));
        var currentBudget = currentBudgetWeights.Values.Sum();
        var allCurrentPoints = GetCurrentEquipmentPoints(equipment);
        var slotWeight = _options.GetSlotBudgetWeight(equipment.EquipmentBase.EquipmentType);
        var allocation = EquipmentBudgetAllocator.AllocateConstrained(
            equipment.Tier,
            currentBudget * (ratio - 1d),
            currentBudgetWeights,
            CreateItemConstraints(equipment, slotWeight),
            currentBudgetWeights,
            allCurrentPoints,
            EquipmentConstraintProfile.GetPerItemCapMultiplier(slotWeight));

        foreach (var (attribute, addedPoints) in allocation.AddedPoints)
        {
            var modifier = equipment.InstanceModifiers
                .First(x => x.AttributeType == attribute);
            modifier.Amount = AttributeValueQuantizer.Quantize(
                attribute,
                modifier.Amount + (float)addedPoints);
        }
    }

    private static void QuantizeInstanceModifiers(EquipmentInstance equipment)
    {
        foreach (var modifier in equipment.InstanceModifiers)
        {
            modifier.Amount = AttributeValueQuantizer.Quantize(
                modifier.AttributeType,
                modifier.Amount);
        }
    }

    private IReadOnlyList<EquipmentLinearBudgetConstraint> CreateItemConstraints(
        EquipmentInstance equipment,
        double slotWeight) =>
        EquipmentConstraintProfile.CreateItemConstraints(
            EquipmentConstraintProfile.CreateTierBaseline(equipment.Tier),
            equipment.Tier,
            slotWeight,
            _options.GetMaximumCombatLoadoutBudgetWeight(),
            EquipmentConstraintProfile.MinimumSupportedBasicAttackIntervalMultiplier);

    private static Dictionary<Domain.Models.Attributes.AttributeType, double>
        GetCurrentEquipmentPoints(EquipmentInstance equipment) =>
        equipment.AttributeModifiers
            .Where(modifier => modifier.ModifierType == ModifierType.Flat)
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(
                group => group.Key,
                group => (double)group.Sum(modifier => modifier.Amount));

    private static ItemQuality? GetNextQuality(ItemQuality current)
    {
        var qualities = Enum.GetValues<ItemQuality>().OrderBy(x => x).ToArray();
        var index = Array.IndexOf(qualities, current);
        return index >= 0 && index < qualities.Length - 1
            ? qualities[index + 1]
            : null;
    }

    private static T PickWeighted<T>(IReadOnlyList<T> items, Func<T, double> weightSelector, Random rng)
    {
        var totalWeight = items.Sum(item => Math.Max(0d, weightSelector(item)));
        var roll = rng.NextDouble() * totalWeight;
        foreach (var item in items)
        {
            roll -= Math.Max(0d, weightSelector(item));
            if (roll <= 0)
                return item;
        }

        return items[^1];
    }

    private sealed record WeightedCandidate(
        TemperingStatWeightDefinition Definition,
        double EffectiveWeight);

    private sealed record DirectedImprovement(
        Domain.Models.Attributes.AttributeType Stat,
        float PreviousValue,
        float NewValue);

    private sealed record RarityProgressResult(
        bool Upgraded,
        DirectedImprovement? Improvement);
}
