using Domain.Models.Bonuses;
using Services.LL.Interfaces;

namespace Services.LL.Bonuses;
public sealed class BonusService : IBonusService
{
    private readonly IEnumerable<IBonusProvider> _providers;

    public BonusService(IEnumerable<IBonusProvider> providers)
    {
        _providers = providers;
    }

    public async ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(Guid characterId, DateTimeOffset now, CancellationToken ct = default)
    {
        // 1. ask all providers in parallel
        var accumulator = new Dictionary<BonusKind, double>();

        foreach (var provider in _providers)
        {
            // 2. merge by (Kind, Mode)
            var bonuses = await provider.GetBonusesAsync(characterId, now, ct);
            foreach (var bonus in bonuses)
            {
                accumulator[bonus.Kind] = accumulator.GetValueOrDefault(bonus.Kind) + bonus.Value;
            }
        }

        return accumulator;
    }
}
