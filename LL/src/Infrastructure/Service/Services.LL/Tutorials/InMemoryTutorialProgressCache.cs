using System.Collections.Concurrent;
using Application.Interfaces.Services.LL.Tutorials;

namespace Services.LL.Tutorials;

public sealed class InMemoryTutorialProgressCache : ITutorialProgressCache
{
    private readonly ConcurrentDictionary<Guid, CachedTutorialProgress> _cache = new();

    public CachedTutorialProgress? Get(Guid characterId) =>
        _cache.TryGetValue(characterId, out var progress) ? progress : null;

    public void SetActive(Guid characterId, string tutorialId, string currentStep) =>
        _cache[characterId] = new CachedTutorialProgress(true, tutorialId, currentStep);

    public void SetInactive(Guid characterId) =>
        _cache[characterId] = new CachedTutorialProgress(false, null, null);

    public void Remove(Guid characterId) =>
        _cache.TryRemove(characterId, out _);
}
