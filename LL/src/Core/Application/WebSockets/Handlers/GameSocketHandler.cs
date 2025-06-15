using Application.Common.Mappings;
using Application.Interfaces.WebSockets;
using Application.UseCases.Inventories.Events;
using MediatR;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Application.WebSockets.Handlers;
public class GameSocketHandler
{
    private readonly IMediator _mediator;   // for in-process requests / commands
    private readonly IEventStream _events;  // domain-event feed (IAsyncEnumerable)
    private readonly JsonSerializerOptions _json;
    private readonly DomainToClientMapper _domainToClientMapper;

    public GameSocketHandler(IMediator mediator, IEventStream events, DomainToClientMapper domainToClientMapper)
    {
        _mediator = mediator;
        _events = events;
        _domainToClientMapper = domainToClientMapper;
        _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public async Task RunAsync(WebSocket ws, Guid characterId, CancellationToken ct)
    {
        // TODO: Work on ReceiveLoop when I need responses that are sub 30ms
        // 1. Spawn a reader task (client->server messages)
        //var receive = Task.Run(() => ReceiveLoop(ws, ct), ct);

        // 2. Spawn a writer task (domain events -> client)
        var send = Task.Run(() => SendLoop(ws, characterId, ct), ct);

        await Task.WhenAny(/*receive,*/ send);
    }

    /* ---------- private loops ---------- */

    private async Task ReceiveLoop(WebSocket ws, CancellationToken ct)
    {
        //var buf = new byte[4 * 1024];
        //while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
        //{
        //    var res = await ws.ReceiveAsync(buf, ct);
        //    if (res.MessageType == WebSocketMessageType.Close) break;

        //    var msgJson = Encoding.UTF8.GetString(buf.AsSpan(0, res.Count));
        //    var envelope = JsonSerializer.Deserialize<ClientEnvelope>(msgJson, _json);

        //    // Route to MediatR command handler
        //    await _mediator.Send(envelope.ToCommand(), ct);
        //}
    }

    private async Task SendLoop(WebSocket ws, Guid characterId, CancellationToken ct)
    {
        await foreach (var @event in _events.Listen(ct))
        {
            if (@event is LootGeneratedEvent l && l.CharacterId != characterId)
                continue;

            var dto = _domainToClientMapper.Map(@event);
            var payload = JsonSerializer.Serialize(dto, dto.GetType(), _json);

            await ws.SendAsync(
                Encoding.UTF8.GetBytes(payload),
                WebSocketMessageType.Text,
                true,
                ct);
        }
    }
}

