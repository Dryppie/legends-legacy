using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Services.LL.Professions.Craftings;

public sealed class TemperingRecipeResolver : ITemperingRecipeResolver
{
    private readonly ICraftingDefinitionProvider _definitions;

    public TemperingRecipeResolver(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public TemperingRecipeDefinition? ResolveFor(EquipmentInstance equipment, string? preferredRecipeId = null)
    {
        if (equipment.EquipmentBase.EquipmentType == EquipmentType.Tool)
            return null;

        var candidates = _definitions.GetTemperingRecipes()
            .Where(recipe => IsApplicable(recipe, equipment))
            .ToList();

        if (!string.IsNullOrWhiteSpace(preferredRecipeId))
        {
            return candidates.FirstOrDefault(recipe =>
                recipe.Id.Equals(preferredRecipeId, StringComparison.OrdinalIgnoreCase));
        }

        if (candidates.Count == 0)
            return null;

        var blueprintTags = GetBlueprintTags(equipment);
        var itemTags = equipment.AffinityTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .OrderByDescending(recipe => CountRequiredTagMatches(recipe, blueprintTags))
            .ThenByDescending(recipe => recipe.RequiredItemAffinityTags.Count)
            .ThenByDescending(recipe => CountDirectionTagMatches(recipe, itemTags))
            .ThenBy(recipe => recipe.Id, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static bool IsApplicable(TemperingRecipeDefinition recipe, EquipmentInstance equipment)
    {
        return (recipe.ApplicableItemTypes.Count == 0 ||
                recipe.ApplicableItemTypes.Contains(equipment.EquipmentBase.EquipmentType)) &&
               recipe.RequiredItemAffinityTags.All(tag =>
                   equipment.AffinityTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    private IReadOnlySet<string> GetBlueprintTags(EquipmentInstance equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.BlueprintId))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return _definitions.GetBlueprint(equipment.BlueprintId)?.Tags
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static int CountRequiredTagMatches(
        TemperingRecipeDefinition recipe,
        IReadOnlySet<string> tags)
    {
        return recipe.RequiredItemAffinityTags.Count(tags.Contains);
    }

    private static int CountDirectionTagMatches(
        TemperingRecipeDefinition recipe,
        IReadOnlySet<string> tags)
    {
        return recipe.DirectionTags.Count(tags.Contains);
    }
}
