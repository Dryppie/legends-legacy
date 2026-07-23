using Domain.Models.Attributes;

namespace Domain.Models.Professions.Crafting.V2;

public sealed record EquipmentCraftingDesign(
    CraftingRecipeDefinition Recipe,
    BlueprintDefinition? Blueprint,
    string Name,
    string Description,
    EquipmentBehaviorDefinition Behavior,
    IReadOnlyDictionary<AttributeType, double> InitialStatProfile,
    TemperingProfileDefinition TemperingProfile,
    IReadOnlyList<MaterialRequirementDefinition> AdditionalMaterialRequirements,
    IReadOnlyList<string> Tags);

public static class EquipmentCraftingDesignComposer
{
    public static bool IsCompatible(CraftingRecipeDefinition recipe, BlueprintDefinition blueprint)
    {
        if (!recipe.Enabled || !blueprint.Enabled)
            return false;

        var tags = recipe.Tags
            .Concat(recipe.AffinityTags)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (blueprint.CompatibleRecipeIds.Count > 0 &&
            !blueprint.CompatibleRecipeIds.Contains(recipe.Id, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (blueprint.RequiredRecipeTags.Any(required => !tags.Contains(required)))
            return false;

        if (blueprint.AnyRecipeTags.Count > 0 &&
            !blueprint.AnyRecipeTags.Any(tags.Contains))
        {
            return false;
        }

        return !blueprint.ExcludedRecipeTags.Any(tags.Contains);
    }

    public static EquipmentCraftingDesign Compose(
        CraftingRecipeDefinition recipe,
        BlueprintDefinition? blueprint)
    {
        if (blueprint != null && !IsCompatible(recipe, blueprint))
            throw new InvalidOperationException(
                $"Blueprint '{blueprint.Id}' is not compatible with recipe '{recipe.Id}'.");

        if (blueprint == null)
        {
            return new EquipmentCraftingDesign(
                recipe,
                null,
                recipe.Name,
                recipe.Description,
                recipe.Behavior,
                recipe.InitialStatProfile,
                recipe.TemperingProfile,
                [],
                recipe.Tags
                    .Concat(recipe.AffinityTags)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList());
        }

        var blueprintName = blueprint.Name.Replace("Blueprint:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        var name = blueprint.NameFormat
            .Replace("{BlueprintName}", blueprintName, StringComparison.OrdinalIgnoreCase)
            .Replace("{BaseName}", recipe.Name, StringComparison.OrdinalIgnoreCase)
            .Trim();
        var influence = Math.Clamp(blueprint.StatProfileInfluence, 0d, 1d);

        return new EquipmentCraftingDesign(
            recipe,
            blueprint,
            name,
            string.IsNullOrWhiteSpace(blueprint.Description) ? recipe.Description : blueprint.Description,
            ComposeBehavior(recipe.Behavior, blueprint.BehaviorModifiers),
            BlendStatProfiles(recipe.InitialStatProfile, blueprint.StatProfile, influence),
            BlendTemperingProfiles(recipe, blueprint, influence),
            blueprint.AdditionalMaterialRequirements,
            recipe.Tags
                .Concat(recipe.AffinityTags)
                .Concat(blueprint.Tags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static EquipmentBehaviorDefinition ComposeBehavior(
        EquipmentBehaviorDefinition recipe,
        EquipmentBehaviorDefinition overlay) =>
        new()
        {
            Handedness = string.IsNullOrWhiteSpace(overlay.Handedness) ? recipe.Handedness : overlay.Handedness,
            AttackCategory = string.IsNullOrWhiteSpace(overlay.AttackCategory)
                ? recipe.AttackCategory
                : overlay.AttackCategory,
            RangeCategory = string.IsNullOrWhiteSpace(overlay.RangeCategory)
                ? recipe.RangeCategory
                : overlay.RangeCategory,
            Role = string.IsNullOrWhiteSpace(overlay.Role) ? recipe.Role : overlay.Role,
            BasicAttackIntervalMultiplier =
                recipe.BasicAttackIntervalMultiplier * overlay.BasicAttackIntervalMultiplier,
            BasicAttackDamageMultiplier =
                recipe.BasicAttackDamageMultiplier * overlay.BasicAttackDamageMultiplier
        };

    private static IReadOnlyDictionary<AttributeType, double> BlendStatProfiles(
        IReadOnlyDictionary<AttributeType, double> recipe,
        IReadOnlyDictionary<AttributeType, double> blueprint,
        double influence)
    {
        if (blueprint.Count == 0 || influence <= 0)
            return recipe;

        var combined = new Dictionary<AttributeType, double>();
        foreach (var pair in recipe)
            combined[pair.Key] = pair.Value * (1d - influence);
        foreach (var pair in blueprint)
            combined[pair.Key] = combined.GetValueOrDefault(pair.Key) + (pair.Value * influence);

        var total = combined.Values.Sum();
        return total <= 0
            ? recipe
            : combined.ToDictionary(pair => pair.Key, pair => pair.Value / total);
    }

    private static TemperingProfileDefinition BlendTemperingProfiles(
        CraftingRecipeDefinition recipe,
        BlueprintDefinition blueprint,
        double influence)
    {
        if (blueprint.TemperingProfile.Stats.Count == 0 || influence <= 0)
            return recipe.TemperingProfile;

        var stats = recipe.TemperingProfile.Stats
            .Select(stat => Scale(stat, 1d - influence))
            .ToDictionary(stat => stat.Stat);

        foreach (var overlay in blueprint.TemperingProfile.Stats)
        {
            var scaled = Scale(overlay, influence);
            if (!stats.TryGetValue(overlay.Stat, out var existing))
            {
                stats[overlay.Stat] = scaled;
                continue;
            }

            var maxBudgetShare = Math.Max(existing.MaxBudgetShare ?? 0, overlay.MaxBudgetShare ?? 0);
            stats[overlay.Stat] = new TemperingStatWeightDefinition
            {
                Stat = overlay.Stat,
                Weight = existing.Weight + scaled.Weight,
                Category = overlay.Category == TemperingStatCategory.Primary
                    ? TemperingStatCategory.Primary
                    : existing.Category,
                CanIntroduce = existing.CanIntroduce || overlay.CanIntroduce,
                CanIncrease = existing.CanIncrease || overlay.CanIncrease,
                MaxBudgetShare = maxBudgetShare > 0 ? maxBudgetShare : null,
                MinimumTier = Math.Min(existing.MinimumTier ?? 1, overlay.MinimumTier ?? 1)
            };
        }

        return new TemperingProfileDefinition
        {
            Id = $"{recipe.Id}.{blueprint.Id}.tempering",
            Name = $"{blueprint.Name} {recipe.Name} Tempering",
            Stats = stats.Values
                .Where(stat => stat.Weight > 0)
                .OrderBy(stat => stat.Category)
                .ThenByDescending(stat => stat.Weight)
                .ToList()
        };
    }

    private static TemperingStatWeightDefinition Scale(
        TemperingStatWeightDefinition stat,
        double multiplier) =>
        new()
        {
            Stat = stat.Stat,
            Weight = stat.Weight * multiplier,
            Category = stat.Category,
            CanIntroduce = stat.CanIntroduce,
            CanIncrease = stat.CanIncrease,
            MaxBudgetShare = stat.MaxBudgetShare,
            MinimumTier = stat.MinimumTier
        };
}
