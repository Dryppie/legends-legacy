using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.Tutorials;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Items.EssenceItems;
using Domain.Models.MarketPlaces;
using Domain.Models.Professions.Gathering.GatheringNodes;
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
    public async Task GetStateAsync_completes_new_tutorial_and_prepares_starter_setup_when_debug_tutorial_is_disabled()
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

        Assert.Null(state);
        Assert.True(progress.IsCompleted);
        Assert.True(progress.TrainingEssenceRewardGranted);
        Assert.False(progress.CompletionRewardGranted);
        Assert.Equal(0, character.Cinders);
        Assert.Equal(TutorialConstants.TutorialEssenceDefinitionId, playerEssence.EssenceDefinitionId);
        Assert.Contains(activeLoadout.Slots, slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Empty(await db.EquipmentSlots.Where(slot => slot.EntityId == characterId).ToListAsync());
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

        Assert.Null(state);
        Assert.Equal(TutorialConstants.TutorialEssenceDefinitionId, playerEssence.EssenceDefinitionId);
        Assert.Contains(activeLoadout.Slots, slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Empty(await db.EquipmentSlots.Where(slot => slot.EntityId == characterId).ToListAsync());
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

        var lootWriter = new RecordingLootRewardWriter(db);
        var equipmentSlots = new RecordingEquipmentSlotService(db);
        var service = CreateService(
            db,
            tutorialEnabled: true,
            lootWriter: lootWriter,
            equipmentSlots: equipmentSlots);

        var completion = await service.SkipAsync(characterId, CancellationToken.None);
        await service.SkipAsync(characterId, CancellationToken.None);

        var progress = await db.CharacterTutorialProgresses.SingleAsync();
        var character = await db.Characters.SingleAsync(x => x.Id == characterId);
        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);

        Assert.True(completion.WasSkipped);
        Assert.Equal(0, completion.RewardCinders);
        Assert.True(progress.IsCompleted);
        Assert.True(progress.TrainingEssenceRewardGranted);
        Assert.False(progress.CompletionRewardGranted);
        Assert.Equal(0, character.Cinders);
        Assert.Contains(
            activeLoadout.Slots,
            slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        var mace = Assert.IsType<EquipmentInstance>(
            lootWriter.Items.Single(item =>
                item.ItemInstance.ItemBaseId ==
                TutorialConstants.TutorialStarterWeaponItemBaseId).ItemInstance);
        Assert.Contains(
            equipmentSlots.EquipCalls,
            call =>
                call.EntityId == characterId &&
                call.EquipmentId == mace.Id &&
                call.SlotType == EquipmentSlotType.MainHand);
        var equippedMainHand = await db.EquipmentSlots
            .Include(slot => slot.EquipmentInstance)
            .SingleAsync(slot =>
                slot.EntityId == characterId &&
                slot.EquipmentSlotType == EquipmentSlotType.MainHand);
        Assert.Equal(mace.Id, equippedMainHand.EquipmentInstanceId);
        Assert.Equal(
            TutorialConstants.TutorialStarterWeaponItemBaseId,
            equippedMainHand.EquipmentInstance?.ItemBaseId);
        Assert.Equal(
            new[]
            {
                "basic_hatchet",
                "basic_pickaxe",
                "basic_skinning_knife",
                TutorialConstants.TutorialStarterWeaponItemBaseId
            },
            lootWriter.Items
                .Select(item => item.ItemInstance.ItemBaseId)
                .OrderBy(itemBaseId => itemBaseId));
    }

    [Fact]
    public async Task AcknowledgeWelcomeAsync_persists_the_first_login_decision()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "New Hero",
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

        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider());

        var initialState = await service.GetStateAsync(
            characterId,
            CancellationToken.None);
        var acknowledgedState = await service.AcknowledgeWelcomeAsync(
            characterId,
            CancellationToken.None);
        var persistedProgress = await db.CharacterTutorialProgresses.SingleAsync();

        Assert.NotNull(initialState);
        Assert.True(initialState.RequiresWelcome);
        Assert.NotNull(acknowledgedState);
        Assert.False(acknowledgedState.RequiresWelcome);
        Assert.NotNull(persistedProgress.WelcomeAcknowledgedAt);
    }

    [Fact]
    public async Task AttuneStarterEssenceAsync_starts_crafting_and_grants_weapon_materials()
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
        var lootWriter = new RecordingLootRewardWriter();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider(),
            lootWriter: lootWriter);

        var state = await service.AttuneStarterEssenceAsync(
            characterId,
            CancellationToken.None);

        var playerEssence = await db.PlayerEssences.SingleAsync();
        var activeLoadout = await db.EssenceLoadouts
            .Include(x => x.Slots)
            .SingleAsync(x => x.CharacterId == characterId && x.IsActive);

        Assert.NotNull(state);
        Assert.Equal(TutorialConstants.StepCraftEquipment, state.CurrentStep);
        Assert.Equal(2, state.CurrentStepIndex);
        Assert.Equal(5, state.TotalSteps);
        Assert.Contains(
            activeLoadout.Slots,
            slot => slot.SlotIndex == 0 && slot.PlayerEssenceId == playerEssence.Id);
        Assert.Equal(
            TutorialConstants.TutorialCraftingOreQuantity,
            lootWriter.Items
                .Where(item => item.ItemInstance.ItemBaseId == TutorialConstants.TutorialCraftingOreItemBaseId)
                .Sum(item => item.Quantity));
        Assert.Equal(
            TutorialConstants.TutorialCraftingWoodQuantity,
            lootWriter.Items
                .Where(item => item.ItemInstance.ItemBaseId == TutorialConstants.TutorialCraftingWoodItemBaseId)
                .Sum(item => item.Quantity));
    }

    [Fact]
    public async Task GetStateAsync_moves_legacy_chest_step_back_to_weapon_crafting()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Migrating Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepEquipEquipment,
            CraftedTierOneEquipmentCount =
                TutorialConstants.RequiredCraftedEquipmentCount
        });
        await db.SaveChangesAsync();
        var lootWriter = new RecordingLootRewardWriter();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider(),
            lootWriter: lootWriter);

        var state = await service.GetStateAsync(characterId, CancellationToken.None);

        Assert.Equal(TutorialConstants.StepCraftEquipment, state?.CurrentStep);
        Assert.Equal(0, state?.CurrentAmount);
        Assert.Equal(
            TutorialConstants.TutorialCraftingOreQuantity +
            TutorialConstants.TutorialCraftingWoodQuantity,
            lootWriter.Items.Sum(item => item.Quantity));
    }

    [Fact]
    public async Task CraftedEquipment_only_advances_for_an_allowed_tier_one_weapon()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        db.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Crafting Hero",
            UserId = Guid.NewGuid()
        });
        db.Inventories.Add(new Inventory { CharacterId = characterId });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepCraftEquipment
        });
        await db.SaveChangesAsync();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider());

        var armorResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.CraftedEquipment(["heavy_helm"], [1]),
            CancellationToken.None);
        var highTierResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.CraftedEquipment(["shortsword"], [2]),
            CancellationToken.None);
        var twoHandedResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.CraftedEquipment(["battle_axe"], [1]),
            CancellationToken.None);
        var weaponResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.CraftedEquipment(["shortsword"], [1]),
            CancellationToken.None);

        Assert.Null(armorResult);
        Assert.Null(highTierResult);
        Assert.Null(twoHandedResult);
        Assert.True(weaponResult?.Progressed);
        Assert.Equal(
            TutorialConstants.StepEquipEquipment,
            weaponResult?.State?.CurrentStep);
    }

    [Fact]
    public async Task Equipped_weapon_grants_tools_then_equipped_tool_unlocks_Lumo_combat()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var weaponBase = new EquipmentBase
        {
            Id = "shortsword",
            Name = "Shortsword",
            EquipmentType = EquipmentType.OneHanded
        };
        var weapon = new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = weaponBase.Id,
            ItemBase = weaponBase,
            Tier = 1,
            BaseRecipeId = "recipe.weapon.one_handed.shortsword"
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
            EquipmentSlotType = EquipmentSlotType.MainHand,
            EquipmentInstanceId = weapon.Id,
            EquipmentInstance = weapon
        });
        db.CharacterTutorialProgresses.Add(new CharacterTutorialProgress
        {
            CharacterId = characterId,
            TutorialId = TutorialConstants.FirstStepsTutorialId,
            CurrentStep = TutorialConstants.StepEquipEquipment
        });
        await db.SaveChangesAsync();
        var lootWriter = new RecordingLootRewardWriter();
        var service = CreateService(
            db,
            tutorialEnabled: true,
            definitionProvider: new ActiveTutorialDefinitionProvider(),
            lootWriter: lootWriter);

        var equipmentResult = await service.TryProgressAsync(
            characterId,
            TutorialTrigger.EquipmentChanged(),
            CancellationToken.None);
        var grantedTool = Assert.IsType<EquipmentInstance>(
            lootWriter.Items.Single(item =>
                item.ItemInstance.ItemBaseId == "basic_pickaxe").ItemInstance);
        db.EquipmentSlots.Add(new EquipmentSlot
        {
            EntityId = characterId,
            EquipmentSlotType = EquipmentSlotType.Tool,
            EquipmentInstanceId = grantedTool.Id,
            EquipmentInstance = grantedTool
        });
        await db.SaveChangesAsync();

        var toolResult = await service.TryProgressAsync(
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
            TutorialConstants.StepEquipGatheringTool,
            equipmentResult?.State?.CurrentStep);
        Assert.Equal(4, equipmentResult?.State?.CurrentStepIndex);
        Assert.Equal(5, equipmentResult?.State?.TotalSteps);
        Assert.Equal(
            TutorialConstants.TutorialGatheringToolItemBaseIds.OrderBy(itemId => itemId),
            lootWriter.Items
                .Select(item => item.ItemInstance.ItemBaseId)
                .Where(TutorialConstants.TutorialGatheringToolItemBaseIds.Contains)
                .OrderBy(itemId => itemId));
        Assert.True(toolResult?.Progressed);
        Assert.Equal(
            TutorialConstants.StepStartLumoRuins,
            toolResult?.State?.CurrentStep);
        Assert.Equal(5, toolResult?.State?.CurrentStepIndex);
        Assert.Equal(5, toolResult?.State?.TotalSteps);
        Assert.True(completionResult?.Progressed);
        Assert.Null(completionResult?.State);
        Assert.True(progress.IsCompleted);
        Assert.Equal(0, character.Cinders);
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
        RecordingInventoryRepository? inventory = null,
        RecordingLootRewardWriter? lootWriter = null,
        RecordingEquipmentSlotService? equipmentSlots = null)
    {
        return new TutorialService(
            db,
            new RecordingItemBaseRepository(),
            inventory ?? new RecordingInventoryRepository(),
            equipmentSlots ?? new RecordingEquipmentSlotService(),
            new InventoryItemFactory(),
            lootWriter ?? new RecordingLootRewardWriter(),
            definitionProvider ?? new EmptyTutorialDefinitionProvider(),
            new InMemoryTutorialProgressCache(),
            debugOptions: Options.Create(new TutorialDebugOptions
            {
                Enabled = tutorialEnabled,
                IsDevelopment = true
            }));
    }

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
            Version = 3,
            InitialStepKey = TutorialConstants.StepDefeatTrainingCreature,
            Steps =
            [
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepEquipEssence,
                    Objective = "Attune the Goblin Essence.",
                    ActionLabel = "Open Essences",
                    DestinationRoute = "/game/character/essences",
                    NextStepKey = TutorialConstants.StepCraftEquipment,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EssenceLoadoutChanged",
                        EssenceDefinitionId = TutorialConstants.TutorialEssenceDefinitionId
                    }
                },
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepCraftEquipment,
                    Objective = "Craft a Tier 1 weapon.",
                    ActionLabel = "Open Crafting",
                    DestinationRoute = "/game/professions/crafting",
                    NextStepKey = TutorialConstants.StepEquipEquipment,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "CraftedEquipment",
                        RequiredCount = 1,
                        ItemBaseIds = [.. TutorialConstants.TutorialOneHandedWeaponItemBaseIds]
                    }
                },
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepEquipEquipment,
                    Objective = "Equip the weapon you crafted.",
                    ActionLabel = "Open Inventory",
                    DestinationRoute = "/game/character/inventory",
                    NextStepKey = TutorialConstants.StepEquipGatheringTool,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EquipmentChanged",
                        RequiredCount = 1,
                        ItemBaseIds = [.. TutorialConstants.TutorialOneHandedWeaponItemBaseIds]
                    }
                },
                new TutorialStepDefinition
                {
                    Key = TutorialConstants.StepEquipGatheringTool,
                    Objective = "Equip a gathering tool.",
                    ActionLabel = "Open Inventory",
                    DestinationRoute = "/game/character/inventory",
                    NextStepKey = TutorialConstants.StepStartLumoRuins,
                    Trigger = new TutorialStepTriggerDefinition
                    {
                        Type = "EquipmentChanged",
                        RequiredCount = 1,
                        ItemBaseIds = [.. TutorialConstants.TutorialGatheringToolItemBaseIds]
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
            },
            [TutorialConstants.TutorialCraftingOreItemBaseId] = new ItemBase
            {
                Id = TutorialConstants.TutorialCraftingOreItemBaseId,
                Name = "Ore",
                ItemType = ItemType.Resource,
                Stackable = true
            },
            [TutorialConstants.TutorialCraftingWoodItemBaseId] = new ItemBase
            {
                Id = TutorialConstants.TutorialCraftingWoodItemBaseId,
                Name = "Wood",
                ItemType = ItemType.Resource,
                Stackable = true
            },
            [TutorialConstants.TutorialStarterWeaponItemBaseId] = new EquipmentBase
            {
                Id = TutorialConstants.TutorialStarterWeaponItemBaseId,
                Name = "Mace",
                ItemType = ItemType.Equipment,
                EquipmentType = EquipmentType.OneHanded
            },
            ["basic_pickaxe"] = new EquipmentBase
            {
                Id = "basic_pickaxe",
                Name = "Pickaxe",
                ItemType = ItemType.Equipment,
                EquipmentType = EquipmentType.Tool,
                GatheringType = GatheringType.Mining
            },
            ["basic_hatchet"] = new EquipmentBase
            {
                Id = "basic_hatchet",
                Name = "Hatchet",
                ItemType = ItemType.Equipment,
                EquipmentType = EquipmentType.Tool,
                GatheringType = GatheringType.Woodcutting
            },
            ["basic_skinning_knife"] = new EquipmentBase
            {
                Id = "basic_skinning_knife",
                Name = "Skinning Knife",
                ItemType = ItemType.Equipment,
                EquipmentType = EquipmentType.Tool,
                GatheringType = GatheringType.Skinning
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
        private readonly LLDbContext? _db;

        public RecordingLootRewardWriter(LLDbContext? db = null)
        {
            _db = db;
        }

        public List<InventoryItem> Items { get; } = [];

        public Task AddLootAsync(
            Guid characterId,
            IReadOnlyCollection<InventoryItem> items,
            CancellationToken cancellationToken)
        {
            Items.AddRange(items);
            if (_db is not null)
            {
                _db.InventoryItems.AddRange(items);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEquipmentSlotService : IEquipmentSlotService
    {
        private readonly LLDbContext? _db;

        public RecordingEquipmentSlotService(LLDbContext? db = null)
        {
            _db = db;
        }

        public List<(Guid EntityId, Guid EquipmentId, EquipmentSlotType? SlotType)> EquipCalls { get; } = [];

        public Task<List<EquipmentSlot>> GetEquipmentSlotsByEntityIdAsync(
            Guid entityId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new List<EquipmentSlot>());

        public async Task<bool> EquipEquipmentAsync(
            Guid entityId,
            Guid equipmentId,
            EquipmentSlotType? slotType,
            CancellationToken cancellationToken)
        {
            EquipCalls.Add((entityId, equipmentId, slotType));
            if (_db is null)
            {
                return true;
            }

            var inventoryItem = await _db.InventoryItems
                .Include(item => item.ItemInstance)
                .FirstOrDefaultAsync(
                    item =>
                        item.InventoryId == entityId &&
                        item.ItemInstanceId == equipmentId,
                    cancellationToken);
            if (inventoryItem?.ItemInstance is not EquipmentInstance equipment)
            {
                return false;
            }

            var targetSlotType = slotType ?? EquipmentSlotType.MainHand;
            var targetSlot = await _db.EquipmentSlots.FirstOrDefaultAsync(
                slot =>
                    slot.EntityId == entityId &&
                    slot.EquipmentSlotType == targetSlotType,
                cancellationToken);
            if (targetSlot is null)
            {
                targetSlot = new EquipmentSlot
                {
                    EntityId = entityId,
                    EquipmentSlotType = targetSlotType
                };
                _db.EquipmentSlots.Add(targetSlot);
            }

            targetSlot.EquipmentInstanceId = equipment.Id;
            targetSlot.EquipmentInstance = equipment;
            _db.InventoryItems.Remove(inventoryItem);
            return true;
        }

        public Task<bool> UnequipEquipmentAsync(
            Guid entityId,
            EquipmentSlotType slotType,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
