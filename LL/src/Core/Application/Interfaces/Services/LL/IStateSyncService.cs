using Application.WebSockets.Contracts;

namespace Application.Interfaces.Services.LL;

public interface IStateSyncService
{
    IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId);

    Task InvalidateCharacterAsync(
        Guid characterId,
        string reason,
        CancellationToken cancellationToken = default);

    Task InvalidateCharacterScopeAsync(
        Guid characterId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default);

    Task InvalidateWorldScopeAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default);

    Task<StateSyncCheckpoint> GetCheckpointAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);
}
