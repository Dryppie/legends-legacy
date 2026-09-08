using Application.Common.Interfaces;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.WorldTower;

public sealed class WorldTowerProgressRepository(IDbContext db) : IWorldTowerProgressRepository
{
    public Task<bool> HasClearedFloorAsync(string serverId, int minimumFloor, CancellationToken cancellationToken) =>
        db.TowerFloorProgresses.AsNoTracking().AnyAsync(
            progress => progress.ServerId == serverId && progress.FloorNumber >= minimumFloor && progress.IsCleared,
            cancellationToken);
}
