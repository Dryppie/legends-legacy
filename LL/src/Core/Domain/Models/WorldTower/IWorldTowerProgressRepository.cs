namespace Domain.Models.WorldTower;

public interface IWorldTowerProgressRepository
{
    Task<bool> HasClearedFloorAsync(string serverId, int minimumFloor, CancellationToken cancellationToken);
}
