using System.Text.Json;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items;
using Domain.Models.Professions.Crafting.V2;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Professions.Craftings;

public class JsonCraftingDefinitionProvider : ICraftingDefinitionProvider
{
    private readonly Lazy<DefinitionSet> _definitions;

    public JsonCraftingDefinitionProvider(IConfiguration config, string contentRootPath, JsonSerializerOptions options)
    {
        _definitions = new Lazy<DefinitionSet>(() => Load(config, contentRootPath, options));
    }

    public IReadOnlyList<MaterialDefinition> GetMaterials() => _definitions.Value.Materials;
    public IReadOnlyList<CraftingRecipeDefinition> GetRecipes() => _definitions.Value.Recipes;
    public IReadOnlyList<BlueprintDefinition> GetBlueprints() => _definitions.Value.Blueprints;
    public IReadOnlyList<TemperingRecipeDefinition> GetTemperingRecipes() => _definitions.Value.TemperingRecipes;
    public IReadOnlyDictionary<Rarity, int> GetTemperingProgressThresholds() => _definitions.Value.TemperingProgressThresholds;

    public MaterialDefinition? GetStandardMaterial(MaterialFamily family, int tier) =>
        _definitions.Value.Materials.FirstOrDefault(x => x.IsStandardTieredMaterial && x.Family == family && x.Tier == tier);

