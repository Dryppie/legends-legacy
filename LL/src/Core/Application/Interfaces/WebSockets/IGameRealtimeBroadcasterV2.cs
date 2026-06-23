using Application.WebSockets.Contracts;
using Application.WebSockets.Contracts.V2;

namespace Application.Interfaces.WebSockets;

public interface IGameRealtimeBroadcasterV2
{
    Task PublishAsync(
        Audience audience,
        GameRealtimeEventV2 message,
        string sender,
        CancellationToken cancellationToken = default);
}
