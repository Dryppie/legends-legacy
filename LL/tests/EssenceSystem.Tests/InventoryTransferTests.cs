using Domain.Models.Inventories;
using Domain.Models.Economy;
using Domain.Models.Entities.Characters;
using Domain.Models.Items;
using Domain.Models.Transfers;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Inventories;
using Domain.Models.Administration;
using Domain.Models.Users;

namespace EssenceSystem.Tests;

public sealed class InventoryTransferTests
{
    [Fact]
    public async Task Partial_stack_transfer_deducts_sender_and_adds_recipient_stack()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var senderItem = AddItem(db, senderId, "iron_ore", quantity: 10, stackable: true);
        var recipientItem = AddItem(db, recipientId, "iron_ore", quantity: 3, stackable: true, reuseBase: senderItem.ItemInstance.ItemBase);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.TransferItemAsync(
            senderId,
            recipientId,
            senderItem.ItemInstanceId,
            4,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.TransferredItem!.Quantity);
        Assert.Equal(6, (await db.InventoryItems.SingleAsync(x =>
            x.InventoryId == senderId && x.ItemInstanceId == senderItem.ItemInstanceId)).Quantity);
        Assert.Equal(7, (await db.InventoryItems.SingleAsync(x =>
            x.InventoryId == recipientId && x.ItemInstanceId == recipientItem.ItemInstanceId)).Quantity);
        var history = await db.PlayerTransferHistory.SingleAsync();
        Assert.Equal(PlayerTransferKind.InventoryItem, history.Kind);
        Assert.Equal("iron_ore", history.AssetId);
        Assert.Equal(senderItem.ItemInstanceId, history.SourceItemInstanceId);
        Assert.Equal(recipientItem.ItemInstanceId, history.DestinationItemInstanceId);
        Assert.Equal(4, history.Quantity);
        var ledgerEntry = await db.EconomyLedger.SingleAsync();
        Assert.Equal(EconomyEventType.DirectItemTransfer, ledgerEntry.EventType);
        Assert.Equal(senderId, ledgerEntry.SenderCharacterId);
        Assert.Equal(recipientId, ledgerEntry.RecipientCharacterId);
        Assert.Equal(senderItem.ItemInstanceId, ledgerEntry.SourceItemInstanceId);
        Assert.Equal(recipientItem.ItemInstanceId, ledgerEntry.DestinationItemInstanceId);
    }

    [Fact]
    public async Task Inventory_grant_records_acquisition_metadata_and_ledger_entry()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        AddInventories(db, characterId);
        await db.SaveChangesAsync();

        var itemBase = new ItemBase
        {
            Id = "quest_token",
            Name = "Quest Token",
            Description = "Acquisition metadata test item.",
            ItemType = ItemType.Resource,
            Stackable = true
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };
        var item = new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = 3
        };

        var before = DateTimeOffset.UtcNow;
        await new InventoryRepository(db).AddItemsToInventory(
            characterId,
            [item],
            "quest-reward",
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.Equal("quest-reward", itemInstance.AcquisitionSource);
        Assert.True(itemInstance.AcquiredAtUtc >= before);
        var ledgerEntry = await db.EconomyLedger.SingleAsync();
        Assert.Equal(EconomyEventType.ItemAcquisition, ledgerEntry.EventType);
        Assert.Equal(characterId, ledgerEntry.RecipientCharacterId);
        Assert.Equal(3, ledgerEntry.Quantity);
        Assert.Equal("quest-reward", ledgerEntry.Source);
    }

    [Fact]
    public async Task Economy_ledger_is_append_only()
    {
        await using var db = CreateDb();
        var entry = new EconomyLedgerEntry
        {
            EventType = EconomyEventType.ItemAcquisition,
            AssetType = EconomyAssetType.Item,
            AssetId = "test-item",
            AssetName = "Test Item",
            Quantity = 1,
            Source = "test"
        };
        db.EconomyLedger.Add(entry);
        await db.SaveChangesAsync();

        entry.Quantity = 2;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Full_non_stackable_transfer_moves_item_instance_to_recipient()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var item = AddItem(db, senderId, "unique_relic", quantity: 1, stackable: false);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            1,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(result.IsSuccess);
        Assert.False(await db.InventoryItems.AnyAsync(x =>
            x.InventoryId == senderId && x.ItemInstanceId == item.ItemInstanceId));
        Assert.True(await db.InventoryItems.AnyAsync(x =>
            x.InventoryId == recipientId && x.ItemInstanceId == item.ItemInstanceId));
        Assert.Single(db.PlayerTransferHistory);
    }

    [Fact]
    public async Task Bound_item_transfer_is_rejected_without_mutating_inventory()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var item = AddItem(db, senderId, "bound_token", quantity: 5, stackable: true, isBound: true);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            2,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(InventoryTransferFailure.ItemIsBound, result.Failure);
        Assert.Equal(5, item.Quantity);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.InventoryId == recipientId));
        Assert.Empty(db.PlayerTransferHistory);
    }

    [Fact]
    public async Task Transfer_to_multiplayer_restricted_recipient_is_rejected_without_mutating_inventory()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var recipientAccountId = db.Characters.Local
            .Single(x => x.Id == recipientId)
            .UserId;
        var item = AddItem(db, senderId, "iron_ore", quantity: 5, stackable: true);
        db.AccountRestrictions.Add(new AccountRestriction
        {
            AccountId = recipientAccountId,
            RestrictionType = AccountRestrictionType.MultiplayerRestriction,
            Reason = "Test restriction",
            CreatedBySubject = "staff|moderator",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await new InventoryRepository(db).TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            2,
            CancellationToken.None);

        Assert.Equal(InventoryTransferFailure.AccountRestricted, result.Failure);
        Assert.Equal(5, item.Quantity);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.InventoryId == recipientId));
        Assert.Empty(db.PlayerTransferHistory);
    }

    [Fact]
    public async Task Transfer_from_multiplayer_restricted_sender_is_rejected_without_mutating_inventory()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var senderAccountId = db.Characters.Local
            .Single(x => x.Id == senderId)
            .UserId;
        var item = AddItem(db, senderId, "iron_ore", quantity: 5, stackable: true);
        db.AccountRestrictions.Add(new AccountRestriction
        {
            AccountId = senderAccountId,
            RestrictionType = AccountRestrictionType.MultiplayerRestriction,
            Reason = "Test restriction",
            CreatedBySubject = "staff|moderator",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await new InventoryRepository(db).TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            2,
            CancellationToken.None);

        Assert.Equal(InventoryTransferFailure.AccountRestricted, result.Failure);
        Assert.Equal(5, item.Quantity);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.InventoryId == recipientId));
        Assert.Empty(db.PlayerTransferHistory);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Transfer_rejects_a_guest_participant_without_mutating_inventory(bool senderIsGuest)
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var participants = db.Characters.Local
            .Where(x => x.Id == senderId || x.Id == recipientId)
            .ToDictionary(x => x.Id);
        var guestAccountId = participants[senderIsGuest ? senderId : recipientId].UserId;
        db.Users.Local.Single(x => x.Id == guestAccountId).IsGuest = true;
        var item = AddItem(db, senderId, "iron_ore", quantity: 5, stackable: true);
        await db.SaveChangesAsync();

        var result = await new InventoryRepository(db).TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            2,
            CancellationToken.None);

        Assert.Equal(InventoryTransferFailure.GuestAccount, result.Failure);
        Assert.Equal(5, item.Quantity);
        Assert.False(await db.InventoryItems.AnyAsync(x => x.InventoryId == recipientId));
        Assert.Empty(db.PlayerTransferHistory);
        Assert.Empty(db.EconomyLedger);
    }

    [Fact]
    public async Task Transfer_larger_than_owned_quantity_is_rejected()
    {
        await using var db = CreateDb();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        AddInventories(db, senderId, recipientId);
        var item = AddItem(db, senderId, "wood", quantity: 2, stackable: true);
        await db.SaveChangesAsync();

        var repository = new InventoryRepository(db);
        var result = await repository.TransferItemAsync(
            senderId,
            recipientId,
            item.ItemInstanceId,
            3,
            CancellationToken.None);

        Assert.Equal(InventoryTransferFailure.InsufficientQuantity, result.Failure);
        Assert.Equal(2, item.Quantity);
        Assert.Empty(db.PlayerTransferHistory);
    }

    private static void AddInventories(LLDbContext db, params Guid[] characterIds)
    {
        foreach (var (id, index) in characterIds.Select((id, index) => (id, index)))
        {
            var user = new AppUser
            {
                Username = $"TransferTester{index}-{id:N}"[..26],
                IsGuest = false
            };
            db.Users.Add(user);
            db.Characters.Add(new Character
            {
                Id = id,
                UserId = user.Id,
                Name = $"TransferTester{index}-{id:N}"[..26]
            });
        }
        db.Inventories.AddRange(characterIds.Select(id => new Inventory { CharacterId = id }));
    }

    private static InventoryItem AddItem(
        LLDbContext db,
        Guid ownerId,
        string itemBaseId,
        int quantity,
        bool stackable,
        bool isBound = false,
        ItemBase? reuseBase = null)
    {
        var itemBase = reuseBase ?? new ItemBase
        {
            Id = itemBaseId,
            Name = itemBaseId,
            Description = "Transfer test item.",
            ItemType = ItemType.Resource,
            Stackable = stackable,
            IsBound = isBound
        };
        var itemInstance = new ItemInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = itemBase.Id,
            ItemBase = itemBase
        };
        var inventoryItem = new InventoryItem
        {
            InventoryId = ownerId,
            ItemInstanceId = itemInstance.Id,
            ItemInstance = itemInstance,
            Quantity = quantity
        };
        db.InventoryItems.Add(inventoryItem);
        return inventoryItem;
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }
}
