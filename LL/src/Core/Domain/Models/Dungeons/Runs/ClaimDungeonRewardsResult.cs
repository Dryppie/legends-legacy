using Domain.Models.Inventories;

namespace Domain.Models.Dungeons.Runs;

public sealed class ClaimDungeonRewardsResult
{
    public required IReadOnlyList<InventoryItem> ClaimedLoot { get; init; }
    public bool WasCompleted { get; init; }
    public string DungeonDefinitionId { get; init; } = string.Empty;
    public string DungeonName { get; init; } = string.Empty;
    public bool CompletedWithoutDefeat { get; init; }
    public bool CompletedWithoutRetreat { get; init; }
    public bool CompletedWithoutWeapon { get; init; }
    public IReadOnlyList<string> DefeatedBossKeys { get; init; } = [];
}
