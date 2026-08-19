using System.Text.Json;
using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Administration;
using Application.Interfaces.Outbox;
using Application.UseCases.Outbox;
using Application.UseCases.Administration;
using Application.WebSockets.Contracts;
using Domain.Models.Administration;
using Domain.Models.Items;
using Domain.Models.Users;
using Microsoft.Extensions.Options;
using Services.LL.Interfaces;

namespace Services.LL.Administration;

public sealed class LiveOpsService(
    IAdministrationRepository administration,
    IRefreshTokenRepository refreshTokens,
    IItemBaseRepository itemBases,
    IInventoryService inventory,
    IInventoryItemFactory itemFactory,
    IGameEventOutbox gameEvents,
    IOptions<LiveOpsOptions> options,
    TimeProvider timeProvider) : ILiveOpsService
{
    private readonly LiveOpsOptions _options = options.Value;

    public Task<IReadOnlyList<PlayerAdministrationSnapshot>> SearchPlayersAsync(
        string query,
        int limit,
        CancellationToken cancellationToken) =>
        administration.SearchPlayersAsync(
            query,
            limit,
            timeProvider.GetUtcNow(),
            cancellationToken);

    public Task<PlayerAdministrationSnapshot?> GetPlayerAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        administration.GetPlayerByCharacterIdAsync(
            characterId,
            timeProvider.GetUtcNow(),
            cancellationToken);

    public Task<IReadOnlyList<AdministrationItemCatalogEntry>> SearchItemsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken) =>
        administration.SearchItemsAsync(query, limit, cancellationToken);

    public Task<IReadOnlyList<AdministrationHistoryEntry>> GetHistoryAsync(
        Guid accountId,
        Guid characterId,
        int limit,
        CancellationToken cancellationToken) =>
        administration.GetHistoryAsync(
            accountId,
            characterId,
            limit,
            cancellationToken);

    public Task<PlayerAdministrationSnapshot?> GetPlayerByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        administration.GetPlayerByAccountIdAsync(
            accountId,
            timeProvider.GetUtcNow(),
            cancellationToken);

    public Task<IReadOnlyList<AdministrationHistoryEntry>> GetAuditAsync(
        AdministrationAuditQuery query,
        CancellationToken cancellationToken) =>
        administration.GetAuditAsync(query, cancellationToken);

    public async Task<AdministrationOperationResult<AccountBanOperation>> BanAccountAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, reason);
        if (validation is not null)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }
        if (!string.IsNullOrWhiteSpace(internalNotes) && internalNotes.Trim().Length > 4_000)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "invalid-notes",
                "Internal notes cannot exceed 4,000 characters.");
        }

        var existingAction = await administration.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != AdminActionType.AccountBanned ||
                existingAction.TargetAccountId != accountId ||
                existingAction.TargetResourceId is not Guid restrictionId)
            {
                return IdempotencyConflict<AccountBanOperation>();
            }

            var existingRestriction = await administration.GetRestrictionAsync(
                restrictionId,
                cancellationToken);
            return existingRestriction is null
                ? AdministrationOperationResult<AccountBanOperation>.Fail(
                    "audit-corrupt",
                    "The original ban audit record no longer resolves to its restriction.")
                : AdministrationOperationResult<AccountBanOperation>.Success(
                    new AccountBanOperation(existingAction, existingRestriction, true));
        }

        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "invalid-expiry",
                "A temporary ban must expire in the future.");
        }

        var player = await administration.GetPlayerByAccountIdAsync(
            accountId,
            now,
            cancellationToken);
        if (player is null)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "account-not-found",
                "The target account was not found.");
        }

        if (player.ActiveBanId.HasValue)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "already-banned",
                "The target account already has an active ban.");
        }

        var restriction = new AccountRestriction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            RestrictionType = AccountRestrictionType.Ban,
            Reason = NormalizeReason(reason),
            InternalNotes = NormalizeOptional(internalNotes),
            CreatedBySubject = actor.Subject.Trim(),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        var action = CreateAction(
            operationId,
            AdminActionType.AccountBanned,
            AdministrationPermissions.AccountModeration,
            actor,
            accountId,
            player.CharacterId,
            restriction.Id,
            reason,
            internalNotes,
            JsonSerializer.Serialize(new { expiresAt }),
            expiresAt.HasValue
                ? AdministrationRiskLevel.Normal
                : AdministrationRiskLevel.Permanent,
            now);

        administration.AddRestriction(restriction);
        administration.AddAction(action);
        await refreshTokens.RevokeActiveTokensForUserAsync(accountId, cancellationToken);

        return AdministrationOperationResult<AccountBanOperation>.Success(
            new AccountBanOperation(action, restriction, false));
    }

    public async Task<AdministrationOperationResult<AccountBanOperation>> RevokeAccountBanAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, reason);
        if (validation is not null)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }

        var existingAction = await administration.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != AdminActionType.AccountBanRevoked ||
                existingAction.TargetResourceId != restrictionId)
            {
                return IdempotencyConflict<AccountBanOperation>();
            }

            var replayRestriction = await administration.GetRestrictionAsync(
                restrictionId,
                cancellationToken);
            return replayRestriction is null
                ? AdministrationOperationResult<AccountBanOperation>.Fail(
                    "restriction-not-found",
                    "The target restriction was not found.")
                : AdministrationOperationResult<AccountBanOperation>.Success(
                    new AccountBanOperation(existingAction, replayRestriction, true));
        }

        var restriction = await administration.GetRestrictionAsync(
            restrictionId,
            cancellationToken);
        if (restriction is null || restriction.RestrictionType != AccountRestrictionType.Ban)
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "restriction-not-found",
                "The target account ban was not found.");
        }

        var now = timeProvider.GetUtcNow();
        if (!restriction.IsActive(now))
        {
            return AdministrationOperationResult<AccountBanOperation>.Fail(
                "restriction-inactive",
                "The target account ban is no longer active.");
        }

        restriction.Revoke(actor.Subject, reason, now);
        var action = CreateAction(
            operationId,
            AdminActionType.AccountBanRevoked,
            AdministrationPermissions.AccountModeration,
            actor,
            restriction.AccountId,
            null,
            restriction.Id,
            reason,
            null,
            "{}",
            AdministrationRiskLevel.Normal,
            now);
        administration.AddAction(action);

        return AdministrationOperationResult<AccountBanOperation>.Success(
            new AccountBanOperation(action, restriction, false));
    }

    public async Task<AdministrationOperationResult<MultiplayerRestrictionOperation>> RestrictMultiplayerAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, reason);
        if (validation is not null)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }
        if (!string.IsNullOrWhiteSpace(internalNotes) && internalNotes.Trim().Length > 4_000)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "invalid-notes",
                "Internal notes cannot exceed 4,000 characters.");
        }

        var existingAction = await administration.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != AdminActionType.MultiplayerRestricted ||
                existingAction.TargetAccountId != accountId ||
                existingAction.TargetResourceId is not Guid existingRestrictionId)
            {
                return IdempotencyConflict<MultiplayerRestrictionOperation>();
            }

            var existingRestriction = await administration.GetRestrictionAsync(
                existingRestrictionId,
                cancellationToken);
            return existingRestriction is null
                ? AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                    "audit-corrupt",
                    "The original restriction audit record no longer resolves to its restriction.")
                : AdministrationOperationResult<MultiplayerRestrictionOperation>.Success(
                    new MultiplayerRestrictionOperation(existingAction, existingRestriction, true));
        }

        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "invalid-expiry",
                "A temporary multiplayer restriction must expire in the future.");
        }

        var player = await administration.GetPlayerByAccountIdAsync(
            accountId,
            now,
            cancellationToken);
        if (player is null)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "account-not-found",
                "The target account was not found.");
        }
        if (player.ActiveMultiplayerRestrictionId.HasValue)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "already-restricted",
                "The target account already has an active multiplayer restriction.");
        }

        var restriction = new AccountRestriction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            RestrictionType = AccountRestrictionType.MultiplayerRestriction,
            Reason = NormalizeReason(reason),
            InternalNotes = NormalizeOptional(internalNotes),
            CreatedBySubject = actor.Subject.Trim(),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        var action = CreateAction(
            operationId,
            AdminActionType.MultiplayerRestricted,
            AdministrationPermissions.AccountModeration,
            actor,
            accountId,
            player.CharacterId,
            restriction.Id,
            reason,
            internalNotes,
            JsonSerializer.Serialize(new { expiresAt }),
            expiresAt.HasValue
                ? AdministrationRiskLevel.Normal
                : AdministrationRiskLevel.Permanent,
            now);

        administration.AddRestriction(restriction);
        administration.AddAction(action);
        await gameEvents.EnqueueAsync(
            GameEventTypes.AccountMultiplayerRestricted,
            new AccountMultiplayerRestrictedPayload(
                restriction.Id,
                accountId,
                player.CharacterId,
                now),
            player.CharacterId,
            accountId,
            cancellationToken);
        await EnqueueAccountAccessChangedAsync(
            accountId,
            player.CharacterId,
            "MultiplayerRestricted",
            now,
            cancellationToken);
        return AdministrationOperationResult<MultiplayerRestrictionOperation>.Success(
            new MultiplayerRestrictionOperation(action, restriction, false));
    }

    public async Task<AdministrationOperationResult<MultiplayerRestrictionOperation>> RevokeMultiplayerRestrictionAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, reason);
        if (validation is not null)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }

        var existingAction = await administration.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != AdminActionType.MultiplayerRestrictionRevoked ||
                existingAction.TargetResourceId != restrictionId)
            {
                return IdempotencyConflict<MultiplayerRestrictionOperation>();
            }

            var replayRestriction = await administration.GetRestrictionAsync(
                restrictionId,
                cancellationToken);
            return replayRestriction is null
                ? AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                    "restriction-not-found",
                    "The target multiplayer restriction was not found.")
                : AdministrationOperationResult<MultiplayerRestrictionOperation>.Success(
                    new MultiplayerRestrictionOperation(existingAction, replayRestriction, true));
        }

        var restriction = await administration.GetRestrictionAsync(
            restrictionId,
            cancellationToken);
        if (restriction is null ||
            restriction.RestrictionType != AccountRestrictionType.MultiplayerRestriction)
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "restriction-not-found",
                "The target multiplayer restriction was not found.");
        }

        var now = timeProvider.GetUtcNow();
        if (!restriction.IsActive(now))
        {
            return AdministrationOperationResult<MultiplayerRestrictionOperation>.Fail(
                "restriction-inactive",
                "The target multiplayer restriction is no longer active.");
        }

        restriction.Revoke(actor.Subject, reason, now);
        var player = await administration.GetPlayerByAccountIdAsync(
            restriction.AccountId,
            now,
            cancellationToken);
        var action = CreateAction(
            operationId,
            AdminActionType.MultiplayerRestrictionRevoked,
            AdministrationPermissions.AccountModeration,
            actor,
            restriction.AccountId,
            null,
            restriction.Id,
            reason,
            null,
            "{}",
            AdministrationRiskLevel.Normal,
            now);
        administration.AddAction(action);
        if (player is not null)
        {
            await EnqueueAccountAccessChangedAsync(
                restriction.AccountId,
                player.CharacterId,
                "MultiplayerRestrictionRevoked",
                now,
                cancellationToken);
        }

        return AdministrationOperationResult<MultiplayerRestrictionOperation>.Success(
            new MultiplayerRestrictionOperation(action, restriction, false));
    }

    public async Task<AdministrationOperationResult<ItemGrantOperation>> GrantCompensationItemsAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string itemBaseId,
        int quantity,
        string reason,
        string? internalNotes,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, reason);
        if (validation is not null)
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }
        if (!string.IsNullOrWhiteSpace(internalNotes) && internalNotes.Trim().Length > 4_000)
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                "invalid-notes",
                "Internal notes cannot exceed 4,000 characters.");
        }

        var normalizedItemBaseId = itemBaseId.Trim();
        if (normalizedItemBaseId.Length == 0)
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                "invalid-item",
                "An item-base ID is required.");
        }
        if (quantity <= 0 || quantity > _options.MaximumGrantQuantity)
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                "invalid-quantity",
                $"Grant quantity must be between 1 and {_options.MaximumGrantQuantity:N0}.");
        }

        var existingAction = await administration.GetActionAsync(operationId, cancellationToken);
        if (existingAction is not null)
        {
            var details = TryReadGrantDetails(existingAction.DetailsJson);
            if (existingAction.ActionType != AdminActionType.CompensationItemsGranted ||
                existingAction.TargetCharacterId != characterId ||
                details is null ||
                !string.Equals(details.ItemBaseId, normalizedItemBaseId, StringComparison.Ordinal) ||
                details.Quantity != quantity ||
                existingAction.TargetAccountId is not Guid existingAccountId)
            {
                return IdempotencyConflict<ItemGrantOperation>();
            }

            return AdministrationOperationResult<ItemGrantOperation>.Success(
                new ItemGrantOperation(
                    existingAction,
                    existingAccountId,
                    characterId,
                    normalizedItemBaseId,
                    quantity,
                    [],
                    true));
        }

        var now = timeProvider.GetUtcNow();
        var player = await administration.GetPlayerByCharacterIdAsync(
            characterId,
            now,
            cancellationToken);
        if (player is null)
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                "character-not-found",
                "The target character was not found.");
        }

        var catalog = await itemBases.GetItemBasesByIdsAsync(
            [normalizedItemBaseId],
            cancellationToken);
        if (!catalog.TryGetValue(normalizedItemBaseId, out var itemBase))
        {
            return AdministrationOperationResult<ItemGrantOperation>.Fail(
                "item-not-found",
                "The item-base ID does not exist in the server catalog.");
        }

        var grantedItems = itemFactory
            .CreateForQuantity(itemBase, quantity, characterId)
            .ToList();
        await inventory.AddItemsToInventory(
            characterId,
            grantedItems,
            ItemAcquisitionSources.AdminCompensation,
            operationId,
            cancellationToken);

        var detailsJson = JsonSerializer.Serialize(
            new GrantDetails(normalizedItemBaseId, quantity));
        var action = CreateAction(
            operationId,
            AdminActionType.CompensationItemsGranted,
            AdministrationPermissions.EconomyCompensation,
            actor,
            player.AccountId,
            characterId,
            null,
            reason,
            internalNotes,
            detailsJson,
            quantity >= Math.Max(1, _options.LargeGrantAuditThreshold)
                ? AdministrationRiskLevel.HighValue
                : AdministrationRiskLevel.Normal,
            now);
        administration.AddAction(action);

        return AdministrationOperationResult<ItemGrantOperation>.Success(
            new ItemGrantOperation(
                action,
                player.AccountId,
                characterId,
                normalizedItemBaseId,
                quantity,
                grantedItems,
                false));
    }

    public async Task<AdministrationOperationResult<AdminAction>> RecordAuditExportAsync(
        Guid operationId,
        AdministrationActor actor,
        int rowCount,
        string detailsJson,
        CancellationToken cancellationToken)
    {
        var validation = ValidateOperation(operationId, actor, "Audit CSV export");
        if (validation is not null)
        {
            return AdministrationOperationResult<AdminAction>.Fail(
                validation.Value.Code,
                validation.Value.Message);
        }
        if (rowCount < 0 || rowCount > 5_000)
        {
            return AdministrationOperationResult<AdminAction>.Fail(
                "invalid-export-size",
                "Audit exports are limited to 5,000 rows.");
        }

        var existingAction = await administration.GetActionAsync(
            operationId,
            cancellationToken);
        if (existingAction is not null)
        {
            return existingAction.ActionType == AdminActionType.AuditExported &&
                string.Equals(existingAction.DetailsJson, detailsJson, StringComparison.Ordinal)
                ? AdministrationOperationResult<AdminAction>.Success(existingAction)
                : IdempotencyConflict<AdminAction>();
        }

        var action = CreateAction(
            operationId,
            AdminActionType.AuditExported,
            AdministrationPermissions.SuperAdmin,
            actor,
            null,
            null,
            null,
            $"Exported {rowCount:N0} authorized audit rows.",
            null,
            detailsJson,
            AdministrationRiskLevel.Normal,
            timeProvider.GetUtcNow());
        administration.AddAction(action);
        return AdministrationOperationResult<AdminAction>.Success(action);
    }

    private Task EnqueueAccountAccessChangedAsync(
        Guid accountId,
        Guid characterId,
        string reason,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var change = new AccountAccessChanged(accountId, reason, occurredAt);
        return gameEvents.EnqueueAsync(
            GameEventTypes.RealtimeDeliveryRequested,
            new RealtimeDeliveryRequestedPayload(
                new RealtimeAudiencePayload("Character", characterId, null),
                nameof(AccountAccessChanged),
                JsonSerializer.SerializeToElement(change),
                "LiveOps"),
            characterId,
            accountId,
            cancellationToken);
    }

    private static (string Code, string Message)? ValidateOperation(
        Guid operationId,
        AdministrationActor actor,
        string reason)
    {
        if (operationId == Guid.Empty)
        {
            return ("invalid-operation-id", "A non-empty operation ID is required.");
        }
        if (string.IsNullOrWhiteSpace(actor.Subject))
        {
            return ("invalid-actor", "The staff identity subject is required.");
        }
        if (actor.Subject.Trim().Length > 320 ||
            (!string.IsNullOrWhiteSpace(actor.DisplayName) && actor.DisplayName.Trim().Length > 320))
        {
            return ("invalid-actor", "The staff identity exceeds the supported length.");
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ("invalid-reason", "A reason or support reference is required.");
        }
        if (reason.Trim().Length > 1_000)
        {
            return ("invalid-reason", "The reason cannot exceed 1,000 characters.");
        }

        return null;
    }

    private static AdminAction CreateAction(
        Guid operationId,
        AdminActionType actionType,
        string permission,
        AdministrationActor actor,
        Guid? accountId,
        Guid? characterId,
        Guid? resourceId,
        string reason,
        string? internalNotes,
        string detailsJson,
        AdministrationRiskLevel riskLevel,
        DateTimeOffset occurredAt) =>
        new()
        {
            Id = operationId,
            ActionType = actionType,
            Permission = permission,
            ActorSubject = actor.Subject.Trim(),
            ActorDisplayName = string.IsNullOrWhiteSpace(actor.DisplayName)
                ? actor.Subject.Trim()
                : actor.DisplayName.Trim(),
            TargetAccountId = accountId,
            TargetCharacterId = characterId,
            TargetResourceId = resourceId,
            Reason = NormalizeReason(reason),
            InternalNotes = NormalizeOptional(internalNotes),
            DetailsJson = detailsJson,
            RiskLevel = riskLevel,
            OccurredAt = occurredAt
        };

    private static AdministrationOperationResult<T> IdempotencyConflict<T>() =>
        AdministrationOperationResult<T>.Fail(
            "idempotency-conflict",
            "The operation ID has already been used for a different request.");

    private static string NormalizeReason(string value) => value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static GrantDetails? TryReadGrantDetails(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<GrantDetails>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record GrantDetails(string ItemBaseId, int Quantity);
}
