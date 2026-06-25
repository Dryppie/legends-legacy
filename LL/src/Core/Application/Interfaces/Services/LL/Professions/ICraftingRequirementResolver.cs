using Domain.Models.Professions.Crafting.V2;

namespace Application.Interfaces.Services.LL.Professions;

public interface ICraftingRequirementResolver
{
    IReadOnlyList<ResolvedMaterialCost> ResolveCosts(
        CraftingRecipeDefinition recipe,
        int targetTier,
        IReadOnlyList<MaterialRequirementDefinition>? additionalRequirements = null);
}
