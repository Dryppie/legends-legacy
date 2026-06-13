using Domain.Models.Dungeons.Runs;

namespace Application.Interfaces.Services.LL.Dungeons;

public interface IDungeonRunService
{
    Task<bool> ClaimRewardsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> DismissFailedRunAsync(Guid characterId, CancellationToken cancellationToken);
    Task<ExecuteDungeonActionResult?> ExecuteActionAsync(Guid runId, string actionId, object? payload, CancellationToken cancellationToken);
    Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(
        Guid characterId,
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(
        IReadOnlyCollection<string> dungeonDefinitionIds,
        CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken);
    Task<DungeonRun?> StartRunAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken);
    //Task<DungeonRun> WithdrawAsync(Guid runId, CancellationToken ct);
    //Task<DungeonRun> SelectTreasureOptionAsync(Guid runId, int optionIndex, CancellationToken ct);
    //Task<DungeonRun> SelectShrineBlessingAsync(Guid runId, Guid blessingId, CancellationToken ct);
}
