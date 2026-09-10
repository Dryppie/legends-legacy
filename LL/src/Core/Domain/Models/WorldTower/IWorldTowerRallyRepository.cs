namespace Domain.Models.WorldTower;

public interface IWorldTowerRallyRepository
{
    Task<IReadOnlyList<TowerRally>> GetActiveForFloorsAsync(
        string serverId, int[] floorNumbers, CancellationToken cancellationToken);
    Task<TowerRally?> GetDetailsAsync(string serverId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerRally?> GetForApplicationAsync(string serverId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerRally?> GetWithApplicationSnapshotsAsync(string serverId, Guid rallyId, CancellationToken cancellationToken);
}
