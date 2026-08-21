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
            publishRealtime: true,
            cancellationToken);
    }

    public Task AdvanceCharacterScopeAsync(
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
            publishRealtime: false,
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
            publishRealtime: true,
            cancellationToken);
    }

    public Task AdvanceWorldScopeAsync(
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
            publishRealtime: false,
            cancellationToken);
    }

    public async Task<long> AdvanceWorldScopeWithRevisionAsync(
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await AdvanceWorldScopeAsync(scope, reason, cancellationToken);
        return GetChangedRevisions(null).GetValueOrDefault(scope);
    }

    public Task InvalidateGuildScopeAsync(
        Guid guildId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (guildId == Guid.Empty)
        {
            throw new ArgumentException("Guild id is required.", nameof(guildId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        if (!StateSyncScopes.GuildResources.Contains(scope, StringComparer.Ordinal))
        {
            throw new ArgumentException($"'{scope}' is not a guild synchronization scope.", nameof(scope));
        }

        return InvalidateAsync(
            GetGuildScopeKey(guildId, scope),
            scope,
            null,
            new Audience.Guild(guildId),
            reason,
            publishRealtime: true,
            cancellationToken);
    }

    public Task InvalidateGuildScopeAsync(
        Guid guildId,
        string reason,
        CancellationToken cancellationToken = default) =>
        InvalidateGuildScopeAsync(
            guildId,
            StateSyncScopes.Guild,
            reason,
            cancellationToken);

    public Task AdvanceGuildScopeAsync(
        Guid guildId,
        string scope,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (guildId == Guid.Empty)
        {
            throw new ArgumentException("Guild id is required.", nameof(guildId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        if (!StateSyncScopes.GuildResources.Contains(scope, StringComparer.Ordinal))
        {
            throw new ArgumentException($"'{scope}' is not a guild synchronization scope.", nameof(scope));
        }

        return InvalidateAsync(
            GetGuildScopeKey(guildId, scope),
            scope,
            null,
            new Audience.Guild(guildId),
            reason,
            publishRealtime: false,
            cancellationToken);
    }

    public Task AdvanceGuildScopeAsync(
        Guid guildId,
        string reason,
        CancellationToken cancellationToken = default) =>
        AdvanceGuildScopeAsync(
            guildId,
            StateSyncScopes.Guild,
            reason,
            cancellationToken);

    public async Task<StateSyncCheckpoint> GetCheckpointAsync(
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        var characterKeys = StateSyncScopes.CharacterResources
            .ToDictionary(scope => scope, scope => GetCharacterScopeKey(characterId, scope));
        var worldKeys = StateSyncScopes.WorldResources
            .ToDictionary(scope => scope, scope => $"world:{scope}");
        var guildId = await context.GuildMembers
            .AsNoTracking()
            .Where(member => member.CharacterId == characterId)
            .Select(member => (Guid?)member.GuildId)
            .FirstOrDefaultAsync(cancellationToken);
        var guildKeys = StateSyncScopes.GuildResources.ToDictionary(
            scope => scope,
            scope => guildId.HasValue ? GetGuildScopeKey(guildId.Value, scope) : null,
            StringComparer.Ordinal);
        var checkpointKeys = characterKeys.Values
            .Concat(worldKeys.Values)
            .Concat(guildKeys.Values.OfType<string>())
            .ToArray();
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
        foreach (var (scope, scopeKey) in guildKeys)
        {
            checkpointRevisions[scope] = scopeKey is null
                ? 0
                : revisions.GetValueOrDefault(scopeKey);
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

    private static string GetGuildScopeKey(Guid guildId, string scope) =>
        $"guild:{guildId:N}:{scope}";

    private async Task InvalidateAsync(
        string scopeKey,
        string publicScope,
        Guid? characterId,
        Audience audience,
        string reason,
        bool publishRealtime,
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
        if (!publishRealtime)
        {
            return;
        }

        InvalidationCounter.Add(
            1,
            new KeyValuePair<string, object?>("scope", publicScope),
            new KeyValuePair<string, object?>(
                "audience",
                audience switch
                {
                    Audience.Character => "character",
                    Audience.Characters => "characters",
                    Audience.Guild => "guild",
                    Audience.Raid => "raid",
                    Audience.TournamentGrounds => "tournament-grounds",
                    Audience.World => "world",
                    _ => "unknown"
                }));

        await realtimeBroadcaster.PublishAsync(
            audience,
            new StateInvalidated(characterId, publicScope, revision.Revision, reason),
            nameof(StateSyncService),
            cancellationToken);
    }
}
