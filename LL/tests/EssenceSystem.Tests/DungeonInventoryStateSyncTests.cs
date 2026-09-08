using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.Interfaces.Outbox;
using Application.Interfaces.Services.LL;
using Application.Interfaces.WebSockets;
using Application.MediatR.Behaviors;
using Application.MediatR.Synchronization;
using Application.UseCases.CharacterActions.Commands.ResolveCharacterAction;
using Application.UseCases.CharacterActions.Dtos.Responses;
using Application.UseCases.Dungeons.Commands.StartDungeonRun;
using Application.UseCases.Dungeons.Dtos;
using Application.UseCases.Outbox;
using Application.WebSockets.Contracts;
using API.LL.HostedServices;
using Common.Primitives;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Persistence.LL;
using Persistence.LL.Repositories.Outbox;
using Services.LL.JsonDefinitions.Dungeons;
using Services.LL.JsonDefinitions.Reader;
using Services.LL.Synchronization;

namespace EssenceSystem.Tests;

public sealed class DungeonInventoryStateSyncTests
{
    private const string Sigil = "sigil_goblin_mines";

    [Theory]
    [InlineData(Sigil, 0, 1, false, true)]
    [InlineData(Sigil, 2, 3, false, true)]
    [InlineData(Sigil, 2, 1, false, true)]
    [InlineData(Sigil, 1, 0, false, true)]
    [InlineData(Sigil, 1, 0, true, true)]
    [InlineData(SigilFragmentItem.ItemBaseId, 5, 10, true, true)]
    [InlineData("iron_ore", 1, 2, false, false)]
    [InlineData(Sigil, 1, 1, false, false)]
    public async Task Inventory_changes_invalidate_dungeons_only_when_availability_changes(
        string itemId, int before, int after, bool saveInsideCommand, bool expected)
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var item = CreateItem(characterId, itemId, before);
        if (before > 0)
        {
            db.InventoryItems.Add(item);
            await db.SaveChangesAsync();
        }
        var realtime = new RecordingRealtime();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior(db, sync).Handle(new ResolveCharacterActionCommand(characterId), async _ =>
        {
            if (before == 0)
            {
                item.Quantity = after;
                db.InventoryItems.Add(item);
            }
            else if (after == 0)
                db.InventoryItems.Remove(item);
            else
            {
                item.Quantity = after;
                // Favorite/seen updates alone must not refresh dungeons.
                item.IsFavorite = true;
                item.SeenAtUtc = DateTimeOffset.UtcNow;
            }
            if (saveInsideCommand) await db.SaveChangesAsync();
            return Response<CharacterActionDto?>.Success(null);
        }, CancellationToken.None);

