using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;

namespace Services.LL.Outbox;

/// <summary>
/// Broadcasts character level-ups once the originating transaction has committed.
/// </summary>
/// <remarks>
/// The level-up message drives client state that is derived from the character row, most notably
/// the number of unlocked essence attunement slots. Publishing from the MediatR handler sent it
/// mid-transaction, so a client could refetch that derived state before the new level was
/// visible and end up stale until a manual reload.
/// </remarks>
public sealed class RealtimeCharacterGameEventOutboxConsumer(
    IGameRealtimeBroadcaster eventPublisher,
    JsonSerializerOptions jsonOptions) : IGameEventOutboxConsumer
{
    public string Consumer => GameEventOutboxConsumerNames.RealtimeCharacter;

    public bool CanHandle(string eventType) =>
        string.Equals(eventType, GameEventTypes.CharacterLevelReached, StringComparison.OrdinalIgnoreCase);

    public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<CharacterLevelReachedPayload>(message.PayloadJson, jsonOptions)
            ?? throw new InvalidOperationException("Character level reached payload is invalid.");

        await eventPublisher.PublishAsync(
            new Audience.Character(payload.CharacterId),
            new CharacterLevelUp(
                payload.CharacterId,
                payload.Level,
                payload.Experience,
                payload.ExperienceUntilNextLevel,
                payload.UnlockedEssenceSlots),
            nameof(RealtimeCharacterGameEventOutboxConsumer),
            cancellationToken);
    }
}
