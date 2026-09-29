using System.Text.Json;
using Application.Interfaces.Services.LL.Items;
using Application.Interfaces.Services.LL.WorldTower;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.LL;
using Services.LL.Items;
using Services.LL.WorldTower;

namespace EssenceSystem.Tests;

public sealed partial class EquipmentAcquisitionTests
{
    internal static TowerEquipmentSupplyCatalog SupplyCatalog() => JsonTowerEquipmentSupplyCatalog.Load(
        Path.Combine(TestContentPaths.FindApiRoot(), "Data/equipment/tower-equipment-supplies.v1.json"),
        JsonStarterEquipmentCatalog.Load(Path.Combine(TestContentPaths.FindApiRoot(),
            "Data/equipment/equipment-starters.v1.json"), balanceVersion: 4));

    [Theory]
    [InlineData(1, 0, 30, 1)]
    [InlineData(1, 2, 30, 1)]
    [InlineData(1, 3, 30, 4)]
    [InlineData(1, 5, 40, 4)]
    [InlineData(1, 6, 40, 7)]
    [InlineData(1, 8, 50, 7)]
    [InlineData(1, 9, 50, 10)]
    [InlineData(1, 10, 60, 10)]
    [InlineData(2, 10, 60, 11)]
    [InlineData(2, 13, 60, 14)]
    [InlineData(1, 9, 49, 7)]
    [InlineData(1, 6, 39, 4)]
    public async Task Tower_supply_is_available_before_its_floor_at_the_declared_level_and_source(
        int region, int cleared, int level, int target)
    {
        var fixture = new SupplyFixture(cleared, level);
        await fixture.Service.CompleteAsync(fixture.Run, region, default);
        var reward = Assert.Single(fixture.Run.PendingRewards);
        Assert.Equal($"item.tower_supply.v1.floor_{target:00}", reward.ItemId);
        Assert.Equal(1, reward.Quantity);
        Assert.True(fixture.Run.State.TowerEquipmentSupplyProcessed);
        Assert.All(fixture.Progress.Requests, request => Assert.Equal("supply-test", request.Server));
        Assert.All(fixture.Progress.Requests, request => Assert.NotEqual(target, request.Floor));
    }

    [Theory]
    [InlineData(1, 0, 29)]
    [InlineData(2, 9, 60)]
    [InlineData(2, 10, 59)]
    [InlineData(3, 99, 100)]
    public async Task Tower_supply_rejects_locked_levels_regions_and_prerequisites(int region, int cleared, int level)
    {
        var fixture = new SupplyFixture(cleared, level);
        await fixture.Service.CompleteAsync(fixture.Run, region, default);
        Assert.Empty(fixture.Run.PendingRewards);
    }

    [Theory]
    [InlineData(DungeonRunStatus.Active)]
    [InlineData(DungeonRunStatus.Failed)]
    [InlineData(DungeonRunStatus.Retreated)]
    [InlineData(DungeonRunStatus.RewardsClaimed)]
    public async Task Tower_supply_requires_a_completed_unclaimed_run(DungeonRunStatus status)
    {
        var fixture = new SupplyFixture(9, 50);
        fixture.Run.Status = status;
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        Assert.Empty(fixture.Run.PendingRewards);
        Assert.False(fixture.Run.State.TowerEquipmentSupplyProcessed);
    }

    [Fact]
    public async Task Tower_supply_retries_freeze_both_awards_and_no_award_decisions()
    {
        var fixture = new SupplyFixture(0, 30);
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        fixture.Progress.Cleared = 9;
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        Assert.EndsWith("floor_01", Assert.Single(fixture.Run.PendingRewards).ItemId);
        // Repair a legacy missing processed marker without duplicating the durable pending reward.
        fixture.Run.State.TowerEquipmentSupplyProcessed = false;
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        Assert.Single(fixture.Run.PendingRewards);

        var unavailable = new SupplyFixture(9, 60);
        await unavailable.Service.CompleteAsync(unavailable.Run, 2, default);
        unavailable.Progress.Cleared = 10;
        await unavailable.Service.CompleteAsync(unavailable.Run, 2, default);
        Assert.Empty(unavailable.Run.PendingRewards);
    }

