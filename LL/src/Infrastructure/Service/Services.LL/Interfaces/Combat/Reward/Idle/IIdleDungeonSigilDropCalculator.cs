using Domain.Models.Bonuses;
using Domain.Models.Inventories;
using Domain.Models.Regions.Areas;

namespace Services.LL.Interfaces.Combat.Reward.Idle;

public interface IIdleDungeonSigilDropCalculator
{
    Task<IReadOnlyList<InventoryItem>> RollAsync(
        Guid characterId,
        Area area,
        int eligibleVictories,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<BonusKind, double>? bonusFactors = null);
}
