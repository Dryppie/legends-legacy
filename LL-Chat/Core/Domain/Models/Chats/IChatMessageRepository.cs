namespace Domain.Models.Chats;
public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> LatestAsync(
        Guid userId,
        int take,
        string? guildChannel,
        DateTimeOffset? after,
        CancellationToken cancellationToken);
}
