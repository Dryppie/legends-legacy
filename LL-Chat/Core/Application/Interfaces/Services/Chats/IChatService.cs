using Domain.Models.Chats;

namespace Application.Interfaces.Services.Chats;
public interface IChatService
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessage>> LatestAsync(string channel, int take, CancellationToken cancellationToken);
}
