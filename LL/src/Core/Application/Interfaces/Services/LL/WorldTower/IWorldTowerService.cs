using Application.UseCases.WorldTower.Dtos;
using Application.UseCases.CharacterActions.Dtos.Responses.CombatDtos;
using Domain.Models.WorldTower;

namespace Application.Interfaces.Services.LL.WorldTower;

public interface IWorldTowerDefinitionProvider
{
    IReadOnlyList<TowerFloorDefinition> GetFloors();
    TowerFloorDefinition? GetFloor(int floorNumber);
}

public interface IWorldTowerService
{
    Task<TowerOverviewDto> GetOverviewAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TowerFloorDetailDto?> GetFloorAsync(Guid characterId, int floorNumber, CancellationToken cancellationToken);
    Task<TowerRallyDto?> GetRallyAsync(Guid characterId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerBattleReportDto?> GetAttemptReportAsync(Guid characterId, Guid attemptId, CancellationToken cancellationToken);
    Task<CombatResultDto?> GetAttemptCombatResultAsync(Guid characterId, Guid attemptId, CancellationToken cancellationToken);
    Task<TowerCombatPlaybackDto?> GetAttemptPlaybackAsync(Guid characterId, Guid attemptId, CancellationToken cancellationToken);
    Task<TowerPlaybackBundleContentDto?> GetAttemptPlaybackBundleAsync(Guid characterId, Guid attemptId, CancellationToken cancellationToken);
    Task<TowerCombatFrameBatchDto?> GetAttemptPlaybackFramesAsync(Guid characterId, Guid attemptId, int afterSequence, CancellationToken cancellationToken);
    Task<IReadOnlyList<TowerHallOfFameEntryDto>> GetHallOfFameAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TowerPersonalExpeditionDto>> GetPersonalExpeditionsAsync(Guid characterId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> CreateRallyAsync(Guid characterId, int floorNumber, TowerRallyMode mode, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> ApplyToRallyAsync(Guid characterId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> AcceptRallyApplicationAsync(Guid characterId, Guid rallyId, Guid applicationId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> DeclineRallyApplicationAsync(Guid characterId, Guid rallyId, Guid applicationId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> LeaveRallyAsync(Guid characterId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> UpdateRallyLoadoutAsync(Guid characterId, Guid rallyId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> UpdateRallyPartiesAsync(Guid characterId, Guid rallyId, IReadOnlyList<TowerPartyAssignment> assignments, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerRallyDto>> TransferRallyLeadershipAsync(Guid characterId, Guid rallyId, Guid targetCharacterId, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerAttemptResultDto>> StartRallyAsync(Guid characterId, Guid rallyId, CancellationToken cancellationToken);
    Task<bool> SimulateQueuedAttemptAsync(Guid attemptId, string leaseOwner, CancellationToken cancellationToken);
    Task<bool> PublishDuePlaybackFrameAsync(Guid attemptId, string leaseOwner, DateTimeOffset now, CancellationToken cancellationToken);
    Task<TowerOperationResult<TowerFloorDetailDto>> ContributeAsync(Guid characterId, int floorNumber, TowerContributionKind kind, int amount, CancellationToken cancellationToken);
}

public sealed record TowerOperationResult<T>(T? Value, string? Error)
{
    public bool Succeeded => Error is null;
    public static TowerOperationResult<T> Success(T value) => new(value, null);
    public static TowerOperationResult<T> Fail(string error) => new(default, error);
}
