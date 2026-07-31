namespace Domain.Models.Professions.Crafting.V2;

public sealed class ResolvedMaterialCost
{
    public string ItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MaterialFamily Family { get; init; }
    public int? Tier { get; init; }
    public int Quantity { get; init; }
    public IReadOnlyList<string> Sources { get; init; } = [];
}
