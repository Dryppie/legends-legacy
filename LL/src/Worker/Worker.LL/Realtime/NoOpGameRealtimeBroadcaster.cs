using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;

namespace Worker.LL.Realtime;

public sealed class NoOpGameRealtimeBroadcaster : IGameRealtimeBroadcaster
{
    private readonly ILogger<NoOpGameRealtimeBroadcaster> _logger;

    public NoOpGameRealtimeBroadcaster(ILogger<NoOpGameRealtimeBroadcaster> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(
        Audience audience,
        GameRealtimeEvent message,
        string sender,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Skipping realtime broadcast from worker. Audience: {AudienceType}, Message: {MessageType}, Sender: {Sender}",
            audience.GetType().Name,
            message.GetType().Name,
            sender);

        return Task.CompletedTask;
    }
}
