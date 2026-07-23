using Application.Interfaces.Services.LL.Professions;
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

        var previousRarity = equipment.Rarity;
        var previousQuality = equipment.Quality;
        var outcome = RollOutcome(previousRarity, rng, negativeOutcomeReductionBps);
        var qualityIncreased = false;
        var rarityUpgraded = false;
        Domain.Models.Attributes.AttributeType? improvedStat = null;
        float? previousValue = null;
        float? newValue = null;

        if (outcome is TemperingOutcome.Positive or TemperingOutcome.Critical)
        {
            var improvement = TryApplyDirectedImprovement(equipment, profile, rng);
            if (improvement is null)
            {
                outcome = TemperingOutcome.Neutral;
            }
            else
            {
                improvedStat = improvement.Stat;
                previousValue = improvement.PreviousValue;
                newValue = improvement.NewValue;
                equipment.TemperingProgress++;
                equipment.ItemXp++;
                rarityUpgraded = ApplyRarityProgress(equipment);

                if (outcome == TemperingOutcome.Critical)
                    qualityIncreased = HandleCriticalOutcome(equipment, previousQuality, rng);
            }
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
            newValue);
    }

    private static DirectedImprovement? TryApplyDirectedImprovement(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng)
    {
        var currentByStat = equipment.InstanceModifiers
            .GroupBy(modifier => modifier.AttributeType)
            .ToDictionary(group => group.Key, group => group.Sum(modifier => modifier.Amount));
        var budgetByStat = currentByStat.ToDictionary(
            pair => pair.Key,
            pair => Math.Max(0d, pair.Value) * EquipmentStatBudgetCatalog.Get(pair.Key).CostPerPoint);
        var totalBudget = Math.Max(1d, budgetByStat.Values.Sum());
        var totalProfileWeight = profile.Stats.Sum(stat => Math.Max(0d, stat.Weight));

        var candidates = profile.Stats
            .Select(stat =>
            {
                var exists = currentByStat.TryGetValue(stat.Stat, out var currentValue);
                var rule = EquipmentStatBudgetCatalog.Get(stat.Stat);
                var currentBudget = budgetByStat.GetValueOrDefault(stat.Stat);
                var currentShare = currentBudget / totalBudget;
                var targetShare = stat.Weight / totalProfileWeight;
                var cap = stat.MaxBudgetShare ?? 1d;

                if ((!exists && !stat.CanIntroduce) ||
                    (exists && !stat.CanIncrease) ||
                    stat.MinimumTier > equipment.Tier ||
                    currentValue >= rule.HardCap ||
                    currentShare >= cap)
                {
                    return null;
                }

                var deficitMultiplier = 1d + (Math.Max(targetShare - currentShare, 0d) * 4d);
                var continuationMultiplier = exists ? 1.15d : 1d;
                var categoryMultiplier = stat.Category == TemperingStatCategory.Primary ? 1.25d : 1d;
                var capMultiplier = Math.Max(0.05d, 1d - (currentShare / cap));
                var effectiveWeight = stat.Weight * deficitMultiplier * continuationMultiplier *
                                      categoryMultiplier * capMultiplier;
                return new WeightedCandidate(stat, effectiveWeight);
            })
            .Where(candidate => candidate is not null && candidate.EffectiveWeight > 0)
            .Select(candidate => candidate!)
            .ToList();

        if (candidates.Count == 0)
            return null;

        var selected = PickWeighted(candidates, candidate => candidate.EffectiveWeight, rng);
        var selectedRule = EquipmentStatBudgetCatalog.Get(selected.Definition.Stat);
        var previous = currentByStat.GetValueOrDefault(selected.Definition.Stat);
        var rollBudget = Math.Max(1d, equipment.Tier * 2d);
        var increase = (float)Math.Max(1d, Math.Round(rollBudget / selectedRule.CostPerPoint));
        increase = Math.Min(increase, selectedRule.HardCap - previous);
        if (increase <= 0)
            return null;

        var existingModifier = equipment.InstanceModifiers
            .FirstOrDefault(modifier => modifier.AttributeType == selected.Definition.Stat);
        if (existingModifier == null)
        {
            equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
                selected.Definition.Stat,
                increase));
        }
        else
        {
            existingModifier.Amount += increase;
        }

        return new DirectedImprovement(selected.Definition.Stat, previous, previous + increase);
    }

    private bool HandleCriticalOutcome(EquipmentInstance equipment, ItemQuality previousQuality, Random rng)
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

    private TemperingOutcome RollOutcome(Rarity rarity, Random rng, double neutralOutcomeReductionBps)
    {
        var rarityIndex = (int)rarity;
        var criticalChance = Math.Clamp(
            _options.CriticalChanceBase + (_options.CriticalChancePerRarityStep * rarityIndex),
            0d,
            1d);
        var neutralChance = 0.05d.ReduceChanceByPercentagePointBps(neutralOutcomeReductionBps);
        var roll = rng.NextDouble();
        if (roll < criticalChance)
            return TemperingOutcome.Critical;
        return roll < criticalChance + neutralChance
            ? TemperingOutcome.Neutral
            : TemperingOutcome.Positive;
    }

    private static bool ApplyRarityProgress(EquipmentInstance equipment)
    {
        var upgraded = false;
        while (equipment.ItemXp >= XpPerRarity && equipment.Rarity < Rarity.Legacy)
        {
            equipment.ItemXp -= XpPerRarity;
            equipment.Rarity++;
            upgraded = true;
        }

        return upgraded;
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
        foreach (var modifier in equipment.InstanceModifiers)
        {
            var hardCap = EquipmentStatBudgetCatalog.Get(modifier.AttributeType).HardCap;
            modifier.Amount = (float)Math.Min(
                hardCap,
                Math.Max(1d, Math.Round(modifier.Amount * ratio)));
        }
    }

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
}