    [Fact]
    public async Task Tower_supply_respects_switches_release_gate_and_snapshot_ownership()
    {
        var fixture = new SupplyFixture(13, 60);
        fixture.Flags.TowerSupplyAcquisitionEnabled = false;
        await fixture.Service.CompleteAsync(fixture.Run, 2, default);
        Assert.Empty(fixture.Run.PendingRewards);
        fixture.Flags.TowerSupplyAcquisitionEnabled = true;
        fixture.Flags.ProtectedAcquisitionEnabled = false;
        await fixture.Service.CompleteAsync(fixture.Run, 2, default);
        Assert.Empty(fixture.Run.PendingRewards);
        fixture.Flags.ProtectedAcquisitionEnabled = true;
        fixture.Floors.ReleasedThrough = 11;
        await fixture.Service.CompleteAsync(fixture.Run, 2, default);
        Assert.EndsWith("floor_11", Assert.Single(fixture.Run.PendingRewards).ItemId);

        var wrongOwner = new SupplyFixture(9, 50);
        wrongOwner.Run.CharacterId = Guid.NewGuid();
        await wrongOwner.Service.CompleteAsync(wrongOwner.Run, 1, default);
        Assert.Empty(wrongOwner.Run.PendingRewards);
        var claimed = new SupplyFixture(9, 50);
        claimed.Run.RewardsClaimedAt = DateTimeOffset.UtcNow;
        await claimed.Service.CompleteAsync(claimed.Run, 1, default);
        Assert.Empty(claimed.Run.PendingRewards);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Normal_dungeon_completion_never_issues_Tower_supplies_even_with_legacy_flag(
        bool legacyFlag, bool ordinaryDrop)
    {
        for (var clear = 0; clear < 8; clear++)
        {
            var fixture = new SupplyFixture(9, 50);
            fixture.Flags.TowerSupplyAcquisitionEnabled = legacyFlag;
            var service = new EquipmentAcquisitionService(Catalog(ordinaryDrop ? 1 : double.Epsilon), new Dungeons(), fixture.Runs,
                Options.Create(fixture.Flags));
            await service.CompleteAsync(fixture.Run, false, default);
            await service.CompleteAsync(fixture.Run, false, default);
            Assert.False(fixture.Run.State.TowerEquipmentSupplyProcessed);
            Assert.DoesNotContain(fixture.Run.PendingRewards, r => r.Source == TowerEquipmentSupplyService.RewardSource
                || r.ItemId.StartsWith("item.tower_supply.", StringComparison.Ordinal));
            if (ordinaryDrop)
            {
                var reward = Assert.Single(fixture.Run.PendingRewards);
                Assert.Equal(ItemType.Equipment, reward.ItemType);
                Assert.NotNull(reward.ProgressionData);
            }
            else Assert.Empty(fixture.Run.PendingRewards);
        }
    }

    [Fact]
    public async Task Tower_supply_is_disabled_by_default_for_direct_historical_adapter_use()
    {
        Assert.False(new EquipmentProgressionOptions().TowerSupplyAcquisitionEnabled);
        var fixture = new SupplyFixture(9, 50);
        fixture.Flags.TowerSupplyAcquisitionEnabled = new EquipmentProgressionOptions().TowerSupplyAcquisitionEnabled;
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        Assert.Empty(fixture.Run.PendingRewards);
        Assert.False(fixture.Run.State.TowerEquipmentSupplyProcessed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Game_registration_cannot_enable_the_withdrawn_supply_reward(bool legacyFlag)
    {
        var root = TestContentPaths.FindApiRoot();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Content:Root"] = Path.Combine(root, "Data"),
            ["EquipmentProgression:TowerSupplyAcquisitionEnabled"] = legacyFlag.ToString()
        }).Build();
        var services = new ServiceCollection();
        services.AddServices(configuration, root);
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(ITowerEquipmentSupplyService)
            || s.ServiceType == typeof(TowerEquipmentSupplyCatalog));
        Assert.Contains(services, s => s.ServiceType == typeof(IEquipmentAcquisitionService)
            && s.ImplementationType == typeof(EquipmentAcquisitionService));
    }

