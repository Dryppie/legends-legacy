using Application.Common.Interfaces;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.MediatR.Synchronization;
using Application.Interfaces.Services.LL;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics.Metrics;

namespace Application.MediatR.Behaviors;
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IDbContext _db;
    private readonly IStateSyncService _stateSync;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IDbContext db,
        IStateSyncService stateSync,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _stateSync = stateSync;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        using var operationScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = typeof(TRequest).Name
        });

        var isCommand = request is ICommandBase;
        var isOptOut = request.GetType().IsDefined(typeof(NonTransactionalAttribute), inherit: true);
        if (!isCommand || isOptOut)
            return await next();

        var characterId = TryGetCharacterId(request);
        if (_db.CurrentTransaction is not null)
        {
            if (characterId.HasValue)
            {
                await _db.AcquireCharacterCommandLockAsync(characterId.Value, ct);
            }

            return await HandleTransactionalCommand(next, ct, characterId);
        }

        if (characterId.HasValue)
        {
            using var commandLock = await CharacterCommandLockRegistry.Instance.AcquireAsync(characterId.Value, ct);
            return await HandleTransactionalCommand(next, ct, characterId);
        }

        return await HandleTransactionalCommand(next, ct, null);
    }

    private async Task<TResponse> HandleTransactionalCommand(
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct,
        Guid? characterId)
    {

        var saveChangesVersion = _db.SaveChangesVersion;

        if (_db.CurrentTransaction is not null)
        {
            var resp = await next();
            if (IsSuccessfulResponse(resp)
                && (_db.HasChanges || _db.SaveChangesVersion > saveChangesVersion))
            {
                await InvalidateChangedScopesAsync(characterId, ct);
            }
            if (_db.HasChanges)
            {
                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var e in ex.Entries)
                    {
                        _logger.LogDebug("Concurrency on {Entity} with key {KeyValues}",
                            e.Metadata.Name,
                            string.Join(",", e.Properties.Where(p => p.Metadata.IsPrimaryKey())
                                                         .Select(p => p.CurrentValue)));
                    }
                    throw;
                }
            }
            return resp;
        }

        var strategy = _db.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.BeginTransactionAsync(ct);
            try
            {
                if (characterId.HasValue)
                {
                    await _db.AcquireCharacterCommandLockAsync(characterId.Value, ct);
                }

                var response = await next();

                if (IsSuccessfulResponse(response)
                    && (_db.HasChanges || _db.SaveChangesVersion > saveChangesVersion))
                {
                    await InvalidateChangedScopesAsync(characterId, ct);
                }

                if (_db.HasChanges)
                    await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                return response;
            }
            catch (Exception)
            {
                try { await tx.RollbackAsync(ct); }
                catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed."); }
                throw;
            }
        });
    }

    private async Task InvalidateChangedScopesAsync(
        Guid? primaryCharacterId,
        CancellationToken cancellationToken)
    {
        var reason = typeof(TRequest).Name;
        var invalidationCount = 0L;
        var revisionWriteCount = 0L;
        var responseOwnedRevisionWriteCount = 0L;
        var scopeProfile = StateSyncCommandScopeCatalog.GetProfile(typeof(TRequest));
        var affectedCharacterIds = _db.GameEventOutboxMessages.Local
            .Where(message =>
                message.CharacterId.HasValue &&
                _db.GetEntry(message).State == EntityState.Added)
            .Select(message => message.CharacterId!.Value)
            .ToHashSet();
        if (primaryCharacterId.HasValue)
        {
            affectedCharacterIds.Add(primaryCharacterId.Value);
        }
        foreach (var member in _db.GuildMembers.Local.Where(member =>
                     _db.GetEntry(member).State is EntityState.Added
                         or EntityState.Modified
                         or EntityState.Deleted))
        {
            affectedCharacterIds.Add(member.CharacterId);
        }
        foreach (var item in _db.InventoryItems.Local.Where(item =>
                     _db.GetEntry(item).State is EntityState.Added
                         or EntityState.Modified
                         or EntityState.Deleted))
        {
            affectedCharacterIds.Add(item.InventoryId);
        }
        foreach (var slot in _db.EquipmentSlots.Local.Where(slot =>
                     _db.GetEntry(slot).State is EntityState.Added
                         or EntityState.Modified
                         or EntityState.Deleted))
        {
            affectedCharacterIds.Add(slot.EntityId);
        }
        foreach (var message in _db.GameEventOutboxMessages.Local.Where(message =>
                     message.EventType == GameEventTypes.RealtimeDeliveryRequested
                     && _db.GetEntry(message).State == EntityState.Added))
        {
            foreach (var characterId in GetMarketplaceAffectedCharacterIds(message.PayloadJson))
            {
                affectedCharacterIds.Add(characterId);
            }
        }

        foreach (var affectedCharacterId in affectedCharacterIds.Order())
        {
            var characterScopes = GetCharacterScopes(affectedCharacterId, scopeProfile)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var responseOwnedScopes = primaryCharacterId == affectedCharacterId
                ? characterScopes
                    .Where(scopeProfile.ResponseOwnedCharacterScopes.Contains)
                    .ToArray()
                : [];
            var invalidatingScopes = characterScopes
                .Except(responseOwnedScopes, StringComparer.Ordinal)
                .ToArray();

            if (responseOwnedScopes.Length == 1)
            {
                await _stateSync.AdvanceCharacterScopeAsync(
                    affectedCharacterId,
                    responseOwnedScopes[0],
                    reason,
                    cancellationToken);
            }
            else if (responseOwnedScopes.Length > 1)
            {
                await _stateSync.AdvanceCharacterScopesAsync(
                    affectedCharacterId,
                    responseOwnedScopes,
                    reason,
                    cancellationToken);
            }

            if (invalidatingScopes.Length == 1)
            {
                await _stateSync.InvalidateCharacterScopeAsync(
                    affectedCharacterId,
                    invalidatingScopes[0],
                    reason,
                    cancellationToken);
            }
            else if (invalidatingScopes.Length > 1)
            {
                await _stateSync.InvalidateCharacterScopesAsync(
                    affectedCharacterId,
                    invalidatingScopes,
                    reason,
                    cancellationToken);
            }

            responseOwnedRevisionWriteCount += responseOwnedScopes.Length;
            invalidationCount += invalidatingScopes.Length;
            revisionWriteCount += characterScopes.Length;
        }

        foreach (var worldScope in scopeProfile.WorldScopes)
        {
            if (StateSyncScopes.GuildResources.Contains(worldScope, StringComparer.Ordinal))
            {
                var guildAudienceIds = await GetGuildAudienceIdsAsync(
                    primaryCharacterId,
                    cancellationToken);
                if (guildAudienceIds.Count > 0)
                {
                    foreach (var guildId in guildAudienceIds)
                    {
                        if (scopeProfile.ResponseOwnedWorldScopes.Contains(worldScope))
                        {
                            await _stateSync.AdvanceGuildScopeAsync(
                                guildId,
                                worldScope,
                                reason,
                                cancellationToken);
                            responseOwnedRevisionWriteCount += 1;
                        }
                        else
                        {
                            await _stateSync.InvalidateGuildScopeAsync(
                                guildId,
                                worldScope,
                                reason,
                                cancellationToken);
                            invalidationCount += 1;
                        }
                        revisionWriteCount += 1;
                    }
                    continue;
                }

                throw new InvalidOperationException(
                    $"{reason} changed guild state without an identifiable guild audience.");
            }

            if (scopeProfile.ResponseOwnedWorldScopes.Contains(worldScope))
            {
                await _stateSync.AdvanceWorldScopeAsync(
                    worldScope,
                    reason,
                    cancellationToken);
                revisionWriteCount += 1;
                responseOwnedRevisionWriteCount += 1;
                continue;
            }

            await _stateSync.InvalidateWorldScopeAsync(
                worldScope,
                reason,
                cancellationToken);
            invalidationCount += 1;
            revisionWriteCount += 1;
        }

        var commandTag = new KeyValuePair<string, object?>("command", reason);
        StateSyncCommandMetrics.RevisionWrites.Record(revisionWriteCount, commandTag);
        StateSyncCommandMetrics.ResponseOwnedRevisionWrites.Record(
            responseOwnedRevisionWriteCount,
            commandTag);
        StateSyncCommandMetrics.OutboxMessages.Record(
            _db.GameEventOutboxMessages.Local.LongCount(message =>
                _db.GetEntry(message).State == EntityState.Added),
            commandTag);

        if (invalidationCount > 0)
        {
            StateSyncCommandMetrics.InvalidatingCommands.Add(
                1,
                new KeyValuePair<string, object?>("command", reason));
            StateSyncCommandMetrics.InvalidationFanout.Record(
                invalidationCount,
                new KeyValuePair<string, object?>("command", reason));
        }
    }

    private async Task<IReadOnlySet<Guid>> GetGuildAudienceIdsAsync(
        Guid? primaryCharacterId,
        CancellationToken cancellationToken)
    {
        var guildIds = _db.Guilds.Local
            .Where(guild => _db.GetEntry(guild).State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            .Select(guild => guild.Id)
            .Where(guildId => guildId != Guid.Empty)
            .ToHashSet();

        AddChangedGuildIds(_db.GuildInvites, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildMembers, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildBuildings, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildActivityLogs, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildMissionOptions, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildMissionInstances, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildMissionContributions, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.PersonalGuildOrders, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildMemberContributionPeriods, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildContributionLedgers, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildShopPurchases, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildRolePermissions, entity => entity.GuildId, guildIds);
        AddChangedGuildIds(_db.GuildVaultItems, entity => entity.GuildId, guildIds);

        foreach (var message in _db.GameEventOutboxMessages.Local.Where(message =>
                     message.EventType == GameEventTypes.RealtimeDeliveryRequested
                     && _db.GetEntry(message).State == EntityState.Added))
        {
            if (TryGetGuildAudienceId(message.PayloadJson, out var guildId))
            {
                guildIds.Add(guildId);
            }
        }

        if (primaryCharacterId.HasValue)
        {
            var currentGuildId = await _db.GuildMembers
                .AsNoTracking()
                .Where(member => member.CharacterId == primaryCharacterId.Value)
                .Select(member => (Guid?)member.GuildId)
                .FirstOrDefaultAsync(cancellationToken);
            if (currentGuildId.HasValue)
            {
                guildIds.Add(currentGuildId.Value);
            }
        }

        return guildIds;
    }

    private void AddChangedGuildIds<TEntity>(
        DbSet<TEntity> entities,
        Func<TEntity, Guid> getGuildId,
        ISet<Guid> guildIds)
        where TEntity : class
    {
        foreach (var entity in entities.Local.Where(entity =>
                     _db.GetEntry(entity).State is EntityState.Added
                         or EntityState.Modified
                         or EntityState.Deleted))
        {
            var guildId = getGuildId(entity);
            if (guildId != Guid.Empty)
            {
                guildIds.Add(guildId);
            }
        }
    }

    private static IReadOnlyList<Guid> GetMarketplaceAffectedCharacterIds(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if ((!root.TryGetProperty("eventName", out var eventName)
                    && !root.TryGetProperty("EventName", out eventName))
                || !string.Equals(
                    eventName.GetString(),
                    nameof(MarketplaceChanged),
                    StringComparison.Ordinal)
                || (!root.TryGetProperty("payload", out var payload)
                    && !root.TryGetProperty("Payload", out payload))
                || (!payload.TryGetProperty("changes", out var changes)
                    && !payload.TryGetProperty("Changes", out changes))
                || (!changes.TryGetProperty("affectedCharacterIds", out var affected)
                    && !changes.TryGetProperty("AffectedCharacterIds", out affected))
                || affected.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var characterIds = new List<Guid>();
            foreach (var value in affected.EnumerateArray())
            {
                if (Guid.TryParse(value.GetString(), out var characterId)
                    && characterId != Guid.Empty)
                {
                    characterIds.Add(characterId);
                }
            }
            return characterIds;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryGetGuildAudienceId(
        string payloadJson,
        out Guid guildId)
    {
        guildId = Guid.Empty;
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if ((!root.TryGetProperty("audience", out var audience)
                    && !root.TryGetProperty("Audience", out audience))
                || (!audience.TryGetProperty("kind", out var kind)
                    && !audience.TryGetProperty("Kind", out kind))
                || !string.Equals(kind.GetString(), "guild", StringComparison.OrdinalIgnoreCase)
                || (!audience.TryGetProperty("targetId", out var targetId)
                    && !audience.TryGetProperty("TargetId", out targetId)))
            {
                return false;
            }

            return Guid.TryParse(targetId.GetString(), out guildId)
                && guildId != Guid.Empty;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private IEnumerable<string> GetCharacterScopes(
        Guid characterId,
        StateSyncCommandScopeProfile profile)
    {
        if (!profile.RefreshCharacterSummaryWhenChanged || HasCharacterSummaryMutation(characterId))
        {
            yield return StateSyncScopes.Character;
        }

        if (profile.RefreshCharacterOverview || HasCharacterOverviewMutation())
        {
            yield return StateSyncScopes.CharacterOverview;
        }

        foreach (var scope in profile.CharacterScopes)
        {
            yield return scope;
        }

        if (HasProphecyMutation(characterId))
        {
            yield return StateSyncScopes.Prophecies;
        }

        if (profile.InventoryWhenChanged)
        {
            // Quest, event-quest, and achievement progress is applied later by
            // dedicated outbox consumers. Inventory only needs an invalidation
            // when this resolution actually changed an inventory row.
            if (HasInventoryMutation(characterId))
            {
                yield return StateSyncScopes.Inventory;
            }
        }
    }

    private bool HasCharacterOverviewMutation() =>
        // Ordinary idle-combat resolutions only change fields already returned
        // by CharacterDto. A level-up or crafting progression changes the richer
        // overview and therefore still requires its own revision.
        _db.GameEventOutboxMessages.Local.Any(message =>
            message.EventType is GameEventTypes.CharacterLevelReached
                or GameEventTypes.EquipmentCrafted
                or GameEventTypes.EquipmentTempered);

    private bool HasCharacterSummaryMutation(Guid characterId) =>
        _db.Characters.Local.Any(character =>
            character.Id == characterId
            && _db.GetEntry(character).State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted);

    private bool HasInventoryMutation(Guid characterId) =>
        _db.InventoryItems.Local.Any(item =>
            item.InventoryId == characterId
            && _db.GetEntry(item).State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
        || _db.GameEventOutboxMessages.Local.Any(message =>
            message.CharacterId == characterId
            && (message.EventType == GameEventTypes.InventoryItemsGranted
                || IsRealtimeEvent(message.PayloadJson, nameof(LootReceived))));

    private bool HasProphecyMutation(Guid characterId) =>
        _db.PlayerProphecyInstances.Local.Any(instance =>
            instance.CharacterId == characterId
            && _db.GetEntry(instance).State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
        || _db.WeeklyRevelationProgress.Local.Any(progress =>
            progress.CharacterId == characterId
            && _db.GetEntry(progress).State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted);

    private static bool IsRealtimeEvent(string payloadJson, string eventName)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            return (root.TryGetProperty("eventName", out var value)
                    || root.TryGetProperty("EventName", out value))
                && string.Equals(value.GetString(), eventName, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsSuccessfulResponse(TResponse response)
    {
        if (response is null)
        {
            return true;
        }

        var property = response.GetType().GetProperty(
            "IsSuccess",
            BindingFlags.Public | BindingFlags.Instance);
        return property?.PropertyType != typeof(bool)
            || property.GetValue(response) is not false;
    }

    private static Guid? TryGetCharacterId(TRequest request)
    {
        var requestType = request.GetType();
        var property = requestType.GetProperty("CharacterId", BindingFlags.Public | BindingFlags.Instance)
            ?? requestType.GetProperty("CurrentCharacterId", BindingFlags.Public | BindingFlags.Instance)
            ?? requestType.GetProperty("EntityId", BindingFlags.Public | BindingFlags.Instance);

        if (property?.PropertyType != typeof(Guid))
        {
            return null;
        }

        var value = property.GetValue(request);
        return value is Guid characterId && characterId != Guid.Empty
            ? characterId
            : null;
    }
}

internal static class StateSyncCommandMetrics
{
    private static readonly Meter Meter = new("LegendsLegacy.StateSync");

    internal static readonly Histogram<long> InvalidationFanout =
        Meter.CreateHistogram<long>(
            "state_sync.command.invalidation_fanout",
            "invalidations");

    internal static readonly Counter<long> InvalidatingCommands =
        Meter.CreateCounter<long>("state_sync.commands_with_invalidations");

    internal static readonly Histogram<long> RevisionWrites =
        Meter.CreateHistogram<long>(
            "state_sync.command.revision_writes",
            "revisions");

    internal static readonly Histogram<long> ResponseOwnedRevisionWrites =
        Meter.CreateHistogram<long>(
            "state_sync.command.response_owned_revision_writes",
            "revisions");

    internal static readonly Histogram<long> OutboxMessages =
        Meter.CreateHistogram<long>(
            "state_sync.command.outbox_messages",
            "messages");
}

