using Domain.Models.Outbox;

namespace Application.Interfaces.Outbox;

public interface IGameEventOutboxConsumer
{
    string Consumer { get; }
    bool CanHandle(string eventType);
    Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken);
}
