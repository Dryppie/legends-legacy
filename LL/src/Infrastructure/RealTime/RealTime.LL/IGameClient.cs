using Application.WebSockets.Contracts;

namespace RealTime.LL;

public interface IGameClient
{
    Task ReceiveEvent(GameRealtimeEnvelope e);
}
