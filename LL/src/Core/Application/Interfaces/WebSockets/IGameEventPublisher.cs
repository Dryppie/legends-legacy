using Application.WebSockets.Contracts;

namespace Application.Interfaces.WebSockets;
public interface IGameEventPublisher
{
    Task PublishAsync(Audience audience, GameEventMsg message);
}
