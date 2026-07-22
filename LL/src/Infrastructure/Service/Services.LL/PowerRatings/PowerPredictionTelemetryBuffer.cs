using System.Collections.Concurrent;
using Application.Interfaces.Services.LL.PowerRatings;

namespace Services.LL.PowerRatings;

public sealed class PowerPredictionTelemetryBuffer : IPowerPredictionTelemetryBuffer
{
    private readonly ConcurrentDictionary<string, BufferedPrediction> _predictions = new(StringComparer.OrdinalIgnoreCase);

    public void Record(Guid characterId, string dungeonId, DungeonReadinessResult result)
    {
        PruneExpired();
        _predictions[Key(characterId, dungeonId)] = new BufferedPrediction(result, DateTimeOffset.UtcNow.AddHours(1));
    }

    public bool TryTake(Guid characterId, string dungeonId, out DungeonReadinessResult result)
    {
        if (_predictions.TryRemove(Key(characterId, dungeonId), out var prediction) &&
            prediction.ExpiresAtUtc >= DateTimeOffset.UtcNow)
        {
            result = prediction.Result;
            return true;
        }

        result = null!;
        return false;
    }

    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _predictions.Where(x => x.Value.ExpiresAtUtc < now))
            _predictions.TryRemove(item.Key, out _);
    }

    private static string Key(Guid characterId, string dungeonId) => $"{characterId:N}:{dungeonId}";
    private sealed record BufferedPrediction(DungeonReadinessResult Result, DateTimeOffset ExpiresAtUtc);
}
