using Application.Interfaces.Services.Chats;
using Domain.Models.Chats;

namespace Services.Chat.Chats;
public class ChatService : IChatService
{
    private readonly IChatMessageRepository _messageRepository;

    public ChatService(IChatMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        await _messageRepository.AddAsync(message, cancellationToken);
    }

    public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _messageRepository.GetByIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<ChatMessage>> LatestAsync(
        Guid userId,
        int take,
        string? guildChannel,
        string? raidChannel,
        DateTimeOffset? after,
        CancellationToken cancellationToken)
    {
        return await _messageRepository.LatestAsync(
            userId,
            take,
            guildChannel,
            raidChannel,
            after,
            cancellationToken);
    }

    public Task<IReadOnlyList<ChatMessage>> SentByAsync(
        Guid senderId,
        int take,
        DateTimeOffset? beforeSentAt,
        Guid? beforeMessageId,
        CancellationToken cancellationToken) =>
        _messageRepository.SentByAsync(
            senderId,
            take,
            beforeSentAt,
            beforeMessageId,
            cancellationToken);

    public Task<ChatConversationEvidence> ConversationEvidenceAsync(
        ChatConversationEvidenceQuery query,
        CancellationToken cancellationToken) =>
        _messageRepository.ConversationEvidenceAsync(query, cancellationToken);
}
