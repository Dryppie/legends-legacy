namespace Domain.Models.Dungeons.Runs;

public interface IDungeonRunRepository
{
    Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
    Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid dungeonId, CancellationToken cancellationToken);
    Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
}
