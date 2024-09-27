using Application.Common.Interfaces;
using Common.Exceptions;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Interfaces;

namespace Persistence.LL.Repositories.LootTables;
public class LootTableRepository : ILootTableRepository
{
    private readonly IUnitOfWork _unitOfWork;

    public LootTableRepository(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LootTable> GetLootTableByIdAsync(Guid lootTableId, CancellationToken cancellationToken)
    {
        var lootTable = await _unitOfWork.Context.LootTables.Include(lt => lt.Items)
            .FirstOrDefaultAsync(lt => lt.Id.Equals(lootTableId), cancellationToken);
        NotFoundException.ThrowIfNull(lootTable, nameof(lootTable), lootTableId);

        return lootTable;
    }

    public async Task<LootTable> GetGatheringNodeLootTableAsync(Guid gatheringNodeId, CancellationToken cancellationToken)
    {
        var gatheringLootTable = await _unitOfWork.Context.GatheringNodes
            .Where(gn => gn.Id == gatheringNodeId)
            .Select(gn => gn.LootTable)
            .FirstOrDefaultAsync(cancellationToken);

        NotFoundException.ThrowIfNull(gatheringLootTable, nameof(gatheringLootTable), gatheringNodeId);

        return gatheringLootTable;
    }

    public Task<LootTable> GetMonsterLootTableAsync(Guid monsterId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<LootTable> GetProfessionTaskLootTableAsync(Guid professionTaskId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}