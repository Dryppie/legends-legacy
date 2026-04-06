using Application.Common.Interfaces;
using Domain.Models.Dungeons.Runs;

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

    public async Task<DungeonRun?> GetDungeonRunAsync(Guid characterId, CancellationToken cancellationToken)
    {
        return await _context.DungeonRuns.FindAsync([characterId], cancellationToken);
    }

    public Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
