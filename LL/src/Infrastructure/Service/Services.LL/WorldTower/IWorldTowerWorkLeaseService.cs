namespace Services.LL.WorldTower;

public interface IWorldTowerWorkLeaseService
{
    Task<IReadOnlyList<Guid>> ClaimSimulationsAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ClaimPlaybackFinalizationsAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> RenewSimulationAsync(
        Guid attemptId,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task ReleasePlaybackFinalizationAsync(
        Guid attemptId,
        string owner,
        CancellationToken cancellationToken);
}
