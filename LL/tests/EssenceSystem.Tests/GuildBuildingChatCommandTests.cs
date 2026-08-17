using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.UseCases.Guilds.Commands.ConstructGuildBuilding;
using Application.UseCases.Guilds.Commands.UpgradeGuildBuilding;
using Application.WebSockets.Contracts;
using Domain.Models.Guilds.Buildings;

namespace EssenceSystem.Tests;

public sealed class GuildBuildingChatCommandTests
{
    [Fact]
    public async Task ConstructPublishesBuiltMessageForResultingBuilding()
    {
        var fixture = new Fixture(GuildBuildingType.MissionBoard, "Mission Board", 1);
        var handler = new ConstructGuildBuildingCommandHandler(
            fixture.Buildings,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new ConstructGuildBuildingCommand(
                fixture.CharacterId,
                GuildBuildingType.MissionBoard),
            CancellationToken.None);

        fixture.AssertMessage(GuildBuildingChatEvent.Constructed);
    }

    [Fact]
    public async Task UpgradePublishesUpgradedMessageForResultingLevel()
    {
        var fixture = new Fixture(GuildBuildingType.MissionBoard, "Mission Board", 3);
        var handler = new UpgradeGuildBuildingCommandHandler(
            fixture.Buildings,
            fixture.Events,
            fixture.Chat);

        await handler.Handle(
            new UpgradeGuildBuildingCommand(
                fixture.CharacterId,
                fixture.BuildingId),
            CancellationToken.None);

        fixture.AssertMessage(GuildBuildingChatEvent.Upgraded);
    }

    private sealed class Fixture
    {
        public Guid GuildId { get; } = Guid.NewGuid();
        public Guid CharacterId { get; } = Guid.NewGuid();
        public Guid BuildingId { get; } = Guid.NewGuid();
        public StubGuildBuildingService Buildings { get; }
        public RecordingEventPublisher Events { get; } = new();
        public RecordingGuildChatPublisher Chat { get; } = new();

        public Fixture(
            GuildBuildingType buildingType,
            string buildingName,
            int buildingLevel)
        {
            var definition = new GuildBuildingDefinitionDto(
                buildingType,
                buildingName,
                string.Empty,
                10,
                false,
                1,
                string.Empty,
                []);
            var building = new GuildBuildingDto(
                BuildingId,
                definition,
                buildingLevel,
                null,
                false,
                true,
                null);
            var overview = new GuildBuildingOverviewDto(
                GuildId,
                1,
                0,
                true,
                null,
                [building],
                []);
            Buildings = new StubGuildBuildingService(overview);
        }

        public void AssertMessage(GuildBuildingChatEvent expectedEvent)
        {
            var message = Assert.Single(Chat.BuildingCalls);
            Assert.Equal(GuildId, message.GuildId);
            Assert.Equal(CharacterId, message.ActorCharacterId);
            Assert.Equal("Mission Board", message.BuildingName);
            Assert.Equal(expectedEvent == GuildBuildingChatEvent.Constructed ? 1 : 3, message.BuildingLevel);
            Assert.Equal(expectedEvent, message.EventType);
        }
    }

    private sealed class StubGuildBuildingService(
        GuildBuildingOverviewDto overview) : IGuildBuildingService
    {
        private readonly GuildOperationResult<GuildBuildingOverviewDto> _result =
            GuildOperationResult<GuildBuildingOverviewDto>.Success(overview);

        public Task<GuildOperationResult<GuildBuildingOverviewDto>> ConstructAsync(Guid characterId, GuildBuildingType buildingType, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(_result);
        public Task<GuildOperationResult<GuildBuildingOverviewDto>> UpgradeAsync(Guid characterId, Guid buildingId, DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult(_result);
        public Task<GuildBuildingOverviewDto?> GetOverviewAsync(Guid characterId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<GuildOperationResult<GuildBuildingOverviewDto>> SetCurrentTargetAsync(Guid characterId, GuildBuildingType buildingType, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class RecordingGuildChatPublisher : IGuildSystemChatPublisher
    {
        public List<BuildingChatCall> BuildingCalls { get; } = [];

        public Task PublishBuildingAsync(
            Guid guildId,
            Guid actorCharacterId,
            string buildingName,
            int buildingLevel,
            GuildBuildingChatEvent eventType,
            CancellationToken cancellationToken)
        {
            BuildingCalls.Add(new BuildingChatCall(
                guildId,
                actorCharacterId,
                buildingName,
                buildingLevel,
                eventType));
            return Task.CompletedTask;
        }

        public Task PublishAsync(Guid guildId, Guid subjectCharacterId, GuildSystemChatEvent eventType, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed record BuildingChatCall(
        Guid GuildId,
        Guid ActorCharacterId,
        string BuildingName,
        int BuildingLevel,
        GuildBuildingChatEvent EventType);

    private sealed class RecordingEventPublisher : IGameEventPublisher
    {
        public Task PublishAsync(Audience audience, GameEventMsg message) => Task.CompletedTask;
    }
}
