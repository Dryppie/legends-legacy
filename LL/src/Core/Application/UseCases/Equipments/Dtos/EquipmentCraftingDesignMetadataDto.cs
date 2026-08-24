using Application.Interfaces.Services.LL.Professions;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Equipments.Dtos;

public sealed class EquipmentCraftingDesignMetadataDto
{
    public string RecipeId { get; init; } = string.Empty;
    public string? BlueprintId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Handedness { get; init; } = string.Empty;
    public string AttackCategory { get; init; } = string.Empty;
    public string RangeCategory { get; init; } = string.Empty;
    public double BasicAttackIntervalMultiplier { get; init; } = 1d;
    public double BasicAttackDamageMultiplier { get; init; } = 1d;
    public string Role { get; init; } = string.Empty;
    public IReadOnlyList<string> PrimaryTemperingStats { get; init; } = [];
    public IReadOnlyList<string> SecondaryTemperingStats { get; init; } = [];
}

public sealed class EquipmentCraftingDesignMetadataResolver
    : IValueResolver<EquipmentInstance, EquipmentInstanceDto, EquipmentCraftingDesignMetadataDto?>
{
    private readonly ICraftingDefinitionProvider? _definitions;

    public EquipmentCraftingDesignMetadataResolver()
    {
    }

    public EquipmentCraftingDesignMetadataResolver(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public EquipmentCraftingDesignMetadataDto? Resolve(
        EquipmentInstance source,
        EquipmentInstanceDto destination,
        EquipmentCraftingDesignMetadataDto? destinationMember,
        ResolutionContext context)
    {
        if (_definitions is null || string.IsNullOrWhiteSpace(source.BaseRecipeId))
            return null;

        var recipe = _definitions.GetRecipe(source.BaseRecipeId);
        var blueprint = string.IsNullOrWhiteSpace(source.BlueprintId)
            ? null
            : _definitions.GetBlueprint(source.BlueprintId);
        if (recipe is null || (!string.IsNullOrWhiteSpace(source.BlueprintId) && blueprint is null))
            return null;

        var design = EquipmentCraftingDesignComposer.Compose(recipe, blueprint);
        return new EquipmentCraftingDesignMetadataDto
        {
            RecipeId = recipe.Id,
            BlueprintId = blueprint?.Id,
            Name = design.Name,
            Handedness = design.Behavior.Handedness,
            AttackCategory = design.Behavior.AttackCategory,
            RangeCategory = design.Behavior.RangeCategory,
            BasicAttackIntervalMultiplier = design.Behavior.BasicAttackIntervalMultiplier,
            BasicAttackDamageMultiplier = design.Behavior.BasicAttackDamageMultiplier,
            Role = design.Behavior.Role,
            PrimaryTemperingStats = design.TemperingProfile.Stats
                .Where(stat => stat.Category == TemperingStatCategory.Primary)
                .Select(stat => stat.Stat.ToString())
                .ToList(),
            SecondaryTemperingStats = design.TemperingProfile.Stats
                .Where(stat => stat.Category == TemperingStatCategory.Secondary)
                .Select(stat => stat.Stat.ToString())
                .ToList()
        };
    }
}
