using Domain.Models.Chats;

namespace Application.Interfaces.Services.Chats;

public interface IChatModerationService
{
    Task<ChatRestriction?> GetActiveMuteAsync(
        Guid characterId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatModerationAction>> GetHistoryAsync(
        Guid characterId,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatModerationAction>> GetAuditAsync(
        ChatModerationAuditQuery query,
        CancellationToken cancellationToken);

    Task<ChatModerationResult> MuteAsync(
        Guid operationId,
        Guid characterId,
        string actorSubject,
        string actorDisplayName,
        string reason,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    Task<ChatModerationResult> UnmuteAsync(
        Guid operationId,
        Guid restrictionId,
        string actorSubject,
        string actorDisplayName,
        string reason,
        CancellationToken cancellationToken);
}
