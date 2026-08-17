using Domain.Models.Chats;

namespace Application.Interfaces.Services.Chats;
public interface IChatService
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
