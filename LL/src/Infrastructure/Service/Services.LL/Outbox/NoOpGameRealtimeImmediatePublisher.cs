using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;
using Microsoft.Extensions.Logging;

namespace Services.LL.Outbox;

public sealed class NoOpGameRealtimeImmediatePublisher(
    ILogger<NoOpGameRealtimeImmediatePublisher> logger) : IGameRealtimeImmediatePublisher
{
    public Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Skipping immediate ephemeral realtime event {Event} from {Sender}; no realtime host is available.",
            message.GetType().Name,
            sender);
        return Task.CompletedTask;
    }
}
