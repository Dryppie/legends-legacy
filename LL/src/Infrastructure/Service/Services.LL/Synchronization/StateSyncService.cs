using Application.Common.Interfaces;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Domain.Models.Synchronization;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

namespace Services.LL.Synchronization;

public sealed class StateSyncService(
    IDbContext context,
    IGameRealtimeBroadcaster realtimeBroadcaster,
    TimeProvider timeProvider) : IStateSyncService
{
    private static readonly Meter Meter = new("LegendsLegacy.StateSync");
    private static readonly Counter<long> InvalidationCounter =
        Meter.CreateCounter<long>("state_sync.invalidations");
    private static readonly Counter<long> CheckpointCounter =
        Meter.CreateCounter<long>("state_sync.checkpoints");
    private readonly HashSet<(Guid TransactionId, string ScopeKey)> _invalidatedScopes = [];
    private readonly Dictionary<(Guid? CharacterId, string Scope), long> _changedRevisions = [];

    public IReadOnlyDictionary<string, long> GetChangedRevisions(Guid? characterId) =>
        _changedRevisions
            .Where(entry =>
                entry.Key.CharacterId is null || entry.Key.CharacterId == characterId)
            .ToDictionary(
                entry => entry.Key.Scope,
                entry => entry.Value,
                StringComparer.Ordinal);

    public Task InvalidateCharacterAsync(
        Guid characterId,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateCharacterScopeAsync(
            characterId,
            StateSyncScopes.Character,
            reason,
            cancellationToken);

    public Task InvalidateCharacterScopeAsync(
        Guid characterId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        return InvalidateAsync(
            GetCharacterScopeKey(characterId, scope),
            scope,
            characterId,
            new Audience.Character(characterId),
            reason,
            cancellationToken);
    }

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
        var characterKeys = StateSyncScopes.CharacterResources
            .ToDictionary(scope => scope, scope => GetCharacterScopeKey(characterId, scope));
        var worldKeys = StateSyncScopes.WorldResources
            .ToDictionary(scope => scope, scope => $"world:{scope}");
        var checkpointKeys = characterKeys.Values.Concat(worldKeys.Values).ToArray();
        var revisions = await context.StateSyncRevisions
            .AsNoTracking()
            .Where(x => checkpointKeys.Contains(x.ScopeKey))
            .ToDictionaryAsync(x => x.ScopeKey, x => x.Revision, cancellationToken);

        var checkpointRevisions = StateSyncScopes.WorldResources.ToDictionary(
            scope => scope,
            scope => revisions.GetValueOrDefault(worldKeys[scope]),
            StringComparer.Ordinal);
        foreach (var (scope, scopeKey) in characterKeys)
        {
            checkpointRevisions[scope] = revisions.GetValueOrDefault(scopeKey);
        }

        CheckpointCounter.Add(1);
        return new StateSyncCheckpoint(
            characterId,
            checkpointRevisions,
            timeProvider.GetUtcNow());
    }

    private static string GetCharacterScopeKey(Guid characterId, string scope) =>
        scope == StateSyncScopes.Character
            ? $"character:{characterId:N}"
            : $"character:{characterId:N}:{scope}";

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

        _changedRevisions[(characterId, publicScope)] = revision.Revision;
        InvalidationCounter.Add(
            1,
            new KeyValuePair<string, object?>("scope", publicScope),
            new KeyValuePair<string, object?>(
                "audience",
                characterId.HasValue ? "character" : "world"));

        await realtimeBroadcaster.PublishAsync(
            audience,
            new StateInvalidated(characterId, publicScope, revision.Revision, reason),
            nameof(StateSyncService),
            cancellationToken);
    }
}
