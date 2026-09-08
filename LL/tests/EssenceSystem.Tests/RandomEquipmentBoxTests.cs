using Application.UseCases.Inventories.SelectionCrates;
using Application.UseCases.Items.Dtos;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Services.LL.Inventories;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed partial class SelectionCrateServiceTests
{
    private static CombatAcquisitionCatalog EquipmentBoxCatalog()
    {
        var root = Path.Combine(TestContentPaths.FindApiRoot(), "Data/equipment");
        return JsonStarterEquipmentCatalog.LoadOrdinary(
            JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json")),
            Path.Combine(root, "equipment-ordinary.v1.json"));
    }

    [Fact]
    public async Task Random_equipment_boxes_consume_one_and_award_two_uncommon_pieces_even_after_restacking()
    {
        var owner = Guid.NewGuid();
        var box = CreateInventoryItem(owner, RandomEquipmentBoxCatalog.UncommonItemBaseId, ItemType.Resource, 2);
        var inventory = new FakeInventoryService(box);
        var catalog = EquipmentBoxCatalog();
        var itemBases = catalog.Equipment.Options.Select(option =>
        {
            var archetype = catalog.Equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null).Archetype;
            return new EquipmentBase { Id = archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType };
        }).DistinctBy(item => item.Id);
        var service = new SelectionCrateService(inventory, new FakeItemBaseRepository(itemBases),
            new InventoryItemFactory(), equipmentCatalog: catalog);

        Assert.False((await service.OpenSelectionContainerAsync(Guid.NewGuid(), box.ItemInstanceId, "random", default)).IsSuccess);
        Assert.False((await service.OpenSelectionContainerAsync(owner, box.ItemInstanceId, "shortsword", default)).IsSuccess);
        Assert.Equal(2, box.Quantity);
        for (var opening = 0; opening < 3; opening++)
        {
            if (opening == 1) box.Quantity++; // Add a newly earned box back to the same stack.
            var before = box.Quantity;
            var result = await service.OpenSelectionContainerAsync(owner, box.ItemInstanceId, "random", default);
            Assert.True(result.IsSuccess);
            Assert.Equal(before - 1, box.Quantity);
            Assert.Equal(2, result.Rewards.Count);
            Assert.All(result.Rewards, item =>
            {
                Assert.Equal(1, item.Quantity);
                Assert.Equal(owner, item.InventoryId);
                var equipment = Assert.IsType<EquipmentInstance>(item.ItemInstance).ProgressionData!;
                Assert.Equal(EquipmentRarity.Uncommon, equipment.Rarity);
                Assert.Equal(1, equipment.State.Tier);
                Assert.Equal(0, equipment.State.Rank);
                Assert.Equal(ItemQuality.Standard, equipment.Quality);
                Assert.Equal(EquipmentAwardKind.ProtectedReward, equipment.State.Provenance.Kind);
            });
        }
        Assert.Equal(6, inventory.AddedRewards.Select(x => x.ItemInstanceId).Distinct().Count());
        Assert.False((await service.OpenSelectionContainerAsync(owner, box.ItemInstanceId, "random", default)).IsSuccess);
        Assert.Equal(6, inventory.AddedRewards.Count);
    }

    [Theory]
    [InlineData(RandomEquipmentBoxCatalog.ArmorChestItemBaseId, EquipmentType.Head, EquipmentType.Chest, EquipmentType.Legs)]
    [InlineData(RandomEquipmentBoxCatalog.JewelryChestItemBaseId, EquipmentType.Ring, EquipmentType.Necklace, EquipmentType.Relic)]
    public async Task Quest_chests_award_one_piece_from_only_their_equipment_category(
        string chestId, params EquipmentType[] allowedTypes)
    {
        var owner = Guid.NewGuid();
        var chest = CreateInventoryItem(owner, chestId, ItemType.Resource, 20);
        var inventory = new FakeInventoryService(chest);
        var catalog = EquipmentBoxCatalog();
        var itemBases = catalog.Equipment.Options.Select(option =>
        {
            var archetype = catalog.Equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null).Archetype;
            return new EquipmentBase { Id = archetype.ItemBaseId, Name = option.Name, EquipmentType = option.EquipmentType };
        }).DistinctBy(item => item.Id);
        var service = new SelectionCrateService(inventory, new FakeItemBaseRepository(itemBases),
            new InventoryItemFactory(), equipmentCatalog: catalog);

        Assert.False((await service.OpenSelectionContainerAsync(Guid.NewGuid(), chest.ItemInstanceId, "random", default)).IsSuccess);
        Assert.False((await service.OpenSelectionContainerAsync(owner, chest.ItemInstanceId, "shortsword", default)).IsSuccess);
        Assert.Equal(20, chest.Quantity);
        for (var opening = 0; opening < 20; opening++)
        {
            var result = await service.OpenSelectionContainerAsync(owner, chest.ItemInstanceId, "random", default);
            Assert.True(result.IsSuccess);
            Assert.Equal(19 - opening, chest.Quantity);
            var item = Assert.Single(result.Rewards);
            Assert.Equal(1, item.Quantity);
            Assert.Equal(owner, item.InventoryId);
            var data = Assert.IsType<EquipmentInstance>(item.ItemInstance).ProgressionData!;
            Assert.Contains(data.EquipmentType, allowedTypes);
            Assert.Equal(EquipmentRarity.Common, data.Rarity);
            Assert.Equal(1, data.State.Tier);
            Assert.Equal(0, data.State.Rank);
            Assert.Equal(ItemQuality.Standard, data.Quality);
            Assert.Equal(chestId, data.State.Provenance.SourceId);
        }
        Assert.Equal(20, inventory.AddedRewards.Select(item => item.ItemInstanceId).Distinct().Count());
        Assert.False((await service.OpenSelectionContainerAsync(owner, chest.ItemInstanceId, "random", default)).IsSuccess);
        Assert.Equal(20, inventory.AddedRewards.Count);
    }

    [Theory]
    [InlineData(RandomEquipmentBoxCatalog.UncommonItemBaseId)]
    [InlineData(RandomEquipmentBoxCatalog.ArmorChestItemBaseId)]
    [InlineData(RandomEquipmentBoxCatalog.JewelryChestItemBaseId)]
    public async Task Missing_equipment_content_does_not_consume_the_box(string boxId)
    {
        var owner = Guid.NewGuid();
        var box = CreateInventoryItem(owner, boxId, ItemType.Resource, 1);
        var inventory = new FakeInventoryService(box);
        var service = new SelectionCrateService(inventory, new FakeItemBaseRepository([]),
            new InventoryItemFactory(), equipmentCatalog: EquipmentBoxCatalog());
        var result = await service.OpenSelectionContainerAsync(owner, box.ItemInstanceId, "random", default);
        Assert.False(result.IsSuccess);
        Assert.Equal(1, box.Quantity);
        Assert.Empty(inventory.AddedRewards);
    }

    [Theory]
    [InlineData(RandomEquipmentBoxCatalog.UncommonItemBaseId)]
    [InlineData(RandomEquipmentBoxCatalog.ArmorChestItemBaseId)]
    [InlineData(RandomEquipmentBoxCatalog.JewelryChestItemBaseId)]
    public void Random_equipment_box_metadata_exposes_opening_without_a_selection(string boxId)
    {
        var item = new ItemBase { Id = boxId };
        var metadata = CreateMapper().Map<ItemBaseDto>(item).SelectionCrate!;
        Assert.True(metadata.IsRandom);
        Assert.Empty(metadata.Options);
    }
}
