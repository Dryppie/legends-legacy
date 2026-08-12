namespace Services.LL.WorldTower;

public interface IWorldTowerWorkLeaseService
{
    Task<IReadOnlyList<Guid>> ClaimSimulationsAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ClaimPlaybackDispatchesAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken);

    Task ReleasePlaybackDispatchAsync(
        Guid attemptId,
        string owner,
        CancellationToken cancellationToken);
}
