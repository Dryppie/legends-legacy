namespace Domain.Models.Professions.Crafting.V2;

public sealed class MaterialDefinition
{
    public string Id { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MaterialFamily Family { get; init; }
    public int? Tier { get; init; }
    public bool IsStandardTieredMaterial { get; init; }
    public bool IsSpecialResource { get; init; }
}
