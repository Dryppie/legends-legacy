using MediatR;

namespace Application.Interfaces.WebSockets;
/// <summary>
/// Read-only, hot stream of domain events for push transports.
/// Implemented with Channel<T> so multiple producers can write
/// while any consumer (WebSocket handler, SignalR Hub, background worker)
/// can asynchronously enumerate them.
/// </summary>
public interface IEventStream
{
    ValueTask PublishAsync(INotification ev, CancellationToken ct = default);
    IAsyncEnumerable<INotification> Listen(CancellationToken ct = default);
}


