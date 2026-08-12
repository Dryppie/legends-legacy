using Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Services.LL.WorldTower;

public sealed class WorldTowerWorkLeaseService(
    IDbContext db,
    IOptions<WorldTowerOptions> options) : IWorldTowerWorkLeaseService
{
    private readonly TimeSpan leaseDuration = TimeSpan.FromSeconds(options.Value.WorkerLeaseSeconds);

    public Task<IReadOnlyList<Guid>> ClaimSimulationsAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken) =>
        db.ClaimWorldTowerSimulationsAsync(
            owner,
            now,
            now.Add(leaseDuration),
            limit,
            cancellationToken);

    public Task<IReadOnlyList<Guid>> ClaimPlaybackDispatchesAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken) =>
        db.ClaimWorldTowerPlaybackDispatchesAsync(
            owner,
            now,
            now.Add(leaseDuration),
            limit,
            cancellationToken);

    public async Task ReleasePlaybackDispatchAsync(
        Guid attemptId,
        string owner,
        CancellationToken cancellationToken)
    {
        await db.ReleaseWorldTowerPlaybackDispatchAsync(
            attemptId,
            owner,
            cancellationToken);
    }
}