    [Fact]
    public async Task Tower_supply_pending_reward_claims_as_a_bound_inventory_chest()
    {
        var fixture = new SupplyFixture(9, 50);
        await fixture.Service.CompleteAsync(fixture.Run, 1, default);
        var inventory = new TreasuryInventory();
        var claimer = new Services.LL.Combat.Layers.Rewards.Dungeon.DungeonRunRewardClaimer(null!, null!,
            new SupplyItemBases(false), new Services.LL.Inventories.InventoryItemFactory(), inventory);
        var items = await claimer.ClaimAsync(fixture.Run, default);
        var item = Assert.Single(items);
        Assert.Equal("item.tower_supply.v1.floor_10", item.ItemInstance.ItemBaseId);
        Assert.True(item.ItemInstance.IsBound);
        Assert.Equal(fixture.Run.CharacterId, item.InventoryId);
        Assert.Equal(1, item.Quantity);
        fixture.Run.RewardsClaimedAt = DateTimeOffset.UtcNow;
        Assert.Empty(await claimer.ClaimAsync(fixture.Run, default));
        Assert.Single(inventory.Items);
    }

    [Fact]
    public async Task Tower_supply_content_failure_does_not_record_a_successful_decision()
    {
        var fixture = new SupplyFixture(9, 50, missingItems: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CompleteAsync(fixture.Run, 1, default));
        Assert.False(fixture.Run.State.TowerEquipmentSupplyProcessed);
        Assert.Empty(fixture.Run.PendingRewards);
    }

    [Fact]
    public void Tower_supply_cycle_repeats_and_authored_content_covers_every_released_floor()
    {
        for (var floor = 1; floor <= 90; floor++)
            Assert.Equal(TowerEquipmentBand.ForFloor(floor), TowerEquipmentBand.ForFloor(floor + 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => TowerEquipmentBand.ForFloor(0));
        var catalog = SupplyCatalog();
        using var items = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data/items/items.json")));
        foreach (var supply in catalog.Supplies)
        {
            var item = items.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetString() == supply.ItemBaseId);
            Assert.True(item.GetProperty("isBound").GetBoolean());
            Assert.True(item.GetProperty("stackable").GetBoolean());
            Assert.Equal(supply.Band.Rarity.ToString(), item.GetProperty("rarity").GetString());
            Assert.Equal(supply.Name, item.GetProperty("name").GetString());
        }
        using var tower = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data/world-tower/tower-floors.json")));
        var released = tower.RootElement.GetProperty("releasedThroughFloor").GetInt32();
        for (var floor = 1; floor <= released; floor++)
        {
            var start = floor - ((floor - 1) % 10) + (((floor - 1) % 10) switch { < 3 => 0, < 6 => 3, < 9 => 6, _ => 9 });
            Assert.Contains(catalog.Supplies, x => x.TargetFloor == start);
        }
        // Region 2 cannot supply the first floor-10 clear; its chest must come from region 1.
        Assert.Equal(1, catalog.Supplies.Single(x => x.TargetFloor == 10).SourceRegion);
        using var dungeons = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContentPaths.FindApiRoot(), "Data/dungeons/dungeons.json")));
        Assert.Contains(dungeons.RootElement.GetProperty("families").EnumerateArray(), x =>
            x.GetProperty("region").GetInt32() == 1 && (!x.TryGetProperty("requiredTowerFloor", out var gate) || gate.ValueKind == JsonValueKind.Null || gate.GetInt32() < 10));
    }

    private sealed class SupplyFixture
    {
        public DungeonRun Run { get; } = EquipmentAcquisitionTests.Run("goblin_mines");
        public Runs Runs { get; } = new();
        public SupplyProgress Progress { get; }
        public SupplyFloors Floors { get; } = new();
        // Explicit legacy-scenario controls; this feature is not part of normal game acquisition.
        public EquipmentProgressionOptions Flags { get; } = new() { TowerSupplyAcquisitionEnabled = true };
        public TowerEquipmentSupplyService Service { get; }
        public SupplyFixture(int cleared, int level, bool missingItems = false)
        {
            Progress = new(cleared);
            Run.CharacterSnapshotId = Guid.NewGuid();
            var snapshot = new CharacterSnapshot { Id = Run.CharacterSnapshotId.Value, CharacterId = Run.CharacterId, Level = level };
            Service = new(SupplyCatalog(), Progress, Floors, new SupplySnapshots(snapshot), Runs,
                new SupplyItemBases(missingItems), Options.Create(new WorldTowerOptions { ServerId = "supply-test" }), Options.Create(Flags));
        }
    }

    private sealed class SupplyProgress(int cleared) : IWorldTowerProgressRepository
    {
        public int Cleared { get; set; } = cleared;
        public List<(string Server, int Floor)> Requests { get; } = [];
        public Task<bool> HasClearedFloorAsync(string serverId, int floor, CancellationToken ct)
        { Requests.Add((serverId, floor)); return Task.FromResult(Cleared >= floor); }
    }
    private sealed class SupplyFloors : IWorldTowerDefinitionProvider
    {
        public int ReleasedThrough { get; set; } = 15;
        public IReadOnlyList<TowerFloorDefinition> GetFloors() => throw new NotSupportedException();
        public TowerFloorDefinition? GetFloor(int floor) => floor <= ReleasedThrough ? new() { FloorNumber = floor } : null;
    }
    private sealed class SupplySnapshots(CharacterSnapshot snapshot) : ICharacterSnapshotRepository
    {
        public Task<CharacterSnapshot?> GetSnapshotByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<CharacterSnapshot?>(id == snapshot.Id ? snapshot : null);
        public Task<CharacterSnapshot?> GetSnapshotByCharacterIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<CharacterSnapshot> CreateAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class SupplyItemBases(bool missing) : IItemBaseRepository
    {
        public Task<IReadOnlyDictionary<string, ItemBase>> GetItemBasesByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, ItemBase>>(missing ? new Dictionary<string, ItemBase>() : ids.ToDictionary(id => id,
                id => new ItemBase { Id = id, IsBound = true, Stackable = true, ItemType = ItemType.Resource }));
        public Task<IReadOnlyDictionary<string, string>> GetEssenceItemBaseIdsByDefinitionIdAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task AddMissingItemBasesAsync(IReadOnlyCollection<ItemBase> items, CancellationToken ct) => throw new NotSupportedException();
    }
}

