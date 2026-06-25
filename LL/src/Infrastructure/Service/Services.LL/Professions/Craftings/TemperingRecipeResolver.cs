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

        if (!string.IsNullOrWhiteSpace(preferredRecipeId))
        {
            var preferred = _definitions.GetTemperingRecipe(preferredRecipeId);
            if (preferred != null && IsApplicable(preferred, equipment))
                return preferred;
        }

        var blueprintProfile = ResolveBlueprintProfile(equipment);
        if (blueprintProfile != null)
            return blueprintProfile;

        var candidates = _definitions.GetTemperingRecipes()
            .Where(recipe => IsApplicable(recipe, equipment))
            .ToList();

        if (candidates.Count == 0)
            return null;

        var itemTags = equipment.AffinityTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return candidates
            .OrderByDescending(recipe => recipe.RequiredItemAffinityTags.Count)
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

    private TemperingRecipeDefinition? ResolveBlueprintProfile(EquipmentInstance equipment)
    {
        if (string.IsNullOrWhiteSpace(equipment.BlueprintId))
            return null;

        var profile = _definitions.GetBlueprint(equipment.BlueprintId)?.TemperingProfile;
        if (profile == null)
            return null;

        return IsApplicable(profile, equipment)
            ? profile
            : null;
    }

    private static int CountDirectionTagMatches(
        TemperingRecipeDefinition recipe,
        IReadOnlySet<string> tags)
    {
        return recipe.DirectionTags.Count(tags.Contains);
    }
}
