using Application.Interfaces.Services.LL.Professions;
using AutoMapper;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Domain.Models.Professions.Crafting.V2;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Services.LL.Professions.Craftings;

public sealed class TemperingMechanicsService : ITemperingMechanicsService
{
    private readonly ICraftingDefinitionProvider _definitions;

    public TemperingMechanicsService(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public TemperingAttemptResult ApplyTemperingAttempt(
        EquipmentInstance equipment,
        TemperingProfileDefinition profile,
        Random rng)
    {
        var previousRarity = equipment.Rarity;
        var outcome = RollOutcome(previousRarity, rng);

        switch (outcome)
        {
            case TemperingOutcome.Critical:
                HandleCriticalOutcome(equipment, rng);
                break;

            case TemperingOutcome.Positive:
                HandlePositiveOutcome(equipment, profile, rng/*, doubleItemExpChance*/);
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

        var upgraded = false;
        

        return new TemperingAttemptResult(
            equipment,
            outcome,
            TemperingConstants.PotentialCost,
            previousRarity,
            equipment.Rarity,
            upgraded);
    }

    private void HandlePositiveOutcome(EquipmentInstance equipment, TemperingProfileDefinition profile, Random rng/*, double doubleItemExpChance*/)
    {
        var experience = 1;
        //if (rng.NextDouble() < (doubleItemExpChance / 100)) experience *= 2;

        equipment.ItemXp += experience;

        TryUpgradeRarity(equipment, profile, rng);
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

    private static void HandleCriticalOutcome(EquipmentInstance eq, Random rng)
    {
        // 90 % → Masterpiece, 10 % → Leveling Item
        if (rng.NextDouble() < 0.9)
        {
            eq.IsMasterpiece = true;
            eq.IsLevelingItem = false;
            //temperingSummary.Masterpieces++;
        }
        else
        {
            eq.IsLevelingItem = true;
            eq.IsMasterpiece = false;
            //temperingSummary.LevelingItems++;
        }
    }

    private static TemperingOutcome RollOutcome(Rarity rarity, Random rng)
    {
        /* ---------------- probability tables ----------------
        • Critical  : 0.0005 % doubled per rarity step
        • Negative  : 5 % base  +5 % per rarity step
        • Positive  : See PositiveChance()
        • Neutral   : remainder
        ----------------------------------------------------- */

        int rarityIndex = (int)rarity; // Common = 0 … Legacy = 6

        double pCritical = 0.00005 * rarityIndex;  // 0.005 % → 0.035 %
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
            Rarity.Epic => 0.05,
            Rarity.Unique => 0.01,
            _ => 0
        };
    }

    private void TryUpgradeRarity(EquipmentInstance equipment, TemperingProfileDefinition profile, Random rng)
    {
        const int XpPerTier = 10;

        while (equipment.ItemXp >= XpPerTier && equipment.Rarity < Rarity.Legacy)
        {
            equipment.ItemXp -= XpPerTier;
            equipment.Rarity = equipment.Rarity + 1;        // next tier
            ApplyRarityUpgradeReward(equipment, profile, rng);
        }
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
