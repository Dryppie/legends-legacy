using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Services.LL.Professions.Craftings;

public sealed class CraftingBalanceOptions
{
    public double CriticalChanceBase { get; set; } = 0.00001d;
    public double CriticalChancePerRarityStep { get; set; } = 0.00001d;
    public double CriticalLevelingItemChance { get; set; } = 0.02d;

    public Dictionary<int, double> TierPowerBudgets { get; set; } = new()
    {
        [1] = 100d,
        [2] = 145d,
        [3] = 205d,
        [4] = 285d,
        [5] = 390d,
        [6] = 525d,
        [7] = 700d,
        [8] = 920d,
        [9] = 1190d,
        [10] = 1520d
    };

    public Dictionary<EquipmentType, double> SlotBudgetWeights { get; set; } = new()
    {
        [EquipmentType.TwoHanded] = 1.70d,
        [EquipmentType.OneHanded] = 0.85d,
        [EquipmentType.OffHand] = 0.65d,
        [EquipmentType.Chest] = 1.15d,
        [EquipmentType.Head] = 0.85d,
        [EquipmentType.Legs] = 0.95d,
        [EquipmentType.Ring] = 0.45d,
        [EquipmentType.Necklace] = 0.60d,
        [EquipmentType.Relic] = 0.75d
    };

    public Dictionary<ItemQuality, double> QualityStatMultipliers { get; set; } = new()
    {
        [ItemQuality.Crude] = 0.90d,
        [ItemQuality.Standard] = 1.00d,
        [ItemQuality.Fine] = 1.04d,
        [ItemQuality.Exceptional] = 1.08d,
        [ItemQuality.Masterwork] = 1.12d
    };

    public Dictionary<EquipmentType, double> PotentialSlotWeights { get; set; } = new()
    {
        [EquipmentType.TwoHanded] = 1.00d,
        [EquipmentType.OneHanded] = 1.00d,
        [EquipmentType.OffHand] = 1.00d,
        [EquipmentType.Chest] = 1.00d,
        [EquipmentType.Head] = 1.00d,
        [EquipmentType.Legs] = 1.00d,
        [EquipmentType.Ring] = 1.00d,
        [EquipmentType.Necklace] = 1.00d,
        [EquipmentType.Relic] = 1.00d
    };

    public Dictionary<ItemQuality, double> PotentialQualityMultipliers { get; set; } = new()
    {
        [ItemQuality.Crude] = 0.75d,
        [ItemQuality.Standard] = 1.00d,
        [ItemQuality.Fine] = 1.15d,
        [ItemQuality.Exceptional] = 1.35d,
        [ItemQuality.Masterwork] = 1.60d
    };

    public double GetTierPowerBudget(int tier)
    {
        var normalizedTier = Math.Clamp(tier, 1, 10);
        if (TierPowerBudgets.TryGetValue(normalizedTier, out var budget) && budget > 0)
            return budget;

        return 100d + ((normalizedTier - 1) * 50d);
    }

    public double GetSlotBudgetWeight(EquipmentType equipmentType) =>
        SlotBudgetWeights.TryGetValue(equipmentType, out var weight) && weight > 0
            ? weight
            : 1d;

    public double GetMaximumCombatLoadoutBudgetWeight()
    {
        var fixedSlots =
            GetSlotBudgetWeight(EquipmentType.Head)
            + GetSlotBudgetWeight(EquipmentType.Chest)
            + GetSlotBudgetWeight(EquipmentType.Legs)
            + GetSlotBudgetWeight(EquipmentType.Ring)
            + GetSlotBudgetWeight(EquipmentType.Necklace)
            + GetSlotBudgetWeight(EquipmentType.Relic);
        var maximumHandConfiguration = Math.Max(
            GetSlotBudgetWeight(EquipmentType.TwoHanded),
            Math.Max(
                GetSlotBudgetWeight(EquipmentType.OneHanded) * 2d,
                GetSlotBudgetWeight(EquipmentType.OneHanded)
                + GetSlotBudgetWeight(EquipmentType.OffHand)));
        return fixedSlots + maximumHandConfiguration;
    }

    public double GetQualityStatMultiplier(ItemQuality quality) =>
        QualityStatMultipliers.TryGetValue(quality, out var multiplier) && multiplier > 0
            ? multiplier
            : 1d;

    public double GetPotentialSlotWeight(EquipmentType equipmentType) =>
        PotentialSlotWeights.TryGetValue(equipmentType, out var weight) && weight > 0
            ? weight
            : 1d;

    public double GetPotentialQualityMultiplier(ItemQuality quality) =>
        PotentialQualityMultipliers.TryGetValue(quality, out var multiplier) && multiplier > 0
            ? multiplier
            : 1d;
}
