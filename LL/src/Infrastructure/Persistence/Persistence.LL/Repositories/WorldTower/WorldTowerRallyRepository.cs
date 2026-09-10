using Application.Common.Interfaces;
using Domain.Models.WorldTower;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.WorldTower;

public sealed class WorldTowerRallyRepository(IDbContext db) : IWorldTowerRallyRepository
{
    public async Task<IReadOnlyList<TowerRally>> GetActiveForFloorsAsync(
        string serverId,
        int[] floorNumbers,
        CancellationToken cancellationToken) =>
        await db.TowerRallies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(rally => rally.Participants)
            .Include(rally => rally.Applications)
            .Where(rally => rally.ServerId == serverId
                            && floorNumbers.Contains(rally.FloorNumber)
                            && (rally.Status == TowerRallyStatus.Recruiting
                                || rally.Status == TowerRallyStatus.Ready
                                || rally.Status == TowerRallyStatus.InProgress))
            .OrderBy(rally => rally.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<TowerRally?> GetDetailsAsync(
        string serverId,
        Guid rallyId,
        CancellationToken cancellationToken) =>
        db.TowerRallies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(rally => rally.Participants)
            .Include(rally => rally.Applications)
            .Include(rally => rally.Attempt)
                .ThenInclude(attempt => attempt!.Playback)
            .SingleOrDefaultAsync(rally => rally.Id == rallyId && rally.ServerId == serverId, cancellationToken);

    public Task<TowerRally?> GetForApplicationAsync(
        string serverId,
        Guid rallyId,
        CancellationToken cancellationToken) =>
        db.TowerRallies
            // Avoid multiplying participant rows by application rows; keep tracking for the mutation.
            .AsSplitQuery()
            .Include(rally => rally.Participants)
            .Include(rally => rally.Applications)
            .Include(rally => rally.Attempt)
            .SingleOrDefaultAsync(rally => rally.Id == rallyId && rally.ServerId == serverId, cancellationToken);

    public Task<TowerRally?> GetWithApplicationSnapshotsAsync(
        string serverId,
        Guid rallyId,
        CancellationToken cancellationToken) =>
        db.TowerRallies
            .AsSplitQuery()
            .Include(rally => rally.Participants)
            .Include(rally => rally.Applications)
                .ThenInclude(application => application.CharacterSnapshot)
            .Include(rally => rally.Attempt)
            .SingleOrDefaultAsync(rally => rally.Id == rallyId && rally.ServerId == serverId, cancellationToken);
}
