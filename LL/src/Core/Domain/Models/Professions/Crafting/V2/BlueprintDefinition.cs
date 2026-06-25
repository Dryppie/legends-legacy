namespace Domain.Models.Professions.Crafting.V2;

public sealed class BlueprintDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? BlueprintFamily { get; init; }
    public string UnlocksRecipeId { get; init; } = string.Empty;
    public string? ItemId { get; init; }
    public string? SourceType { get; init; }
    public string? SourceId { get; init; }
    public IReadOnlyList<string> AllowedBaseRecipeIds { get; init; } = [];
    public IReadOnlyList<string> AllowedRecipeTags { get; init; } = [];
    public string? OutputNameTemplate { get; init; }
    public IReadOnlyList<BlueprintOutputNameDefinition> SpecialOutputNames { get; init; } = [];
    public IReadOnlyList<MaterialRequirementDefinition> SpecialResourceRequirements { get; init; } = [];
    public IReadOnlyList<string> Tags { get; init; } = [];
    public TemperingRecipeDefinition? TemperingProfile { get; init; }
}
