using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonRunRewardClaimer
{
    Task<IReadOnlyList<InventoryItem>> ClaimAsync(DungeonRun run, CancellationToken cancellationToken);
}
