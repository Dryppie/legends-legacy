using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Options;

namespace Services.LL.Professions.Craftings;

public sealed class TemperingMechanicsService : ITemperingMechanicsService
{
    private readonly CraftingBalanceOptions _options;

    public TemperingMechanicsService(IOptions<CraftingBalanceOptions>? options = null)
    {
        _options = options?.Value ?? new CraftingBalanceOptions();
    }

    public TemperingAttemptResult ApplyTemperingAttempt(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng)
    {
        var previousRarity = equipment.Rarity;
        var previousQuality = equipment.Quality;
        var outcome = RollOutcome(previousRarity, rng);
        var upgraded = false;
        var qualityIncreased = false;

        switch (outcome)
        {
            case TemperingOutcome.Critical:
                qualityIncreased = HandleCriticalOutcome(equipment, previousQuality, rng);
                break;

            case TemperingOutcome.Positive:
                upgraded = HandlePositiveOutcome(equipment, profile, rng);
                break;

            case TemperingOutcome.Negative:
                HandleNegativeOutcome(equipment, rng);
                break;

            case TemperingOutcome.Neutral:
            default:
                // nothing happens
                break;
        }

        equipment.Potential -= TemperingConstants.PotentialCost;

        return new TemperingAttemptResult(
            equipment,
            outcome,
            TemperingConstants.PotentialCost,
            previousRarity,
            equipment.Rarity,
            upgraded,
            qualityIncreased,
            qualityIncreased ? previousQuality : null,
            qualityIncreased ? equipment.Quality : null);
    }

    private static bool HandlePositiveOutcome(EquipmentInstance equipment, TemperingProfileDefinition profile, Random rng)
    {
        var experience = 1;

        equipment.ItemXp += experience;

        return TryUpgradeRarity(equipment, profile, rng);
    }

    private static void HandleNegativeOutcome(EquipmentInstance equipment, Random rng)
    {
        // 1 extra point of Potential is consumed (if available) and XP is reduced by one
        if (rng.NextDouble() < 0.8)
        {
            if (equipment.Potential > 0)
                equipment.Potential--;
        }
        else
        {
            if (equipment.ItemXp > 0)
                equipment.ItemXp--;
        }
    }

    private bool HandleCriticalOutcome(EquipmentInstance equipment, ItemQuality previousQuality, Random rng)
    {
        if (rng.NextDouble() < Math.Clamp(_options.CriticalLevelingItemChance, 0d, 1d))
        {
            equipment.IsLevelingItem = true;
            equipment.IsMasterpiece = false;
            return false;
        }

        return TryIncreaseQuality(equipment, previousQuality);
    }

    private TemperingOutcome RollOutcome(Rarity rarity, Random rng)
    {
        /* ---------------- probability tables ----------------
        • Critical  : extremely rare, configurable base + additive rarity step
        • Negative  : 5 % base  +5 % per rarity step
        • Positive  : See PositiveChance()
        • Neutral   : remainder
        ----------------------------------------------------- */

        int rarityIndex = (int)rarity; // Common = 0 … Legacy = 6

        double pCritical = Math.Clamp(
            _options.CriticalChanceBase + (_options.CriticalChancePerRarityStep * rarityIndex),
            0d,
            1d);
        double pNegative = (0.05 + 0.05 * rarityIndex); // (5 + -10 = -5) // 5% → 35%
        double pPositive = PositiveChance(rarity);

        double roll = rng.NextDouble();
        if (roll < pCritical) return TemperingOutcome.Critical;
        roll -= pCritical;

        if (roll < pPositive) return TemperingOutcome.Positive;
        roll -= pPositive;

        if (roll < pNegative) return TemperingOutcome.Negative;
        return TemperingOutcome.Neutral;
    }

    private static double PositiveChance(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => 0.06,
            Rarity.Uncommon => 0.03,
            Rarity.Rare => 0.015,
            Rarity.Epic => 0.005,
            Rarity.Unique => 0.001,
            _ => 0
        };
    }

    private static bool TryUpgradeRarity(EquipmentInstance equipment, TemperingProfileDefinition profile, Random rng)
    {
        const int XpPerTier = 10;
        var upgraded = false;

        while (equipment.ItemXp >= XpPerTier && equipment.Rarity < Rarity.Legacy)
        {
            equipment.ItemXp -= XpPerTier;
            equipment.Rarity = equipment.Rarity + 1;        // next tier
            ApplyRarityUpgradeReward(equipment, profile, rng);
            upgraded = true;
        }

        return upgraded;
    }

    private bool TryIncreaseQuality(EquipmentInstance equipment, ItemQuality previousQuality)
    {
        var nextQuality = GetNextQuality(previousQuality);
        if (nextQuality == null) return false;

        equipment.Quality = nextQuality.Value;
        ApplyQualityStatMultiplierChange(equipment, previousQuality, nextQuality.Value);
        return true;
    }

    private void ApplyQualityStatMultiplierChange(EquipmentInstance equipment, ItemQuality previousQuality, ItemQuality newQuality)
    {
        var previousMultiplier = _options.GetQualityStatMultiplier(previousQuality);
        var newMultiplier = _options.GetQualityStatMultiplier(newQuality);
        if (previousMultiplier <= 0 || newMultiplier <= 0) return;

        var ratio = newMultiplier / previousMultiplier;
        foreach (var modifier in equipment.InstanceModifiers)
        {
            modifier.Amount = (float)Math.Max(1d, Math.Round(modifier.Amount * ratio));
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

    private static void ApplyRarityUpgradeReward(EquipmentInstance equipment, TemperingProfileDefinition profile, Random rng)
    {
        var affix = PickWeighted(
            profile.ResolvedAffixPool.Where(x => equipment.Rarity >= x.MinRarity).ToList(),
            x => x.Weight,
            rng);
        if (affix != null)
        {
            equipment.InstanceModifiers.Add(new InstanceAttributeModifier(
                affix.StatModifier.Stat,
                Math.Max(1, affix.StatModifier.Weight * equipment.Tier),
                ModifierType.Flat));
        }

        var special = PickWeighted(
            profile.ResolvedSpecialModifierPool.Where(x => equipment.Rarity >= x.MinRarity).ToList(),
            x => x.Weight,
            rng);
        if (special != null && equipment.SpecialModifiers.All(x => x != special.Id))
            equipment.SpecialModifiers.Add(special.Id);
    }

    private static T? PickWeighted<T>(IReadOnlyList<T> items, Func<T, int> weightSelector, Random rng)
        where T : class
    {
        var totalWeight = items.Sum(x => Math.Max(0, weightSelector(x)));
        if (totalWeight <= 0) return null;

        var roll = rng.Next(1, totalWeight + 1);
        foreach (var item in items)
        {
            roll -= Math.Max(0, weightSelector(item));
            if (roll <= 0) return item;
        }

        return items.LastOrDefault();
    }
}
