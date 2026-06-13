namespace Domain.Models.Dungeons.Runs;

public interface IDungeonRunRepository
{
    Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
    Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
    Task<bool> AddPendingRewardAsync(DungeonRun dungeonRun, RunReward reward, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid dungeonId, CancellationToken cancellationToken);
    Task<bool> HasCompletedDungeonAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken);
    Task MarkDungeonCompletedAsync(Guid characterId, string dungeonDefinitionId, DateTimeOffset completedAt, CancellationToken cancellationToken);
    Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
}
