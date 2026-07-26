using Application.Interfaces.Services.LL.PowerRatings;
using System.Collections.Immutable;

namespace Services.LL.PowerRatings;

public sealed class DungeonPowerRecommendationStore : IDungeonPowerRecommendationStore
{
    private int _isCalibrationComplete;
    private ImmutableDictionary<string, DungeonPowerRecommendation> _recommendations =
        ImmutableDictionary.Create<string, DungeonPowerRecommendation>(StringComparer.OrdinalIgnoreCase);

    public bool IsCalibrationComplete => Volatile.Read(ref _isCalibrationComplete) == 1;

    public bool TryGet(string dungeonId, out DungeonPowerRecommendation recommendation) =>
        _recommendations.TryGetValue(dungeonId, out recommendation!);

    public IReadOnlyDictionary<string, DungeonPowerRecommendation> GetAll() =>
        Volatile.Read(ref _recommendations);

    public void MarkCalibrationComplete() =>
        Interlocked.Exchange(ref _isCalibrationComplete, 1);

    public void Publish(IReadOnlyDictionary<string, DungeonPowerRecommendation> recommendations)
    {
        var accepted = recommendations
            .Where(entry => entry.Value.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence)
            .ToImmutableDictionary(
                entry => entry.Key,
                entry => entry.Value,
                StringComparer.OrdinalIgnoreCase);
        Interlocked.Exchange(ref _recommendations, accepted);
    }

    public bool Remove(string dungeonId) =>
        ImmutableInterlocked.TryRemove(ref _recommendations, dungeonId, out _);

    public void Set(string dungeonId, DungeonPowerRecommendation recommendation)
    {
        if (recommendation.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence)
            ImmutableInterlocked.AddOrUpdate(
                ref _recommendations,
                dungeonId,
                recommendation,
                (_, _) => recommendation);
    }
}