    public MaterialDefinition? GetMaterialByItemId(string itemId) =>
        _definitions.Value.Materials.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    public CraftingRecipeDefinition? GetRecipe(string recipeId) =>
        _definitions.Value.Recipes.FirstOrDefault(x => x.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase));

    public BlueprintDefinition? GetBlueprint(string blueprintId) =>
        _definitions.Value.Blueprints.FirstOrDefault(x => x.Id.Equals(blueprintId, StringComparison.OrdinalIgnoreCase));

    public BlueprintDefinition? GetBlueprintByItemId(string itemId) =>
        _definitions.Value.Blueprints.FirstOrDefault(x => x.ItemId != null && x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    public TemperingRecipeDefinition? GetTemperingRecipe(string recipeId) =>
        _definitions.Value.TemperingRecipes.FirstOrDefault(x => x.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase));

    private static DefinitionSet Load(IConfiguration config, string contentRootPath, JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var craftingRoot = Path.Combine(contentRootPath, contentRoot, "crafting");

        var materials = Read<IReadOnlyList<MaterialDefinition>>(craftingRoot, "materials.json", options);
        var baseRecipes = Read<IReadOnlyList<CraftingRecipeDefinition>>(craftingRoot, "base-recipes.json", options);
        var blueprints = Read<IReadOnlyList<BlueprintDefinition>>(craftingRoot, "blueprints.json", options);
        var affixes = Read<IReadOnlyList<WeightedAffixDefinition>>(craftingRoot, "affixes.json", options);
        var specialModifiers = Read<IReadOnlyList<WeightedAffixDefinition>>(craftingRoot, "special-modifiers.json", options);
        var tierBudgets = Read<IReadOnlyList<TemperingTierBudgetDefinition>>(craftingRoot, "tier-budgets.json", options);
        var rawTemperingRecipes = Read<IReadOnlyList<TemperingRecipeDefinition>>(craftingRoot, "tempering-recipes.json", options);
        var temperingRecipes = ResolveTemperingRecipes(rawTemperingRecipes, affixes, specialModifiers);

        var recipes = baseRecipes.ToList();
        Validate(materials, recipes, blueprints, temperingRecipes, affixes, specialModifiers, tierBudgets);
        var temperingProgressThresholds = tierBudgets.ToDictionary(x => x.Rarity, x => x.ProgressRequired);

        return new DefinitionSet(materials, recipes, blueprints, temperingRecipes, temperingProgressThresholds);
    }

    private static T Read<T>(string root, string fileName, JsonSerializerOptions options)
    {
        var path = Path.Combine(root, fileName);
        if (!File.Exists(path)) return JsonSerializer.Deserialize<T>("[]", options)!;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, options)
            ?? throw new InvalidOperationException($"Unable to parse crafting definition file '{fileName}'.");
    }

    private static void Validate(
        IReadOnlyList<MaterialDefinition> materials,
        IReadOnlyList<CraftingRecipeDefinition> recipes,
        IReadOnlyList<BlueprintDefinition> blueprints,
        IReadOnlyList<TemperingRecipeDefinition> temperingRecipes,
        IReadOnlyList<WeightedAffixDefinition> affixes,
        IReadOnlyList<WeightedAffixDefinition> specialModifiers,
        IReadOnlyList<TemperingTierBudgetDefinition> tierBudgets)
    {
        var duplicateMaterial = materials
            .Where(x => x.IsStandardTieredMaterial)
            .GroupBy(x => new { x.Family, x.Tier })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateMaterial != null)
            throw new InvalidOperationException($"Duplicate standard material for {duplicateMaterial.Key.Family} tier {duplicateMaterial.Key.Tier}.");

        var recipeIds = recipes.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.OutputItemId) && recipe.Forms.Count == 0)
                throw new InvalidOperationException($"Recipe '{recipe.Id}' must define either an output item or at least one form.");

            var duplicateForm = recipe.Forms
                .GroupBy(x => x.FormId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateForm != null)
                throw new InvalidOperationException($"Recipe '{recipe.Id}' contains duplicate form '{duplicateForm.Key}'.");
        }

        foreach (var blueprint in blueprints)
        {
            if (!string.IsNullOrWhiteSpace(blueprint.UnlocksRecipeId) && !recipeIds.Contains(blueprint.UnlocksRecipeId))
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' references missing recipe '{blueprint.UnlocksRecipeId}'.");

            var hasCompatibility = blueprint.AllowedBaseRecipeIds.Count > 0 || blueprint.AllowedRecipeTags.Count > 0;
            if (string.IsNullOrWhiteSpace(blueprint.UnlocksRecipeId) && !hasCompatibility)
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' must define an unlock recipe or compatibility tags.");

            var missingBaseRecipeId = blueprint.AllowedBaseRecipeIds.FirstOrDefault(x => !recipeIds.Contains(x));
            if (missingBaseRecipeId != null)
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' references missing compatible recipe '{missingBaseRecipeId}'.");

            foreach (var requirement in blueprint.SpecialResourceRequirements)
            {
                if (requirement.Type == RequirementType.SpecialResource &&
                    (requirement.ItemId == null || materials.All(x => x.ItemId != requirement.ItemId)))
                    throw new InvalidOperationException($"Blueprint '{blueprint.Id}' references missing special resource '{requirement.ItemId}'.");
            }
        }

        foreach (var recipe in recipes)
        {
            foreach (var requirement in recipe.MaterialRequirements
                         .Concat(recipe.AdditionalMaterialRequirements)
                         .Concat(recipe.SpecialResourceRequirements))
            {
                if (requirement.Type == RequirementType.SpecialResource &&
                    (requirement.ItemId == null || materials.All(x => x.ItemId != requirement.ItemId)))
                    throw new InvalidOperationException($"Recipe '{recipe.Id}' references missing special resource '{requirement.ItemId}'.");
            }
        }

        var duplicateTempering = temperingRecipes
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateTempering != null)
            throw new InvalidOperationException($"Duplicate tempering recipe '{duplicateTempering.Key}'.");

        ValidateModifierDefinitions("affix", affixes);
        ValidateModifierDefinitions("special modifier", specialModifiers);

        var duplicateBudget = tierBudgets
            .GroupBy(x => x.Rarity)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateBudget != null)
            throw new InvalidOperationException($"Duplicate tempering tier budget for rarity '{duplicateBudget.Key}'.");

        foreach (var rarity in new[] { Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Unique, Rarity.Legendary, Rarity.Legacy })
        {
            if (tierBudgets.All(x => x.Rarity != rarity))
                throw new InvalidOperationException($"Missing tempering tier budget for rarity '{rarity}'.");
        }
    }

    private static void ValidateModifierDefinitions(string label, IReadOnlyList<WeightedAffixDefinition> definitions)
    {
        var duplicate = definitions
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new InvalidOperationException($"Duplicate {label} definition '{duplicate.Key}'.");
    }

    private static IReadOnlyList<TemperingRecipeDefinition> ResolveTemperingRecipes(
        IReadOnlyList<TemperingRecipeDefinition> recipes,
        IReadOnlyList<WeightedAffixDefinition> affixes,
        IReadOnlyList<WeightedAffixDefinition> specialModifiers)
    {
        var affixesById = affixes.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var specialModifiersById = specialModifiers.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        return recipes
            .Select(recipe => new TemperingRecipeDefinition
            {
                Id = recipe.Id,
                Name = recipe.Name,
                ApplicableItemTypes = recipe.ApplicableItemTypes,
                RequiredItemAffinityTags = recipe.RequiredItemAffinityTags,
                DirectionTags = recipe.DirectionTags,
                ProgressOnOutcome = recipe.ProgressOnOutcome,
                StatImprovementPool = recipe.StatImprovementPool,
                AffixPool = recipe.AffixPool,
                SpecialModifierPool = recipe.SpecialModifierPool,
                ResolvedAffixPool = ResolveModifierPool(recipe.Id, "affix", recipe.AffixPool, affixesById),
                ResolvedSpecialModifierPool = ResolveModifierPool(recipe.Id, "special modifier", recipe.SpecialModifierPool, specialModifiersById)
            })
            .ToList();
    }

    private static IReadOnlyList<WeightedAffixDefinition> ResolveModifierPool(
        string recipeId,
        string label,
        IReadOnlyList<WeightedModifierReferenceDefinition> references,
        IReadOnlyDictionary<string, WeightedAffixDefinition> definitionsById)
    {
        return references.Select(reference =>
        {
            if (!definitionsById.TryGetValue(reference.Id, out var definition))
                throw new InvalidOperationException($"Tempering recipe '{recipeId}' references missing {label} '{reference.Id}'.");

            return new WeightedAffixDefinition
            {
                Id = definition.Id,
                Name = definition.Name,
                MinRarity = definition.MinRarity,
                Weight = reference.Weight,
                StatModifier = definition.StatModifier
            };
        }).ToList();
    }

    private sealed record DefinitionSet(
        IReadOnlyList<MaterialDefinition> Materials,
        IReadOnlyList<CraftingRecipeDefinition> Recipes,
        IReadOnlyList<BlueprintDefinition> Blueprints,
        IReadOnlyList<TemperingRecipeDefinition> TemperingRecipes,
        IReadOnlyDictionary<Rarity, int> TemperingProgressThresholds);
}
