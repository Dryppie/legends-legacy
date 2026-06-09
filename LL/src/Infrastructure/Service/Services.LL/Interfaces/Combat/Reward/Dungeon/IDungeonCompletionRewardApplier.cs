using Domain.Models.Dungeons.Runs;

namespace Services.LL.Interfaces.Combat.Reward.Dungeon;

public interface IDungeonCompletionRewardApplier
{
    Task ApplyAsync(DungeonRun run, CancellationToken cancellationToken);
}
