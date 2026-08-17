namespace Application.UsesCases.Chats.Dtos;

public sealed record ChatRestrictionStateDto(
    Guid Id,
    Guid CharacterId,
    string Reason,
    string CreatedBySubject,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    string? RevokedBySubject,
    DateTimeOffset? RevokedAt,
    string? RevocationReason);

public sealed record ChatModerationHistoryEntryDto(
    Guid OperationId,
    string ActionType,
    Guid CharacterId,
    Guid RestrictionId,
    string ActorSubject,
    string ActorDisplayName,
    string Reason,
    DateTimeOffset OccurredAt);

public sealed record ChatModerationStateDto(
    ChatRestrictionStateDto? ActiveMute,
    IReadOnlyList<ChatModerationHistoryEntryDto> History);
