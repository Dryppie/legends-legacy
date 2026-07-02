using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Services.LL.Professions.Craftings;

public sealed class CraftingBalanceOptions
{
    public double QualityIncreaseChanceOnTemper { get; set; } = 0.0005d;

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
        [EquipmentType.TwoHanded] = 1.40d,
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

    public double GetQualityStatMultiplier(ItemQuality quality) =>
        QualityStatMultipliers.TryGetValue(quality, out var multiplier) && multiplier > 0
            ? multiplier
            : 1d;
}
