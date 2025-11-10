using Domain.Models.CharacterActions.Sessions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.TierPackages;
using Domain.Models.Professions.Crafting;
using Services.LL.Interfaces;

namespace Services.LL.Professions.Craftings;
public class TemperingService : ITemperingService
{
    private readonly ITierPackageProvider _packageProvider;

    public TemperingService(ITierPackageProvider packageProvider)
    {
        _packageProvider = packageProvider;
    }

    public void HandleTempering(CraftingQueueItem current, TemperingSummary temperingSummary, Random rng, Dictionary<TemperingOutcome, double> temperingBonuses)
    {
        temperingBonuses.TryGetValue(TemperingOutcome.Positive, out var doubleItemExpChance);
        temperingBonuses.TryGetValue(TemperingOutcome.Negative, out var craftingNegativeOutcome);
        var outcome = RollOutcome(current.EquipmentInstance.Rarity, rng, craftingNegativeOutcome);

        switch (outcome)
        {
            case TemperingOutcome.Critical:
                HandleCriticalOutcome(current.EquipmentInstance, temperingSummary, rng);
                break;

            case TemperingOutcome.Positive:
                HandlePositiveOutcome(current, rng, doubleItemExpChance);
                break;

            case TemperingOutcome.Negative:
                HandleNegativeOutcome(current, rng);
                break;

            case TemperingOutcome.Neutral:
            default:
                // nothing happens
                break;
        }

        var experience = outcome switch
        {
            TemperingOutcome.Critical => 100,
            _ => 1,
        };

        AllocateExpBasedOnCraftingProfession(temperingSummary, experience, current.CraftType);
    }

    private void HandlePositiveOutcome(CraftingQueueItem current, Random rng, double doubleItemExpChance)
    {
        var experience = 1;
        if (rng.NextDouble() < (doubleItemExpChance / 100)) experience *= 2;

        current.EquipmentInstance.ItemXp += experience;

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

    private static void HandleCriticalOutcome(EquipmentInstance eq, TemperingSummary temperingSummary, Random rng)
    {
        // 90 % → Masterpiece, 10 % → Leveling Item
        if (rng.NextDouble() < 0.9)
        {
            eq.IsMasterpiece = true;
            eq.IsLevelingItem = false;
            temperingSummary.Masterpieces++;
        }
        else
        {
            eq.IsLevelingItem = true;
            eq.IsMasterpiece = false;
            temperingSummary.LevelingItems++;
        }
    }

    private void TryUpgradeRarity(EquipmentInstance eq)
    {
        const int XpPerTier = 10;

        while (eq.ItemXp >= XpPerTier && eq.Rarity < Rarity.Legacy)
        {
            eq.ItemXp -= XpPerTier;
            eq.Rarity = eq.Rarity + 1;        // next tier
            ApplyTierPackage(eq);              // stats / sockets / visuals / etc.
        }
    }

    private void ApplyTierPackage(EquipmentInstance eq)
    {
        var tierPackage = _packageProvider.GetPackage(eq.Rarity);
        if (tierPackage == null) return;
        // Apply the tier package modifiers to the equipment instance
        tierPackage.AttributeModifier.ItemInstanceId = eq.Id;
        tierPackage.AttributeModifier.ItemInstance = null;
        eq.InstanceModifiers.Add(tierPackage.AttributeModifier);
    }



    private static TemperingOutcome RollOutcome(Rarity rarity, Random rng, double craftingNegativeOutcome)
    {
        /* ---------------- probability tables ----------------
           • Critical  : 0.0005 % doubled per rarity step
           • Negative  : 5 % base  +5 % per rarity step
           • Positive  : See PositiveChance()
           • Neutral   : remainder
        ----------------------------------------------------- */

        int rarityIndex = (int)rarity; // Common = 0 … Legacy = 6

        double pCritical = 0.00005 * rarityIndex;  // 0.005 % → 0.035 %
        double pNegative = (0.05 + 0.05 * rarityIndex) + (craftingNegativeOutcome / 100); // (5 + -10 = -5) // 5% → 35%
        double pPositive = PositiveChance(rarity);

        double roll = rng.NextDouble();
        if (roll < pCritical) return TemperingOutcome.Critical;
        roll -= pCritical;

        if (roll < pPositive) return TemperingOutcome.Positive;
        roll -= pPositive;

        if (roll < pNegative) return TemperingOutcome.Negative;
        return TemperingOutcome.Neutral;
    }

    private static void AllocateExpBasedOnCraftingProfession(TemperingSummary temperingSummary, int experience, CraftType craftType)
    {
        switch (craftType)
        {
            case CraftType.ArmorForging:
                temperingSummary.ArmorForgingExperience += experience;
                break;
            case CraftType.JewelryCrafting:
                temperingSummary.JewelryCraftingExperience += experience;
                break;
            case CraftType.WeaponSmithing:
                temperingSummary.WeaponSmithingExperience += experience;
                break;
            default:
                break;
        }
    }
}