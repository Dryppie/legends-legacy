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

        foreach (var affectedCharacterId in affectedCharacterIds.Order())
        {
            foreach (var characterScope in GetCharacterScopes(affectedCharacterId, scopeProfile).Distinct(StringComparer.Ordinal))
            {
                await _stateSync.InvalidateCharacterScopeAsync(
                    affectedCharacterId,
                    characterScope,
                    reason,
                    cancellationToken);
            }
        }

        foreach (var worldScope in scopeProfile.WorldScopes)
        {
            await _stateSync.InvalidateWorldScopeAsync(
                worldScope,
                reason,
                cancellationToken);
        }
    }

    private IEnumerable<string> GetCharacterScopes(
        Guid characterId,
        StateSyncCommandScopeProfile profile)
    {
        yield return StateSyncScopes.Character;

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

