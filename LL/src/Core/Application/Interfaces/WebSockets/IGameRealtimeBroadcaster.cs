using Application.WebSockets.Contracts;

namespace Application.Interfaces.WebSockets;

public interface IGameRealtimeBroadcaster
{
    Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default);
}
