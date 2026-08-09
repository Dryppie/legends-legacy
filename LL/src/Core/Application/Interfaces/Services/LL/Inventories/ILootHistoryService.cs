using Application.UseCases.Inventories.Dtos;
using Application.UseCases.LootHistory.Dtos;

namespace Application.Interfaces.Services.LL.Inventories;

public interface ILootHistoryService
{
    Task<IReadOnlyList<LootHistoryEntryDto>> GetRecentAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task RecordAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItemDto> items,
        string source,
        CancellationToken cancellationToken);

    Task<int> ClearAsync(Guid characterId, CancellationToken cancellationToken);
}
