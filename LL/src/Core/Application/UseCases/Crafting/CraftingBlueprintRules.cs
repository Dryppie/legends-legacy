using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting;

public static class CraftingBlueprintRules
{
    public static bool IsCompatible(
        BlueprintDefinition blueprint,
        CraftingRecipeDefinition recipe,
        CraftingRecipeFormDefinition? form)
    {
        var baseRecipeId = recipe.BaseRecipeId ?? recipe.Id;
        if (blueprint.AllowedBaseRecipeIds.Count > 0 &&
            blueprint.AllowedBaseRecipeIds.Any(x => x.Equals(baseRecipeId, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (blueprint.AllowedRecipeTags.Count == 0) return false;

        var tags = GetCompatibilityTags(recipe, form);
        return blueprint.AllowedRecipeTags.Any(tag => tags.Contains(tag));
    }

    public static IReadOnlyList<string> GetCompatibleFormIds(
        BlueprintDefinition blueprint,
        CraftingRecipeDefinition recipe)
    {
        if (recipe.Forms.Count == 0)
        {
            return IsCompatible(blueprint, recipe, null) ? [] : [];
        }

        return recipe.Forms
            .Where(form => IsCompatible(blueprint, recipe, form))
            .Select(form => form.FormId)
            .ToList();
    }

    public static string ResolveOutputName(
        BlueprintDefinition blueprint,
        CraftingRecipeDefinition recipe,
        CraftingRecipeFormDefinition? form,
        string fallbackName)
    {
        var specialName = blueprint.SpecialOutputNames.FirstOrDefault(x =>
            x.BaseRecipeId.Equals(recipe.Id, StringComparison.OrdinalIgnoreCase) &&
            (form == null || x.FormId.Equals(form.FormId, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(specialName?.OutputName)) return specialName.OutputName;

        var family = blueprint.BlueprintFamily ?? TrimBlueprintPrefix(blueprint.Name);
        var formName = form?.DisplayName ?? recipe.Name;
        var template = string.IsNullOrWhiteSpace(blueprint.OutputNameTemplate)
            ? "{BlueprintName} {FormName}"
            : blueprint.OutputNameTemplate;

        return template
            .Replace("{BlueprintName}", family, StringComparison.OrdinalIgnoreCase)
            .Replace("{FormName}", formName, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private static HashSet<string> GetCompatibilityTags(
        CraftingRecipeDefinition recipe,
        CraftingRecipeFormDefinition? form)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            recipe.Id,
            recipe.BaseRecipeId ?? recipe.Id,
            recipe.Name,
            recipe.RecipeFamily ?? string.Empty,
            recipe.Slot?.ToString() ?? string.Empty,
            recipe.OutputItemType.ToString()
        };

        foreach (var tag in recipe.Tags) tags.Add(tag);
        foreach (var tag in recipe.AffinityTags) tags.Add(tag);
        foreach (var tag in recipe.DefaultTemperingTags) tags.Add(tag);

        if (form != null)
        {
            tags.Add(form.FormId);
            tags.Add(form.DisplayName);
            tags.Add(form.OutputItemType.ToString());
            if (!string.IsNullOrWhiteSpace(form.ArmorWeight)) tags.Add(form.ArmorWeight);
            foreach (var tag in form.Tags) tags.Add(tag);
        }

        tags.Remove(string.Empty);
        return tags;
    }

    private static string TrimBlueprintPrefix(string value)
    {
        const string prefix = "Blueprint:";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..].Trim()
            : value.Trim();
    }
}
