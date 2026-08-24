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

    public Task<IReadOnlyList<Guid>> ClaimPlaybackFinalizationsAsync(
        string owner,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken) =>
        db.ClaimWorldTowerPlaybackFinalizationsAsync(
            owner,
            now,
            now.Add(leaseDuration),
            limit,
            cancellationToken);

    public Task<bool> RenewSimulationAsync(
        Guid attemptId,
        string owner,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        db.RenewWorldTowerSimulationLeaseAsync(
            attemptId,
            owner,
            now.Add(leaseDuration),
            cancellationToken);

    public async Task ReleasePlaybackFinalizationAsync(
        Guid attemptId,
        string owner,
        CancellationToken cancellationToken)
    {
        await db.ReleaseWorldTowerPlaybackFinalizationAsync(
            attemptId,
            owner,
            cancellationToken);
    }
}
