using Domain.Models.Inventories;

namespace Domain.Models.Dungeons.Runs;

public sealed class ClaimDungeonRewardsResult
{
    public required IReadOnlyList<InventoryItem> ClaimedLoot { get; init; }
}
