using System.Text.Json;
using Application.Interfaces.Outbox;
using Application.Interfaces.WebSockets;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using Domain.Models.Guilds;
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
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildBuildings]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildMissions]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildShop]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildMembership]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildInvites]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildDirectory]);
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
            StateSyncScopes.Marketplace,
            "marketplace-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var checkpoint = await service.GetCheckpointAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Marketplace]);
        var publication = Assert.Single(realtime.Messages);
        Assert.IsType<Audience.World>(publication.Audience);
        var notification = Assert.IsType<StateInvalidated>(publication.Message);
        Assert.Equal(StateSyncScopes.Marketplace, notification.Scope);
        Assert.Null(notification.CharacterId);
    }

    [Fact]
    public async Task Guild_scope_invalidation_targets_only_the_guild_and_remains_checkpointed()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var guildId = Guid.NewGuid();
        var memberCharacterId = Guid.NewGuid();
        var unrelatedCharacterId = Guid.NewGuid();
        db.GuildMembers.Add(new GuildMember
        {
            GuildId = guildId,
            CharacterId = memberCharacterId
        });
        await db.SaveChangesAsync(CancellationToken.None);

        await service.InvalidateGuildScopeAsync(
            guildId,
            "guild-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var publication = Assert.Single(realtime.Messages);
        Assert.Equal(guildId, Assert.IsType<Audience.Guild>(publication.Audience).GuildId);
        var notification = Assert.IsType<StateInvalidated>(publication.Message);
        Assert.Equal(StateSyncScopes.Guild, notification.Scope);
        Assert.Null(notification.CharacterId);

        var memberCheckpoint = await service.GetCheckpointAsync(
            memberCharacterId,
            CancellationToken.None);
        var unrelatedCheckpoint = await service.GetCheckpointAsync(
            unrelatedCharacterId,
            CancellationToken.None);
        Assert.Equal(1, memberCheckpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(0, unrelatedCheckpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(
            $"guild:{guildId:N}:guild",
            (await db.StateSyncRevisions.SingleAsync()).ScopeKey);
    }

    [Fact]
    public async Task Guild_generations_advance_independently()
    {
        await using var db = CreateDb();
        var service = new StateSyncService(
            db,
            new RecordingRealtimeBroadcaster(),
            new FixedTimeProvider(Now));
        var firstGuildId = Guid.NewGuid();
        var secondGuildId = Guid.NewGuid();
        var firstMemberId = Guid.NewGuid();
        var secondMemberId = Guid.NewGuid();
        db.GuildMembers.AddRange(
            new GuildMember { GuildId = firstGuildId, CharacterId = firstMemberId },
            new GuildMember { GuildId = secondGuildId, CharacterId = secondMemberId });
        await db.SaveChangesAsync(CancellationToken.None);

        await service.InvalidateGuildScopeAsync(firstGuildId, "first-change", CancellationToken.None);
        await service.InvalidateGuildScopeAsync(secondGuildId, "second-change", CancellationToken.None);
        await service.InvalidateGuildScopeAsync(secondGuildId, "duplicate", CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var firstCheckpoint = await service.GetCheckpointAsync(firstMemberId, CancellationToken.None);
        var secondCheckpoint = await service.GetCheckpointAsync(secondMemberId, CancellationToken.None);

        Assert.Equal(1, firstCheckpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(1, secondCheckpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(2, await db.StateSyncRevisions.CountAsync());
    }

    [Fact]
    public async Task Guild_subresources_advance_independently_for_the_same_audience()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        db.GuildMembers.Add(new GuildMember { GuildId = guildId, CharacterId = characterId });
        await db.SaveChangesAsync(CancellationToken.None);

        await service.InvalidateGuildScopeAsync(
            guildId,
            StateSyncScopes.GuildBuildings,
            "building-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.Guild]);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.GuildBuildings]);
        Assert.Equal(0, checkpoint.Revisions[StateSyncScopes.GuildMissions]);
        Assert.Equal(
            $"guild:{guildId:N}:guild-buildings",
            (await db.StateSyncRevisions.SingleAsync()).ScopeKey);

        var publication = Assert.Single(realtime.Messages);
        Assert.Equal(guildId, Assert.IsType<Audience.Guild>(publication.Audience).GuildId);
        Assert.Equal(
            StateSyncScopes.GuildBuildings,
            Assert.IsType<StateInvalidated>(publication.Message).Scope);
    }

    [Fact]
    public async Task Version_only_guild_advance_is_checkpointed_without_live_delivery()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var guildId = Guid.NewGuid();
        var characterId = Guid.NewGuid();
        db.GuildMembers.Add(new GuildMember { GuildId = guildId, CharacterId = characterId });
        await db.SaveChangesAsync(CancellationToken.None);

        await service.AdvanceGuildScopeAsync(guildId, "response-owned", CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.Empty(realtime.Messages);
        Assert.Equal(1, service.GetChangedRevisions(characterId)[StateSyncScopes.Guild]);
        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Guild]);
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
    public async Task Character_resource_batch_uses_one_delivery_and_preserves_each_revision()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var characterId = Guid.NewGuid();

        await service.InvalidateCharacterScopesAsync(
            characterId,
            [StateSyncScopes.Character, StateSyncScopes.Inventory],
            "marketplace-expiration",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Character]);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Inventory]);
        Assert.Equal(2, await db.StateSyncRevisions.CountAsync());

        var publication = Assert.Single(realtime.Messages);
        Assert.Equal(
            characterId,
            Assert.IsType<Audience.Character>(publication.Audience).CharacterId);
        var invalidations = Assert.IsType<StateInvalidations>(publication.Message);
        Assert.Equal(characterId, invalidations.CharacterId);
        Assert.Equal(1, invalidations.Revisions[StateSyncScopes.Character]);
        Assert.Equal(1, invalidations.Revisions[StateSyncScopes.Inventory]);
    }

    [Fact]
    public async Task Colosseum_revision_is_isolated_to_the_affected_character()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var affectedCharacterId = Guid.NewGuid();

        await service.InvalidateCharacterScopeAsync(
            affectedCharacterId,
            StateSyncScopes.Colosseum,
            "arena-change",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        var affectedCheckpoint = await service.GetCheckpointAsync(
            affectedCharacterId,
            CancellationToken.None);
        var unrelatedCheckpoint = await service.GetCheckpointAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(1, affectedCheckpoint.Revisions[StateSyncScopes.Colosseum]);
        Assert.Equal(0, unrelatedCheckpoint.Revisions[StateSyncScopes.Colosseum]);
        var publication = Assert.Single(realtime.Messages);
        Assert.Equal(
            affectedCharacterId,
            Assert.IsType<Audience.Character>(publication.Audience).CharacterId);
    }

    [Fact]
    public async Task Version_only_advance_is_checkpointed_and_returned_without_realtime_delivery()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));
        var characterId = Guid.NewGuid();

        await service.AdvanceCharacterScopeAsync(
            characterId,
            StateSyncScopes.Inventory,
            "response-owned",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.Empty(realtime.Messages);
        Assert.Equal(1, service.GetChangedRevisions(characterId)[StateSyncScopes.Inventory]);
        var checkpoint = await service.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Inventory]);
    }

    [Fact]
    public async Task Version_only_world_advance_preserves_reconnect_recovery_without_live_dirty_delivery()
    {
        await using var db = CreateDb();
        var realtime = new RecordingRealtimeBroadcaster();
        var service = new StateSyncService(db, realtime, new FixedTimeProvider(Now));

        var firstVersion = await service.AdvanceWorldScopeWithRevisionAsync(
            StateSyncScopes.Marketplace,
            "semantic-event-owned",
            CancellationToken.None);
        var repeatedVersion = await service.AdvanceWorldScopeWithRevisionAsync(
            StateSyncScopes.Marketplace,
            "transaction-pipeline-repeat",
            CancellationToken.None);
        await db.SaveChangesAsync(CancellationToken.None);

        Assert.Empty(realtime.Messages);
        Assert.Equal(1, firstVersion);
        Assert.Equal(firstVersion, repeatedVersion);
        Assert.Equal(1, service.GetChangedRevisions(null)[StateSyncScopes.Marketplace]);
        var checkpoint = await service.GetCheckpointAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Equal(1, checkpoint.Revisions[StateSyncScopes.Marketplace]);
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
