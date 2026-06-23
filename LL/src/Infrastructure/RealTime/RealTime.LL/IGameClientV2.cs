using Application.WebSockets.Contracts.V2;

namespace RealTime.LL;

public interface IGameClientV2
{
    Task ReceiveEvent(GameRealtimeEnvelopeV2 e);
}