public sealed partial class SelectionCrateServiceTests
{
    [Fact]
    public async Task Opening_floor_11_chest_preserves_existing_floor_10_legendary_equipment()
    {
        var catalog = EquipmentAcquisitionTests.SupplyCatalog();
        var owner = Guid.NewGuid();
        var legendary = catalog.Supplies.Single(x => x.TargetFloor == 10);
        var rare = catalog.Supplies.Single(x => x.TargetFloor == 11);
        var crate = CreateInventoryItem(owner, legendary.ItemBaseId, ItemType.Resource, 1);
        var inventory = new FakeInventoryService(crate);
        var choices = new[] { catalog.Choices(legendary.ItemBaseId)[0], catalog.Choices(rare.ItemBaseId)[0] };
        var data = catalog.Award(legendary.ItemBaseId, choices[0].Id, owner, Guid.NewGuid(), "test");
        var service = new Services.LL.Inventories.SelectionCrateService(inventory,
            new FakeItemBaseRepository([new EquipmentBase { Id = data.ItemBaseId, EquipmentType = data.EquipmentType, Stackable = false }]),
            new Services.LL.Inventories.InventoryItemFactory(), towerSupplies: catalog);
        Assert.True((await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, choices[0].Id, default)).IsSuccess);
        var first = Assert.IsType<EquipmentInstance>(Assert.Single(inventory.AddedRewards).ItemInstance);
        var frozen = first.ProgressionData!.Serialize();
        crate.ItemInstance.ItemBaseId = rare.ItemBaseId;
        crate.Quantity = 1;
        Assert.True((await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, choices[1].Id, default)).IsSuccess);
        Assert.Equal(2, inventory.AddedRewards.Count);
        Assert.Equal(frozen, first.ProgressionData.Serialize());
    }

    [Theory]
    [InlineData(1, 1, EquipmentRarity.Rare, ItemQuality.Standard, 2)]
    [InlineData(4, 1, EquipmentRarity.Epic, ItemQuality.Fine, 3)]
    [InlineData(7, 1, EquipmentRarity.Unique, ItemQuality.Exceptional, 4)]
    [InlineData(10, 2, EquipmentRarity.Legendary, ItemQuality.Masterpiece, 5)]
    [InlineData(11, 2, EquipmentRarity.Rare, ItemQuality.Standard, 2)]
    [InlineData(14, 2, EquipmentRarity.Epic, ItemQuality.Fine, 3)]
    public async Task Tower_supply_can_target_every_slot_and_specialization_and_keeps_owned_gear(
        int floor, int tier, EquipmentRarity rarity, ItemQuality quality, int rank)
    {
        var catalog = EquipmentAcquisitionTests.SupplyCatalog();
        var owner = Guid.NewGuid();
        var supply = catalog.Supplies.Single(x => x.TargetFloor == floor);
        var choices = catalog.Choices(supply.ItemBaseId);
        Assert.Contains(choices, x => x.Id.Contains(".spec."));
        Assert.Equal(Enum.GetValues<EquipmentType>().Order(), choices.Select(x => x.EquipmentType).Distinct().Order());
        var crate = CreateInventoryItem(owner, supply.ItemBaseId, ItemType.Resource, choices.Count);
        var inventory = new FakeInventoryService(crate);
        var data = choices.Select(x => catalog.Award(supply.ItemBaseId, x.Id, owner, Guid.NewGuid(), "test")).ToArray();
        var bases = data.DistinctBy(x => x.ItemBaseId).Select(x => new EquipmentBase
            { Id = x.ItemBaseId, EquipmentType = x.EquipmentType, Stackable = false });
        var service = new Services.LL.Inventories.SelectionCrateService(inventory,
            new FakeItemBaseRepository(bases), new Services.LL.Inventories.InventoryItemFactory(), towerSupplies: catalog);
        foreach (var choice in choices)
        {
            var result = await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, choice.Id, default);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            var item = Assert.IsType<EquipmentInstance>(Assert.Single(result.Rewards).ItemInstance);
            Assert.True(item.IsBound);
            Assert.Equal(owner, item.ProgressionData!.State.Ownership.OwnerId);
            Assert.Equal(choice.Id, item.ProgressionData.State.DefinitionId);
            Assert.Equal((tier, rarity, quality, rank, 1d), (item.ProgressionData.State.Tier, item.ProgressionData.Rarity,
                item.ProgressionData.State.Quality, item.ProgressionData.State.Rank, item.ProgressionData.State.AttributeRollMultiplier));
            Assert.Null(item.ProgressionData.State.ActiveStyleId);
        }
        Assert.Equal(choices.Count, inventory.AddedRewards.Count);
        Assert.Equal(choices.Count, inventory.AddedRewards.Select(x => x.ItemInstanceId).Distinct().Count());
        Assert.False((await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, choices[0].Id, default)).IsSuccess);
    }

    [Fact]
    public async Task Tower_supply_rejects_foreign_ownership_wrong_chest_choices_and_missing_bases_without_consumption()
    {
        var catalog = EquipmentAcquisitionTests.SupplyCatalog();
        var owner = Guid.NewGuid();
        var supply = catalog.Supplies.First();
        var crate = CreateInventoryItem(owner, supply.ItemBaseId, ItemType.Resource, 1);
        var inventory = new FakeInventoryService(crate);
        var service = new Services.LL.Inventories.SelectionCrateService(inventory,
            new FakeItemBaseRepository([]), new Services.LL.Inventories.InventoryItemFactory(), towerSupplies: catalog);
        var valid = catalog.Choices(supply.ItemBaseId)[0].Id;
        Assert.False((await service.OpenSelectionContainerAsync(Guid.NewGuid(), crate.ItemInstanceId, valid, default)).IsSuccess);
        Assert.False((await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId,
            catalog.Choices(catalog.Supplies.Single(x => x.TargetFloor == 10).ItemBaseId)[0].Id, default)).IsSuccess);
        Assert.False((await service.OpenSelectionContainerAsync(owner, crate.ItemInstanceId, valid, default)).IsSuccess);
        Assert.Equal(1, crate.Quantity);
        Assert.Empty(inventory.AddedRewards);
    }
}
