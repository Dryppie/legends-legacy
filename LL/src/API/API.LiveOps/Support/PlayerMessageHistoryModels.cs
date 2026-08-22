namespace API.LiveOps.Support;

public sealed record PlayerMessageHistoryEntryDto(
    Guid Id,
    string ChannelType,
    string ContextKey,
    string Body,
    Guid? TargetCharacterId,
    string? TargetCharacterName,
    DateTimeOffset SentAt);

public sealed record PlayerMessageHistoryPageDto(
    IReadOnlyList<PlayerMessageHistoryEntryDto> Entries,
    string? NextCursor);
