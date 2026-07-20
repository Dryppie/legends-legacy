using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Bonuses;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Rewards;
using Microsoft.EntityFrameworkCore;
using Persistence.LL;
using Persistence.LL.Repositories.Items;
using Services.LL.Combat.Layers.Rewards.Dungeon;
using Services.LL.Combat.Layers.Rewards.Models;
using Services.LL.Inventories;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Reward.Idle;
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
            new ItemBaseRepository(db),
            new EmptyRewardRoller(),
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
            new ItemBaseRepository(db),
            new EmptyRewardRoller(),
            pendingRewards,
            new InventoryItemFactory(),
            new NoOpDungeonMasteryService());

        await applier.ApplyAsync(new() { Id = Guid.NewGuid(), DungeonDefinitionId = "dungeon.region_1" }, CancellationToken.None);

        var batch = Assert.Single(pendingRewards.Batches, x => x.Source == "Region 1 Potential Cores");
        var reward = Assert.Single(batch.Loot);
        Assert.Equal("item.essence_potential_core.region_1", reward.ItemInstance.ItemBaseId);
        Assert.InRange(reward.Quantity, 1, 3);
    }

    [Theory]
    [InlineData(RoomType.MiniBoss)]
    [InlineData(RoomType.Boss)]
    public async Task Dungeon_boss_essence_multipliers_only_apply_to_featured_monster(RoomType roomType)
    {
        var featuredMonster = new Creature { Id = Guid.NewGuid(), Name = "Specter" };
        var supportingMonster = new Creature { Id = Guid.NewGuid(), Name = "Skeleton Warrior" };
        var essenceResonance = new CapturingEssenceResonanceService();
        var calculator = new DungeonCombatRewardCalculator(
            new EmptyBonusService(),
            new EmptyLootService(),
            new EmptyDungeonRewardBalanceProvider(),
            essenceResonance,
            new EmptyGatheringRewardProcessor());
        var encounter = new DungeonEncounterRewardFacts(
            EncounterId: Guid.NewGuid(),
            Outcome: BattleOutcome.Victory,
            HostileSourceEntityIds: [featuredMonster.Id, supportingMonster.Id],
            HostileCreatures: [featuredMonster, supportingMonster],
            CombatResult: new CombatResult { Outcome = BattleOutcome.Victory });
        var facts = new DungeonCombatRewardFacts(
            DungeonRunId: Guid.NewGuid(),
            CharacterId: Guid.NewGuid(),
            CurrentRoomIndex: 1,
            DungeonTier: 1,
            RoomType: roomType,
            FeaturedEssenceMonsterDefinitionId: "monster.specter",
            MonsterLootModifiers: new Dictionary<ItemType, double>(),
            PlayerEntityIds: [],
            EquippedTool: null,
            GatheringNodes: [],
            Encounters: [encounter]);

        await calculator.CalculateAsync(facts, CancellationToken.None);

        var boostedRoll = Assert.Single(
            essenceResonance.Rolls,
            roll => roll.Modifiers is not null);
        Assert.Equal(featuredMonster.Id, Assert.Single(boostedRoll.Creatures).Id);
        Assert.Equal(10, boostedRoll.Modifiers!.DropChanceMultiplier);
        Assert.Equal(1_000, boostedRoll.Modifiers.PityProgressionMultiplier);
        Assert.Equal(10, boostedRoll.Modifiers.ResonanceCapMultiplier);

        var standardRoll = Assert.Single(
            essenceResonance.Rolls,
            roll => roll.Modifiers is null);
        Assert.Equal(supportingMonster.Id, Assert.Single(standardRoll.Creatures).Id);
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

        public Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(
            Guid characterId,
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, DungeonMasterySnapshot>>(
                new Dictionary<string, DungeonMasterySnapshot>(StringComparer.OrdinalIgnoreCase));
    }

    private sealed class EmptyRewardRoller : IRewardRoller
    {
        public RewardRollResult Roll(string rewardTableId, RewardRollContext context) => RewardRollResult.Empty;
        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context) => RewardRollResult.Empty;
    }

    private sealed class EmptyBonusService : IBonusService
    {
        public ValueTask<IReadOnlyDictionary<BonusKind, double>> GetAggregatedAsync(
            Guid characterId,
            DateTimeOffset now,
            CancellationToken ct = default) =>
            ValueTask.FromResult<IReadOnlyDictionary<BonusKind, double>>(
                new Dictionary<BonusKind, double>());
    }

    private sealed class EmptyLootService : ILootService
    {
        public int GenerateSoulstoneLoot(int seconds) => 0;

        public Task<List<InventoryItem>> GenerateIdleCombatLootAsync(
            List<Entity> enemyCharacters,
            Dictionary<ItemType, double> multipliers,
            CancellationToken cancellationToken) => Task.FromResult(new List<InventoryItem>());

        public Task<IReadOnlyList<IReadOnlyList<InventoryItem>>> GenerateIdleCombatLootBatchAsync(
            IReadOnlyList<IReadOnlyList<Entity>> enemyGroups,
            Dictionary<ItemType, double> multipliers,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<IReadOnlyList<InventoryItem>>>([]);

        public int GenerateCinderLoot(
            Dictionary<Guid, int> creatureKills,
            Dictionary<Guid, int> baseCinderValues,
            double dropChance = 0.2) => 0;
    }

    private sealed class EmptyDungeonRewardBalanceProvider : IDungeonRewardBalanceProvider
    {
        public DungeonEncounterReward GetEncounterReward(int dungeonTier, RoomType roomType) => new(0, 0);
    }

    private sealed class CapturingEssenceResonanceService : IEssenceResonanceService
    {
        public List<CapturedEssenceRoll> Rolls { get; } = [];

        public Task PrepareEssenceDropsAsync(
            Guid characterId,
            IReadOnlyList<Creature> defeatedCreatures,
            bool loadEssenceFocus,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<EssenceDropRollResult> RollMonsterEssenceDropAsync(
            Guid characterId,
            string monsterId,
            bool eligible,
            CancellationToken cancellationToken,
            EssenceDropRollModifiers? modifiers = null) =>
            Task.FromResult(new EssenceDropRollResult(false, null, 0, 0));

        public Task<IReadOnlyList<InventoryItem>> RollEssenceDropsAsync(
            Guid characterId,
            IReadOnlyList<Creature> defeatedCreatures,
            bool eligible,
            CancellationToken cancellationToken,
            IReadOnlyDictionary<BonusKind, double>? bonusFactors = null,
            EssenceDropRollModifiers? modifiers = null)
        {
            Rolls.Add(new CapturedEssenceRoll(defeatedCreatures, modifiers));
            return Task.FromResult<IReadOnlyList<InventoryItem>>([]);
        }
    }

    private sealed record CapturedEssenceRoll(
        IReadOnlyList<Creature> Creatures,
        EssenceDropRollModifiers? Modifiers);

    private sealed class EmptyGatheringRewardProcessor : ICombatGatheringRewardProcessor
    {
        public Task<IReadOnlyList<GatheringRewardResult>> ProcessAsync(
            CombatGatheringRewardFacts facts,
            CancellationToken cancellationToken,
            IReadOnlyDictionary<BonusKind, double>? bonusFactors = null) =>
            Task.FromResult<IReadOnlyList<GatheringRewardResult>>([]);
    }
}
