using Domain.Models.Outbox;

namespace Application.Interfaces.Outbox;

public interface IGameEventOutboxConsumer
{
    string Consumer { get; }
    bool CanHandle(string eventType);
    Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Allows a consumer to narrow its state-sync invalidations to the resources
/// that its most recently handled message actually changed.
/// </summary>
public interface IReportsGameEventOutboxStateSyncScopes
{
    IReadOnlyList<string> ChangedCharacterScopes { get; }
}
