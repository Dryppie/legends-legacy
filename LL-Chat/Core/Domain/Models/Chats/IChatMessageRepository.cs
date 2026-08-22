namespace Domain.Models.Chats;
public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> LatestAsync(
        Guid userId,
        int take,
        string? guildChannel,
        string? raidChannel,
        DateTimeOffset? after,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessage>> SentByAsync(
        Guid senderId,
        int take,
        DateTimeOffset? beforeSentAt,
        Guid? beforeMessageId,
        CancellationToken cancellationToken);

    Task<ChatConversationEvidence> ConversationEvidenceAsync(
        ChatConversationEvidenceQuery query,
        CancellationToken cancellationToken);
}
