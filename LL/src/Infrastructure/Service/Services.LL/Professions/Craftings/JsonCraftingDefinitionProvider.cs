using System.Text.Json;
using Application.Interfaces.Services.LL.Professions;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Attributes;
using Microsoft.Extensions.Configuration;

namespace Services.LL.Professions.Craftings;

public sealed class JsonCraftingDefinitionProvider : ICraftingDefinitionProvider
{
    private readonly Lazy<DefinitionSet> _definitions;

    public JsonCraftingDefinitionProvider(IConfiguration config, string contentRootPath, JsonSerializerOptions options)
    {
        _definitions = new Lazy<DefinitionSet>(() => Load(config, contentRootPath, options));
    }

    public IReadOnlyList<MaterialDefinition> GetMaterials() => _definitions.Value.Materials;
    public IReadOnlyList<CraftingRecipeDefinition> GetRecipes() => _definitions.Value.Recipes;
    public IReadOnlyList<BlueprintDefinition> GetBlueprints() => _definitions.Value.Blueprints;
    public IReadOnlyDictionary<string, EquipmentBase> GetEquipmentBases() =>
        _definitions.Value.EquipmentBases;

    public MaterialDefinition? GetStandardMaterial(MaterialFamily family, int tier) =>
        _definitions.Value.Materials.FirstOrDefault(x => x.IsStandardTieredMaterial && x.Family == family && x.Tier == tier);

    public MaterialDefinition? GetMaterialByItemId(string itemId) =>
        _definitions.Value.Materials.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    public CraftingRecipeDefinition? GetRecipe(string recipeId) =>
        _definitions.Value.Recipes.FirstOrDefault(x => x.Id.Equals(recipeId, StringComparison.OrdinalIgnoreCase));

    public BlueprintDefinition? GetBlueprint(string blueprintId) =>
        _definitions.Value.Blueprints.FirstOrDefault(x => x.Id.Equals(blueprintId, StringComparison.OrdinalIgnoreCase));

