using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class WeightedAffixDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Rarity MinRarity { get; init; } = Rarity.Uncommon;
    public int Weight { get; init; } = 1;
    public WeightedStatDefinition StatModifier { get; init; } = new();
}
