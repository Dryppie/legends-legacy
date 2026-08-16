namespace Application.Interfaces.Services.LL.Quests;

public interface ICombatAreaAccessService
{
    Task<CombatAreaAccessResult> GetAccessAsync(
        Guid characterId,
        string areaId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CombatAreaAccessResult>> GetAllAccessAsync(
        Guid characterId,
        CancellationToken cancellationToken);
}

public sealed record CombatAreaAccessResult(
    string AreaId,
    bool CanAccess,
    bool IsVisible,
    int RequiredLevel,
    int? CharacterLevel,
    IReadOnlyList<string> RequiredQuestIds,
    IReadOnlyList<string> UnmetQuestIds,
    int? RequiredTowerFloor,
    bool IsRequiredTowerFloorCleared,
    string? ReasonCode,
    string? PlayerMessage);
