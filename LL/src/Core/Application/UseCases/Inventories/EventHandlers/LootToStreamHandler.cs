using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Events;
using MediatR;

namespace Application.UseCases.Inventories.EventHandlers;
public sealed class LootToStreamHandler :
    INotificationHandler<LootGeneratedEvent>
{
    private readonly IEventStream _stream;
    public LootToStreamHandler(IEventStream stream) => _stream = stream;

    public Task Handle(LootGeneratedEvent ev, CancellationToken ct)
        => _stream.PublishAsync(ev, ct).AsTask();
}