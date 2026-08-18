namespace Domain.Models.Chats;

public enum ChatModerationActionType
{
    Muted,
    Unmuted
}

public sealed class ChatModerationAction
{
    public Guid Id { get; set; }
    public ChatModerationActionType ActionType { get; set; }
    public Guid TargetCharacterId { get; set; }
    public Guid RestrictionId { get; set; }
    public string ActorSubject { get; set; } = string.Empty;
    public string ActorDisplayName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed record ChatModerationAuditQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    ChatModerationActionType? ActionType,
    string? Actor,
    string? Reference,
    Guid? OperationId,
    IReadOnlyCollection<Guid> CharacterIds,
    Guid? RestrictionId,
    DateTimeOffset? BeforeOccurredAt,
    Guid? BeforeOperationId,
    int Limit);
