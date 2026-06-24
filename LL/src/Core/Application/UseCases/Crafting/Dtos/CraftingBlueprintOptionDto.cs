using Domain.Models.Professions.Crafting.V2;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftingBlueprintOptionDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? BlueprintFamily { get; init; }
    public string OutputNameTemplate { get; init; } = string.Empty;
    public IReadOnlyList<BlueprintOutputNameDefinition> SpecialOutputNames { get; init; } = [];
    public IReadOnlyList<string> CompatibleFormIds { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<CraftingMaterialCostDto> MaterialCosts { get; init; } = [];
}
