using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;

namespace Services.LL.Professions.Craftings;

public class ItemPotentialService : IItemPotentialService
{
    private static readonly IReadOnlyDictionary<ItemQuality, double> QualityMultipliers =
        new Dictionary<ItemQuality, double>
        {
            [ItemQuality.Crude] = 0.75d,
            [ItemQuality.Standard] = 1.00d,
            [ItemQuality.Fine] = 1.15d,
            [ItemQuality.Exceptional] = 1.35d,
            [ItemQuality.Masterwork] = 1.60d
        };

    private static readonly IReadOnlyDictionary<EquipmentType, double> SlotWeights =
        new Dictionary<EquipmentType, double>
        {
            [EquipmentType.OneHanded] = 1.00d,
            [EquipmentType.TwoHanded] = 1.00d,
            [EquipmentType.Chest] = 1.00d,
            [EquipmentType.Legs] = 1.00d,
            [EquipmentType.Head] = 1.00d,
            [EquipmentType.OffHand] = 1.00d,
            [EquipmentType.Ring] = 1.00d,
            [EquipmentType.Necklace] = 1.00d,
            [EquipmentType.Relic] = 1.00d
        };

    public int CalculateStartingPotential(EquipmentBase equipment, int targetTier, ItemQuality quality, int masteryLevel, int craftingLevel)
    {
        var basePotential = 100 + (Math.Max(targetTier, 1) * 100);
        var slotWeight = SlotWeights.GetValueOrDefault(equipment.EquipmentType, 1.0d);
        var qualityMultiplier = QualityMultipliers.GetValueOrDefault(quality, 1.0d);
        var masteryBonus = Math.Clamp(masteryLevel, 0, 100) * 10;
        var craftingLevelMultiplier = 10 * craftingLevel;

        return (int)Math.Round((basePotential * slotWeight * qualityMultiplier) + masteryBonus + craftingLevelMultiplier);
    }
}
