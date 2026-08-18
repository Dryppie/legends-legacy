using System.Security.Cryptography;
using System.Text.Json;
using Application.Interfaces.Services.LL.Administration;
using Common.Primitives;
using Domain.Models.Administration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Administration;

namespace API.LiveOps.Previews;

public sealed class LiveOpsActionPreviewService(
    IDbContextFactory<LLDbContext> contextFactory,
    ILiveOpsService liveOps,
    IChatModerationGateway chat,
    IOptions<LiveOpsOptions> options,
    TimeProvider timeProvider)
{
    private readonly LiveOpsOptions _options = options.Value;

    public async Task<Response<ActionPreviewDto>> CreateAccountBanAsync(
        Guid operationId,
        Guid accountId,
        AdministrationActor actor,
        string reason,
        string? internalNotes,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, internalNotes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return Response<ActionPreviewDto>.Fail("A temporary ban must expire in the future.");
        }

        var player = await liveOps.GetPlayerByAccountIdAsync(
            accountId,
            cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");
        if (player.ActiveBanId.HasValue)
        {
            return Response<ActionPreviewDto>.Fail("The target account already has an active ban.");
        }

        var requestHash = RequestHash(AdminActionPreviewKinds.AccountBan, new
        {
            AccountId = accountId,
            Reason = reason.Trim(),
            InternalNotes = Normalize(internalNotes),
            ExpiresAt = expiresAt?.ToUniversalTime()
        });
        var stateHash = StateHash(new { player.AccountId, player.CharacterId, player.ActiveBanId });
        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.AccountBan,
            actor,
            accountId,
            requestHash,
            stateHash,
            new PreviewContext(player.CharacterId, null),
            "Apply account ban",
            player.CharacterName,
            expiresAt.HasValue ? "Normal" : "Permanent",
            expiresAt.HasValue ? null : player.CharacterName,
            [
                new("Account", player.AccountLabel),
                new("Character", player.CharacterName),
                new("Current account restriction", "None"),
                new("Expiry", expiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Reason", reason.Trim()),
                new("Internal notes", Normalize(internalNotes) ?? "None")
            ],
            expiresAt.HasValue
                ? ["Active sessions will be revoked immediately."]
                : ["This ban is permanent until explicitly revoked.", "Active sessions will be revoked immediately."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateAccountBanRevokeAsync(
        Guid operationId,
        Guid restrictionId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var restriction = await database.AccountRestrictions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == restrictionId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (restriction is null || !restriction.IsActive(now))
        {
            return Response<ActionPreviewDto>.Fail("The account ban is no longer active.");
        }
        var player = await liveOps.GetPlayerByAccountIdAsync(
            restriction.AccountId,
            cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target account was not found.");

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.AccountBanRevoke,
            actor,
            restrictionId,
            RequestHash(AdminActionPreviewKinds.AccountBanRevoke, new
            {
                RestrictionId = restrictionId,
                Reason = reason.Trim()
            }),
            RestrictionStateHash(restriction),
            new PreviewContext(player.CharacterId, null),
            "Revoke account ban",
            player.CharacterName,
            "Normal",
            null,
            [
                new("Character", player.CharacterName),
                new("Current Chat restriction", "None"),
                new("Current restriction", restriction.Reason),
                new("Current expiry", restriction.ExpiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Revocation reason", reason.Trim())
            ],
            ["Account access will be restored immediately."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateChatMuteAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return Response<ActionPreviewDto>.Fail("A temporary mute must expire in the future.");
        }
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var state = await chat.GetStateAsync(characterId, 1, cancellationToken);
        if (!state.IsSuccess) return Response<ActionPreviewDto>.Fail(state.ErrorMessage);
        if (state.ActiveMute is not null)
        {
            return Response<ActionPreviewDto>.Fail("The target character already has an active mute.");
        }

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.ChatMute,
            actor,
            characterId,
            RequestHash(AdminActionPreviewKinds.ChatMute, new
            {
                CharacterId = characterId,
                Reason = reason.Trim(),
                ExpiresAt = expiresAt?.ToUniversalTime()
            }),
            ChatStateHash(characterId, state.ActiveMute),
            new PreviewContext(characterId, null),
            "Mute chat access",
            player.CharacterName,
            expiresAt.HasValue ? "Normal" : "Permanent",
            expiresAt.HasValue ? null : player.CharacterName,
            [
                new("Character", player.CharacterName),
                new("Expiry", expiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Reason", reason.Trim())
            ],
            expiresAt.HasValue
                ? ["The player will be unable to send Chat messages until expiry."]
                : ["This mute is permanent until explicitly removed."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateChatUnmuteAsync(
        Guid operationId,
        Guid restrictionId,
        Guid characterId,
        AdministrationActor actor,
        string reason,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, null);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var state = await chat.GetStateAsync(characterId, 1, cancellationToken);
        if (!state.IsSuccess) return Response<ActionPreviewDto>.Fail(state.ErrorMessage);
        if (state.ActiveMute?.Id != restrictionId)
        {
            return Response<ActionPreviewDto>.Fail("The Chat mute is no longer active.");
        }

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.ChatUnmute,
            actor,
            restrictionId,
            RequestHash(AdminActionPreviewKinds.ChatUnmute, new
            {
                RestrictionId = restrictionId,
                CharacterId = characterId,
                Reason = reason.Trim()
            }),
            ChatStateHash(characterId, state.ActiveMute),
            new PreviewContext(characterId, null),
            "Remove chat mute",
            player.CharacterName,
            "Normal",
            null,
            [
                new("Character", player.CharacterName),
                new("Current restriction", state.ActiveMute.Reason),
                new("Current expiry", state.ActiveMute.ExpiresAt?.ToUniversalTime().ToString("O") ?? "Permanent"),
                new("Removal reason", reason.Trim())
            ],
            ["Chat posting access will be restored immediately."],
            cancellationToken);
    }

    public async Task<Response<ActionPreviewDto>> CreateCompensationGrantAsync(
        Guid operationId,
        Guid characterId,
        AdministrationActor actor,
        string itemBaseId,
        int quantity,
        string reason,
        string? internalNotes,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCommon(operationId, reason, internalNotes);
        if (validation is not null) return Response<ActionPreviewDto>.Fail(validation);
        var normalizedItemId = itemBaseId?.Trim() ?? string.Empty;
        if (normalizedItemId.Length == 0)
        {
            return Response<ActionPreviewDto>.Fail("An item-base ID is required.");
        }
        if (quantity <= 0 || quantity > _options.MaximumGrantQuantity)
        {
            return Response<ActionPreviewDto>.Fail(
                $"Grant quantity must be between 1 and {_options.MaximumGrantQuantity:N0}.");
        }
        var player = await liveOps.GetPlayerAsync(characterId, cancellationToken);
        if (player is null) return Response<ActionPreviewDto>.Fail("The target character was not found.");
        var item = (await liveOps.SearchItemsAsync(normalizedItemId, 20, cancellationToken))
            .FirstOrDefault(x => string.Equals(x.Id, normalizedItemId, StringComparison.Ordinal));
        if (item is null) return Response<ActionPreviewDto>.Fail("The item-base ID does not exist in the server catalog.");
        var isHighValue = quantity >= Math.Max(1, _options.LargeGrantAuditThreshold) ||
            !item.Stackable ||
            item.Rarity >= Domain.Models.Items.Rarity.Rare;

        return await PersistAsync(
            operationId,
            AdminActionPreviewKinds.CompensationGrant,
            actor,
            characterId,
            RequestHash(AdminActionPreviewKinds.CompensationGrant, new
            {
                CharacterId = characterId,
                ItemBaseId = normalizedItemId,
                Quantity = quantity,
                Reason = reason.Trim(),
                InternalNotes = Normalize(internalNotes)
            }),
            GrantStateHash(player, item),
            new PreviewContext(characterId, normalizedItemId),
            "Grant compensation items",
            player.CharacterName,
            isHighValue ? "HighValue" : "Normal",
            isHighValue ? player.CharacterName : null,
            [
                new("Character", player.CharacterName),
                new("Item", $"{item.Name} ({item.Id})"),
                new("Quantity", quantity.ToString("N0")),
                new("Type and rarity", $"{item.ItemType} · {item.Rarity}"),
                new("Behavior", $"{(item.Stackable ? "Stackable" : "Individual instances")} · {(item.IsBound ? "Bound" : "Unbound")}"),
                new("Reason", reason.Trim()),
                new("Internal notes", Normalize(internalNotes) ?? "None")
            ],
            isHighValue
                ? ["This quantity is classified as a high-value grant.", "The operation writes inventory, provenance, economy ledger, realtime, and audit records."]
                : ["The operation writes inventory, provenance, economy ledger, realtime, and audit records."],
            cancellationToken);
    }

    public Task<PreviewSubmissionResult> BeginAccountBanAsync(
        Guid token, Guid operationId, Guid accountId, AdministrationActor actor,
        string reason, string? internalNotes, DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.AccountBan, accountId, actor,
            RequestHash(AdminActionPreviewKinds.AccountBan, new
            {
                AccountId = accountId, Reason = NormalizeRequired(reason), InternalNotes = Normalize(internalNotes),
                ExpiresAt = expiresAt?.ToUniversalTime()
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginAccountBanRevokeAsync(
        Guid token, Guid operationId, Guid restrictionId, AdministrationActor actor,
        string reason, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.AccountBanRevoke, restrictionId, actor,
            RequestHash(AdminActionPreviewKinds.AccountBanRevoke, new
            {
                RestrictionId = restrictionId, Reason = NormalizeRequired(reason)
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginChatMuteAsync(
        Guid token, Guid operationId, Guid characterId, AdministrationActor actor,
        string reason, DateTimeOffset? expiresAt, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.ChatMute, characterId, actor,
            RequestHash(AdminActionPreviewKinds.ChatMute, new
            {
                CharacterId = characterId, Reason = NormalizeRequired(reason), ExpiresAt = expiresAt?.ToUniversalTime()
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginChatUnmuteAsync(
        Guid token, Guid operationId, Guid restrictionId, Guid characterId,
        AdministrationActor actor, string reason, CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.ChatUnmute, restrictionId, actor,
            RequestHash(AdminActionPreviewKinds.ChatUnmute, new
            {
                RestrictionId = restrictionId, CharacterId = characterId, Reason = NormalizeRequired(reason)
            }), cancellationToken);

    public Task<PreviewSubmissionResult> BeginCompensationGrantAsync(
        Guid token, Guid operationId, Guid characterId, AdministrationActor actor,
        string itemBaseId, int quantity, string reason, string? internalNotes,
        CancellationToken cancellationToken) => BeginAsync(
            token, operationId, AdminActionPreviewKinds.CompensationGrant, characterId, actor,
            RequestHash(AdminActionPreviewKinds.CompensationGrant, new
            {
                CharacterId = characterId, ItemBaseId = itemBaseId?.Trim() ?? string.Empty, Quantity = quantity,
                Reason = NormalizeRequired(reason), InternalNotes = Normalize(internalNotes)
            }), cancellationToken);

    public async Task CompleteAsync(
        Guid token,
        bool success,
        CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var preview = await database.AdminActionPreviews
            .FirstOrDefaultAsync(x => x.Id == token, cancellationToken);
        if (preview is null) return;
        var now = timeProvider.GetUtcNow();
        if (success) preview.CompletedAt ??= now;
        else preview.InvalidatedAt ??= now;
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<PreviewSubmissionResult> BeginAsync(
        Guid token,
        Guid operationId,
        string actionKind,
        Guid targetId,
        AdministrationActor actor,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (token == Guid.Empty)
        {
            return PreviewSubmissionResult.Fail("A server preview is required before submission.");
        }
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var preview = await database.AdminActionPreviews
            .FirstOrDefaultAsync(x => x.Id == token, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (preview is null || preview.ExpiresAt <= now)
        {
            return PreviewSubmissionResult.Fail("The preview expired. Review the operation again.", true);
        }
        if (preview.InvalidatedAt.HasValue)
        {
            return PreviewSubmissionResult.Fail("The preview is no longer valid. Review the operation again.", true);
        }
        if (preview.OperationId != operationId ||
            preview.TargetId != targetId ||
            !string.Equals(preview.ActionKind, actionKind, StringComparison.Ordinal) ||
            !string.Equals(preview.ActorSubject, actor.Subject.Trim(), StringComparison.Ordinal) ||
            !string.Equals(preview.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return PreviewSubmissionResult.Fail("The submitted operation does not match its preview.", true);
        }

        if (preview.SubmittedAt.HasValue) return PreviewSubmissionResult.Success();
        var currentState = await CurrentStateHashAsync(preview, cancellationToken);
        if (!currentState.IsSuccess)
        {
            return PreviewSubmissionResult.Fail(currentState.ErrorMessage);
        }
        if (!string.Equals(preview.StateHash, currentState.Hash, StringComparison.Ordinal))
        {
            preview.InvalidatedAt = now;
            await database.SaveChangesAsync(cancellationToken);
            return PreviewSubmissionResult.Fail(
                "The target changed after preview. Review the operation again.",
                true);
        }

        preview.SubmittedAt = now;
        await database.SaveChangesAsync(cancellationToken);
        return PreviewSubmissionResult.Success();
    }

    private async Task<StateResult> CurrentStateHashAsync(
        AdminActionPreview preview,
        CancellationToken cancellationToken)
    {
        var context = JsonSerializer.Deserialize<PreviewContext>(preview.ContextJson)
            ?? new PreviewContext(null, null);
        switch (preview.ActionKind)
        {
            case AdminActionPreviewKinds.AccountBan:
            {
                var player = await liveOps.GetPlayerByAccountIdAsync(
                    preview.TargetId,
                    cancellationToken);
                return player is null
                    ? StateResult.Fail("The target account is no longer available.")
                    : StateResult.Success(StateHash(new
                    {
                        player.AccountId,
                        player.CharacterId,
                        player.ActiveBanId
                    }));
            }
            case AdminActionPreviewKinds.AccountBanRevoke:
            {
                await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
                var restriction = await database.AccountRestrictions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == preview.TargetId, cancellationToken);
                return restriction is null
                    ? StateResult.Fail("The account ban is no longer available.")
                    : StateResult.Success(RestrictionStateHash(restriction));
            }
            case AdminActionPreviewKinds.ChatMute:
            case AdminActionPreviewKinds.ChatUnmute:
            {
                if (!context.CharacterId.HasValue)
                {
                    return StateResult.Fail("The Chat preview context is invalid.");
                }
                var state = await chat.GetStateAsync(context.CharacterId.Value, 1, cancellationToken);
                return state.IsSuccess
                    ? StateResult.Success(ChatStateHash(context.CharacterId.Value, state.ActiveMute))
                    : StateResult.Fail(state.ErrorMessage);
            }
            case AdminActionPreviewKinds.CompensationGrant:
            {
                if (!context.CharacterId.HasValue || string.IsNullOrWhiteSpace(context.ItemBaseId))
                {
                    return StateResult.Fail("The compensation preview context is invalid.");
                }
                var player = await liveOps.GetPlayerAsync(context.CharacterId.Value, cancellationToken);
                var item = (await liveOps.SearchItemsAsync(context.ItemBaseId, 20, cancellationToken))
                    .FirstOrDefault(x => string.Equals(x.Id, context.ItemBaseId, StringComparison.Ordinal));
                return player is null || item is null
                    ? StateResult.Fail("The player or item is no longer available.")
                    : StateResult.Success(GrantStateHash(player, item));
            }
            default:
                return StateResult.Fail("The preview action type is unsupported.");
        }
    }

    private async Task<Response<ActionPreviewDto>> PersistAsync(
        Guid operationId,
        string actionKind,
        AdministrationActor actor,
        Guid targetId,
        string requestHash,
        string stateHash,
        PreviewContext context,
        string title,
        string targetName,
        string riskLevel,
        string? confirmationText,
        IReadOnlyList<ActionPreviewField> fields,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddSeconds(Math.Clamp(_options.PreviewLifetimeSeconds, 60, 600));
        var preview = new AdminActionPreview
        {
            Id = Guid.NewGuid(),
            OperationId = operationId,
            ActionKind = actionKind,
            ActorSubject = actor.Subject.Trim(),
            TargetId = targetId,
            RequestHash = requestHash,
            StateHash = stateHash,
            ContextJson = JsonSerializer.Serialize(context),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (database.Database.IsRelational())
        {
            await database.AdminActionPreviews
                .Where(x => x.ExpiresAt < now)
                .ExecuteDeleteAsync(cancellationToken);
        }
        database.AdminActionPreviews.Add(preview);
        await database.SaveChangesAsync(cancellationToken);
        return Response<ActionPreviewDto>.Success(new ActionPreviewDto(
            preview.Id,
            operationId,
            actionKind,
            title,
            targetName,
            targetId,
            riskLevel,
            expiresAt,
            confirmationText,
            fields,
            warnings));
    }

    private static string? ValidateCommon(Guid operationId, string reason, string? internalNotes)
    {
        if (operationId == Guid.Empty) return "A non-empty operation ID is required.";
        if (string.IsNullOrWhiteSpace(reason)) return "A reason or support reference is required.";
        if (reason.Trim().Length > 1_000) return "The reason cannot exceed 1,000 characters.";
        if (!string.IsNullOrWhiteSpace(internalNotes) && internalNotes.Trim().Length > 4_000)
        {
            return "Internal notes cannot exceed 4,000 characters.";
        }
        return null;
    }

    private static string RequestHash<T>(string actionKind, T request) =>
        Hash(JsonSerializer.SerializeToUtf8Bytes(new { ActionKind = actionKind, Request = request }));

    private static string StateHash<T>(T state) =>
        Hash(JsonSerializer.SerializeToUtf8Bytes(state));

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string RestrictionStateHash(AccountRestriction restriction) =>
        StateHash(new
        {
            restriction.Id,
            restriction.AccountId,
            restriction.ExpiresAt,
            restriction.RevokedAt
        });

    private static string ChatStateHash(
        Guid characterId,
        ChatRestrictionGatewaySnapshot? mute) =>
        StateHash(new
        {
            CharacterId = characterId,
            RestrictionId = mute?.Id,
            mute?.ExpiresAt,
            mute?.RevokedAt
        });

    private static string GrantStateHash(
        PlayerAdministrationSnapshot player,
        AdministrationItemCatalogEntry item) =>
        StateHash(new
        {
            player.AccountId,
            player.CharacterId,
            ItemId = item.Id,
            item.Name,
            item.ItemType,
            item.Rarity,
            item.Stackable,
            item.IsBound
        });

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeRequired(string? value) =>
        value?.Trim() ?? string.Empty;

    private sealed record PreviewContext(Guid? CharacterId, string? ItemBaseId);
    private sealed record StateResult(bool IsSuccess, string Hash, string ErrorMessage)
    {
        public static StateResult Success(string hash) => new(true, hash, string.Empty);
        public static StateResult Fail(string error) => new(false, string.Empty, error);
    }
}
