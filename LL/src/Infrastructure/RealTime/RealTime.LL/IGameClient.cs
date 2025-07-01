namespace RealTime.LL;
public interface IGameClient
{
    Task Publish(GameEventEnvelope e);
}
