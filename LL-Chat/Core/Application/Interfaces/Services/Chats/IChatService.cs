using Domain.Models.Chats;

namespace Application.Interfaces.Services.Chats;
public interface IChatService
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> LatestAsync(Guid userId, int take, string? guildChannel, CancellationToken cancellationToken);
}
