using MediatR;
using System.Text.Json;

namespace Application.WebSockets.Messages;
public sealed record ClientEnvelope(string Type, JsonElement Payload)
{
    /// Maps the incoming message to a concrete MediatR request.
    /// Throw if the type is unknown so the client gets a 4xx-style close code.
    public IRequest ToCommand()
        => Type switch
        {
            //"equip" => Payload.Deserialize<EquipItemCommand>(),
            //"move" => Payload.Deserialize<MoveCommand>(),
            //"buy" => Payload.Deserialize<BuyItemCommand>(),
            //"chat" => Payload.Deserialize<SendChatMessageCommand>(),
            _ => throw new NotSupportedException($"Unknown client msg '{Type}'")
        };
}
