using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface IItemStatRollService
{
    IReadOnlyList<InstanceAttributeModifier> RollBaseStats(EquipmentBase equipment, CraftingRecipeDefinition recipe, int targetTier, ItemQuality quality, Random rng);
}
