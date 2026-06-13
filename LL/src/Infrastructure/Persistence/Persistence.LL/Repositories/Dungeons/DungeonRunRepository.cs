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

    public Task<bool> AddPendingRewardAsync(
        DungeonRun dungeonRun,
        RunReward reward,
        CancellationToken cancellationToken)
    {
        dungeonRun.PendingRewards.Add(reward);
        _context.RunRewards.Add(reward);

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

    public async Task<bool> HasCompletedDungeonAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken)
    {
        return await _context.DungeonCompletionRecords.AnyAsync(
            x => x.CharacterId == characterId && x.DungeonDefinitionId == dungeonDefinitionId,
            cancellationToken);
    }

    public async Task MarkDungeonCompletedAsync(Guid characterId, string dungeonDefinitionId, DateTimeOffset completedAt, CancellationToken cancellationToken)
    {
        var record = await _context.DungeonCompletionRecords.FirstOrDefaultAsync(
            x => x.CharacterId == characterId && x.DungeonDefinitionId == dungeonDefinitionId,
            cancellationToken);

        if (record is null)
        {
            await _context.DungeonCompletionRecords.AddAsync(new DungeonCompletionRecord
            {
                Id = Guid.NewGuid(),
                CharacterId = characterId,
                DungeonDefinitionId = dungeonDefinitionId,
                FirstCompletedAt = completedAt,
                LastCompletedAt = completedAt,
                CompletionCount = 1
            }, cancellationToken);

            return;
        }

        record.LastCompletedAt = completedAt;
        record.CompletionCount++;
    }

    public Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
