using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;
using Microsoft.Extensions.Logging;

namespace RealTime.LL;

internal sealed class RealtimeDeliveryGameEventOutboxConsumer(
    GameRealtimeEnvelopeSender sender,
    JsonSerializerOptions jsonOptions,
    ILogger<RealtimeDeliveryGameEventOutboxConsumer> logger) : IGameEventOutboxConsumer
{
    public const string ConsumerName = "realtime-delivery";

    public string Consumer => ConsumerName;

    public bool CanHandle(string eventType) =>
        string.Equals(
            eventType,
            GameEventTypes.RealtimeDeliveryRequested,
            StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(
        GameEventOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var delivery = JsonSerializer.Deserialize<RealtimeDeliveryRequestedPayload>(
            message.PayloadJson,
            jsonOptions)
            ?? throw new InvalidOperationException("Realtime delivery payload is invalid.");

        var audience = ToAudience(delivery.Audience);
        var envelope = new GameRealtimeEnvelope
        {
            UpdateId = message.Id,
            OccurredAt = message.CreatedAt,
            Event = delivery.EventName,
            Payload = delivery.Payload
        };

        logger.LogInformation(
            "Durable game realtime send {Event} updateId={UpdateId} target={Target} sender={Sender} createdAt={CreatedAt:o}",
            envelope.Event,
            envelope.UpdateId,
            DescribeAudience(audience),
            delivery.Sender,
            envelope.OccurredAt);

        await sender.SendAsync(audience, envelope);
    }

    private static Audience ToAudience(RealtimeAudiencePayload audience) =>
        audience.Kind.ToLowerInvariant() switch
        {
            "character" when audience.TargetId.HasValue =>
                new Audience.Character(audience.TargetId.Value),
            "characters" when audience.CharacterIds is not null =>
                new Audience.Characters(audience.CharacterIds),
            "guild" when audience.TargetId.HasValue =>
                new Audience.Guild(audience.TargetId.Value),
            "world" => new Audience.World(),
            _ => throw new InvalidOperationException(
                $"Realtime audience '{audience.Kind}' is invalid.")
        };

    private static string DescribeAudience(Audience audience) => audience switch
    {
        Audience.Character character => $"character:{character.CharacterId}",
        Audience.Characters characters => $"characters:{characters.CharacterIds.Count}",
        Audience.Guild guild => $"guild:{guild.GuildId}",
        Audience.World => "world",
        _ => audience.GetType().Name
    };
}
