using Application.Interfaces.Services.LL.Professions;
using AutoMapper;
using Domain.Models.Items;

namespace Application.UseCases.Items.Dtos;

public sealed class BlueprintItemMetadataDto
{
    public string BlueprintId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> RequiredRecipeTags { get; init; } = [];
    public IReadOnlyList<string> AnyRecipeTags { get; init; } = [];
    public int CompatibleRecipeCount { get; init; }
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
}

public sealed class BlueprintItemMetadataResolver
    : IValueResolver<ItemBase, ItemBaseDto, BlueprintItemMetadataDto?>
{
    private readonly ICraftingDefinitionProvider? _definitions;

    public BlueprintItemMetadataResolver()
    {
    }

    public BlueprintItemMetadataResolver(ICraftingDefinitionProvider definitions)
    {
        _definitions = definitions;
    }

    public BlueprintItemMetadataDto? Resolve(
        ItemBase source,
        ItemBaseDto destination,
        BlueprintItemMetadataDto? destinationMember,
        ResolutionContext context)
    {
        if (_definitions is null)
            return null;

        var blueprint = _definitions.GetBlueprints().FirstOrDefault(candidate =>
            candidate.Enabled &&
            candidate.ItemId.Equals(source.Id, StringComparison.OrdinalIgnoreCase));
        if (blueprint is null)
            return null;

        return new BlueprintItemMetadataDto
        {
            BlueprintId = blueprint.Id,
            Name = blueprint.Name,
            Description = blueprint.Description,
            RequiredRecipeTags = blueprint.RequiredRecipeTags,
            AnyRecipeTags = blueprint.AnyRecipeTags,
            CompatibleRecipeCount = _definitions.GetRecipes()
                .Count(recipe => Domain.Models.Professions.Crafting.V2.EquipmentCraftingDesignComposer.IsCompatible(recipe, blueprint)),
            SourceType = blueprint.SourceType,
            SourceId = blueprint.SourceId
        };
    }
}
