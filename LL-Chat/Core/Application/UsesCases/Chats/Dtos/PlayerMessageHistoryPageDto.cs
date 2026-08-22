namespace Application.UsesCases.Chats.Dtos;

public sealed record PlayerMessageHistoryPageDto(
    IReadOnlyList<ChatMessageDto> Entries,
    string? NextCursor);
