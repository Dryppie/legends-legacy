using Domain.Models.Inventories;

namespace Domain.Models.Dungeons.Runs;

public sealed class ClaimDungeonRewardsResult
{
    public required IReadOnlyList<InventoryItem> ClaimedLoot { get; init; }
    public bool WasCompleted { get; init; }
    public string DungeonDefinitionId { get; init; } = string.Empty;
    public bool CompletedWithoutDefeat { get; init; }
    public bool CompletedWithoutCheckpointRetreat { get; init; }
    public IReadOnlyList<string> DefeatedBossKeys { get; init; } = [];
}
