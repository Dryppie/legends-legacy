namespace Application.UsesCases.Chats.Dtos;

public sealed record ChatConversationEvidenceDto(
    int FirstToSecondMessageCount,
    int SecondToFirstMessageCount,
    int ImmediateMessageCount,
    DateTimeOffset? FirstMessageAt,
    DateTimeOffset? LastMessageAt,
    int SharedChannelCount,
    int SharedChannelMessageCount,
    IReadOnlyList<ChatMessageDto> Messages,
    bool HasMoreMessages);
