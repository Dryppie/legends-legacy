using Application.Common.Interfaces;
using Application.MediatR.Attributes;
using Application.MediatR.Markers;
using Application.Interfaces.Services.LL;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

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
            await _stateSync.InvalidateCharacterAsync(
                affectedCharacterId,
                reason,
                cancellationToken);
        }

        foreach (var worldScope in GetWorldScopes())
        {
            await _stateSync.InvalidateWorldScopeAsync(
                worldScope,
                reason,
                cancellationToken);
        }
    }

    private static IEnumerable<string> GetWorldScopes()
    {
        var requestNamespace = typeof(TRequest).Namespace;
        if (requestNamespace?.Contains(".MarketPlaces.", StringComparison.Ordinal) == true)
        {
            yield return "marketplace";
        }
        if (requestNamespace?.Contains(".Guilds.", StringComparison.Ordinal) == true)
        {
            yield return "guild";
        }
        if (requestNamespace?.Contains(".Colosseum.", StringComparison.Ordinal) == true)
        {
            yield return "colosseum";
        }
        if (requestNamespace?.Contains(".Colosseum.Tournaments.", StringComparison.Ordinal) == true)
        {
            yield return "tournament";
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

