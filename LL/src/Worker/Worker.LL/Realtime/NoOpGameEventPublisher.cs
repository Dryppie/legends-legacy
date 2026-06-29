using Application.Interfaces.WebSockets;
using Application.WebSockets.Contracts;

namespace Worker.LL.Realtime;

public sealed class NoOpGameEventPublisher : IGameEventPublisher
{
    private readonly ILogger<NoOpGameEventPublisher> _logger;

    public NoOpGameEventPublisher(ILogger<NoOpGameEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(Audience audience, GameEventMsg message)
    {
        _logger.LogDebug(
            "Skipping realtime publish from worker. Audience: {AudienceType}, Message: {MessageType}",
            audience.GetType().Name,
            message.GetType().Name);

        return Task.CompletedTask;
    }
}
