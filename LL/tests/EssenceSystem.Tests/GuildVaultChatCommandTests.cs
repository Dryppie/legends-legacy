using Application.Common.Mappings;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL.Guilds;
using Application.Interfaces.WebSockets;
using Application.UseCases.Guilds.Commands.DonateGuildVaultItem;
using Application.UseCases.Guilds.Commands.WithdrawGuildVaultItem;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using AutoMapper;
using Domain.Models.Items.Equipments;
using Microsoft.Extensions.Logging.Abstractions;

namespace EssenceSystem.Tests;

public sealed class GuildVaultChatCommandTests
{
    [Fact]
    public async Task DonatePublishesImmediateMessageWithSameIdAsPersistentOutboxMessage()
    {
        var fixture = new CommandFixture();
        var handler = new DonateGuildVaultItemCommandHandler(
            fixture.Vault,
            fixture.Publisher,
            fixture.Outbox,
            fixture.Mapper);

        await handler.Handle(
            new DonateGuildVaultItemCommand(fixture.CharacterId, fixture.Equipment.Id),
            CancellationToken.None);

        fixture.AssertMessage("donated");
    }

    [Fact]
    public async Task WithdrawPublishesImmediateMessageWithSameIdAsPersistentOutboxMessage()
    {
        var fixture = new CommandFixture();
        var handler = new WithdrawGuildVaultItemCommandHandler(
            fixture.Vault,
            fixture.Publisher,
            fixture.Outbox,
            fixture.Mapper);

        await handler.Handle(
            new WithdrawGuildVaultItemCommand(fixture.CharacterId, Guid.NewGuid()),
            CancellationToken.None);

        fixture.AssertMessage("withdrew");
    }

    private sealed class CommandFixture
    {
        public Guid GuildId { get; } = Guid.NewGuid();
        public Guid CharacterId { get; } = Guid.NewGuid();
        public EquipmentInstance Equipment { get; } = new()
        {
            Id = Guid.NewGuid(),
            ItemBaseId = "plain-hatchet",
            ItemBase = new EquipmentBase
            {
                Id = "plain-hatchet",
                Name = "Plain Hatchet",
                EquipmentType = EquipmentType.Tool
            }
        };
        public RecordingPublisher Publisher { get; } = new();
        public RecordingOutbox Outbox { get; } = new();
        public IMapper Mapper { get; } = new MapperConfiguration(
            configuration => configuration.AddProfile<MappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        public StubVaultService Vault { get; }

        public CommandFixture()
        {
            Vault = new StubVaultService(new GuildVaultMutation(
                GuildId,
                CharacterId,
                "admin",
                Equipment));
        }

        public void AssertMessage(string expectedAction)
        {
            var payload = Assert.IsType<GuildVaultChatMessagePayload>(
                Assert.Single(Outbox.Calls).Payload);
            var realtime = Assert.Single(Publisher.Messages.OfType<GuildVaultChatMessage>());

            Assert.Equal(GameEventTypes.GuildVaultChatMessage, Outbox.Calls[0].EventType);
            Assert.Equal(expectedAction, payload.Body);
            Assert.Equal(expectedAction, realtime.Action);
            Assert.Equal(payload.MessageId, realtime.MessageId);
            Assert.Equal(payload.Equipment.Id, realtime.Equipment.Id);
            var stateChanged = Assert.Single(Publisher.Messages.OfType<GuildStateChanged>());
            Assert.Equal(CharacterId, stateChanged.ActorCharacterId);
            Assert.True(stateChanged.InitiatorHandled);
        }
    }

    private sealed class StubVaultService(GuildVaultMutation mutation) : IGuildVaultService
    {
        public Task<GuildOperationResult<GuildVaultMutation>> DonateAsync(
            Guid characterId,
            Guid equipmentInstanceId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuildOperationResult<GuildVaultMutation>.Success(mutation));

        public Task<GuildOperationResult<bool>> BorrowAsync(
            Guid characterId,
            Guid vaultItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuildOperationResult<bool>.Success(true));

        public Task<GuildOperationResult<bool>> ReturnAsync(
            Guid characterId,
            Guid vaultItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuildOperationResult<bool>.Success(true));

        public Task<GuildOperationResult<GuildVaultMutation>> WithdrawAsync(
            Guid characterId,
            Guid vaultItemId,
            CancellationToken cancellationToken) =>
            Task.FromResult(GuildOperationResult<GuildVaultMutation>.Success(mutation));
    }

    private sealed class RecordingPublisher : IGameRealtimeBroadcaster
    {
        public List<GameRealtimeEvent> Messages { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
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
            Calls.Add(new OutboxCall(eventType, payload!));
            return Task.CompletedTask;
        }
    }

    private sealed record OutboxCall(string EventType, object Payload);
}
