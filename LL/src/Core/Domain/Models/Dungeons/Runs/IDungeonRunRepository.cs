namespace Domain.Models.Dungeons.Runs;

public interface IDungeonRunRepository
{
    Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
    Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken);
    Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken);
}
