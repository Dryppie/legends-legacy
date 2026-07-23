using Domain.Models.Attributes;
using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface IItemStatRollService
{
    IReadOnlyList<InstanceAttributeModifier> RollBaseStats(
        EquipmentBase equipment,
        EquipmentCraftingDesign design,
        int targetTier,
        ItemQuality quality,
        Random rng);

    IReadOnlyList<CraftedAttributeRange> GetBaseStatRanges(
        EquipmentBase equipment,
        EquipmentCraftingDesign design,
        int targetTier,
        IReadOnlyCollection<ItemQuality> possibleQualities);
}

public sealed record CraftedAttributeRange(
    AttributeType AttributeType,
    float MinimumAmount,
    float MaximumAmount);
