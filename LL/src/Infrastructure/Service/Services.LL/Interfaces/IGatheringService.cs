using Domain.Models.Inventories;

namespace Services.LL.Interfaces;
public interface IGatheringService
{
    /// <summary>
    /// Perform gathering
    /// </summary>
    /// <param name="lootTableId"></param>
    /// <param name="actionsToPerform"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<InventoryItem>> PerformGatheringAsync(Guid lootTableId, int actionsToPerform, CancellationToken cancellationToken);
}