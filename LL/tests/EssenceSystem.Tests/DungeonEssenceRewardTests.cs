using Application.Interfaces.Services.LL;
using Application.Interfaces.Services.LL.Items;
using Domain.Models.Items.Equipments.Progression;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Essences;
using Application.Interfaces.Services.LL.Prophecies;
using Application.Interfaces.Services.LL.Rewards;
using Application.UseCases.Prophecies.Events;
using Domain.Models.Bonuses;
using Domain.Models.Combat;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Definitions.Rooms;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Inventories;
using Domain.Models.Items;
using Domain.Models.Rewards;
using Microsoft.EntityFrameworkCore;
using MediatR;
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
    [Theory]
    [InlineData(5, 11)]
    [InlineData(10, 11)]
    public async Task Dungeon_completion_applies_mastery_currency_bonus(
        int masteryLevel,
        int expectedCurrency)
    {
        await using var db = CreateDb();
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "mastered_dungeon",
            State = new DungeonRunState { MasteryLevelAtStart = masteryLevel }
        };
        var definition = new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Region = 0,
            Tier = 1,
            CompletionRewardTableIds = ["reward.dungeon.mastered.completion"]
        };
        var rewardRoller = new CurrencyRewardRoller();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(definition),
            new EmptyDungeonRunRepository(run),
            new ItemBaseRepository(db),
            rewardRoller,
            new CapturingDungeonPendingRewardWriter(),
            new InventoryItemFactory(),
            new NoOpDungeonMasteryService(),
            new RecordingPublisher());

        await applier.ApplyAsync(run, CancellationToken.None);

        Assert.Single(rewardRoller.RewardTableIds);
        Assert.Equal(expectedCurrency, run.PendingCinders);
        Assert.Equal(expectedCurrency, run.PendingSoulstones);
    }

    [Theory]
    [InlineData(1, 9, 10, false, 50)]
    [InlineData(2, 9, 10, false, 100)]
    [InlineData(3, 9, 10, false, 200)]
    [InlineData(1, 10, 10, false, 0)]
    [InlineData(1, 9, 10, true, 0)]
    [InlineData(1, 8, 9, false, 0)]
    public async Task Dungeon_completion_awards_fixed_soulstones_only_when_level_ten_is_first_reached(
        int dungeonTier,
        int previousLevel,
        int awardedLevel,
        bool alreadyAwarded,
        int expectedMasterySoulstones)
    {
        await using var db = CreateDb();
        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "mastered_dungeon",
            State = new DungeonRunState { MasteryLevelAtStart = previousLevel }
        };
        var definition = new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Region = 0,
            Tier = dungeonTier,
            CompletionRewardTableIds = ["reward.dungeon.mastered.completion"]
        };
        var rewardRoller = new CurrencyRewardRoller();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(definition),
            new EmptyDungeonRunRepository(run),
            new ItemBaseRepository(db),
            rewardRoller,
            new CapturingDungeonPendingRewardWriter(),
            new InventoryItemFactory(),
            new FixedDungeonMasteryService(previousLevel, awardedLevel, alreadyAwarded),
            new RecordingPublisher());

        await applier.ApplyAsync(run, CancellationToken.None);

        Assert.Single(rewardRoller.RewardTableIds);
        Assert.Equal(11, run.PendingCinders);
        Assert.Equal(11 + expectedMasterySoulstones, run.PendingSoulstones);
    }

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
            new NoOpDungeonMasteryService(),
            new RecordingPublisher());

        await applier.ApplyAsync(new() { Id = Guid.NewGuid(), DungeonDefinitionId = "dungeon.tier_1" }, CancellationToken.None);

        var batch = Assert.Single(pendingRewards.Batches, x => x.Source == "Grade I Monster Cores");
        var reward = Assert.Single(batch.Loot);
        Assert.Equal("item.monster_core.lesser", reward.ItemInstance.ItemBaseId);
        Assert.InRange(reward.Quantity, 3, 6);
    }

    [Fact]
    public async Task Dungeon_resources_publish_aggregated_treasure_prophecy_progress()
    {
        await using var db = CreateDb();
        db.ItemBases.AddRange(
            new ItemBase
            {
                Id = "completion_item",
                Name = "Completion Item",
                ItemType = ItemType.Resource,
                Stackable = true
            },
            new ItemBase
            {
                Id = "item.monster_core.lesser",
                Name = "Lesser Monster Core",
                ItemType = ItemType.Resource,
                Stackable = true
            });
        await db.SaveChangesAsync();

        var run = new DungeonRun
        {
            Id = Guid.NewGuid(),
            CharacterId = Guid.NewGuid(),
            DungeonDefinitionId = "completion_prophecy_dungeon",
            CompletedAt = DateTimeOffset.UtcNow
        };
        var definition = new DungeonDefinition
        {
            Id = run.DungeonDefinitionId,
            Tier = 1,
            RewardTable = new DungeonRewardTable
            {
                CompletionRewards =
                [
                    new DungeonRewardGrant
                    {
                        ItemId = "completion_item",
                        Chance = 1,
                        MinAmount = 2,
                        MaxAmount = 2
                    }
                ]
            }
        };
        var pendingRewards = new CapturingDungeonPendingRewardWriter();
        var publisher = new RecordingPublisher();
        var applier = new DungeonCompletionRewardApplier(
            new SingleDungeonDefinitions(definition),
            new EmptyDungeonRunRepository(run),
            new ItemBaseRepository(db),
            new EmptyRewardRoller(),
            pendingRewards,
            new InventoryItemFactory(),
            new NoOpDungeonMasteryService(),
            publisher);

        await applier.ApplyAsync(run, CancellationToken.None);
        var loot = pendingRewards.Batches.SelectMany(x => x.Loot).ToArray();
        Assert.Equal(2, loot.Where(x => x.ItemInstance.ItemBaseId == "completion_item").Sum(x => x.Quantity));
        Assert.Contains(loot, x => x.ItemInstance.ItemBaseId == "item.monster_core.lesser");

        var expectedQuantity = pendingRewards.Batches
            .SelectMany(batch => batch.Loot)
            .Sum(item => Math.Max(1, item.Quantity));
        var notification = Assert.IsType<ProphecyProgressNotification>(
            Assert.Single(publisher.Notifications));
        Assert.Equal(run.CharacterId, notification.ProgressEvent.CharacterId);
        Assert.Equal(run.CompletedAt, notification.ProgressEvent.OccurredAt);
        Assert.Equal(ProphecyProgressKind.TreasureProgress, notification.ProgressEvent.Kind);
        Assert.Equal(expectedQuantity, notification.ProgressEvent.Amount);
        Assert.True(expectedQuantity >= 5);
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
            essenceResonance);
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

    private sealed class RecordingPublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Publish((object)notification, cancellationToken);
    }

    private sealed class EmptyDungeonRunRepository(
        DungeonRun? run = null,
        bool hasCompleted = false) : IDungeonRunRepository
    {
        public Task<bool> CreateDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> DeleteDungeonRunAsync(DungeonRun dungeonRun, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> AddPendingRewardAsync(DungeonRun dungeonRun, RunReward reward, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult<DungeonRun?>(null);
        public Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid dungeonId, CancellationToken cancellationToken) =>
            Task.FromResult(run?.Id == dungeonId ? run : null);
        public Task<bool> HasActiveDungeonRunAsync(Guid characterId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(Guid characterId, IReadOnlyCollection<string> dungeonDefinitionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DungeonCompletionRecord>>([]);
        public Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(IReadOnlyCollection<string> dungeonDefinitionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DungeonCompletionLeaderboardEntry>>([]);
        public Task<bool> HasCompletedDungeonAsync(Guid characterId, string dungeonDefinitionId, CancellationToken cancellationToken) => Task.FromResult(hasCompleted);
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

    private sealed class CapturingRewardRoller : IRewardRoller
    {
        public List<RewardRollContext> Contexts { get; } = [];

        public RewardRollResult Roll(string rewardTableId, RewardRollContext context)
        {
            Contexts.Add(context);
            return RewardRollResult.Empty;
        }

        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context)
        {
            Contexts.Add(context);
            return RewardRollResult.Empty;
        }
    }

    private sealed class FixedDungeonMasteryService(
        int previousLevel,
        int level,
        bool alreadyAwarded) : IDungeonMasteryService
    {
        public int CalculateLevel(long experience) => level;
        public int? GetExperienceRequiredForNextLevel(int currentLevel) => null;

        public Task<DungeonMasteryAwardResult> AwardCompletionAsync(
            DungeonRun run,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DungeonMasteryAwardResult(
                run.DungeonDefinitionId,
                0,
                0,
                previousLevel,
                level,
                0,
                [],
                alreadyAwarded));

        public Task<IReadOnlyDictionary<string, DungeonMasterySnapshot>> GetMasteryByDungeonAsync(
            Guid characterId,
            IReadOnlyCollection<string> dungeonDefinitionIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, DungeonMasterySnapshot>>(
                new Dictionary<string, DungeonMasterySnapshot>(StringComparer.OrdinalIgnoreCase));
    }

    private sealed class CurrencyRewardRoller : IRewardRoller
    {
        public List<string> RewardTableIds { get; } = [];

        public RewardRollResult Roll(string rewardTableId, RewardRollContext context)
        {
            RewardTableIds.Add(rewardTableId);
            return new RewardRollResult([], 10, 10, 0, []);
        }

        public RewardRollResult Roll(RewardTableDefinition table, RewardRollContext context) =>
            Roll(table.Id, context);
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

}
