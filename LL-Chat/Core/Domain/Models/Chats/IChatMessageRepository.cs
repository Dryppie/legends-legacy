namespace Domain.Models.Chats;
public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> LatestAsync(string channel, int take, CancellationToken cancellationToken);
}
