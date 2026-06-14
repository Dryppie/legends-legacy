using Domain.Models.Inventories;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleDungeonSigilDropCalculator
{
    Task<IReadOnlyList<InventoryItem>> RollAsync(
        Area area,
        int eligibleVictories,
        CancellationToken cancellationToken);
}
