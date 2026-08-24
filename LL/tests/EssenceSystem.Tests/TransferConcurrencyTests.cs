using Application.Common.Interfaces;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Entities.Characters;
using Persistence.LL.Repositories.Inventories;

namespace EssenceSystem.Tests;

public sealed class TransferConcurrencyTests
{
    [Fact]
    public async Task Currency_transfer_requests_locks_for_both_participants()
    {
        await using var db = CreateLockCapturingDbContext();
        var sender = AddParticipant(db, "LockSender", 100);
        var recipient = AddParticipant(db, "LockRecipient", 0);
        await db.SaveChangesAsync();

        var result = await new CurrencyTransferRepository(db).TransferCindersAsync(
            sender.Id,
            recipient.Id,
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(db.RequestedCharacterLocks);
        Assert.True(new HashSet<Guid> { sender.Id, recipient.Id }
            .SetEquals(db.RequestedCharacterLocks));
    }

    [Fact]
    public async Task Inventory_transfer_requests_locks_for_both_participants()
    {
        await using var db = CreateLockCapturingDbContext();
        var sender = AddParticipant(db, "ItemLockSender", 0, withInventory: true);
        var recipient = AddParticipant(db, "ItemLockRecipient", 0, withInventory: true);
        var itemBase = new ItemBase
        {
            Id = "lock_test_item",
            Name = "Lock Test Item",
            Description = "Participant lock test item.",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        var item = AddInventoryItem(db, sender.Id, itemBase, 2);
        await db.SaveChangesAsync();

        var result = await new InventoryRepository(db).TransferItemAsync(
            sender.Id,
            recipient.Id,
            item.ItemInstanceId,
            1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(db.RequestedCharacterLocks);
        Assert.True(new HashSet<Guid> { sender.Id, recipient.Id }
            .SetEquals(db.RequestedCharacterLocks));
    }

    [Fact]
    public async Task Concurrent_cinder_transfers_to_the_same_recipient_are_serialized()
    {
        var connectionString = GetPostgresConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await RunInIsolatedSchemaAsync(connectionString, "cinders", async isolatedConnectionString =>
        {
            Guid firstSenderId;
            Guid secondSenderId;
            Guid recipientId;

            await using (var seedDb = CreatePostgresDbContext(isolatedConnectionString))
            {
                firstSenderId = AddParticipant(seedDb, "CinderSenderOne", 1_000).Id;
                secondSenderId = AddParticipant(seedDb, "CinderSenderTwo", 1_000).Id;
                recipientId = AddParticipant(seedDb, "CinderRecipient", 100).Id;
                await seedDb.SaveChangesAsync();
            }

            await using var firstDb = CreatePostgresDbContext(isolatedConnectionString);
            await using var firstTransaction = await firstDb.Database.BeginTransactionAsync();
            var firstResult = await new CurrencyTransferRepository(firstDb).TransferCindersAsync(
                firstSenderId,
                recipientId,
                200,
                CancellationToken.None);
            Assert.True(firstResult.IsSuccess);
            await firstDb.SaveChangesAsync();

            await using var secondDb = CreatePostgresDbContext(isolatedConnectionString);
            await using var secondTransaction = await secondDb.Database.BeginTransactionAsync();
            var secondTransfer = new CurrencyTransferRepository(secondDb).TransferCindersAsync(
                secondSenderId,
                recipientId,
                300,
                CancellationToken.None);

            var completedBeforeFirstCommit = await CompletesWithinAsync(
                secondTransfer,
                TimeSpan.FromMilliseconds(500));

            await firstTransaction.CommitAsync();
            var secondResult = await secondTransfer.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(secondResult.IsSuccess);
            await secondDb.SaveChangesAsync();
            await secondTransaction.CommitAsync();

            Assert.False(completedBeforeFirstCommit);

            await using var verifyDb = CreatePostgresDbContext(isolatedConnectionString);
            var balances = await verifyDb.Characters
                .AsNoTracking()
                .Where(x => x.Id == firstSenderId || x.Id == secondSenderId || x.Id == recipientId)
                .ToDictionaryAsync(x => x.Id, x => x.Cinders);
            Assert.Equal(800, balances[firstSenderId]);
            Assert.Equal(700, balances[secondSenderId]);
            Assert.Equal(600, balances[recipientId]);
            Assert.Equal(2, await verifyDb.PlayerTransferHistory.CountAsync());
            Assert.Equal(2, await verifyDb.EconomyLedger.CountAsync(
                x => x.EventType == EconomyEventType.DirectCurrencyTransfer));
        });
    }

    [Fact]
    public async Task Concurrent_stackable_item_transfers_to_the_same_recipient_are_serialized()
    {
        var connectionString = GetPostgresConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await RunInIsolatedSchemaAsync(connectionString, "inventory", async isolatedConnectionString =>
        {
            Guid firstSenderId;
            Guid secondSenderId;
            Guid recipientId;
            Guid firstSenderItemId;
            Guid secondSenderItemId;

            await using (var seedDb = CreatePostgresDbContext(isolatedConnectionString))
            {
                firstSenderId = AddParticipant(seedDb, "ItemSenderOne", 0, withInventory: true).Id;
                secondSenderId = AddParticipant(seedDb, "ItemSenderTwo", 0, withInventory: true).Id;
                recipientId = AddParticipant(seedDb, "ItemRecipient", 0, withInventory: true).Id;

                var itemBase = new ItemBase
                {
                    Id = "concurrency_ore",
                    Name = "Concurrency Ore",
                    Description = "Transfer concurrency test item.",
                    ItemType = ItemType.Resource,
                    Stackable = true
                };
                firstSenderItemId = AddInventoryItem(seedDb, firstSenderId, itemBase, 10).ItemInstanceId;
                secondSenderItemId = AddInventoryItem(seedDb, secondSenderId, itemBase, 10).ItemInstanceId;
                AddInventoryItem(seedDb, recipientId, itemBase, 3);
                await seedDb.SaveChangesAsync();
            }

            await using var firstDb = CreatePostgresDbContext(isolatedConnectionString);
            await using var firstTransaction = await firstDb.Database.BeginTransactionAsync();
            var firstResult = await new InventoryRepository(firstDb).TransferItemAsync(
                firstSenderId,
                recipientId,
                firstSenderItemId,
                4,
                CancellationToken.None);
            Assert.True(firstResult.IsSuccess);
            await firstDb.SaveChangesAsync();

            await using var secondDb = CreatePostgresDbContext(isolatedConnectionString);
            await using var secondTransaction = await secondDb.Database.BeginTransactionAsync();
            var secondTransfer = new InventoryRepository(secondDb).TransferItemAsync(
                secondSenderId,
                recipientId,
                secondSenderItemId,
                5,
                CancellationToken.None);

            var completedBeforeFirstCommit = await CompletesWithinAsync(
                secondTransfer,
                TimeSpan.FromMilliseconds(500));

            await firstTransaction.CommitAsync();
            var secondResult = await secondTransfer.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(secondResult.IsSuccess);
            await secondDb.SaveChangesAsync();
            await secondTransaction.CommitAsync();

            Assert.False(completedBeforeFirstCommit);

            await using var verifyDb = CreatePostgresDbContext(isolatedConnectionString);
            var recipientStacks = await verifyDb.InventoryItems
                .AsNoTracking()
                .Include(x => x.ItemInstance)
                .Where(x => x.InventoryId == recipientId &&
                            x.ItemInstance.ItemBaseId == "concurrency_ore")
                .ToListAsync();
            var recipientStack = Assert.Single(recipientStacks);
            Assert.Equal(12, recipientStack.Quantity);
            Assert.Equal(6, await verifyDb.InventoryItems
                .Where(x => x.InventoryId == firstSenderId && x.ItemInstanceId == firstSenderItemId)
                .Select(x => x.Quantity)
                .SingleAsync());
            Assert.Equal(5, await verifyDb.InventoryItems
                .Where(x => x.InventoryId == secondSenderId && x.ItemInstanceId == secondSenderItemId)
                .Select(x => x.Quantity)
                .SingleAsync());
            Assert.Equal(2, await verifyDb.PlayerTransferHistory.CountAsync());
            Assert.Equal(2, await verifyDb.EconomyLedger.CountAsync(
                x => x.EventType == EconomyEventType.DirectItemTransfer));
        });
    }

    [Fact]
    public async Task Opposite_direction_cinder_transfers_complete_with_pipeline_advisory_locks()
    {
        var connectionString = GetPostgresConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        await RunInIsolatedSchemaAsync(connectionString, "reverse", async isolatedConnectionString =>
        {
            Guid firstCharacterId;
            Guid secondCharacterId;

            await using (var seedDb = CreatePostgresDbContext(isolatedConnectionString))
            {
                firstCharacterId = AddParticipant(seedDb, "ReverseOne", 100).Id;
                secondCharacterId = AddParticipant(seedDb, "ReverseTwo", 100).Id;
                await seedDb.SaveChangesAsync();
            }

            var readyCount = 0;
            var bothAdvisoryLocksAcquired = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            async Task TransferAsync(Guid senderId, Guid recipientId)
            {
                await using var db = CreatePostgresDbContext(isolatedConnectionString);
                await using var transaction = await db.Database.BeginTransactionAsync();
                await db.AcquireCharacterCommandLockAsync(senderId);
                if (Interlocked.Increment(ref readyCount) == 2)
                    bothAdvisoryLocksAcquired.TrySetResult();
                await bothAdvisoryLocksAcquired.Task.WaitAsync(TimeSpan.FromSeconds(15));

                var result = await new CurrencyTransferRepository(db).TransferCindersAsync(
                    senderId,
                    recipientId,
                    10,
                    CancellationToken.None);
                Assert.True(result.IsSuccess);
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await Task.WhenAll(
                    TransferAsync(firstCharacterId, secondCharacterId),
                    TransferAsync(secondCharacterId, firstCharacterId))
                .WaitAsync(TimeSpan.FromSeconds(20));

            await using var verifyDb = CreatePostgresDbContext(isolatedConnectionString);
            var balances = await verifyDb.Characters
                .AsNoTracking()
                .Where(x => x.Id == firstCharacterId || x.Id == secondCharacterId)
                .ToDictionaryAsync(x => x.Id, x => x.Cinders);
            Assert.Equal(100, balances[firstCharacterId]);
            Assert.Equal(100, balances[secondCharacterId]);
            Assert.Equal(2, await verifyDb.PlayerTransferHistory.CountAsync());
        });
    }

    private static async Task<bool> CompletesWithinAsync(Task task, TimeSpan timeout) =>
        await Task.WhenAny(task, Task.Delay(timeout)) == task;

    private static Character AddParticipant(
        LLDbContext db,
        string name,
        long cinders,
        bool withInventory = false)
    {
        var user = new AppUser
        {
            Username = $"{name}-{Guid.NewGuid():N}"[..Math.Min(26, name.Length + 1 + 32)],
            IsGuest = false
        };
        var character = new Character
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = name,
            Cinders = cinders
        };
        character.NormalizeName();
        db.Users.Add(user);
        db.Characters.Add(character);
        if (withInventory)
            db.Inventories.Add(new Inventory { CharacterId = character.Id });
        return character;
    }

    private static InventoryItem AddInventoryItem(
        LLDbContext db,
        Guid characterId,
        ItemBase itemBase,
        int quantity)
    {
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };
        var inventoryItem = new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = quantity
        };
        db.InventoryItems.Add(inventoryItem);
        return inventoryItem;
    }

    private static string? GetPostgresConnectionString() =>
        Environment.GetEnvironmentVariable("LL_TEST_TRANSFER_POSTGRES_CONNECTION") ??
        Environment.GetEnvironmentVariable("LL_TEST_TOURNAMENT_POSTGRES_CONNECTION");

    private static async Task RunInIsolatedSchemaAsync(
        string connectionString,
        string scenario,
        Func<string, Task> test)
    {
        var schemaName = $"ll_transfer_{scenario}_{Guid.NewGuid():N}";
        await using var adminDb = CreatePostgresDbContext(connectionString);
        var createSchemaSql = $"CREATE SCHEMA \"{schemaName}\"";
        var dropSchemaSql = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await adminDb.Database.ExecuteSqlRawAsync(createSchemaSql);

        try
        {
            var isolatedConnectionString = WithSearchPath(connectionString, schemaName);
            await using (var migrationDb = CreatePostgresDbContext(
                             isolatedConnectionString,
                             schemaName))
            {
                await migrationDb.Database.MigrateAsync();
            }

            await test(isolatedConnectionString);
        }
        finally
        {
            await adminDb.Database.ExecuteSqlRawAsync(dropSchemaSql);
        }
    }

    private static LLDbContext CreatePostgresDbContext(
        string connectionString,
        string? migrationsSchema = null)
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseNpgsql(connectionString, postgres =>
            {
                if (!string.IsNullOrWhiteSpace(migrationsSchema))
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema);
            })
            .Options;
        return new LLDbContext(options);
    }

    private static LockCapturingDbContext CreateLockCapturingDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LockCapturingDbContext(options);
    }

    private static string WithSearchPath(string connectionString, string schemaName) =>
        $"{connectionString.Trim().TrimEnd(';')};Search Path={schemaName}";

    private sealed class LockCapturingDbContext(DbContextOptions<LLDbContext> options)
        : LLDbContext(options), IDbContext
    {
        public IReadOnlyList<Guid>? RequestedCharacterLocks { get; private set; }

        Task IDbContext.AcquireCharacterRowsLockAsync(
            IReadOnlyCollection<Guid> characterIds,
            CancellationToken ct)
        {
            RequestedCharacterLocks = characterIds.ToList();
            return Task.CompletedTask;
        }
    }
}
