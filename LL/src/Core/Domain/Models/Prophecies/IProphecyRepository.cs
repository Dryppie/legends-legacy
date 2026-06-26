namespace Domain.Models.Prophecies;

public interface IProphecyRepository
{
    Task<IReadOnlyList<ProphecyDefinition>> GetEnabledDefinitionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProphecyDefinition>> SyncDefinitionsAsync(IReadOnlyCollection<ProphecyDefinition> definitions, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerProphecyInstance>> GetInstancesForPeriodAsync(
        Guid playerId,
        Guid characterId,
        ProphecyScope scope,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    Task<PlayerProphecyInstance?> GetInstanceAsync(Guid instanceId, Guid playerId, Guid characterId, CancellationToken cancellationToken);
    Task AddInstancesAsync(IReadOnlyCollection<PlayerProphecyInstance> instances, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressAsync(
        Guid characterId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerProphecyInstance>> GetRecentInstancesAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken);

    Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken);

    Task AddWeeklyProgressAsync(WeeklyRevelationProgress progress, CancellationToken cancellationToken);
}
