using Application.Interfaces.Services.LL.Items;
using Domain.Models.Combat;
using Domain.Models.Entities.Characters;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Items.Equipments;
using Domain.Models.Items.Equipments.Progression;
using Domain.Models.Regions.Areas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Equipments;
using Persistence.LL.Repositories.Inventories;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat.Layers.Rewards.Idle;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class IdleCombatInventoryTrackingTests
{
    private static readonly Guid CharacterId = Guid.Parse("70eb3747-fac5-4609-b391-e799434fbc4c");
    private static readonly DateTimeOffset Epoch = new(2026, 9, 2, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Buffered_equipment_batches_persist_unique_instances_with_shared_item_bases(
        bool existingInventory,
        bool replayWithNewGeneration)
    {
        await using var db = new LLDbContext(new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var catalog = CreateCatalog();
        db.Characters.Add(new Character
        {
            Id = CharacterId,
            UserId = Guid.NewGuid(),
            Name = "Inventory tracking test",
            Level = 10
        });
        db.Inventories.Add(new Inventory { CharacterId = CharacterId });

        var bases = catalog.Equipment.Options
            .Select(option =>
            {
                var evaluated = catalog.Equipment.Evaluator.Evaluate(option.DefinitionId, 1, 0, null);
                return new EquipmentBase
                {
                    Id = evaluated.Archetype.ItemBaseId,
                    Name = option.Name,
                    EquipmentType = option.EquipmentType
                };
            })
            .DistinctBy(item => item.Id)
            .ToArray();
        db.ItemBases.AddRange(bases);
        await db.SaveChangesAsync();

        var inventory = new InventoryRepository(db);
        var processor = new CombatAcquisitionRewardProcessor(
            catalog,
            new ItemBaseRepository(db),
            Options.Create(new EquipmentProgressionOptions { OrdinaryAcquisitionEnabled = true }),
            new PlainEquipmentRepository(db),
            JsonEquipmentBlueprintCatalog.Load(
                Path.Combine(ContentRoot(), "equipment-blueprints.v1.json"), catalog.Equipment));

        const int batchSize = 40;
        var existingIds = new HashSet<Guid>();
        var existingValues = new Dictionary<Guid, (string Progression, DateTimeOffset AcquiredAt)>();
        if (existingInventory)
        {
            var initial = await processor.ProcessAsync(
                Facts(replayWithNewGeneration ? 0 : -batchSize, batchSize), default);
            existingIds.UnionWith(initial.Equipment.Select(item => item.ItemInstanceId));
            await inventory.AddItemsToInventory(CharacterId, initial.Equipment.ToList(),
                ItemAcquisitionSources.CombatReward, default);
            await db.SaveChangesAsync();
            foreach (var item in initial.Equipment)
            {
                var instance = Assert.IsType<EquipmentInstance>(item.ItemInstance);
                existingValues[instance.Id] = (instance.ProgressionData!.Serialize(), instance.AcquiredAtUtc);
            }
        }

        db.ClearTrackedEntities();
        var trackedInventory = await inventory.GetInventoryByIdAsync(CharacterId, default);
        var pendingLoot = new List<InventoryItem>();
        for (var batch = 0; batch < 3; batch++)
        {
            // Rewinding a test schedule must start a new generation even when the
            // first batch covers the exact timestamps of already persisted rewards.
            var outcome = await processor.ProcessAsync(
                Facts(batch * batchSize, batchSize, replayWithNewGeneration ? 2 : 1), default);
            Assert.Equal(batchSize, outcome.Equipment.Count);
            pendingLoot.AddRange(outcome.Equipment);
        }

        var generatedIds = pendingLoot.Select(item => item.ItemInstanceId).ToHashSet();
        Assert.Equal(pendingLoot.Count, generatedIds.Count);
        Assert.Empty(existingIds.Intersect(generatedIds));
        Assert.Contains(pendingLoot.GroupBy(item => item.ItemInstance.ItemBaseId), group => group.Count() > 1);
        if (existingInventory)
        {
            Assert.Contains(pendingLoot, item => item.ItemInstance.ItemBase.ItemInstances
                .Any(existing => existingIds.Contains(existing.Id)));
        }

        await inventory.AddItemsToInventory(CharacterId, pendingLoot,
            ItemAcquisitionSources.CombatReward, default);
        await db.SaveChangesAsync();

        var expectedCount = pendingLoot.Count + existingIds.Count;
        Assert.Equal(expectedCount, trackedInventory.InventoryItems.Count);
        Assert.Equal(expectedCount, await db.ItemInstances.OfType<EquipmentInstance>().CountAsync());
        Assert.Equal(expectedCount, await db.InventoryItems.CountAsync());
        Assert.Equal(expectedCount, await db.PlainEquipmentEntitlements.SumAsync(item => item.Copies));
        foreach (var (id, expected) in existingValues)
        {
            var instance = await db.ItemInstances.OfType<EquipmentInstance>().SingleAsync(item => item.Id == id);
            Assert.Equal(expected.Progression, instance.ProgressionData!.Serialize());
            Assert.Equal(expected.AcquiredAt, instance.AcquiredAtUtc);
            Assert.Equal(ItemAcquisitionSources.CombatReward, instance.AcquisitionSource);
        }
        Assert.All(db.ChangeTracker.Entries<EquipmentInstance>(), entry =>
            Assert.Equal(EntityState.Unchanged, entry.State));
    }

    private static IdleCombatRewardFacts Facts(int start, int count, long generation = 1) => new(
        CharacterId,
        Epoch.AddSeconds(start * 10),
        Epoch.AddSeconds((start + count) * 10),
        Epoch.AddSeconds((start + count) * 10),
        TimeSpan.FromSeconds(count * 10),
        new Area { Id = "region_01_area_01", Name = "Tracking test area" },
        [CharacterId],
        Enumerable.Range(start, count).Select((value, index) => new IdleEncounterRewardFacts(
            Guid.NewGuid(), index + 1, Epoch.AddSeconds(value * 10),
            BattleOutcome.Victory, [], [], null!)).ToArray())
    { ScheduleGeneration = generation };

    private static CombatAcquisitionCatalog CreateCatalog()
    {
        var root = ContentRoot();
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        var source = JsonStarterEquipmentCatalog.LoadOrdinary(
            equipment, Path.Combine(root, "equipment-ordinary.v1.json"));
        return new CombatAcquisitionCatalog(equipment, source.Pools.Select(rules => rules with
        {
            AreaEquipment = rules.AreaEquipment with { DropChance = 1 },
            SigilDropChance = double.Epsilon
        }));
    }

    private static string ContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException("Equipment content directory was not found.");
    }
}
