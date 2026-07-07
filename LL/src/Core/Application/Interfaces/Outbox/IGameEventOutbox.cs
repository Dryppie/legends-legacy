namespace Application.Interfaces.Outbox;

public interface IGameEventOutbox
{
    Task EnqueueAsync<TPayload>(
        string eventType,
        TPayload payload,
        Guid? characterId,
        Guid? accountId,
        CancellationToken cancellationToken);
}
