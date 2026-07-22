using System.Collections.Concurrent;
using Application.Interfaces.Services.LL.PowerRatings;

namespace Services.LL.PowerRatings;

public sealed class DungeonPowerRecommendationStore : IDungeonPowerRecommendationStore
{
    private int _isCalibrationComplete;
    private readonly ConcurrentDictionary<string, DungeonPowerRecommendation> _recommendations =
        new(StringComparer.OrdinalIgnoreCase);

    public bool IsCalibrationComplete => Volatile.Read(ref _isCalibrationComplete) == 1;

    public bool TryGet(string dungeonId, out DungeonPowerRecommendation recommendation) =>
        _recommendations.TryGetValue(dungeonId, out recommendation!);

    public IReadOnlyDictionary<string, DungeonPowerRecommendation> GetAll() =>
        new Dictionary<string, DungeonPowerRecommendation>(
            _recommendations,
            StringComparer.OrdinalIgnoreCase);

    public void MarkCalibrationComplete() =>
        Interlocked.Exchange(ref _isCalibrationComplete, 1);

    public void Set(string dungeonId, DungeonPowerRecommendation recommendation)
    {
        if (recommendation.State is PowerAnalysisState.Available or PowerAnalysisState.LowConfidence)
            _recommendations[dungeonId] = recommendation;
    }
}
