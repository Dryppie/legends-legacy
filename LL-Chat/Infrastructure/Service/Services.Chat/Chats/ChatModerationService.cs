using Application.Interfaces.Services.Chats;
using Domain.Models.Chats;

namespace Services.Chat.Chats;

public sealed class ChatModerationService(
    IChatRestrictionRepository restrictions,
    TimeProvider timeProvider) : IChatModerationService
{
    public Task<ChatRestriction?> GetActiveMuteAsync(
        Guid characterId,
        CancellationToken cancellationToken) =>
        restrictions.GetActiveMuteAsync(
            characterId,
            timeProvider.GetUtcNow(),
            cancellationToken);

    public Task<IReadOnlyList<ChatModerationAction>> GetHistoryAsync(
        Guid characterId,
        int limit,
        CancellationToken cancellationToken) =>
        restrictions.GetActionsAsync(
            characterId,
            limit,
            cancellationToken);

    public async Task<ChatModerationResult> MuteAsync(
        Guid operationId,
        Guid characterId,
        string actorSubject,
        string actorDisplayName,
        string reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCommon(
            operationId,
            actorSubject,
            actorDisplayName,
            reason);
        if (validationError is not null)
        {
            return ChatModerationResult.Fail(validationError);
        }
        if (characterId == Guid.Empty)
        {
            return ChatModerationResult.Fail("A target character ID is required.");
        }

        var existingAction = await restrictions.GetActionAsync(
            operationId,
            cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != ChatModerationActionType.Muted ||
                existingAction.TargetCharacterId != characterId)
            {
                return ChatModerationResult.Fail(
                    "The operation ID has already been used for a different request.");
            }

            var replayRestriction = await restrictions.GetRestrictionAsync(
                existingAction.RestrictionId,
                cancellationToken);
            return replayRestriction is null
                ? ChatModerationResult.Fail(
                    "The original moderation action no longer resolves to its restriction.")
                : ChatModerationResult.Success(replayRestriction, true);
        }

        var now = timeProvider.GetUtcNow();
        if (expiresAt.HasValue && expiresAt.Value <= now)
        {
            return ChatModerationResult.Fail("A temporary mute must expire in the future.");
        }
        if (await restrictions.GetActiveMuteAsync(
                characterId,
                now,
                cancellationToken) is not null)
        {
            return ChatModerationResult.Fail(
                "The target character already has an active mute.");
        }

        var restriction = new ChatRestriction
        {
            Id = Guid.NewGuid(),
            TargetCharacterId = characterId,
            Reason = reason.Trim(),
            CreatedBySubject = actorSubject.Trim(),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        var action = CreateAction(
            operationId,
            ChatModerationActionType.Muted,
            restriction,
            actorSubject,
            actorDisplayName,
            reason,
            now);
        restrictions.AddRestriction(restriction);
        restrictions.AddAction(action);
        await restrictions.SaveChangesAsync(cancellationToken);

        return ChatModerationResult.Success(restriction);
    }

    public async Task<ChatModerationResult> UnmuteAsync(
        Guid operationId,
        Guid restrictionId,
        string actorSubject,
        string actorDisplayName,
        string reason,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCommon(
            operationId,
            actorSubject,
            actorDisplayName,
            reason);
        if (validationError is not null)
        {
            return ChatModerationResult.Fail(validationError);
        }
        if (restrictionId == Guid.Empty)
        {
            return ChatModerationResult.Fail("A restriction ID is required.");
        }

        var existingAction = await restrictions.GetActionAsync(
            operationId,
            cancellationToken);
        if (existingAction is not null)
        {
            if (existingAction.ActionType != ChatModerationActionType.Unmuted ||
                existingAction.RestrictionId != restrictionId)
            {
                return ChatModerationResult.Fail(
                    "The operation ID has already been used for a different request.");
            }

            var replayRestriction = await restrictions.GetRestrictionAsync(
                restrictionId,
                cancellationToken);
            return replayRestriction is null
                ? ChatModerationResult.Fail("The target chat restriction was not found.")
                : ChatModerationResult.Success(replayRestriction, true);
        }

        var restriction = await restrictions.GetRestrictionAsync(
            restrictionId,
            cancellationToken);
        if (restriction is null)
        {
            return ChatModerationResult.Fail("The target chat restriction was not found.");
        }

        var now = timeProvider.GetUtcNow();
        if (!restriction.IsActive(now))
        {
            return ChatModerationResult.Fail(
                "The target chat restriction is no longer active.");
        }

        restriction.Revoke(actorSubject, reason, now);
        restrictions.AddAction(CreateAction(
            operationId,
            ChatModerationActionType.Unmuted,
            restriction,
            actorSubject,
            actorDisplayName,
            reason,
            now));
        await restrictions.SaveChangesAsync(cancellationToken);

        return ChatModerationResult.Success(restriction);
    }

    private static ChatModerationAction CreateAction(
        Guid operationId,
        ChatModerationActionType actionType,
        ChatRestriction restriction,
        string actorSubject,
        string actorDisplayName,
        string reason,
        DateTimeOffset occurredAt) =>
        new()
        {
            Id = operationId,
            ActionType = actionType,
            TargetCharacterId = restriction.TargetCharacterId,
            RestrictionId = restriction.Id,
            ActorSubject = actorSubject.Trim(),
            ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName)
                ? actorSubject.Trim()
                : actorDisplayName.Trim(),
            Reason = reason.Trim(),
            OccurredAt = occurredAt
        };

    private static string? ValidateCommon(
        Guid operationId,
        string actorSubject,
        string actorDisplayName,
        string reason)
    {
        if (operationId == Guid.Empty)
        {
            return "A non-empty operation ID is required.";
        }
        if (string.IsNullOrWhiteSpace(actorSubject))
        {
            return "The staff identity subject is required.";
        }
        if (actorSubject.Trim().Length > 320)
        {
            return "The staff identity subject exceeds the supported length.";
        }
        if (!string.IsNullOrWhiteSpace(actorDisplayName) &&
            actorDisplayName.Trim().Length > 320)
        {
            return "The staff identity display name exceeds the supported length.";
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            return "A moderation reason is required.";
        }
        if (reason.Trim().Length > 1_000)
        {
            return "The moderation reason cannot exceed 1,000 characters.";
        }

        return null;
    }
}
