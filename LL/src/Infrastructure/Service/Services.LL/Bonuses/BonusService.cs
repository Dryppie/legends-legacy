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
        var tasks = _providers.Select(p => p.GetBonusesAsync(characterId, now, ct).AsTask());
        var results = await Task.WhenAll(tasks);

        // 2. merge by (Kind, Mode)
        var accumulator = new Dictionary<BonusKind, double>();

        foreach (var bonus in results.SelectMany(b => b))
        {
            accumulator[bonus.Kind] = accumulator.GetValueOrDefault(bonus.Kind) + bonus.Value;
        }

        return accumulator;
    }
}
