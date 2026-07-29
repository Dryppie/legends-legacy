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

    [Fact]
    public async Task SkipAsync_completes_once_and_prepares_a_playable_starter_setup()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Skipping Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        await db.SaveChangesAsync();

        var service = CreateService(db, tutorialEnabled: true);

        var completion = await service.SkipAsync(characterId, CancellationToken.None);
        await service.SkipAsync(characterId, CancellationToken.None);

        var progress = await db.CharacterTutorialProgresses.SingleAsync();
        var character = await db.Characters.SingleAsync(x => x.Id == characterId);
        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);
        var chestSlot = await db.EquipmentSlots
            .Include(x => x.EquipmentInstance)
            .SingleAsync(x =>
                x.EntityId == characterId &&
                x.EquipmentSlotType == EquipmentSlotType.Chest);

        Assert.True(completion.WasSkipped);
        Assert.Equal(TutorialConstants.CompletionCinders, completion.RewardCinders);
        Assert.True(progress.IsCompleted);
        Assert.True(progress.TrainingEssenceRewardGranted);
        Assert.True(progress.CompletionRewardGranted);
        Assert.Equal(TutorialConstants.CompletionCinders, character.Cinders);
        Assert.Contains(
            activeLoadout.Slots,
            slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Equal(
            TutorialConstants.TutorialChestItemBaseId,
            chestSlot.EquipmentInstance?.ItemBaseId);
    }

    [Fact]
    public async Task AttuneStarterEssenceAsync_skips_loadout_setup_and_grants_the_chest()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Attuning Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepEquipEssence
        });
        await db.SaveChangesAsync();
        var inventory = new RecordingInventoryRepository();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider(),
            inventory: inventory);

        var state = await service.AttuneStarterEssenceAsync(
            characterId,
            CancellationToken.None);

        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);

        Assert.NotNull(state);
        Assert.Equal(TutorialConstants.StepEquipEquipment, state.CurrentStep);
        Assert.Equal(2, state.CurrentStepIndex);
        Assert.Equal(3, state.TotalSteps);
        Assert.Contains(
            activeLoadout.Slots,
            slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Contains(
            inventory.Items,
            item => item.ItemInstance.ItemBaseId == TutorialConstants.TutorialChestItemBaseId);
    }

    [Fact]
    public async Task Equipment_then_Lumo_combat_start_completes_the_tutorial()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var chestBase = new EquipmentBase
        {
            Id = TutorialConstants.TutorialChestItemBaseId,
            Name = "Tutorial Chest",
            EquipmentType = EquipmentType.Chest
        };
        var chest = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = chestBase.Id,
            ItemBase = chestBase,
            Tier = 1
        };
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Lumo Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.EquipmentSlots.Add(new EquipmentSlot
        {
            EntityId = characterId,
            EquipmentSlotType = EquipmentSlotType.Chest,
            EquipmentInstanceId = chest.Id,
            EquipmentInstance = chest
        });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepEquipEquipment
        });
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider());

        var equipmentResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.EquipmentChanged(),
            CancellationToken.None);
        var completionResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.CombatActionStarted(TutorialConstants.LumoRuinsAreaId),
            CancellationToken.None);

        var progress = await db.CharacterTutorialProgresses.SingleAsync();
        var character = await db.Characters.SingleAsync(x => x.Id == characterId);

        Assert.True(equipmentResult?.Progressed);
        Assert.Equal(
            TutorialConstants.StepStartLumoRuins,
            equipmentResult?.State?.CurrentStep);
        Assert.Equal(3, equipmentResult?.State?.CurrentStepIndex);
        Assert.Equal(3, equipmentResult?.State?.TotalSteps);
        Assert.True(completionResult?.Progressed);
        Assert.Null(completionResult?.State);
        Assert.True(progress.IsCompleted);
        Assert.Equal(TutorialConstants.CompletionCinders, character.Cinders);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private static TutorialService CreateService(
        LLDbContext db,
        bool tutorialEnabled = false,
        ITutorialDefinitionProvider? definitionProvider = null,
        RecordingInventoryRepository? inventory = null) =>
        new(
            db,
            new RecordingItemBaseRepository(),
            inventory ?? new RecordingInventoryRepository(),
            new InventoryItemFactory(),
            new RecordingLootRewardWriter(),
            definitionProvider ?? new EmptyTutorialDefinitionProvider(),
            new InMemoryTutorialProgressCache(),
            debugOptions: Options.Create(new TutorialDebugOptions
            {
                Enabled = tutorialEnabled,
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

    private sealed class ActiveTutorialDefinitionProvider : ITutorialDefinitionProvider
    {
        private readonly TutorialDefinition _definition = new()
        {
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            Title = "First Steps",
            Version = 1,
            InitialStepKey = TutorialConstants.StepDefeatTrainingCreature,
            Steps =
            [
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepEquipEssence,
                    Objective = "Attune the Goblin Essence.",
                    ActionLabel = "Open Essences",
                    DestinationRoute = "/game/character/essences",
                    NextStepKey = TutorialConstants.StepEquipEquipment,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EssenceLoadoutChanged",
                        EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId
                    }
                },
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepEquipEquipment,
                    Objective = "Equip the starter chest.",
                    ActionLabel = "Open Inventory",
                    DestinationRoute = "/game/character/inventory",
                    NextStepKey = TutorialConstants.StepStartLumoRuins,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EquipmentChanged",
                        RequiredCount = 1,
                        ItemBaseIds = [TutorialConstants.TutorialChestItemBaseId]
                    }
                },
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepStartLumoRuins,
                    Objective = "Start fighting in Lumo Ruins.",
                    ActionLabel = "Open Lumo Ruins",
                    DestinationRoute = "/game/world/shenic?area=region_01_area_01",
                    NextStepKey = TutorialConstants.StepComplete,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "CombatActionStarted",
                        AreaId = TutorialConstants.LumoRuinsAreaId
                    }
                }
            ]
        };

        public TutorialDefinition Get(string tutorialId) => _definition;

        public TutorialStepDefinition? GetStep(string tutorialId, string stepKey) =>
            _definition.Steps.FirstOrDefault(
                step => step.Key.Equals(stepKey, StringComparison.OrdinalIgnoreCase));
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
