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

    async Task InvalidateCharacterScopesAsync(
        Guid characterId,
        IReadOnlyCollection<string> scopes,
        string reason,
        CancellationToken cancellationToken = default)
    {
        foreach (var scope in scopes.Distinct(StringComparer.Ordinal))
        {
            await InvalidateCharacterScopeAsync(characterId, scope, reason, cancellationToken);
        }
    }

    Task AdvanceCharacterScopeAsync(
        Guid characterId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateCharacterScopeAsync(characterId, scope, reason, cancellationToken);

    async Task AdvanceCharacterScopesAsync(
        Guid characterId,
        IReadOnlyCollection<string> scopes,
        string reason,
        CancellationToken cancellationToken = default)
    {
        foreach (var scope in scopes.Distinct(StringComparer.Ordinal))
        {
            await AdvanceCharacterScopeAsync(characterId, scope, reason, cancellationToken);
        }
    }

    Task InvalidateWorldScopeAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default);

    Task AdvanceWorldScopeAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateWorldScopeAsync(scope, reason, cancellationToken);

    async Task<long> AdvanceWorldScopeWithRevisionAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await AdvanceWorldScopeAsync(scope, reason, cancellationToken);
        return GetChangedRevisions(null).GetValueOrDefault(scope);
    }

    Task InvalidateGuildScopeAsync(
        Guid guildId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateWorldScopeAsync(
            scope,
            reason,
            cancellationToken);

    Task InvalidateGuildScopeAsync(
        Guid guildId,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateGuildScopeAsync(guildId, StateSyncScopes.Guild, reason, cancellationToken);

    Task AdvanceGuildScopeAsync(
        Guid guildId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateGuildScopeAsync(guildId, scope, reason, cancellationToken);

    Task AdvanceGuildScopeAsync(
        Guid guildId,
        string reason,
        CancellationToken cancellationToken = default) =>
        AdvanceGuildScopeAsync(guildId, StateSyncScopes.Guild, reason, cancellationToken);

    Task<StateSyncCheckpoint> GetCheckpointAsync(
        Guid characterId,
        CancellationToken cancellationToken = default);
}
