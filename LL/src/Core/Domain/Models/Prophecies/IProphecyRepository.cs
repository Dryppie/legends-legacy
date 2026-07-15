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

    Task<IReadOnlyList<PlayerProphecyInstance>> GetAcceptedInstancesForProgressWindowAsync(
        Guid characterId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PlayerProphecyInstance>> GetRecentInstancesAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset since,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> GetRecentDefinitionIdsAsync(
        Guid playerId,
        Guid characterId,
        ProphecyScope scope,
        DateTimeOffset since,
        DateTimeOffset before,
        CancellationToken cancellationToken);

    Task<bool> TryConsumeDailyRerollAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        DateTimeOffset usedAt,
        CancellationToken cancellationToken);

    Task<DailyProphecyRerollState?> GetDailyRerollStateAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken) =>
        Task.FromResult<DailyProphecyRerollState?>(null);

    Task AddDailyRerollStateAsync(
        DailyProphecyRerollState state,
        CancellationToken cancellationToken) => Task.CompletedTask;

    Task<bool> TrySpendFateEchoAsync(
        Guid characterId,
        long amount,
        CancellationToken cancellationToken);

    Task<WeeklyRevelationProgress?> GetWeeklyProgressAsync(
        Guid playerId,
        Guid characterId,
        DateTimeOffset periodStart,
        CancellationToken cancellationToken);

    Task AddWeeklyProgressAsync(WeeklyRevelationProgress progress, CancellationToken cancellationToken);
}
