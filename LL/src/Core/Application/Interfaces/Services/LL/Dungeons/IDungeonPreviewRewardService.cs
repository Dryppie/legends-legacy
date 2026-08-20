using Domain.Models.Dungeons;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonPreviewRewardService
{
    Task<IReadOnlyList<DungeonPreviewReward>> GetPossibleCompletionRewardsAsync(
        DungeonDefinition dungeon,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, IReadOnlyList<DungeonPreviewReward>>> GetPossibleCompletionRewardsAsync(
        IReadOnlyCollection<DungeonDefinition> dungeons,
        CancellationToken cancellationToken);
}
