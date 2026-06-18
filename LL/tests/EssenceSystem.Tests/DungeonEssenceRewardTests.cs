using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Domain.Models.Dungeons;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.LootTables;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;
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
            Id = "item.monster_core.tier_1",
            Name = "Tier 1 Monster Core",
            ItemType = ItemType.Resource,
            Stackable = true
        });
        await db.SaveChangesAsync();

        var pendingRewards = new CapturingDungeonPendingRewardWriter();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(new DungeonDefinition { Id = "dungeon.tier_1", Tier = 1 }),
            new EmptyLootTableRepository(),
            new ItemBaseRepository(db),
            new EmptyLootService(),
            pendingRewards);

        await applier.ApplyAsync(new() { Id = Guid.NewGuid(), DungeonDefinitionId = "dungeon.tier_1" }, CancellationToken.None);

        var reward = Assert.Single(pendingRewards.Loot);
        Assert.Equal("item.monster_core.tier_1", reward.ItemInstance.ItemBaseId);
        Assert.Equal(1, reward.Quantity);
        Assert.Equal("Tier 1 Monster Core", pendingRewards.Source);
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
            return Task.CompletedTask;
        }
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
        public int GenerateSoulstoneLoot(int seconds, double dropRate, double doubleChance) => 0;

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
