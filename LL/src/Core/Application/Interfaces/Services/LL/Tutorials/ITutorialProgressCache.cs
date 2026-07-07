namespace Application.Interfaces.Services.LL.Tutorials;

public interface ITutorialProgressCache
{
    CachedTutorialProgress? Get(Guid characterId);
    void SetActive(Guid characterId, string tutorialId, string currentStep);
    void SetInactive(Guid characterId);
    void Remove(Guid characterId);
}

public sealed record CachedTutorialProgress(
    bool IsActive,
    string? TutorialId,
    string? CurrentStep);
