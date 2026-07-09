using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Inventories;
using Services.LL.Interfaces.Combat.Reward.Dungeon;

namespace EssenceSystem.Tests;

public sealed class DungeonEssenceRewardTests
{
    [Fact]
    public async Task Dungeon_completion_awards_monster_core_for_essence_ascension_tier()
    {
        await using var db = CreateDb();
        db.ItemBases.Add(new ItemBase
        {
            Id = "item.monster_core.lesser",
            Name = "Lesser Monster Core",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var pendingRewards = new CapturingDungeonPendingRewardWriter();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(new DungeonDefinition { Id = "dungeon.tier_1", Tier = 1 }),
            new EmptyDungeonRunRepository(),
            new EmptyLootTableRepository(),
            new ItemBaseRepository(db),
            new EmptyLootService(),
            pendingRewards,
            new InventoryItemFactory(),
            new NoOpDungeonMasteryService());

        await applier.ApplyAsync(new() { Id = Guid.NewGuid(), DungeonDefinitionId = "dungeon.tier_1" }, CancellationToken.None);

        var batch = Assert.Single(pendingRewards.Batches, x => x.Source == "Grade I Monster Cores");
        var reward = Assert.Single(batch.Loot);
        Assert.Equal("item.monster_core.lesser", reward.ItemInstance.ItemBaseId);
        Assert.InRange(reward.Quantity, 3, 6);
    }

    [Fact]
    public async Task Dungeon_completion_awards_region_potential_core_for_essence_stat_cap()
    {
        await using var db = CreateDb();
        db.ItemBases.Add(new ItemBase
        {
            Id = "item.essence_potential_core.region_1",
            Name = "Region 1 Potential Core",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var pendingRewards = new CapturingDungeonPendingRewardWriter();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(new DungeonDefinition { Id = "dungeon.region_1", Region = 1, Tier = 1 }),
            new EmptyDungeonRunRepository(),
            new EmptyLootTableRepository(),
            new ItemBaseRepository(db),
            new EmptyLootService(),
            pendingRewards,
            new InventoryItemFactory(),
            new NoOpDungeonMasteryService());

        await applier.ApplyAsync(new() { Id = Guid.NewGuid(), DungeonDefinitionId = "dungeon.region_1" }, CancellationToken.None);

        var batch = Assert.Single(pendingRewards.Batches, x => x.Source == "Region 1 Potential Cores");
        var reward = Assert.Single(batch.Loot);
        Assert.Equal("item.essence_potential_core.region_1", reward.ItemInstance.ItemBaseId);
        Assert.InRange(reward.Quantity, 1, 3);
    }

    private static LLDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LLDbContext(options);
    }

    private sealed class SingleDungeonDefinitions(DungeonDefinition definition) : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) => definition;
        public IReadOnlyList<DungeonDefinition> GetAll() => [definition];
    }

    private sealed class CapturingDungeonPendingRewardWriter : IDungeonPendingRewardWriter
    {
        public IReadOnlyList<InventoryItem> Loot { get; private set; } = [];
        public string Source { get; private set; } = string.Empty;
        public List<CapturedLootBatch> Batches { get; } = [];

        public Task AddAsync(
            DungeonCombatRewardFacts facts,
            DungeonCombatCalculatedOutcome outcome,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddLootAsync(
            Guid dungeonRunId,
            IReadOnlyList<InventoryItem> loot,
            string source,
            CancellationToken cancellationToken)
        {
            Loot = loot;
            Source = source;
            Batches.Add(new(source, loot));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedLootBatch(string Source, IReadOnlyList<InventoryItem> Loot);

    private sealed class EmptyDungeonRunRepository : IDungeonRunRepository
    {
        public Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> AddPendingRewardAsync(DungeonRun dungeonRun, RunReward reward, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult<DungeonRun?>(null);
        public Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid dungeonId, CancellationToken cancellationToken) => Task.FromResult<DungeonRun?>(null);
        public Task<bool> HasActiveDungeonRunAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(Guid characterId, IReadOnlyCollection<string> dungeonDefinitionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DungeonCompletionRecord>>([]);
        public Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(IReadOnlyCollection<string> dungeonDefinitionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DungeonCompletionLeaderboardEntry>>([]);
        public Task<bool> HasCompletedDungeonAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task MarkDungeonCompletedAsync(Guid characterId, string dungeonDefinitionId, DateTimeOffset completedAt, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> UpdateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class NoOpDungeonMasteryService : IDungeonMasteryService
    {
        public int CalculateLevel(long experience) => 0;
        public int? GetExperienceRequiredForNextLevel(int level) => null;

        public Task<DungeonMasteryAwardResult> AwardCompletionAsync(
            DungeonRun run,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DungeonMasteryAwardResult(
                run.DungeonDefinitionId,
                0,
                0,
                0,
                0,
                0,
                [],
                AlreadyAwarded: false));

        public Task ApplyStartBonusesAsync(DungeonRun run, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(
            Guid characterId,
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, DungeonMasterySnapshot>>(
                new Dictionary<string, DungeonMasterySnapshot>(StringComparer.OrdinalIgnoreCase));
    }

    private sealed class EmptyLootTableRepository : ILootTableRepository
    {
        public Task<LootTable> GetLootTableByIdAsync(Guid lootTableId, CancellationToken cancellationToken) =>
            Task.FromResult(new LootTable { Id = lootTableId });

        public Task<LootTable> GetMonsterLootTableAsync(Guid monsterId, CancellationToken cancellationToken) =>
            Task.FromResult(new LootTable());

        public Task<LootTable> GetProfessionTaskLootTableAsync(Guid professionTaskId, CancellationToken cancellationToken) =>
            Task.FromResult(new LootTable());
    }

    private sealed class EmptyLootService : ILootService
    {
        public int GenerateSoulstoneLoot(int seconds) => 0;

        public List<InventoryItem> GenerateGatheringLootAsync(
            LootTable lootTable,
            CancellationToken cancellationToken,
            double rareEntryWeightBonusPercent = 0,
            int numberOfRolls = 1) => [];

        public List<InventoryItem> GenerateIdleCombatLootAsync(List<Domain.Models.Entities.Entity> enemyCharacters, Dictionary<ItemType, double> multipliers) => [];

        public List<InventoryItem> GenerateDungeonLoot(LootTable lootTable, Dictionary<ItemType, double>? multipliers = null) => [];

        public int GenerateCinderLoot(Dictionary<Guid, int> creatureKills, Dictionary<Guid, int> baseCinderValues, double dropChance = 0.2) => 0;
    }
}