    public BlueprintDefinition? GetBlueprintByItemId(string itemId) =>
        _definitions.Value.Blueprints.FirstOrDefault(x => x.ItemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));

    private static DefinitionSet Load(
        IConfiguration config,
        string contentRootPath,
        JsonSerializerOptions options)
    {
        var contentRoot = config["Content:Root"] ?? "Data";
        var craftingRoot = Path.Combine(contentRootPath, contentRoot, "crafting");

        var materials = Read<IReadOnlyList<MaterialDefinition>>(craftingRoot, "materials.json", options);
        var recipes = Read<IReadOnlyList<CraftingRecipeDefinition>>(craftingRoot, "base-recipes.json", options);
        var blueprints = Read<IReadOnlyList<BlueprintDefinition>>(craftingRoot, "blueprints.json", options);
        var equipmentBases = ReadEquipmentBases(
            Path.Combine(contentRootPath, contentRoot, "items", "items.json"),
            options);

        Validate(materials, recipes, blueprints, equipmentBases);
        return new DefinitionSet(materials, recipes, blueprints, equipmentBases);
    }

    private static IReadOnlyDictionary<string, EquipmentBase> ReadEquipmentBases(
        string path,
        JsonSerializerOptions options)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException("Required item definition file 'items/items.json' does not exist.");

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .EnumerateArray()
            .Where(element =>
                element.TryGetProperty("itemType", out var itemType)
                && itemType.GetString()?.Equals(
                    "Equipment",
                    StringComparison.OrdinalIgnoreCase) == true)
            .Select(element =>
                JsonSerializer.Deserialize<EquipmentBase>(element.GetRawText(), options)
                ?? throw new InvalidOperationException("Unable to parse an equipment item definition."))
            .ToDictionary(
                equipment => equipment.Id,
                StringComparer.OrdinalIgnoreCase);
    }

    private static T Read<T>(string root, string fileName, JsonSerializerOptions options)
    {
        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException($"Required crafting definition file '{fileName}' does not exist.");

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException($"Unable to parse crafting definition file '{fileName}'.");
    }

    private static void Validate(
        IReadOnlyList<MaterialDefinition> materials,
        IReadOnlyList<CraftingRecipeDefinition> recipes,
        IReadOnlyList<BlueprintDefinition> blueprints,
        IReadOnlyDictionary<string, EquipmentBase> equipmentBases)
    {
        ValidateEquipmentUnitContract();
        EnsureUnique(materials.Select(x => x.Id), "material");
        EnsureUnique(recipes.Select(x => x.Id), "equipment recipe");
        EnsureUnique(recipes.Select(x => x.OutputItemId), "recipe output item");
        EnsureUnique(blueprints.Select(x => x.Id), "blueprint");
        EnsureUnique(blueprints.Select(x => x.ItemId), "blueprint item");

        if (recipes.Count == 0)
            throw new InvalidOperationException("Equipment crafting requires at least one concrete recipe.");

        var duplicateMaterial = materials
            .Where(x => x.IsStandardTieredMaterial)
            .GroupBy(x => new { x.Family, x.Tier })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateMaterial != null)
            throw new InvalidOperationException(
                $"Duplicate standard material for {duplicateMaterial.Key.Family} tier {duplicateMaterial.Key.Tier}.");

        foreach (var recipe in recipes)
        {
            if (recipe.TierRange.Min < 1 || recipe.TierRange.Max < recipe.TierRange.Min)
                throw new InvalidOperationException($"Equipment recipe '{recipe.Id}' has an invalid tier range.");

            ValidateRequirements(recipe.Id, recipe.MaterialRequirements
                .Concat(recipe.AdditionalMaterialRequirements)
                .Concat(recipe.SpecialResourceRequirements), materials);

            if (string.IsNullOrWhiteSpace(recipe.OutputItemId))
                throw new InvalidOperationException($"Equipment recipe '{recipe.Id}' has no output item.");
            if (!equipmentBases.TryGetValue(recipe.OutputItemId, out var equipmentBase))
                throw new InvalidOperationException(
                    $"Equipment recipe '{recipe.Id}' references missing item base '{recipe.OutputItemId}'.");
            if (equipmentBase.EquipmentType != recipe.OutputItemType)
                throw new InvalidOperationException(
                    $"Equipment recipe '{recipe.Id}' output type does not match item base '{recipe.OutputItemId}'.");

            ValidateProfile(recipe.Id, recipe.InitialStatProfile, recipe.TemperingProfile);
            ValidateBaseRecipeStatContract(recipe);
            ValidateHandedness(recipe.Id, recipe.OutputItemType, recipe.Behavior);
        }

        foreach (var blueprint in blueprints)
        {
            if (string.IsNullOrWhiteSpace(blueprint.SourceType) || string.IsNullOrWhiteSpace(blueprint.SourceId))
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' has no acquisition source.");

            if (blueprint.BonusStatBudgetMultiplier is <= 0 or > 1)
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' has an invalid bonus-stat budget multiplier.");

            if (!recipes.Any(recipe => EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint)))
                throw new InvalidOperationException($"Blueprint '{blueprint.Id}' has no compatible equipment recipes.");

            ValidateProfile(blueprint.Id, blueprint.BonusStatProfile, blueprint.TemperingProfile);
            ValidateRequirements(blueprint.Id, blueprint.AdditionalMaterialRequirements, materials);
        }
    }

    private static void ValidateEquipmentUnitContract()
    {
        foreach (var definition in AttributeCatalog.All.Where(x => x.IsEquipmentEligible))
        {
            if (!EquipmentStatBudgetCatalog.IsKnown(definition.AttributeType))
            {
                throw new InvalidOperationException(
                    $"Equipment attribute '{definition.AttributeType}' has no budget rule.");
            }

            var scalingKind = EquipmentStatBudgetCatalog.Get(definition.AttributeType).ScalingKind;
            var expectedUnit = scalingKind switch
            {
                EquipmentStatScalingKind.ProgressionNormalizedRating => AttributeUnit.Rating,
                EquipmentStatScalingKind.DirectPercentage => AttributeUnit.PercentagePoints,
                _ => definition.AttributeType == AttributeType.HealthRegeneration
                    ? AttributeUnit.HealthPerFiveSeconds
                    : AttributeUnit.FlatPoints
            };
            if (definition.EquipmentUnit != expectedUnit)
            {
                throw new InvalidOperationException(
                    $"Equipment attribute '{definition.AttributeType}' uses unit " +
                    $"'{definition.EquipmentUnit}' but its scaling kind requires '{expectedUnit}'.");
            }
        }
    }

    private static void ValidateBaseRecipeStatContract(CraftingRecipeDefinition recipe)
    {
        const double tolerance = 0.000001d;
        var totalWeight = recipe.InitialStatProfile.Values.Sum();
        if (Math.Abs(totalWeight - 1d) > tolerance)
        {
            throw new InvalidOperationException(
                $"Equipment recipe '{recipe.Id}' initial stat weights must sum to one.");
        }

        foreach (var attribute in recipe.InitialStatProfile.Keys)
        {
            if (!AttributeCatalog.IsEquipmentEligible(attribute)
                || !EquipmentStatBudgetCatalog.IsKnown(attribute))
            {
                throw new InvalidOperationException(
                    $"Equipment recipe '{recipe.Id}' uses unsupported attribute '{attribute}'.");
            }
        }

        var anchorShare = recipe.InitialStatProfile
            .Where(entry => EquipmentStatBudgetCatalog.IsTierAnchor(entry.Key))
            .Sum(entry => entry.Value);
        if (anchorShare < 0.25d - tolerance)
        {
            throw new InvalidOperationException(
                $"Equipment recipe '{recipe.Id}' has only {anchorShare:P0} tier-anchor budget; at least 25% is required.");
        }

        ValidateAdjacentTierDominance(recipe);

        var temperingByStat = recipe.TemperingProfile.Stats.ToDictionary(stat => stat.Stat);
        if (!temperingByStat.Keys.ToHashSet().SetEquals(recipe.InitialStatProfile.Keys))
        {
            throw new InvalidOperationException(
                $"Equipment recipe '{recipe.Id}' tempering attributes must exactly match its base profile.");
        }

        var temperingWeightTotal = temperingByStat.Values.Sum(stat => stat.Weight);
        foreach (var (attribute, weight) in recipe.InitialStatProfile)
        {
            var tempering = temperingByStat[attribute];
            var normalizedTemperingWeight = tempering.Weight / temperingWeightTotal;
            if (Math.Abs(normalizedTemperingWeight - weight) > tolerance
                || Math.Abs((tempering.MaxBudgetShare ?? 0d) - weight) > tolerance)
            {
                throw new InvalidOperationException(
                    $"Equipment recipe '{recipe.Id}' tempering weight/cap for '{attribute}' " +
                    "must match the approved base allocation.");
            }
        }

        if (recipe.OutputItemType is EquipmentType.Head or EquipmentType.Chest or EquipmentType.Legs)
            ValidateArmorProfile(recipe, anchorShare, tolerance);
    }

    private static void ValidateAdjacentTierDominance(CraftingRecipeDefinition recipe)
    {
        foreach (var (attribute, weight) in recipe.InitialStatProfile.Where(entry =>
                     EquipmentStatBudgetCatalog.IsTierAnchor(entry.Key)))
        {
            var cost = EquipmentStatBudgetCatalog.Get(attribute).CostPerPoint;
            for (var tier = EquipmentStatBudgetCatalog.MinimumTier;
                 tier < EquipmentTierBudgetCurve.MaximumCalibrationTier;
                 tier++)
            {
                var previousMaximum = AttributeValueQuantizer.Quantize(
                    attribute,
                    EquipmentTierBudgetCurve.GetBudget(tier) * weight * 1.05d / cost);
                var nextMinimum = AttributeValueQuantizer.Quantize(
                    attribute,
                    EquipmentTierBudgetCurve.GetBudget(tier + 1) * weight * 0.95d / cost);
                if (nextMinimum <= previousMaximum)
                {
                    throw new InvalidOperationException(
                        $"Equipment recipe '{recipe.Id}' tier anchor '{attribute}' fails " +
                        $"ordinary-roll dominance between tiers {tier} and {tier + 1}.");
                }
            }
        }
    }

    private static void ValidateArmorProfile(
        CraftingRecipeDefinition recipe,
        double anchorShare,
        double tolerance)
    {
        var expected = recipe.Behavior.Role.ToLowerInvariant() switch
        {
            "heavy" => new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.35d,
                [AttributeType.MaxHealth] = 0.35d,
                [AttributeType.Resistance] = 0.30d
            },
            "medium" => new Dictionary<AttributeType, double>
            {
                [AttributeType.Armor] = 0.25d,
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.CritChance] = 0.25d,
                [AttributeType.CritDamage] = 0.25d
            },
            "light" => new Dictionary<AttributeType, double>
            {
                [AttributeType.MaxHealth] = 0.25d,
                [AttributeType.HealthRegeneration] = 0.25d,
                [AttributeType.DodgeChance] = 0.25d,
                [AttributeType.AttackSpeed] = 0.25d
            },
            "cloth" => new Dictionary<AttributeType, double>
            {
                [AttributeType.Resistance] = 0.25d,
                [AttributeType.HealthRegeneration] = 0.25d,
                [AttributeType.HealingPowerPercent] = 0.25d,
                [AttributeType.Cooldown] = 0.25d
            },
            _ => throw new InvalidOperationException(
                $"Armor recipe '{recipe.Id}' has unsupported family '{recipe.Behavior.Role}'.")
        };

        if (recipe.InitialStatProfile.Count != expected.Count
            || expected.Any(entry =>
                !recipe.InitialStatProfile.TryGetValue(entry.Key, out var actual)
                || Math.Abs(actual - entry.Value) > tolerance))
        {
            throw new InvalidOperationException(
                $"Armor recipe '{recipe.Id}' does not match the approved {recipe.Behavior.Role} profile.");
        }

        var expectedAnchorShare = recipe.Behavior.Role.Equals("Heavy", StringComparison.OrdinalIgnoreCase)
            ? 1d
            : 0.5d;
        if (Math.Abs(anchorShare - expectedAnchorShare) > tolerance)
        {
            throw new InvalidOperationException(
                $"Armor recipe '{recipe.Id}' has an invalid tier-anchor share.");
        }
    }

    private static void ValidateProfile(
        string ownerId,
        IReadOnlyDictionary<Domain.Models.Attributes.AttributeType, double> initialStatProfile,
        TemperingProfileDefinition profile)
    {
        if (profile.Stats.Count == 0)
            throw new InvalidOperationException($"Crafting definition '{ownerId}' has an empty Tempering Profile.");

        var duplicateStat = profile.Stats
            .GroupBy(x => x.Stat)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateStat != null)
            throw new InvalidOperationException(
                $"Crafting definition '{ownerId}' has duplicate tempering stat '{duplicateStat.Key}'.");

        if (profile.Stats.Any(x => x.Weight < 0) || profile.Stats.Sum(x => x.Weight) <= 0)
            throw new InvalidOperationException($"Crafting definition '{ownerId}' has invalid tempering weights.");

        if (profile.Stats.Any(x => !x.CanIntroduce && !x.CanIncrease))
            throw new InvalidOperationException(
                $"Crafting definition '{ownerId}' has a tempering stat that can neither be introduced nor increased.");

        if (profile.Stats.Any(x => x.MaxBudgetShare is <= 0 or > 1))
            throw new InvalidOperationException($"Crafting definition '{ownerId}' has an invalid budget-share cap.");

        if (initialStatProfile.Count == 0 || initialStatProfile.Values.Any(x => x < 0) ||
            initialStatProfile.Values.Sum() <= 0)
            throw new InvalidOperationException($"Crafting definition '{ownerId}' has an invalid initial stat profile.");
    }

    private static void ValidateHandedness(
        string ownerId,
        EquipmentType outputItemType,
        EquipmentBehaviorDefinition behavior)
    {
        var expected = outputItemType switch
        {
            EquipmentType.OneHanded => "OneHanded",
            EquipmentType.TwoHanded => "TwoHanded",
            EquipmentType.OffHand => "OffHand",
            _ => string.Empty
        };

        if (expected.Length > 0 &&
            !behavior.Handedness.Equals(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Equipment recipe '{ownerId}' has handedness '{behavior.Handedness}', expected '{expected}'.");
        }
    }

    private static void ValidateRequirements(
        string ownerId,
        IEnumerable<MaterialRequirementDefinition> requirements,
        IReadOnlyList<MaterialDefinition> materials)
    {
        foreach (var requirement in requirements)
        {
            if (requirement.Type == RequirementType.SpecialResource &&
                (string.IsNullOrWhiteSpace(requirement.ItemId) ||
                 materials.All(x => !x.ItemId.Equals(requirement.ItemId, StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException(
                    $"Crafting definition '{ownerId}' references missing special resource '{requirement.ItemId}'.");
            }
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string label)
    {
        var invalid = values.FirstOrDefault(string.IsNullOrWhiteSpace);
        if (invalid != null)
            throw new InvalidOperationException($"A {label} definition has an empty ID.");

        var duplicate = values
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
            throw new InvalidOperationException($"Duplicate {label} definition '{duplicate.Key}'.");
    }

    private sealed record DefinitionSet(
        IReadOnlyList<MaterialDefinition> Materials,
        IReadOnlyList<CraftingRecipeDefinition> Recipes,
        IReadOnlyList<BlueprintDefinition> Blueprints,
        IReadOnlyDictionary<string, EquipmentBase> EquipmentBases);
}
