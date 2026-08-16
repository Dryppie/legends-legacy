using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Professions.Craftings;

public sealed class CraftingBalanceOptions
{
    public double CriticalChanceBase { get; set; } = 0.00001d;
    public double CriticalChancePerRarityStep { get; set; } = 0.00001d;
    public double CriticalLevelingItemChance { get; set; } = 0.02d;

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

    public double GetTierPowerBudget(int tier) => EquipmentTierBudgetCurve.GetBudget(tier);

    public double GetSlotBudgetWeight(EquipmentType equipmentType) =>
        equipmentType == EquipmentType.TwoHanded ? 2d : 1d;

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
