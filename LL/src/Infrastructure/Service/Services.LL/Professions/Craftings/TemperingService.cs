using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;
public class TemperingService : ITemperingService
{
    public TemperingResult HandleTempering(CraftingQueueItem current, Random rng)
    {
        var temperingResult = new TemperingResult();
        var outcome = RollOutcome(current.EquipmentInstance.ItemBase.Rarity, rng);

        switch (outcome)
        {
            case TemperingOutcome.Critical:
                HandleCriticalOutcome(current.EquipmentInstance, rng);
                break;

            case TemperingOutcome.Positive:
                HandlePositiveOutcome(current);
                break;

            case TemperingOutcome.Negative:
                HandleNegativeOutcome(current, rng);

                break;

            case TemperingOutcome.Neutral:
            default:
                // literally nothing happens
                break;
        }

        temperingResult.ExperienceGained = outcome switch
        {
            TemperingOutcome.Critical => 100,
            _ => 1,
        };

        return temperingResult;
    }

    private static void HandlePositiveOutcome(CraftingQueueItem current)
    {
        current.EquipmentInstance.ItemXp++;
        TryUpgradeRarity(current.EquipmentInstance);
    }

    private static void HandleNegativeOutcome(CraftingQueueItem current, Random rng)
    {
        // 1 extra point of Potential is consumed (if available) and XP is reduced by one
        if (rng.NextDouble() < 0.8)
        {
            if (current.EquipmentInstance.Potential > 0)
                current.EquipmentInstance.Potential--;
        }
        else
        {
            if (current.EquipmentInstance.ItemXp > 0)
                current.EquipmentInstance.ItemXp--;
        }
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

    private static void HandleCriticalOutcome(EquipmentInstance eq, Random rng)
    {
        // 90 % → Masterpiece, 10 % → Leveling Item
        if (rng.NextDouble() < 0.9)
        {
            eq.IsMasterpiece = true;
            eq.IsLevelingItem = false;
        }
        else
        {
            eq.IsLevelingItem = true;
            eq.IsMasterpiece = false;
        }
    }

    private static void TryUpgradeRarity(EquipmentInstance eq)
    {
        const int XpPerTier = 10;

        while (eq.ItemXp >= XpPerTier && eq.ItemBase.Rarity < Rarity.Legacy)
        {
            eq.ItemXp -= XpPerTier;
            eq.ItemBase.Rarity = eq.ItemBase.Rarity + 1;        // next tier
            ApplyTierPackage(eq);              // stats / sockets / visuals / etc.
        }
    }

    /// <summary>Everything in §2 of your design doc (stats, visuals, sockets, …)</summary>
    private static void ApplyTierPackage(EquipmentInstance eq)
    {
        // Implement the details in one place so it can be reused from everywhere.
    }



    private static TemperingOutcome RollOutcome(Rarity rarity, Random rng)
    {
        /* ---------------- probability tables ----------------
           • Critical  : 0.0005 % doubled per rarity step
           • Negative  : 5 % base  +5 % per rarity step
           • Positive  : See PositiveChance()
           • Neutral   : remainder
        ----------------------------------------------------- */

        int rarityIndex = (int)rarity;                 // Common = 0 … Legacy = 6

        double pCritical = 0.00005 * rarityIndex;  // 0.005 % → 0.035 %
        double pNegative = 0.05 + 0.05 * rarityIndex;        // 5 %   → 35 %
        double pPositive = PositiveChance(rarity);

        double roll = rng.NextDouble();
        if (roll < pCritical) return TemperingOutcome.Critical;
        roll -= pCritical;

        if (roll < pPositive) return TemperingOutcome.Positive;
        roll -= pPositive;

        if (roll < pNegative) return TemperingOutcome.Negative;
        return TemperingOutcome.Neutral;
    }
}