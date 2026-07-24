using Domain.Models.Inventories;

namespace Domain.Models.CharacterActions.Sessions;

public sealed class CombatRewardBreakdown
{
    public IReadOnlyList<InventoryItem> PowerItems { get; init; } = [];
    public IReadOnlyList<InventoryItem> CraftingItems { get; init; } = [];
    public IReadOnlyList<InventoryItem> EssenceItems { get; init; } = [];
    public IReadOnlyList<InventoryItem> DungeonAccessItems { get; init; } = [];
}
