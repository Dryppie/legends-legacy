using Application.WebSockets.Contracts;

namespace Application.Interfaces.WebSockets;

/// <summary>
/// Immediate, process-local delivery for sequenced ephemeral streams only.
/// Persistent game-state changes must use <see cref="IGameRealtimeBroadcaster"/>.
/// </summary>
public interface IGameRealtimeImmediatePublisher
{
    Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default);
}
