using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.UseCases.Guilds.Commands.SelectGuildMission;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Outbox;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed class GuildMissionSelectionRealtimeTests
{
    [Fact]
    public async Task Successful_selection_queues_a_committed_guild_sync_event()
    {
        var characterId = Guid.NewGuid();
        var missionOptionId = Guid.NewGuid();
        var guildId = Guid.NewGuid();
        var outbox = new RecordingOutbox();
        var handler = new SelectGuildMissionCommandHandler(
            new StubGuildMissionService(CreateOverview(guildId)),
            outbox);

        var response = await handler.Handle(
            new SelectGuildMissionCommand(characterId, missionOptionId),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        var call = Assert.Single(outbox.Calls);
        Assert.Equal(GameEventTypes.GuildMissionSelected, call.EventType);
        Assert.Equal(characterId, call.CharacterId);
        Assert.Equal(guildId, Assert.IsType<GuildMissionSelectedPayload>(call.Payload).GuildId);
    }

    [Fact]
    public async Task Committed_selection_event_notifies_the_whole_guild()
    {
        var guildId = Guid.NewGuid();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var publisher = new RecordingPublisher();
        var consumer = new RealtimeGuildMissionGameEventOutboxConsumer(publisher, options);

        await consumer.HandleAsync(
            new GameEventOutboxMessage
            {
                EventType = GameEventTypes.GuildMissionSelected,
                PayloadJson = JsonSerializer.Serialize(new GuildMissionSelectedPayload(guildId), options)
            },
            CancellationToken.None);

        var audience = Assert.IsType<Audience.Guild>(publisher.Audience);
        Assert.Equal(guildId, audience.GuildId);
        Assert.Equal(guildId, Assert.IsType<GuildStateChangedMsg>(publisher.Message).GuildId);
        Assert.Equal(GameEventOutboxConsumerNames.RealtimeGuildMission, consumer.Consumer);
        Assert.True(consumer.CanHandle(GameEventTypes.GuildMissionSelected));
    }

    [Fact]
    public void Registry_routes_mission_selection_to_the_realtime_guild_consumer()
    {
        var registry = new GameEventOutboxConsumerRegistry();

        Assert.Equal(
            [GameEventOutboxConsumerNames.RealtimeGuildMission],
            registry.GetConsumers(GameEventTypes.GuildMissionSelected));
    }

    private static GuildMissionOverviewDto CreateOverview(Guid guildId) =>
        new(
            guildId,
            0,
            1,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(7),
            false,
            [],
            null,
            null,
            [],
            new GuildContributionSummaryDto(string.Empty, string.Empty, 0, 0, 0, 0, 0, 0),
            []);

    private sealed class StubGuildMissionService(GuildMissionOverviewDto overview) : IGuildMissionService
    {
        public Task<GuildOperationResult<GuildMissionOverviewDto>> SelectMissionAsync(
            Guid characterId,
            Guid missionOptionId,
            DateTimeOffset now,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuildOperationResult<GuildMissionOverviewDto>.Success(overview));

        public Task<GuildMissionOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimPersonalOrderRewardAsync(Guid characterId, Guid orderId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildOperationResult<GuildMissionOverviewDto>> ClaimWeeklyRewardAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<GuildContributionResult> RecordContributionAsync(GuildContributionEvent contributionEvent, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingOutbox : IGameEventOutbox
    {
        public List<OutboxCall> Calls { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Calls.Add(new OutboxCall(eventType, payload!, characterId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPublisher : IGameEventPublisher
    {
        public Audience? Audience { get; private set; }
        public GameEventMsg? Message { get; private set; }

        public Task PublishAsync(Audience audience, GameEventMsg message)
        {
            Audience = audience;
            Message = message;
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxCall(string EventType, object Payload, Guid? CharacterId);
}
