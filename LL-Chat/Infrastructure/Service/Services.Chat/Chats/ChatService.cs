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

    public async Task<IReadOnlyList<ChatMessage>> LatestAsync(Guid userId, int take, string? guildChannel, CancellationToken cancellationToken)
    {
        return await _messageRepository.LatestAsync(userId, take, guildChannel, cancellationToken);
    }
}
