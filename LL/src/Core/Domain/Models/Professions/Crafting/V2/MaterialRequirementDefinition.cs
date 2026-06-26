namespace Domain.Models.Professions.Crafting.V2;

public sealed class MaterialRequirementDefinition
{
    public RequirementType Type { get; init; } = RequirementType.TieredMaterial;
    public MaterialFamily? Family { get; init; }
    public string? ItemId { get; init; }
    public int? MinimumTier { get; init; }
    public int? TierOffset { get; init; }
    public int BaseAmount { get; init; }
    public int AmountPerTier { get; init; }
}
