using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Synchronization;
using Microsoft.EntityFrameworkCore;

namespace Services.LL.Synchronization;

public sealed class StateSyncService(
    IDbContext context,
    IGameRealtimeBroadcaster realtimeBroadcaster,
    TimeProvider timeProvider) : IStateSyncService
{
    public const string CharacterScope = "character";
    public const string MarketplaceScope = "marketplace";
    public const string GuildScope = "guild";
    public const string ColosseumScope = "colosseum";
    public const string TournamentScope = "tournament";

    private readonly HashSet<(Guid TransactionId, string ScopeKey)> _invalidatedScopes = [];

    public Task InvalidateCharacterAsync(
        Guid characterId,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateAsync(
            $"{CharacterScope}:{characterId:N}",
            CharacterScope,
            characterId,
            new Audience.Character(characterId),
            reason,
            cancellationToken);

    public Task InvalidateWorldScopeAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return InvalidateAsync(
            $"world:{scope}",
            scope,
            null,
            new Audience.World(),
            reason,
            cancellationToken);
    }

    public async Task<StateSyncCheckpoint> GetCheckpointAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var characterKey = $"{CharacterScope}:{characterId:N}";
        string[] worldScopes =
        [
            MarketplaceScope,
            GuildScope,
            ColosseumScope,
            TournamentScope
        ];
        var worldKeys = worldScopes.Select(scope => $"world:{scope}").ToArray();
        var checkpointKeys = worldKeys.Append(characterKey).ToArray();
        var revisions = await context.StateSyncRevisions
            .AsNoTracking()
            .Where(x => checkpointKeys.Contains(x.ScopeKey))
            .ToDictionaryAsync(x => x.ScopeKey, x => x.Revision, cancellationToken);

        var checkpointRevisions = worldScopes.ToDictionary(
            scope => scope,
            scope => revisions.GetValueOrDefault($"world:{scope}"),
            StringComparer.Ordinal);
        checkpointRevisions[CharacterScope] = revisions.GetValueOrDefault(characterKey);

        return new StateSyncCheckpoint(
            characterId,
            checkpointRevisions,
            timeProvider.GetUtcNow());
    }

    private async Task InvalidateAsync(
        string scopeKey,
        string publicScope,
        Guid? characterId,
        Audience audience,
        string reason,
        CancellationToken cancellationToken)
    {
        var transactionId = context.CurrentTransaction?.TransactionId ?? Guid.Empty;
        if (!_invalidatedScopes.Add((transactionId, scopeKey)))
        {
            return;
        }

        await context.AcquireStateSyncScopeLockAsync(scopeKey, cancellationToken);

        var revision = context.StateSyncRevisions.Local
            .SingleOrDefault(x => x.ScopeKey == scopeKey)
            ?? await context.StateSyncRevisions
                .SingleOrDefaultAsync(x => x.ScopeKey == scopeKey, cancellationToken);

        if (revision is null)
        {
            revision = new StateSyncRevision
            {
                ScopeKey = scopeKey,
                Revision = 1,
                UpdatedAt = timeProvider.GetUtcNow()
            };
            context.StateSyncRevisions.Add(revision);
        }
        else
        {
            revision.Revision++;
            revision.UpdatedAt = timeProvider.GetUtcNow();
        }

        await realtimeBroadcaster.PublishAsync(
            audience,
            new StateInvalidated(characterId, publicScope, revision.Revision, reason),
            nameof(StateSyncService),
            cancellationToken);
    }
}
