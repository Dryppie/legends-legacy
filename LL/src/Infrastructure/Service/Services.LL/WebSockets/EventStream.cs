using Application.Interfaces.WebSockets;
using MediatR;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Services.LL.WebSockets;
public sealed class EventStream : IEventStream
{
    private readonly Channel<INotification> _channel =
        Channel.CreateUnbounded<INotification>(
            new() { SingleReader = false, SingleWriter = false });

    public ValueTask PublishAsync(INotification ev, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(ev, ct);

    public async IAsyncEnumerable<INotification> Listen(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (await _channel.Reader.WaitToReadAsync(ct))
            while (_channel.Reader.TryRead(out var ev))
                yield return ev;
    }
}

