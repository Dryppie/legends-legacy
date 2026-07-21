using Domain.Models.Prophecies;

namespace Application.Interfaces.Services.LL.Prophecies;

public interface IProphecyService
{
    Task<PropheciesOverview> GetOverviewAsync(Guid playerId, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProphecyOperationResult<PropheciesOverview>> AcceptAsync(Guid playerId, Guid characterId, Guid prophecyId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProphecyOperationResult<PropheciesOverview>> RerollAsync(Guid playerId, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProphecyOperationResult<ProphecyClaimResult>> ClaimAsync(Guid playerId, Guid characterId, Guid prophecyId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProphecyOperationResult<WeeklyRevelationClaimResult>> ClaimWeeklyMilestoneAsync(Guid playerId, Guid characterId, int favorRequired, DateTimeOffset now, CancellationToken cancellationToken);
    Task<ProphecyOperationResult<ProphecyCacheOpenResult>> OpenCacheAsync(Guid characterId, string cacheItemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(ProphecyProgressEvent progressEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(IReadOnlyList<ProphecyProgressEvent> progressEvents, CancellationToken cancellationToken);
}

public sealed record ProphecyProgressEvent(
    Guid CharacterId,
    DateTimeOffset OccurredAt,
    ProphecyProgressKind Kind,
    int Amount = 1,
    string? CreatureDefinitionId = null,
    int? EnemyCount = null,
    string? Profession = null,
    string? ResourceId = null,
    int? PotentialSpent = null);

public enum ProphecyProgressKind
{
    CreatureDefeated = 1,
    EncounterWon = 2,
    EncounterLost = 3,
    DungeonRoomCleared = 4,
    DungeonCompleted = 5,
    EssenceXpGained = 7,
    EssenceAbsorbed = 8,
    ResourceGathered = 9,
    ItemTempered = 10,
    PotentialSpent = 11,
    TreasureProgress = 12
}
