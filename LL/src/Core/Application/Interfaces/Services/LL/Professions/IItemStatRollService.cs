using Domain.Models.Attributes.Modifiers;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface IItemStatRollService
{
    IReadOnlyList<InstanceAttributeModifier> RollBaseStats(CraftingRecipeDefinition recipe, int targetTier, ItemQuality quality, Random rng);
}
