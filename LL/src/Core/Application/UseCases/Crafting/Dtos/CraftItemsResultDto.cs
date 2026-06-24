using Application.UseCases.Inventories.Dtos;
using Domain.Models.Items;

namespace Application.UseCases.Crafting.Dtos;

public sealed class CraftItemsResultDto
{
    public string RecipeId { get; init; } = string.Empty;
    public int TargetTier { get; init; }
    public IReadOnlyList<Guid> CreatedItemIds { get; init; } = [];
    public IReadOnlyList<InventoryItemDto> CreatedItems { get; init; } = [];
    public IReadOnlyDictionary<ItemQuality, int> QualityCounts { get; init; } = new Dictionary<ItemQuality, int>();
    public int MasteryXpGained { get; init; }
    public int NewMasteryLevel { get; init; }
}
