using System.Text.Json;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class RealtimeCharacterGameEventOutboxConsumerTests
{
    [Fact]
    public async Task Publishes_the_committed_level_up_with_its_unlocked_slot_count()
    {
        var characterId = Guid.NewGuid();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = new CharacterLevelReachedPayload(characterId, 11, 240, 900, 3);
        var publisher = new RecordingPublisher();
        var consumer = new RealtimeCharacterGameEventOutboxConsumer(publisher, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.CharacterLevelReached,
                PayloadJson = JsonSerializer.Serialize(payload, options)
            },
            CancellationToken.None);

        var message = Assert.IsType<CharacterLevelUpMsg>(publisher.Message);
        Assert.Equal(characterId, message.CharacterId);
        Assert.Equal(11, message.Level);
        Assert.Equal(240, message.Experience);
        Assert.Equal(900, message.ExperienceUntilNextLevel);
        Assert.Equal(3, message.UnlockedEssenceSlots);
        var audience = Assert.IsType<Audience.Character>(publisher.Audience);
        Assert.Equal(characterId, audience.CharacterId);
    }

    [Fact]
    public async Task Outbox_rows_queued_before_the_payload_grew_still_deserialize()
    {
        var characterId = Guid.NewGuid();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var publisher = new RecordingPublisher();
        var consumer = new RealtimeCharacterGameEventOutboxConsumer(publisher, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.CharacterLevelReached,
                PayloadJson = $$"""{"characterId":"{{characterId}}","level":7}"""
            },
            CancellationToken.None);

        var message = Assert.IsType<CharacterLevelUpMsg>(publisher.Message);
        Assert.Equal(characterId, message.CharacterId);
        Assert.Equal(7, message.Level);
        Assert.Equal(0, message.UnlockedEssenceSlots);
    }

    [Fact]
    public void Only_handles_the_character_level_reached_event()
    {
        var consumer = new RealtimeCharacterGameEventOutboxConsumer(
            new RecordingPublisher(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.True(consumer.CanHandle(GameEventTypes.CharacterLevelReached));
        Assert.False(consumer.CanHandle(GameEventTypes.InventoryItemsGranted));
        Assert.Equal(GameEventOutboxConsumerNames.RealtimeCharacter, consumer.Consumer);
    }

    private sealed class RecordingPublisher : IGameEventPublisher
    {
        public GameEventMsg? Message { get; private set; }
        public Audience? Audience { get; private set; }

        public Task PublishAsync(Audience audience, GameEventMsg message)
        {
            Audience = audience;
            Message = message;
            return Task.CompletedTask;
        }
    }
}
