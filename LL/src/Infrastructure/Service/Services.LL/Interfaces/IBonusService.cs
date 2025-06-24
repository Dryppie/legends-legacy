using Domain.Models.Bonuses;

namespace Services.LL.Interfaces;
public interface IBonusService
{
    ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default);
}
