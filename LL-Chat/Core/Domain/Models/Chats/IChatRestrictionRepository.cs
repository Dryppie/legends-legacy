namespace Domain.Models.Chats;

public interface IChatRestrictionRepository
{
    Task<ChatModerationAction?> GetActionAsync(
        Guid operationId,
        CancellationToken cancellationToken);
    Task<ChatRestriction?> GetRestrictionAsync(
        Guid restrictionId,
        CancellationToken cancellationToken);
    Task<ChatRestriction?> GetActiveMuteAsync(
        Guid characterId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatModerationAction>> GetActionsAsync(
        Guid characterId,
        int limit,
        CancellationToken cancellationToken);
    void AddRestriction(ChatRestriction restriction);
    void AddAction(ChatModerationAction action);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
