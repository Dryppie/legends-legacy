using Application.Common.Interfaces;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.Interfaces.Services.LL;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Application.MediatR.Behaviors;
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> CharacterCommandLocks = new();

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
            var commandLock = CharacterCommandLocks.GetOrAdd(
                characterId.Value,
                _ => new SemaphoreSlim(1, 1));

            await commandLock.WaitAsync(ct);
            try
            {
                return await HandleTransactionalCommand(next, ct, characterId);
            }
            finally
            {
                commandLock.Release();
            }
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
                        _logger.LogError("Concurrency on {Entity} with key {KeyValues}",
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
            catch (Exception ex)
            {
                try { await tx.RollbackAsync(ct); }
                catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed."); }
                _logger.LogError(ex, "Command {Command} failed; tx rolled back.", typeof(TRequest).Name);
                throw;
            }
        });
    }

    private async Task InvalidateChangedScopesAsync(
        Guid? primaryCharacterId,
        CancellationToken cancellationToken)
    {
        var reason = typeof(TRequest).Name;
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

        foreach (var affectedCharacterId in affectedCharacterIds.Order())
        {
            foreach (var characterScope in GetCharacterScopes(affectedCharacterId).Distinct(StringComparer.Ordinal))
            {
                await _stateSync.InvalidateCharacterScopeAsync(
                    affectedCharacterId,
                    characterScope,
                    reason,
                    cancellationToken);
            }
        }

        foreach (var worldScope in GetWorldScopes())
        {
            await _stateSync.InvalidateWorldScopeAsync(
                worldScope,
                reason,
                cancellationToken);
        }
    }

    private IEnumerable<string> GetCharacterScopes(Guid characterId)
    {
        var requestNamespace = typeof(TRequest).Namespace ?? string.Empty;

        yield return StateSyncScopes.Character;

        if (ShouldInvalidateCharacterOverview(requestNamespace))
        {
            yield return StateSyncScopes.CharacterOverview;
        }

        if (requestNamespace.Contains(".Inventories.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Inventory;
        }
        if (requestNamespace.Contains(".Equipments.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Equipment;
            yield return StateSyncScopes.Inventory;
            yield return StateSyncScopes.Quests;
        }
        if (requestNamespace.Contains(".Quests.Events.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.EventQuests;
            yield return StateSyncScopes.Inventory;
        }
        else if (requestNamespace.Contains(".Quests.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Quests;
            yield return StateSyncScopes.AreaAccess;
        }
        if (requestNamespace.Contains(".Achievements.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Achievements;
        }
        if (requestNamespace.Contains(".Dungeons.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Dungeons;
            yield return StateSyncScopes.Inventory;
            yield return StateSyncScopes.Quests;
        }
        if (requestNamespace.Contains(".Essences.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Essences;
            yield return StateSyncScopes.Inventory;
            yield return StateSyncScopes.Equipment;
            yield return StateSyncScopes.Quests;
        }
        if (requestNamespace.Contains(".MarketPlaces.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Inventory;
        }
        if (requestNamespace.Contains(".Guilds.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Inventory;
            yield return StateSyncScopes.Equipment;
        }
        if (requestNamespace.Contains(".Colosseum.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Inventory;
        }
        if (requestNamespace.Contains(".Crafting.", StringComparison.Ordinal)
            || requestNamespace.Contains(".Soulstones.", StringComparison.Ordinal))
        {
            yield return StateSyncScopes.Inventory;
            yield return StateSyncScopes.Quests;
        }
        if (requestNamespace.Contains(".CharacterActions.", StringComparison.Ordinal))
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

    private bool ShouldInvalidateCharacterOverview(string requestNamespace)
    {
        if (!requestNamespace.Contains(".CharacterActions.", StringComparison.Ordinal))
        {
            return true;
        }

        // Ordinary idle-combat resolutions only change fields already returned
        // by CharacterDto. A level-up or crafting progression changes the richer
        // overview and therefore still requires its own revision.
        return _db.GameEventOutboxMessages.Local.Any(message =>
            message.EventType is GameEventTypes.CharacterLevelReached
                or GameEventTypes.EquipmentCrafted
                or GameEventTypes.EquipmentTempered);
    }

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

    private static IEnumerable<string> GetWorldScopes()
    {
        var requestNamespace = typeof(TRequest).Namespace;
        if (requestNamespace?.Contains(".MarketPlaces.", StringComparison.Ordinal) == true)
        {
            yield return StateSyncScopes.Marketplace;
        }
        if (requestNamespace?.Contains(".Guilds.", StringComparison.Ordinal) == true)
        {
            yield return StateSyncScopes.Guild;
        }
        if (requestNamespace?.Contains(".Colosseum.", StringComparison.Ordinal) == true)
        {
            yield return StateSyncScopes.Colosseum;
        }
        if (requestNamespace?.Contains(".Colosseum.Tournaments.", StringComparison.Ordinal) == true)
        {
            yield return StateSyncScopes.Tournament;
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

