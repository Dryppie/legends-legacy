using System.Text.Json;
using Application.UseCases.Outbox;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Microsoft.EntityFrameworkCore;
using Persistence.LL.Repositories.Inventories;
using Services.LL.Administration;
using Services.LL.Inventories;
using Services.LL.Outbox;

namespace EssenceSystem.Tests;

public sealed partial class EventQuestSystemTests
{
    [Theory]
    [InlineData(ItemAcquisitionSources.CombatReward, false, true)]
    [InlineData(ItemAcquisitionSources.DungeonReward, false, true)]
    [InlineData(ItemAcquisitionSources.RaidReward, true, true)]
    [InlineData(ItemAcquisitionSources.SelectionContainer, false, false)]
    [InlineData(ItemAcquisitionSources.EventQuestReward, false, false)]
    [InlineData(ItemAcquisitionSources.QuestReward, false, false)]
    [InlineData(ItemAcquisitionSources.Marketplace, false, false)]
    [InlineData(ItemAcquisitionSources.AdminCompensation, false, false)]
    public async Task Only_discovered_equipment_produces_durable_event_contribution(
        string source, bool correlated, bool contributes)
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        db.Characters.Add(new Character { Id = id, UserId = accountId, Name = "Treasure hunter" });
        db.Inventories.Add(new Inventory { CharacterId = id });
        CompleteTutorial(db, id);
        var equipmentBase = new EquipmentBase { Id = "test_sword", Name = "Sword", EquipmentType = EquipmentType.OneHanded };
        var resourceBase = new ItemBase { Id = "test_resource", Name = "Resource", ItemType = ItemType.Resource, Stackable = true };
        db.ItemBases.AddRange(equipmentBase, resourceBase);
        await db.SaveChangesAsync();
        var factory = new InventoryItemFactory();
        var loot = factory.CreateForQuantity(equipmentBase, 2, id)
            .Concat(factory.CreateForQuantity(resourceBase, 50, id)).ToList();
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var registry = new GameEventOutboxConsumerRegistry();
        var outbox = new GameEventOutbox(db, registry, json, new FixedTimeProvider(Now));
        var inventory = new InventoryService(new InventoryRepository(db), outbox);
        if (correlated)
            await inventory.AddItemsToInventory(id, loot, source, Guid.NewGuid(), default);
        else
            await inventory.AddItemsToInventory(id, loot, source, default);
        await db.SaveChangesAsync();

        var messages = await db.GameEventOutboxMessages.ToListAsync();
        if (!contributes)
        {
            Assert.Empty(messages);
            return;
        }
        var message = Assert.Single(messages);
        Assert.Equal(GameEventTypes.EquipmentFound, message.EventType);
        Assert.Equal(2, JsonSerializer.Deserialize<EquipmentFoundPayload>(message.PayloadJson, json)!.Quantity);
        Assert.Equal(GameEventOutboxConsumerNames.EventQuests, Assert.Single(await db.GameEventOutboxDeliveries.ToListAsync()).Consumer);
        var definition = CreateActiveDefinition(1000);
        definition.Objectives[0].Type = "EquipmentFound";
        var service = CreateService(db, definition, new RecordingPublisher());
        var consumer = new EventQuestGameEventOutboxConsumer(service, new AccountRestrictionIndex(TimeProvider.System), json);
        Assert.True(consumer.CanHandle(message.EventType));
        message.AccountId = accountId;
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        var state = Assert.Single((await service.GetJournalAsync(id, default)).Events);
        Assert.Equal(2, state.MyContribution);
        Assert.Equal(2, Assert.Single(state.Objectives).CurrentAmount);
    }
}
