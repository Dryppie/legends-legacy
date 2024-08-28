using Application.Interfaces.Services.LL;
using Domain.Models.LootTables;

namespace Services.LL.LootTables;
public class LootTableService : ILootTableService
{
    private readonly ILootTableRepository _lootTableRepository;
    public LootTableService(ILootTableRepository lootTableRepository)
    {
        _lootTableRepository = lootTableRepository;
    }

    public Task<LootTable> GetLootTableByIdAsync(Guid lootTableId, CancellationToken cancellationToken)
    {
        return _lootTableRepository.GetLootTableByIdAsync(lootTableId, cancellationToken);
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