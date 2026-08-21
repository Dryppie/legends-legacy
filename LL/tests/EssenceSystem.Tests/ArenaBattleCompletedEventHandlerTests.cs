using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Colosseum.EventHandlers;
using Application.UseCases.Colosseum.Events;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Combat;

namespace EssenceSystem.Tests;

public sealed class ArenaBattleCompletedEventHandlerTests
{
    [Fact]
    public async Task Handle_records_achievement_and_publishes_completion_to_both_characters()
    {
        var characterId = Guid.NewGuid();
        var enemyId = Guid.NewGuid();
        var publisher = new RecordingGameEventPublisher();
        var outbox = new RecordingGameEventOutbox();
        var handler = new ArenaBattleCompletedEventHandler(publisher, outbox);

        await handler.Handle(
            new ArenaBattleCompletedEvent(
                characterId,
                enemyId,
                BattleOutcome.Victory,
                CharacterRatingBefore: 1000,
                CharacterRatingAfter: 1024,
                EnemyRatingBefore: 980,
                EnemyRatingAfter: 956),
            CancellationToken.None);

        var outboxCall = Assert.Single(outbox.Calls);
        Assert.Equal(GameEventTypes.ColosseumBattleCompleted, outboxCall.EventType);
        Assert.Equal(characterId, outboxCall.CharacterId);
        Assert.Null(outboxCall.AccountId);
        var payload = Assert.IsType<ColosseumBattleCompletedPayload>(outboxCall.Payload);
        Assert.Equal(characterId, payload.CharacterId);
        Assert.Equal(enemyId, payload.OpponentCharacterId);
        Assert.Equal(BattleOutcome.Victory, payload.Outcome);
        Assert.Equal(1000, payload.CharacterRatingBefore);
        Assert.Equal(980, payload.OpponentRatingBefore);

        Assert.Collection(
            publisher.Published,
            published =>
            {
                var audience = Assert.IsType<Audience.Character>(published.Audience);
                Assert.Equal(characterId, audience.CharacterId);
                AssertArenaMessage(published.Message, characterId, enemyId);
            },
            published =>
            {
                var audience = Assert.IsType<Audience.Character>(published.Audience);
                Assert.Equal(enemyId, audience.CharacterId);
                AssertArenaMessage(published.Message, characterId, enemyId);
            });
    }

    private static void AssertArenaMessage(
        GameRealtimeEvent message,
        Guid characterId,
        Guid enemyId)
    {
        var arenaMessage = Assert.IsType<ArenaBattleCompleted>(message);
        Assert.Equal(characterId, arenaMessage.CharacterId);
        Assert.Equal(enemyId, arenaMessage.EnemyId);
        Assert.Equal("Victory", arenaMessage.Outcome);
        Assert.Equal(1000, arenaMessage.CharacterRatingBefore);
        Assert.Equal(1024, arenaMessage.CharacterRatingAfter);
        Assert.Equal(980, arenaMessage.EnemyRatingBefore);
        Assert.Equal(956, arenaMessage.EnemyRatingAfter);
    }

    private sealed class RecordingGameEventPublisher : IGameRealtimeBroadcaster
    {
        public List<(Audience Audience, GameRealtimeEvent Message)> Published { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Published.Add((audience, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<OutboxCall> Calls { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Calls.Add(new OutboxCall(eventType, payload, characterId, accountId));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxCall(
        string EventType,
        object? Payload,
        Guid? CharacterId,
        Guid? AccountId);
}
