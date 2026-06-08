using Domain.Models.Dungeons.Runs;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonRunRewardClaimer
{
    Task ClaimAsync(DungeonRun run, CancellationToken cancellationToken);
}
