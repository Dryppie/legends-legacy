using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Services.LL.Outbox;
using Services.LL.Synchronization;

namespace EssenceSystem.Tests;

public sealed class StateSyncServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Character_invalidation_advances_once_per_scope_and_is_visible_in_checkpoint()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var characterId = Guid.NewGuid();

        await service.InvalidateCharacterAsync(characterId, "test", CancellationToken.None);
        await service.InvalidateCharacterAsync(characterId, "duplicate", CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var revision = Assert.Single(await db.StateSyncRevisions.ToListAsync());
        Assert.Equal(1, revision.Revision);
        Assert.Equal(Now, revision.UpdatedAt);

        var notification = Assert.IsType<StateInvalidated>(Assert.Single(realtime.Messages).Message);
        Assert.Equal(characterId, notification.CharacterId);
        Assert.Equal(StateSyncScopes.Character, notification.Scope);
        Assert.Equal(1, notification.Revision);

        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Character]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.CharacterOverview]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Inventory]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Soulstones]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Prophecies]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Marketplace]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Colosseum]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Tournament]);
    }

    [Fact]
    public async Task World_scope_invalidation_is_visible_to_every_character_checkpoint()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));

        await service.InvalidateWorldScopeAsync(
            StateSyncScopes.Guild,
            "guild-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var checkpoint = await service.GetCheckpointAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Guild]);
        var publication = Assert.Single(realtime.Messages);
        Assert.IsType<Audience.World>(publication.Audience);
        var notification = Assert.IsType<StateInvalidated>(publication.Message);
        Assert.Equal(StateSyncScopes.Guild, notification.Scope);
        Assert.Null(notification.CharacterId);
    }

    [Fact]
    public async Task Character_resource_revisions_advance_independently()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var characterId = Guid.NewGuid();

        await service.InvalidateCharacterScopeAsync(
            characterId,
            StateSyncScopes.Inventory,
            "inventory-change",
            CancellationToken.None);
        await service.InvalidateCharacterScopeAsync(
            characterId,
            StateSyncScopes.Equipment,
            "equipment-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Character]);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Inventory]);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Equipment]);
        Assert.Equal(2, service.GetChangedRevisions(characterId).Count);
        Assert.Collection(
            realtime.Messages,
            first => Assert.Equal(
                StateSyncScopes.Inventory,
                Assert.IsType<StateInvalidated>(first.Message).Scope),
            second => Assert.Equal(
                StateSyncScopes.Equipment,
                Assert.IsType<StateInvalidated>(second.Message).Scope));
    }

    [Fact]
    public async Task Response_revisions_include_only_the_request_character_and_world_scopes()
    {
        await using var db = CreateDb();
        var service = new StateSyncService(
            db,
            new RecordingRealtimeBroadcaster(),
            new FixedTimeProvider(Now));
        var requestCharacterId = Guid.NewGuid();
        var otherCharacterId = Guid.NewGuid();

        await service.InvalidateCharacterScopeAsync(
            requestCharacterId,
            StateSyncScopes.Inventory,
            "request-character",
            CancellationToken.None);
        await service.InvalidateCharacterScopeAsync(
            otherCharacterId,
            StateSyncScopes.Equipment,
            "other-character",
            CancellationToken.None);
        await service.InvalidateWorldScopeAsync(
            StateSyncScopes.Marketplace,
            "world",
            CancellationToken.None);

        var revisions = service.GetChangedRevisions(requestCharacterId);

        Assert.Equal(2, revisions.Count);
        Assert.Contains(StateSyncScopes.Inventory, revisions.Keys);
        Assert.Contains(StateSyncScopes.Marketplace, revisions.Keys);
        Assert.DoesNotContain(StateSyncScopes.Equipment, revisions.Keys);
    }

    [Fact]
    public async Task Realtime_broadcaster_enqueues_persistent_delivery_with_character_metadata()
    {
        var outbox = new RecordingOutbox();
        var immediate = new RecordingImmediatePublisher();
        var broadcaster = new OutboxGameRealtimeBroadcaster(
            outbox,
            immediate,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var characterId = Guid.NewGuid();

        await broadcaster.PublishAsync(
            new Audience.Character(characterId),
            new StateInvalidated(characterId, "character", 7, "test"),
            "test",
            CancellationToken.None);

        var queued = Assert.Single(outbox.Messages);
        Assert.Equal(GameEventTypes.RealtimeDeliveryRequested, queued.EventType);
        Assert.Equal(characterId, queued.CharacterId);
        var payload = Assert.IsType<RealtimeDeliveryRequestedPayload>(queued.Payload);
        Assert.Equal(nameof(StateInvalidated), payload.EventName);
        Assert.Equal("character", payload.Audience.Kind);
        Assert.Empty(immediate.Messages);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingRealtimeBroadcaster : IGameRealtimeBroadcaster
    {
        public List<(Audience Audience, GameRealtimeEvent Message)> Messages { get; } = [];

        public Task PublishAsync(
            Audience audience,
            GameRealtimeEvent message,
            string sender,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((audience, message));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingImmediatePublisher : IGameRealtimeImmediatePublisher
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
        public List<QueuedMessage> Messages { get; } = [];

        public Task EnqueueAsync<TPayload>(
            string eventType,
            TPayload payload,
            Guid? characterId,
            Guid? accountId,
            CancellationToken cancellationToken)
        {
            Messages.Add(new QueuedMessage(eventType, payload!, characterId));
            return Task.CompletedTask;
        }
    }

    private sealed record QueuedMessage(string EventType, object Payload, Guid? CharacterId);
}
