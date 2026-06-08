using Application.Common.Interfaces;
using Domain.Models.Dungeons.Runs;
using Microsoft.EntityFrameworkCore;

namespace Persistence.LL.Repositories.Dungeons;

public class DungeonRunRepository : IDungeonRunRepository
{
    private readonly IDbContext _context;
    public DungeonRunRepository(IDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken)
    {
        await _context.DungeonRuns.AddAsync(dungeonRun, cancellationToken);

        return true;
    }

    public Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken)
    {
        _context.DungeonRuns.Remove(dungeonRun);

        return Task.FromResult(true);
    }

    public async Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.DungeonRuns
            .Include(x => x.Rooms.OrderBy(r => r.RoomIndex))
            .Include(x => x.PendingRewards)
            .FirstOrDefaultAsync(x => x.CharacterId.Equals(characterId), cancellationToken);
    }

    public async Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid dungeonId, CancellationToken cancellationToken)
    {
        return await _context.DungeonRuns
             .Include(x => x.Rooms.OrderBy(r => r.RoomIndex))
             .Include(x => x.PendingRewards)
             .FirstOrDefaultAsync(x => x.Id.Equals(dungeonId), cancellationToken);
    }

    public Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
