using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Professions.Craftings;

public class CraftingRequirementResolver : ICraftingRequirementResolver
{
    private readonly ICraftingDefinitionProvider _definitionProvider;

    public CraftingRequirementResolver(ICraftingDefinitionProvider definitionProvider)
    {
        _definitionProvider = definitionProvider;
    }

    public IReadOnlyList<ResolvedMaterialCost> ResolveCosts(
        CraftingRecipeDefinition recipe,
        int targetTier,
        IReadOnlyList<MaterialRequirementDefinition>? additionalRequirements = null)
    {
        var requirements = recipe.MaterialRequirements
            .Concat(recipe.AdditionalMaterialRequirements)
            .Concat(recipe.SpecialResourceRequirements)
            .Concat(additionalRequirements ?? []);

        var costs = new List<ResolvedMaterialCost>();
        foreach (var requirement in requirements)
        {
            var quantity = requirement.BaseAmount + (requirement.AmountPerTier * Math.Max(targetTier - 1, 0));
            if (quantity <= 0) continue;

            if (requirement.Type == RequirementType.TieredMaterial)
            {
                var materialTier = Math.Max(requirement.MinimumTier ?? 1, targetTier + (requirement.TierOffset ?? 0));
                var family = requirement.Family ?? throw new InvalidOperationException($"Tiered requirement on '{recipe.Id}' has no family.");
                var material = _definitionProvider.GetStandardMaterial(family, materialTier)
                    ?? throw new InvalidOperationException($"No standard material for {family} tier {materialTier}.");

                costs.Add(new ResolvedMaterialCost
                {
                    ItemId = material.ItemId,
                    Name = material.Name,
                    Family = material.Family,
                    Tier = material.Tier,
                    Quantity = quantity
                });
                continue;
            }

            var special = _definitionProvider.GetMaterialByItemId(requirement.ItemId ?? string.Empty)
                ?? throw new InvalidOperationException($"No special material '{requirement.ItemId}'.");
            costs.Add(new ResolvedMaterialCost
            {
                ItemId = special.ItemId,
                Name = special.Name,
                Family = special.Family,
                Tier = special.Tier,
                Quantity = quantity
            });
        }

        return costs
            .GroupBy(x => x.ItemId)
            .Select(g => new ResolvedMaterialCost
            {
                ItemId = g.Key,
                Name = g.First().Name,
                Family = g.First().Family,
                Tier = g.First().Tier,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();
    }
}
