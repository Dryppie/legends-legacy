using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonPreviewRewardService
{
    Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken);
}
