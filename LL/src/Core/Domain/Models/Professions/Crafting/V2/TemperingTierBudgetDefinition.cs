using Domain.Models.Items;

namespace Domain.Models.Professions.Crafting.V2;

public sealed class TemperingTierBudgetDefinition
{
    public Rarity Rarity { get; init; }
    public int ProgressRequired { get; init; }
}