        var checkpoint = await sync.GetCheckpointAsync(characterId, CancellationToken.None);
        Assert.Equal(expected ? 1L : 0L, checkpoint.Revisions[StateSyncScopes.Dungeons]);
        Assert.Equal(expected ? 1 : 0, realtime.DungeonInvalidationCount(characterId));
    }

    [Fact]
    public async Task Batched_loot_is_coalesced_and_inventory_transfers_notify_both_owners()
    {
        await using var db = CreateDb();
        var sender = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var source = CreateItem(sender, Sigil, 3);
        db.InventoryItems.Add(source);
        await db.SaveChangesAsync();
        var realtime = new RecordingRealtime();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior(db, sync).Handle(new ResolveCharacterActionCommand(sender), async _ =>
        {
            db.InventoryItems.Remove(source);
            var grant = CreateItem(recipient, Sigil, 3);
            grant.ItemInstance.ItemBase = source.ItemInstance.ItemBase;
            db.InventoryItems.Add(grant);
            await db.SaveChangesAsync();
            var destination = await db.InventoryItems.SingleAsync(item => item.InventoryId == recipient);
            destination.Quantity += 5;
            await db.SaveChangesAsync();
            destination.Quantity += 2;
            return Response<CharacterActionDto?>.Success(null);
        }, CancellationToken.None);

        Assert.Equal(1, realtime.DungeonInvalidationCount(sender));
        Assert.Equal(1, realtime.DungeonInvalidationCount(recipient));
        Assert.Equal(0, realtime.DungeonInvalidationCount(Guid.NewGuid()));
    }

    [Fact]
    public async Task Consolidating_stacks_does_not_invalidate_unchanged_totals()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var first = CreateItem(characterId, Sigil, 2);
        var second = CreateItem(characterId, Sigil, 3);
        second.ItemInstance.ItemBase = first.ItemInstance.ItemBase;
        db.InventoryItems.AddRange(first, second);
        await db.SaveChangesAsync();
        var realtime = new RecordingRealtime();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior(db, sync).Handle(new ResolveCharacterActionCommand(characterId), async _ =>
        {
            first.Quantity += second.Quantity;
            db.InventoryItems.Remove(second);
            await db.SaveChangesAsync();
            return Response<CharacterActionDto?>.Success(null);
        }, CancellationToken.None);

        Assert.Equal(0, realtime.DungeonInvalidationCount(characterId));
    }

    [Fact]
    public async Task Quantity_changes_are_detected_without_loaded_item_navigation_and_reset_for_the_next_transaction()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.InventoryItems.Add(CreateItem(characterId, Sigil, 2));
        await db.SaveChangesAsync();
        db.ClearTrackedEntities();
        var item = await db.InventoryItems.SingleAsync();
        Assert.Null(item.ItemInstance);
        var realtime = new RecordingRealtime();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        await CreateBehavior(db, sync).Handle(new ResolveCharacterActionCommand(characterId), _ =>
        {
            item.Quantity--;
            return Task.FromResult(Response<CharacterActionDto?>.Success(null));
        }, CancellationToken.None);
        Assert.Equal(1, realtime.DungeonInvalidationCount(characterId));

        var nextRealtime = new RecordingRealtime();
        var nextSync = new StateSyncService(db, nextRealtime, TimeProvider.System);
        await CreateBehavior(db, nextSync).Handle(new ResolveCharacterActionCommand(characterId), _ =>
        {
            item.IsFavorite = true;
            return Task.FromResult(Response<CharacterActionDto?>.Success(null));
        }, CancellationToken.None);
        Assert.Equal(0, nextRealtime.DungeonInvalidationCount(characterId));
    }

    [Fact]
    public async Task Dungeon_response_advances_its_revision_without_an_extra_invalidation()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var item = CreateItem(characterId, Sigil, 1);
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        var realtime = new RecordingRealtime();
        var sync = new StateSyncService(db, realtime, TimeProvider.System);
        var behavior = new TransactionBehavior<StartDungeonRunCommand, Response<StartDungeonRunResponseDto>>(
            db, sync, NullLogger<TransactionBehavior<StartDungeonRunCommand, Response<StartDungeonRunResponseDto>>>.Instance,
            CreateSync(db));
        await behavior.Handle(new StartDungeonRunCommand(characterId, "goblin_mines", DungeonTier.Normal), _ =>
        {
            db.InventoryItems.Remove(item);
            return Task.FromResult(Response<StartDungeonRunResponseDto>.Success(null!));
        }, CancellationToken.None);

        Assert.Equal(1, sync.GetChangedRevisions(characterId)[StateSyncScopes.Dungeons]);
        Assert.Equal(0, realtime.DungeonInvalidationCount(characterId));
    }

    [Fact]
    public void Catalog_uses_configured_costs_and_sigil_ids_instead_of_item_name_prefixes()
    {
        var reader = CreateReader();
        reader.Value.Families.Add(new DungeonFamilyDefinition
        {
            Id = "test", SigilItemId = "entry_token",
            EntryCosts = [new() { ItemId = "ritual_stone", Amount = 2 }, new() { ItemId = "free_item", Amount = 0 }]
        });
        var catalog = new JsonDungeonInventoryItemCatalog(reader);
        Assert.True(catalog.AffectsDungeonAvailability("ENTRY_TOKEN"));
        Assert.True(catalog.AffectsDungeonAvailability("ritual_stone"));
        Assert.True(catalog.AffectsDungeonAvailability(SigilFragmentItem.ItemBaseId));
        Assert.False(catalog.AffectsDungeonAvailability("free_item"));
        Assert.False(catalog.AffectsDungeonAvailability("sigil_unrelated"));
    }

    [Theory]
    [InlineData(Sigil, true)]
    [InlineData(SigilFragmentItem.ItemBaseId, true)]
    [InlineData("iron_ore", false)]
    public async Task Outbox_rewards_invalidate_dungeons_even_when_the_consumer_saves_before_returning(string itemId, bool expected)
    {
        var characterId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<LLDbContext>(options => options.UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<IDbContext>(provider => provider.GetRequiredService<LLDbContext>());
        services.AddScoped<IGameEventOutboxRepository, GameEventOutboxRepository>();
        services.AddScoped<IGameEventOutboxConsumer, SavingInventoryRewardConsumer>();
        services.AddScoped<IStateSyncService, StateSyncService>();
        services.AddScoped(provider => CreateSync(provider.GetRequiredService<LLDbContext>()));
        services.AddSingleton<IGameRealtimeBroadcaster, RecordingRealtime>();
        services.AddSingleton(TimeProvider.System);
        await using var provider = services.BuildServiceProvider();
        using (var seedScope = provider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<LLDbContext>();
            var message = new GameEventOutboxMessage
            {
                Id = Guid.NewGuid(), CharacterId = characterId,
                EventType = GameEventTypes.CharacterCreated,
                PayloadJson = itemId, CreatedAt = DateTimeOffset.UtcNow,
                AvailableAt = DateTimeOffset.UtcNow
            };
            db.GameEventOutboxDeliveries.Add(new GameEventOutboxDelivery
            {
                Id = deliveryId, MessageId = message.Id, Message = message,
                Consumer = "test-inventory-reward", Status = GameEventOutboxDeliveryStatus.Pending,
                CreatedAt = message.CreatedAt, AvailableAt = message.AvailableAt
            });
            await db.SaveChangesAsync();
        }

        using var worker = new GameEventOutboxWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GameEventOutboxWorker>.Instance, TimeProvider.System);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LLDbContext>();
                var delivery = await db.GameEventOutboxDeliveries.SingleAsync(item => item.Id == deliveryId, timeout.Token);
                if (delivery.Status == GameEventOutboxDeliveryStatus.Processed)
                {
                    var checkpoint = await scope.ServiceProvider.GetRequiredService<IStateSyncService>()
                        .GetCheckpointAsync(characterId, timeout.Token);
                    Assert.Equal(expected ? 1L : 0L, checkpoint.Revisions[StateSyncScopes.Dungeons]);
                    break;
                }
                await Task.Delay(20, timeout.Token);
            }
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private sealed class SavingInventoryRewardConsumer(LLDbContext db) : IGameEventOutboxConsumer
    {
        public string Consumer => "test-inventory-reward";
        public bool CanHandle(string eventType) => eventType == GameEventTypes.CharacterCreated;
        public async Task HandleAsync(GameEventOutboxMessage message, CancellationToken cancellationToken)
        {
            db.InventoryItems.Add(CreateItem(message.CharacterId!.Value, message.PayloadJson, 1));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    internal static DungeonInventoryStateSync CreateSync(LLDbContext db) => new(db, new JsonDungeonInventoryItemCatalog(CreateReader()));

    internal static JsonDocumentReader<DungeonCatalogDocument> CreateReader()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return new(TestContentPaths.FindApiRoot(), "Data/dungeons/dungeons.json", options);
    }

    internal static InventoryItem CreateItem(Guid characterId, string itemId, int quantity)
    {
        var instanceId = Guid.NewGuid();
        return new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = instanceId,
            Quantity = quantity,
            ItemInstance = new ItemInstance
            {
                Id = instanceId,
                ItemBaseId = itemId,
                ItemBase = new ItemBase { Id = itemId, Name = itemId, Stackable = true, ItemType = ItemType.Resource }
            }
        };
    }

    private static TransactionBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>> CreateBehavior(
        LLDbContext db, StateSyncService sync) => new(db, sync,
        NullLogger<TransactionBehavior<ResolveCharacterActionCommand, Response<CharacterActionDto?>>>.Instance, CreateSync(db));

    private static LLDbContext CreateDb() => new(new DbContextOptionsBuilder<LLDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private sealed class RecordingRealtime : IGameRealtimeBroadcaster
    {
        private readonly List<GameRealtimeEvent> _messages = [];
        public int DungeonInvalidationCount(Guid characterId) => _messages.Count(message => message switch
        {
            StateInvalidated invalidation => invalidation.CharacterId == characterId && invalidation.Scope == StateSyncScopes.Dungeons,
            StateInvalidations invalidations => invalidations.CharacterId == characterId && invalidations.Revisions.ContainsKey(StateSyncScopes.Dungeons),
            _ => false
        });
        public Task PublishAsync(Audience audience, GameRealtimeEvent message, string sender, CancellationToken cancellationToken = default)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
