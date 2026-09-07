using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.Items;
using Common.Randomness;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Domain.Models.Dungeons.Runs;
using Domain.Models.Items;
using Domain.Models.Items.Equipments.Progression;
using Microsoft.Extensions.Options;
using Services.LL.Items;

namespace EssenceSystem.Tests;

public sealed class EquipmentAcquisitionTests
{
    [Theory]
    [InlineData(-1, 0.50)]
    [InlineData(0, 0.50)]
    [InlineData(1, 0.55)]
    [InlineData(2, 0.60)]
    [InlineData(3, 0.65)]
    [InlineData(4, 0.70)]
    [InlineData(5, 0.75)]
    [InlineData(6, 0.80)]
    [InlineData(7, 0.85)]
    [InlineData(8, 0.90)]
    [InlineData(9, 0.95)]
    [InlineData(10, 1.00)]
    [InlineData(11, 1.00)]
    public void Dungeon_chance_adds_five_percentage_points_per_mastery_and_caps_at_one(int mastery, double expected)
    {
        var root = ContentRoot();
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        var catalog = JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Path.Combine(root, "equipment-ordinary.v1.json"));
        Assert.All(catalog.Pools, pool => Assert.Equal(expected, pool.DungeonEquipment.DropChanceAtMastery(mastery), 12));
    }

    [Theory]
    [InlineData(0, 0.50)]
    [InlineData(5, 0.75)]
    [InlineData(10, 1.00)]
    public async Task Completions_apply_mastery_snapshot_to_equipment_chance(int mastery, double expectedChance)
    {
        var catalog = Catalog(0.5);
        var repository = new Runs();
        var service = new EquipmentAcquisitionService(catalog, new Dungeons(), repository,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }));
        const int sampleSize = 1000;
        for (var i = 0; i < sampleSize; i++)
        {
            var run = Run("goblin_mines");
            run.Id = StableRandom.Guid(["mastery-equipment-rate", i.ToString()]);
            run.State = new DungeonRunState { MasteryLevelAtStart = mastery };
            await service.CompleteAsync(run, false, default);
            await service.CompleteAsync(run, false, default);
        }

        if (mastery == 10) Assert.Equal(sampleSize, repository.Rewards.Count);
        else Assert.InRange(repository.Rewards.Count / (double)sampleSize, expectedChance - 0.04, expectedChance + 0.04);
    }

    [Theory]
    [InlineData(1, "goblin_mines")]
    [InlineData(2, "tangled_cave")]
    public async Task Completed_dungeons_drop_random_equipment_at_region_tier_and_dungeon_rank(
        int region, string dungeonId)
    {
        var catalog = Catalog(1, EquipmentRarity.Epic, ItemQuality.Fine);
        var repository = new Runs();
        var service = new EquipmentAcquisitionService(catalog, new Dungeons(), repository,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }));
        var run = Run(dungeonId);

        await service.CompleteAsync(run, firstCompletion: false, CancellationToken.None);

        var reward = Assert.Single(repository.Rewards);
        var equipment = Assert.IsType<EquipmentData>(reward.ProgressionData);
        Assert.Equal(EquipmentRarity.Epic, equipment.Rarity);
        Assert.Equal(ItemQuality.Fine, equipment.Quality);
        Assert.InRange(equipment.AttributeRollMultiplier, 0.95d, 1.05d);
        Assert.Equal(region, equipment.State.Tier);
        Assert.Equal(1, equipment.State.Rank);
        Assert.Equal(EquipmentAwardKind.RandomDiscovery, equipment.State.Provenance.Kind);
        Assert.Equal(EquipmentOwnershipKind.UnboundPersonal, equipment.State.Ownership.Kind);
        Assert.Null(equipment.State.ActiveStyleId);
    }

    [Fact]
    public async Task Dungeon_drop_is_deterministic_and_completion_retries_are_idempotent()
    {
        var repository = new Runs();
        var service = new EquipmentAcquisitionService(Catalog(1, EquipmentRarity.Legacy), new Dungeons(), repository,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }));
        var run = Run("great_tree");

        await service.CompleteAsync(run, false, CancellationToken.None);
        var first = Assert.Single(repository.Rewards).ProgressionData!.Serialize();
        await service.CompleteAsync(run, false, CancellationToken.None);

        Assert.Single(repository.Rewards);
        Assert.Equal(first, repository.Rewards[0].ProgressionData!.Serialize());
    }

    [Theory]
    [InlineData(false, DungeonRunStatus.Completed)]
    [InlineData(true, DungeonRunStatus.Active)]
    public async Task Disabled_or_incomplete_dungeons_do_not_drop_equipment(bool enabled, DungeonRunStatus status)
    {
        var repository = new Runs();
        var service = new EquipmentAcquisitionService(Catalog(1, EquipmentRarity.Common), new Dungeons(), repository,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = enabled }));
        var run = Run("goblin_mines");
        run.Status = status;

        await service.CompleteAsync(run, false, CancellationToken.None);

        Assert.Empty(repository.Rewards);
    }

    [Fact]
    public async Task Blueprint_guarantee_is_independent_of_equipment_and_completion_retry_safe()
    {
        var equipment = Catalog(double.Epsilon, EquipmentRarity.Common);
        var authored = JsonEquipmentBlueprintCatalog.Load(Path.Combine(ContentRoot(), "equipment-blueprints.v1.json"), equipment.Equipment);
        var blueprints = new EquipmentBlueprintCatalog
        {
            Blueprints = authored.Blueprints, Sources = authored.Sources, DropChance = 0, GuaranteeCompletions = 4
        };
        var progress = new BlueprintProgressRepository();
        var runs = new Runs();
        var service = new EquipmentAcquisitionService(equipment, new Dungeons(), runs,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }), blueprints, progress);
        DungeonRun? first = null;
        for (var completion = 1; completion <= 4; completion++)
        {
            var run = Run("goblin_mines");
            run.Id = Guid.NewGuid();
            first ??= run;
            await service.CompleteAsync(run, false, default);
            await service.CompleteAsync(run, false, default);
            Assert.Equal(completion % 4, progress.Progress.Misses);
            Assert.Equal(completion == 4 ? 1 : 0, run.PendingRewards.Count);
        }
        // Even an old completed run cannot advance or reset the current counter again.
        await service.CompleteAsync(first!, false, default);
        var reward = Assert.Single(runs.Rewards);
        Assert.Contains(reward.ItemId, new[] { "item.blueprint_fury", "item.blueprint_phoenix" });
        Assert.Null(reward.ProgressionData);
        Assert.Equal(0, progress.Progress.Misses);
        var failed = Run("goblin_mines");
        failed.Status = DungeonRunStatus.Active;
        await service.CompleteAsync(failed, false, default);
        Assert.Equal(0, progress.Progress.Misses);
    }

    [Theory]
    [InlineData("goblin_mines", 1, DungeonGrade.GradeI, EquipmentRarity.Uncommon, EquipmentRarity.Rare, EquipmentRarity.Epic)]
    [InlineData("goblin_mines", 1, DungeonGrade.GradeII, EquipmentRarity.Rare, EquipmentRarity.Epic, EquipmentRarity.Unique)]
    [InlineData("goblin_mines", 1, DungeonGrade.GradeIII, EquipmentRarity.Epic, EquipmentRarity.Unique, EquipmentRarity.Legendary)]
    [InlineData("tangled_cave", 2, DungeonGrade.GradeI, EquipmentRarity.Uncommon, EquipmentRarity.Rare, EquipmentRarity.Epic)]
    [InlineData("tangled_cave", 2, DungeonGrade.GradeII, EquipmentRarity.Rare, EquipmentRarity.Epic, EquipmentRarity.Unique)]
    [InlineData("tangled_cave", 2, DungeonGrade.GradeIII, EquipmentRarity.Epic, EquipmentRarity.Unique, EquipmentRarity.Legendary)]
    public async Task Dungeon_difficulty_selects_the_rarity_range_for_base_and_variant_drops(
        string dungeonId, int tier, DungeonGrade grade,
        EquipmentRarity first, EquipmentRarity second, EquipmentRarity third)
    {
        var catalog = Catalog(1);
        var repository = new Runs();
        var blueprints = JsonEquipmentBlueprintCatalog.Load(
            Path.Combine(ContentRoot(), "equipment-blueprints.v1.json"), catalog.Equipment);
        var service = new EquipmentAcquisitionService(catalog, new Dungeons(grade), repository,
            Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }),
            blueprints, new BlueprintProgressRepository());

        for (var seed = 0; seed < 512; seed++)
        {
            var run = Run(dungeonId);
            run.Id = StableRandom.Guid(["dungeon-rarity-test", seed.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            run.Seed = seed;
            await service.CompleteAsync(run, false, CancellationToken.None);
            await service.CompleteAsync(run, false, CancellationToken.None);
        }

        var equipment = repository.Rewards.Where(reward => reward.ProgressionData is not null)
            .Select(reward => reward.ProgressionData!).ToArray();
        Assert.Equal(512, equipment.Length);
        Assert.Equal(new[] { first, second, third }, equipment.Select(item => item.Rarity).Distinct().Order());
        Assert.Contains(equipment, item => item.State.ActiveStyleId is null);
        Assert.Contains(equipment, item => item.State.ActiveStyleId is not null);
        Assert.All(equipment, item =>
        {
            Assert.Equal(tier, item.State.Tier);
            Assert.Equal(1, item.State.Rank);
            Assert.InRange(item.AttributeRollMultiplier, 0.95, 1.05);
        });
    }

    [Theory]
    [InlineData("goblin_mines", "fury", "phoenix")]
    [InlineData("forgotten_catacombs", "arcane", "endurance")]
    [InlineData("tangled_cave", "execution")]
    [InlineData("great_tree", "spirit")]
    public async Task Every_difficulty_drops_one_direct_blueprint_from_its_dungeon_pool(
        string dungeonId, params string[] blueprintNames)
    {
        var equipment = Catalog(double.Epsilon);
        var authored = JsonEquipmentBlueprintCatalog.Load(Path.Combine(ContentRoot(), "equipment-blueprints.v1.json"), equipment.Equipment);
        var blueprints = new EquipmentBlueprintCatalog
        {
            Blueprints = authored.Blueprints, Sources = authored.Sources, DropChance = 1
        };
        var expected = blueprintNames.Select(name => $"item.blueprint_{name}").Order().ToArray();
        foreach (var grade in Enum.GetValues<DungeonGrade>())
        {
            var rewards = new Runs();
            var service = new EquipmentAcquisitionService(equipment, new Dungeons(grade), rewards,
                Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }),
                blueprints, new BlueprintProgressRepository());
            for (var seed = 0; seed < 32; seed++)
            {
                var run = Run(dungeonId);
                run.Id = StableRandom.Guid(["direct-blueprint-test", seed.ToString()]);
                await service.CompleteAsync(run, false, default);
                await service.CompleteAsync(run, false, default);
                var reward = Assert.Single(run.PendingRewards);
                Assert.Contains(reward.ItemId, expected);
                Assert.StartsWith("Blueprint: ", reward.Name);
                Assert.Equal(1, reward.Quantity);
                Assert.Null(reward.ProgressionData);
                // Replaying from the same pre-completion state chooses the same blueprint.
                var replay = Run(dungeonId);
                replay.Id = run.Id;
                var replayService = new EquipmentAcquisitionService(equipment, new Dungeons(grade), new Runs(),
                    Options.Create(new EquipmentProgressionOptions { ProtectedAcquisitionEnabled = true }),
                    blueprints, new BlueprintProgressRepository());
                await replayService.CompleteAsync(replay, false, default);
                Assert.Equal(reward.ItemId, Assert.Single(replay.PendingRewards).ItemId);
            }
            Assert.Equal(32, rewards.Rewards.Count);
            Assert.Equal(expected, rewards.Rewards.Select(x => x.ItemId).Distinct().Order());
        }
    }

    private sealed class BlueprintProgressRepository : IEquipmentBlueprintRepository
    {
        public EquipmentBlueprintProgress Progress { get; } = new();
        public Task<EquipmentBlueprintProgress> LoadForCompletionAsync(Guid characterId, string familyId, CancellationToken ct) => Task.FromResult(Progress);
        public Task<IReadOnlyList<EquipmentBlueprintProgress>> GetProgressAsync(Guid characterId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<EquipmentBlueprintProgress>>([Progress]);
    }

    private static DungeonRun Run(string dungeonId) => new()
    {
        Id = Guid.Parse("7944f810-926d-4a89-92cf-e07691b18f76"),
        CharacterId = Guid.Parse("04633c4a-36c5-4c8b-977f-dc2cc1ee490c"),
        DungeonDefinitionId = dungeonId,
        Status = DungeonRunStatus.Completed,
        Seed = 173
    };

    private static CombatAcquisitionCatalog Catalog(
        double chance, EquipmentRarity? rarity = null, ItemQuality? quality = null)
    {
        var root = ContentRoot();
        var equipment = JsonStarterEquipmentCatalog.Load(Path.Combine(root, "equipment-starters.v1.json"));
        var source = JsonStarterEquipmentCatalog.LoadOrdinary(equipment, Path.Combine(root, "equipment-ordinary.v1.json"));
        return new CombatAcquisitionCatalog(equipment, source.Pools.Select(rules => rules with
        {
            DungeonEquipment = rules.DungeonEquipment with
            {
                DropChance = chance,
                Rarities = rarity.HasValue
                    ? new DungeonEquipmentRarityWeights(Only(rarity.Value), Only(rarity.Value), Only(rarity.Value))
                    : rules.DungeonEquipment.Rarities,
                Qualities = quality.HasValue ? Only(quality.Value) : rules.DungeonEquipment.Qualities
            }
        }));
    }

    private static EquipmentRarityWeights Only(EquipmentRarity rarity) => new(
        rarity == EquipmentRarity.Common ? 1 : 0,
        rarity == EquipmentRarity.Uncommon ? 1 : 0,
        rarity == EquipmentRarity.Rare ? 1 : 0,
        rarity == EquipmentRarity.Epic ? 1 : 0,
        rarity == EquipmentRarity.Unique ? 1 : 0,
        rarity == EquipmentRarity.Legendary ? 1 : 0,
        rarity == EquipmentRarity.Legacy ? 1 : 0);

    private static EquipmentQualityWeights Only(ItemQuality quality) => new(
        quality == ItemQuality.Crude ? 1 : 0,
        quality == ItemQuality.Standard ? 1 : 0,
        quality == ItemQuality.Fine ? 1 : 0,
        quality == ItemQuality.Exceptional ? 1 : 0,
        quality == ItemQuality.Masterpiece ? 1 : 0);

    private static string ContentRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "LL/src/API/API.LL/Data/equipment");
            if (Directory.Exists(path)) return path;
        }
        throw new DirectoryNotFoundException();
    }

    private sealed class Dungeons(DungeonGrade grade = DungeonGrade.GradeI) : IDungeonDefinitions
    {
        private readonly DungeonDefinition[] _values =
        [
            new() { Id = "goblin_mines", Name = "Goblin Mines", SigilItemId = "sigil_goblin_mines", Region = 1, Tier = 1, Grade = grade },
            new() { Id = "forgotten_catacombs", Name = "Forgotten Catacombs", SigilItemId = "sigil_forgotten_catacombs", Region = 1, Tier = 1, Grade = grade },
            new() { Id = "tangled_cave", Name = "Tangled Cave", SigilItemId = "sigil_tangled_cave", Region = 2, Tier = 2, Grade = grade },
            new() { Id = "great_tree", Name = "Great Tree", SigilItemId = "sigil_great_tree", Region = 2, Tier = 2, Grade = grade }
        ];

        public DungeonDefinition GetByKey(string key) => _values.Single(x => x.Id == key);
        public IReadOnlyList<DungeonDefinition> GetAll() => _values;
    }

    private sealed class Runs : IDungeonRunRepository
    {
        public List<RunReward> Rewards { get; } = [];
        public Task<bool> AddPendingRewardAsync(DungeonRun run, RunReward reward, CancellationToken ct)
        {
            run.PendingRewards.Add(reward);
            Rewards.Add(reward);
            return Task.FromResult(true);
        }

        public Task<bool> CreateDungeonRunAsync(DungeonRun run, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteDungeonRunAsync(DungeonRun run, CancellationToken ct) => throw new NotSupportedException();
        public Task<DungeonRun?> GetDungeonRunByCharacterIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<DungeonRun?> GetDungeonRunByDungeonIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> HasActiveDungeonRunAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<DungeonCompletionRecord>> GetCompletionRecordsAsync(Guid id,
            IReadOnlyCollection<string> dungeonIds, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<DungeonCompletionLeaderboardEntry>> GetCompletionLeaderboardAsync(
            IReadOnlyCollection<string> dungeonIds, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> HasCompletedDungeonAsync(Guid id, string dungeonId, CancellationToken ct) => throw new NotSupportedException();
        public Task MarkDungeonCompletedAsync(Guid id, string dungeonId, DateTimeOffset completedAt, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> UpdateDungeonRunAsync(DungeonRun run, CancellationToken ct) => throw new NotSupportedException();
    }
}
