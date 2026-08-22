namespace Domain.Models.Chats;

public sealed record ChatConversationEvidenceQuery(
    Guid FirstCharacterId,
    Guid SecondCharacterId,
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset ImmediateFrom,
    DateTimeOffset ImmediateTo,
    DateTimeOffset? BeforeSentAt,
    Guid? BeforeMessageId,
    int Take);

public sealed record ChatConversationEvidence(
    int FirstToSecondMessageCount,
    int SecondToFirstMessageCount,
    int ImmediateMessageCount,
    DateTimeOffset? FirstMessageAt,
    DateTimeOffset? LastMessageAt,
    int SharedChannelCount,
    int SharedChannelMessageCount,
    IReadOnlyList<ChatMessage> Messages);
