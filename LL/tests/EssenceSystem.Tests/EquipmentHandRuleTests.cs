using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Slots;
using Domain.Models.Professions;
using Domain.Models.Professions.Crafting.V2;
using Domain.Models.Professions.Gathering.GatheringNodes;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Inventories;

namespace EssenceSystem.Tests;

public sealed class EquipmentHandRuleTests
{
    [Fact]
    public async Task EquippingOffhandReturnsSharedTwoHandedItemExactlyOnce()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var twoHanded = Equipment("greatsword", EquipmentType.TwoHanded);
        var shield = Equipment("towershield", EquipmentType.OffHand);
        var inventory = new Inventory
        {
            CharacterId = characterId,
            InventoryItems =
            [
                new InventoryItem
                {
                    InventoryId = characterId,
                    ItemInstanceId = shield.Id,
                    ItemInstance = shield,
                    Quantity = 1
                }
            ]
        };
        var character = new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "Hand Rules",
            NormalizedName = "HAND RULES",
            Inventory = inventory,
            EquipmentSlots =
            [
                Slot(characterId, EquipmentSlotType.MainHand, twoHanded),
                Slot(characterId, EquipmentSlotType.OffHand, twoHanded),
                .. Enum.GetValues<EquipmentSlotType>()
                    .Where(type => type is not EquipmentSlotType.MainHand and not EquipmentSlotType.OffHand)
                    .Select(type => Slot(characterId, type, null))
            ]
        };
        inventory.Character = character;
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        var equipped = await repository.EquipEquipmentAsync(
            characterId,
            shield.Id,
            EquipmentSlotType.OffHand,
            CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.True(equipped);
        var slots = await repository.GetEquipmentSlotsByEntityIdAsync(characterId, CancellationToken.None);
        Assert.Null(slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
        Assert.Equal(
            shield.Id,
            slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.OffHand).EquipmentInstanceId);
        var returned = await db.InventoryItems
            .Where(item => item.InventoryId == characterId && item.ItemInstanceId == twoHanded.Id)
            .ToListAsync();
        Assert.Single(returned);
        Assert.DoesNotContain(
            await db.InventoryItems.Where(item => item.InventoryId == characterId).ToListAsync(),
            item => item.ItemInstanceId == shield.Id);
    }

    [Fact]
    public async Task EquippingTwoHandedReturnsBothPreviouslyEquippedItems()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var sword = Equipment("shortsword", EquipmentType.OneHanded);
        var shield = Equipment("towershield", EquipmentType.OffHand);
        var greatsword = Equipment("greatsword", EquipmentType.TwoHanded);
        var character = CharacterWithHands(
            characterId,
            sword,
            shield,
            [greatsword]);
        sword.IsFavorite = true;
        character.Inventory!.InventoryItems.Single().IsFavorite = true;
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        Assert.True(await repository.EquipEquipmentAsync(
            characterId,
            greatsword.Id,
            null,
            CancellationToken.None));
        await db.SaveChangesAsync();

        var slots = await repository.GetEquipmentSlotsByEntityIdAsync(characterId, CancellationToken.None);
        Assert.Equal(greatsword.Id, slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
        Assert.Equal(greatsword.Id, slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.OffHand).EquipmentInstanceId);
        Assert.True(slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstance!.IsFavorite);
        var inventoryItems = await db.InventoryItems
            .Where(item => item.InventoryId == characterId)
            .ToListAsync();
        Assert.Contains(inventoryItems, item => item.ItemInstanceId == sword.Id && item.IsFavorite);
        Assert.Contains(inventoryItems, item => item.ItemInstanceId == shield.Id);
        Assert.DoesNotContain(inventoryItems, item => item.ItemInstanceId == greatsword.Id);
    }

    [Fact]
    public async Task AutomaticDualWieldFillsBothHandsThenReplacesMainHand()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var first = Equipment("shortsword", EquipmentType.OneHanded);
        var second = Equipment("dagger", EquipmentType.OneHanded);
        var third = Equipment("mace", EquipmentType.OneHanded);
        var character = CharacterWithHands(characterId, null, null, [first, second, third]);
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        Assert.True(await repository.EquipEquipmentAsync(characterId, first.Id, null, CancellationToken.None));
        Assert.True(await repository.EquipEquipmentAsync(characterId, second.Id, null, CancellationToken.None));
        Assert.True(await repository.EquipEquipmentAsync(characterId, third.Id, null, CancellationToken.None));
        await db.SaveChangesAsync();

        var slots = await repository.GetEquipmentSlotsByEntityIdAsync(characterId, CancellationToken.None);
        Assert.Equal(third.Id, slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand).EquipmentInstanceId);
        Assert.Equal(second.Id, slots.Single(slot => slot.EquipmentSlotType == EquipmentSlotType.OffHand).EquipmentInstanceId);
        Assert.Contains(
            await db.InventoryItems.Where(item => item.InventoryId == characterId).ToListAsync(),
            item => item.ItemInstanceId == first.Id);
    }

    [Fact]
    public async Task FavoritePreferenceSurvivesEquipAndUnequip()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var sword = Equipment("favorite-sword", EquipmentType.OneHanded);
        var character = CharacterWithHands(characterId, null, null, [sword]);
        character.Inventory!.InventoryItems.Single().IsFavorite = true;
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        Assert.True(await repository.EquipEquipmentAsync(
            characterId,
            sword.Id,
            EquipmentSlotType.MainHand,
            CancellationToken.None));
        await db.SaveChangesAsync();

        var equipped = (await repository.GetEquipmentSlotsByEntityIdAsync(characterId, CancellationToken.None))
            .Single(slot => slot.EquipmentSlotType == EquipmentSlotType.MainHand)
            .EquipmentInstance;
        Assert.NotNull(equipped);
        Assert.True(equipped.IsFavorite);

        Assert.True(await repository.UnequipEquipmentAsync(
            characterId,
            EquipmentSlotType.MainHand,
            CancellationToken.None));
        await db.SaveChangesAsync();

        var returned = await db.InventoryItems.SingleAsync(item =>
            item.InventoryId == characterId && item.ItemInstanceId == sword.Id);
        Assert.True(returned.IsFavorite);
    }

    [Fact]
    public async Task EquippedFavoriteCanOnlyBeChangedByItsOwner()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var sword = Equipment("equipped-favorite-sword", EquipmentType.OneHanded);
        var character = CharacterWithHands(characterId, sword, null, []);
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var inventoryRepository = new InventoryRepository(db);

        Assert.True(await inventoryRepository.SetItemFavoriteAsync(
            characterId,
            sword.Id,
            true,
            CancellationToken.None));
        Assert.False(await inventoryRepository.SetItemFavoriteAsync(
            strangerId,
            sword.Id,
            false,
            CancellationToken.None));
        await db.SaveChangesAsync();

        Assert.True(sword.IsFavorite);
    }

    [Fact]
    public async Task TierTwoEquipmentRequiresCharacterLevelFifty()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var tierTwoSword = Equipment("tier-two-sword", EquipmentType.OneHanded);
        tierTwoSword.Tier = 2;
        var character = CharacterWithHands(characterId, null, null, [tierTwoSword]);
        character.Level = 49;
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        Assert.Equal(50, EquipmentTierBudgetCurve.GetRequiredCharacterLevelForTier(2));
        Assert.False(await repository.EquipEquipmentAsync(
            characterId,
            tierTwoSword.Id,
            EquipmentSlotType.MainHand,
            CancellationToken.None));

        character.Level = 50;
        await db.SaveChangesAsync();

        Assert.True(await repository.EquipEquipmentAsync(
            characterId,
            tierTwoSword.Id,
            EquipmentSlotType.MainHand,
            CancellationToken.None));
    }

    [Fact]
    public async Task Rare_gathering_tools_require_profession_level_twenty()
    {
        await using var db = CreateDb();
        var characterId = Guid.NewGuid();
        var pickaxe = Equipment("rare-pickaxe", EquipmentType.Tool);
        pickaxe.Rarity = Rarity.Rare;
        ((EquipmentBase)pickaxe.ItemBase).GatheringType = GatheringType.Mining;
        var character = CharacterWithHands(characterId, null, null, [pickaxe]);
        var mining = new Profession
        {
            CharacterId = characterId,
            ProfessionType = ProfessionType.Mining,
            Level = 19
        };
        character.Professions.Add(mining);
        db.Characters.Add(character);
        await db.SaveChangesAsync();
        var repository = new EquipmentSlotRepository(db);

        Assert.False(await repository.EquipEquipmentAsync(
            characterId,
            pickaxe.Id,
            EquipmentSlotType.Tool,
            CancellationToken.None));

        mining.Level = 20;
        await db.SaveChangesAsync();

        Assert.True(await repository.EquipEquipmentAsync(
            characterId,
            pickaxe.Id,
            EquipmentSlotType.Tool,
            CancellationToken.None));
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static EquipmentInstance Equipment(string id, EquipmentType type)
    {
        var itemBase = new EquipmentBase
        {
            Id = id,
            Name = id,
            EquipmentType = type
        };
        return new EquipmentInstance
        {
            Id = Guid.NewGuid(),
            ItemBaseId = id,
            ItemBase = itemBase
        };
    }

    private static Character CharacterWithHands(
        Guid characterId,
        EquipmentInstance? mainHand,
        EquipmentInstance? offHand,
        IReadOnlyList<EquipmentInstance> inventoryEquipment)
    {
        var inventory = new Inventory
        {
            CharacterId = characterId,
            InventoryItems = inventoryEquipment.Select(equipment => new InventoryItem
            {
                InventoryId = characterId,
                ItemInstanceId = equipment.Id,
                ItemInstance = equipment,
                Quantity = 1
            }).ToList()
        };
        var character = new Character
        {
            Id = characterId,
            UserId = Guid.NewGuid(),
            Name = "Hand Rules",
            NormalizedName = "HAND RULES",
            Inventory = inventory,
            EquipmentSlots =
            [
                Slot(characterId, EquipmentSlotType.MainHand, mainHand),
                Slot(characterId, EquipmentSlotType.OffHand, offHand),
                .. Enum.GetValues<EquipmentSlotType>()
                    .Where(type => type is not EquipmentSlotType.MainHand and not EquipmentSlotType.OffHand)
                    .Select(type => Slot(characterId, type, null))
            ]
        };
        inventory.Character = character;
        return character;
    }

    private static EquipmentSlot Slot(
        Guid characterId,
        EquipmentSlotType type,
        EquipmentInstance? equipment) =>
        new()
        {
            EntityId = characterId,
            EquipmentSlotType = type,
            EquipmentInstanceId = equipment?.Id,
            EquipmentInstance = equipment
        };
}
