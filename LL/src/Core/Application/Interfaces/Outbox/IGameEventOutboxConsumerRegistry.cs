namespace Application.Interfaces.Outbox;

public interface IGameEventOutboxConsumerRegistry
{
    IReadOnlyList<string> GetConsumers(string eventType);
}
