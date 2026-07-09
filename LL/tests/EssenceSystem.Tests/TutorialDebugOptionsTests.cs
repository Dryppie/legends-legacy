using Application.Interfaces.Services.LL.Tutorials;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.MarketPlaces;
using Domain.Models.Tutorials;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Inventories;
using Services.LL.Tutorials;

namespace EssenceSystem.Tests;

public sealed class TutorialDebugOptionsTests
{
    [Fact]
    public async Task GetStateAsync_completes_new_tutorial_and_grants_rewards_when_debug_tutorial_is_disabled()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Debug Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var state = await service.GetStateAsync(characterId, CancellationToken.None);

        var progress = await db.CharacterTutorialProgresses.SingleAsync();
        var character = await db.Characters.SingleAsync(x => x.Id == characterId);
        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);
        var chestSlot = await db.EquipmentSlots
            .Include(x => x.EquipmentInstance)
            .SingleAsync(x => x.EntityId == characterId && x.EquipmentSlotType == EquipmentSlotType.Chest);

        Assert.Null(state);
        Assert.True(progress.IsCompleted);
        Assert.True(progress.TrainingEssenceRewardGranted);
        Assert.True(progress.CompletionRewardGranted);
        Assert.Equal(150, character.Cinders);
        Assert.Equal(TutorialConstants.TutorialEssenceDefinitionId, playerEssence.EssenceDefinitionId);
        Assert.Contains(activeLoadout.Slots, slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Equal(TutorialConstants.TutorialChestItemBaseId, chestSlot.EquipmentInstance?.ItemBaseId);
        Assert.DoesNotContain(db.InventoryItems, item => item.InventoryId == characterId);
    }

    [Fact]
    public async Task GetStateAsync_repairs_completed_debug_tutorial_accounts_that_still_have_unbound_rewards()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var essenceInstanceId = Guid.NewGuid();

        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Existing Debug Hero",
            UserId = userId,
            Cinders = 150
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.ItemInstances.Add(new EssenceItemInstance
        {
            Id = essenceInstanceId,
            ItemBaseId = TutorialConstants.TutorialEssenceItemBaseId
        });
        db.InventoryItems.Add(new InventoryItem
        {
            InventoryId = characterId,
            ItemInstanceId = essenceInstanceId,
            Quantity = 1
        });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepComplete,
            CompletedAt = DateTimeOffset.UtcNow,
            TrainingEssenceRewardGranted = true,
            CompletionRewardGranted = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var state = await service.GetStateAsync(characterId, CancellationToken.None);

        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);
        var chestSlot = await db.EquipmentSlots
            .Include(x => x.EquipmentInstance)
            .SingleAsync(x => x.EntityId == characterId && x.EquipmentSlotType == EquipmentSlotType.Chest);

        Assert.Null(state);
        Assert.Equal(TutorialConstants.TutorialEssenceDefinitionId, playerEssence.EssenceDefinitionId);
        Assert.Contains(activeLoadout.Slots, slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Equal(TutorialConstants.TutorialChestItemBaseId, chestSlot.EquipmentInstance?.ItemBaseId);
        Assert.Empty(await db.InventoryItems.Where(item => item.InventoryId == characterId).ToListAsync());
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static TutorialService CreateService(LLDbContext db) =>
        new(
            db,
            new RecordingItemBaseRepository(),
            new RecordingInventoryRepository(),
            new InventoryItemFactory(),
            new RecordingLootRewardWriter(),
            new EmptyTutorialDefinitionProvider(),
            new InMemoryTutorialProgressCache(),
            debugOptions: Options.Create(new TutorialDebugOptions
            {
                Enabled = false,
                IsDevelopment = true
            }));

    private sealed class EmptyTutorialDefinitionProvider : ITutorialDefinitionProvider
    {
        public TutorialDefinition Get(string tutorialId) => new()
        {
            TutorialId = tutorialId,
            Title = "First Steps",
            Version = 1
        };

        public TutorialStepDefinition? GetStep(string tutorialId, string stepKey) => null;
    }

    private sealed class RecordingItemBaseRepository : IItemBaseRepository
    {
        private readonly Dictionary<string, ItemBase> _itemBases = new(StringComparer.OrdinalIgnoreCase)
        {
            [TutorialConstants.TutorialEssenceItemBaseId] = new EssenceItemBase
            {
                Id = TutorialConstants.TutorialEssenceItemBaseId,
                Name = "Unbound Goblin's Essence",
                ItemType = ItemType.Essence,
                EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId
            }
        };

        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(
            IReadOnlyCollection<string> itemIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<string, ItemBase> result = _itemBases
                .Where(x => itemIds.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

            return Task.FromResult(result);
        }

        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>
                {
                    [TutorialConstants.TutorialEssenceDefinitionId] = TutorialConstants.TutorialEssenceItemBaseId
                });

        public Task<EquipmentBase?> GetCraftableEquipmentBaseAsync(
            string itemBaseId,
            CancellationToken cancellationToken) =>
            Task.FromResult<EquipmentBase?>(null);

        public Task AddMissingItemBasesAsync(
            IReadOnlyCollection<ItemBase> itemBases,
            CancellationToken cancellationToken)
        {
            foreach (var itemBase in itemBases)
            {
                _itemBases.TryAdd(itemBase.Id, itemBase);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingInventoryRepository : IInventoryRepository
    {
        public List<InventoryItem> Items { get; } = [];

        public Task<Inventory> GetInventoryByIdAsync(Guid characterId, CancellationToken cancellationToken) =>
            Task.FromResult(new Inventory { CharacterId = characterId, InventoryItems = Items });

        public Task AddItemsToInventory(Guid characterId, List<InventoryItem> loot, CancellationToken cancellationToken)
        {
            Items.AddRange(loot);
            return Task.CompletedTask;
        }

        public Task CreateInventoryAsync(Guid characterId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> TryRemoveCraftingMaterialsAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> TryRemoveItemsByBaseIdAsync(Guid characterId, Dictionary<string, int> requiredByItemId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<InventoryItem?> GetInventoryItemAsync(Guid characterId, Guid inventoryItemId, CancellationToken cancellationToken) => Task.FromResult<InventoryItem?>(null);
        public Task<int> GetInventoryQuantityAsync(Guid characterId, string itemBaseId, CancellationToken cancellationToken) => Task.FromResult(0);
        public void RemoveInventoryItem(InventoryItem inventoryItem) { }
        public Task<bool> TryRemoveItemsForMarketPlaceListingAsync(Guid characterId, MarketPlaceListing listing, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> AddItemInstanceBackToInventory(Guid characterId, ItemInstance itemInstance, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task AddItemToInventoryFromMarketPlace(Guid characterId, InventoryItem item, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<InventoryItem?> ScrapEquipments(Guid characterId, List<Guid> parsedGuids, CancellationToken cancellationToken) => Task.FromResult<InventoryItem?>(null);
    }

    private sealed class RecordingLootRewardWriter : ILootRewardWriter
    {
        public List<InventoryItem> Items { get; } = [];

        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            CancellationToken cancellationToken)
        {
            Items.AddRange(items);
            return Task.CompletedTask;
        }
    }
}
