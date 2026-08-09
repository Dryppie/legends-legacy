using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.UseCases.Prophecies.Events;
using Application.WebSockets.Contracts;

namespace EssenceSystem.Tests;

public sealed class ProphecyQuestEventTests
{
    [Fact]
    public async Task Single_progress_handler_enqueues_only_completed_daily_prophecies()
    {
        var characterId = Guid.NewGuid();
        var dailyId = Guid.NewGuid();
        var prophecies = new RecordingProphecyService
        {
            SingleUpdates =
            [
                CreateUpdate(characterId, dailyId, "Daily", completed: true),
                CreateUpdate(characterId, Guid.NewGuid(), "Weekly", completed: true),
                CreateUpdate(characterId, Guid.NewGuid(), "Daily", completed: false)
            ]
        };
        var outbox = new RecordingGameEventOutbox();
        var handler = new ProphecyProgressNotificationHandler(
            prophecies,
            new RecordingGameEventPublisher(),
            outbox);

        await handler.Handle(
            new ProphecyProgressNotification(
                new ProphecyProgressEvent(
                    characterId,
                    DateTimeOffset.UtcNow,
                    ProphecyProgressKind.EncounterWon)),
            CancellationToken.None);

        var message = Assert.Single(outbox.Messages);
        Assert.Equal(GameEventTypes.ProphecyCompleted, message.EventType);
        Assert.Equal(characterId, message.CharacterId);
        var payload = Assert.IsType<ProphecyCompletedPayload>(message.Payload);
        Assert.Equal(dailyId, payload.ProphecyId);
        Assert.Equal("Daily", payload.Scope);
    }

    [Fact]
    public async Task Batch_progress_handler_enqueues_a_completed_daily_prophecy()
    {
        var characterId = Guid.NewGuid();
        var dailyId = Guid.NewGuid();
        var prophecies = new RecordingProphecyService
        {
            BatchUpdates = [CreateUpdate(characterId, dailyId, "Daily", completed: true)]
        };
        var outbox = new RecordingGameEventOutbox();
        var handler = new ProphecyProgressBatchNotificationHandler(
            prophecies,
            new RecordingGameEventPublisher(),
            outbox);

        await handler.Handle(
            new ProphecyProgressBatchNotification(
            [
                new ProphecyProgressEvent(
                    characterId,
                    DateTimeOffset.UtcNow,
                    ProphecyProgressKind.EncounterWon)
            ]),
            CancellationToken.None);

        var payload = Assert.IsType<ProphecyCompletedPayload>(Assert.Single(outbox.Messages).Payload);
        Assert.Equal(dailyId, payload.ProphecyId);
    }

    private static ProphecyProgressUpdate CreateUpdate(
        Guid characterId,
        Guid prophecyId,
        string scope,
        bool completed) =>
        new(
            characterId,
            prophecyId,
            "Test Prophecy",
            scope,
            "Standard",
            completed ? "Completed" : "Accepted",
            completed ? 1 : 0,
            1,
            1,
            completed);

    private sealed class RecordingProphecyService : IProphecyService
    {
        public IReadOnlyList<ProphecyProgressUpdate> SingleUpdates { get; init; } = [];
        public IReadOnlyList<ProphecyProgressUpdate> BatchUpdates { get; init; } = [];

        public Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(
            ProphecyProgressEvent progressEvent,
            CancellationToken cancellationToken) => Task.FromResult(SingleUpdates);

        public Task<IReadOnlyList<ProphecyProgressUpdate>> TrackProgressAsync(
            IReadOnlyList<ProphecyProgressEvent> progressEvents,
            CancellationToken cancellationToken) => Task.FromResult(BatchUpdates);

        public Task<PropheciesOverview> GetOverviewAsync(Guid playerId, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProphecyOperationResult<PropheciesOverview>> AcceptAsync(Guid playerId, Guid characterId, Guid prophecyId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProphecyOperationResult<PropheciesOverview>> RerollAsync(Guid playerId, Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProphecyOperationResult<ProphecyClaimResult>> ClaimAsync(Guid playerId, Guid characterId, Guid prophecyId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProphecyOperationResult<WeeklyRevelationClaimResult>> ClaimWeeklyMilestoneAsync(Guid playerId, Guid characterId, int favorRequired, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProphecyOperationResult<ProphecyCacheOpenResult>> OpenCacheAsync(Guid characterId, string cacheItemId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingGameEventOutbox : IGameEventOutbox
    {
        public List<RecordedOutboxMessage> Messages { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Messages.Add(new RecordedOutboxMessage(eventType, payload!, characterId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingGameEventPublisher : IGameEventPublisher
    {
        public Task PublishAsync(Audience audience, GameEventMsg message) => Task.CompletedTask;
    }

    private sealed record RecordedOutboxMessage(
        string EventType,
        object Payload,
        Guid? CharacterId);
}
