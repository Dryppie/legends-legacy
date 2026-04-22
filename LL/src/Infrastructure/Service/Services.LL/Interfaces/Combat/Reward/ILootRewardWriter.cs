using Domain.Models.Inventories;

namespace Services.LL.Interfaces.Combat.Reward;

public interface ILootRewardWriter
{
    Task AddLootAsync(
        Guid characterId,
        IReadOnlyCollection<InventoryItem> items,
        CancellationToken cancellationToken);
}
