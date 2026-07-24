using API.LL.HostedServices;
using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Persistence.LL;
using Persistence.LL.Repositories.Dungeons;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class DungeonPowerPersistenceTests
{
    [Fact]
    public async Task Repository_round_trips_and_replaces_a_recommendation()
    {
        await using var context = CreateDbContext();
        var repository = new DungeonPowerRecommendationRepository(
            context,
            NullLogger<DungeonPowerRecommendationRepository>.Instance);
        var original = CreatePersisted("goblin_mines", recommendedPower: 120);
        var replacement = CreatePersisted("goblin_mines", recommendedPower: 180);

        await repository.UpsertAsync(original, CancellationToken.None);
        await repository.UpsertAsync(replacement, CancellationToken.None);
        var loaded = await repository.GetAllAsync(CancellationToken.None);

        var saved = Assert.Single(loaded);
        Assert.Equal(replacement.Identity, saved.Identity);
        Assert.Equal(180, saved.Recommendation.RecommendedPartyPower);
        Assert.Equal(replacement.Recommendation.Requirements, saved.Recommendation.Requirements);
        Assert.Equal(
            replacement.Recommendation.CanonicalPartyCompletionRates,
            saved.Recommendation.CanonicalPartyCompletionRates);
    }

    [Fact]
    public async Task Disabled_calculation_returns_a_current_in_memory_recommendation()
    {
        var dungeon = CreateDungeon("persisted_dungeon");
        var store = new DungeonPowerRecommendationStore();
        var analyzer = CreateDisabledAnalyzer(dungeon, store);
        var identity = analyzer.GetCalibrationIdentity(dungeon.Id);
        var recommendation = CreateRecommendation(identity, 250);
        store.Set(dungeon.Id, recommendation);

        var result = await analyzer.AnalyzeDungeonAsync(
            dungeon.Id,
            DungeonTier.Normal,
            CancellationToken.None);

        Assert.Same(recommendation, result);
    }

    [Fact]
    public async Task Disabled_calculation_does_not_simulate_a_missing_recommendation()
    {
        var dungeon = CreateDungeon("missing_dungeon");
        var analyzer = CreateDisabledAnalyzer(dungeon, new DungeonPowerRecommendationStore());

        var result = await analyzer.AnalyzeDungeonAsync(
            dungeon.Id,
            DungeonTier.Normal,
            CancellationToken.None);

        Assert.Equal(PowerAnalysisState.CalculationFailed, result.State);
        Assert.Contains("disabled", result.StatusMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Startup_loads_a_current_database_recommendation_without_calculating()
    {
        var persisted = CreatePersisted("loaded_dungeon", 220);
        var analyzer = new FixedPowerAnalyzer(persisted.Identity, persisted.Recommendation);
        var repository = new FixedRecommendationRepository([persisted]);
        var store = new DungeonPowerRecommendationStore();
        await using var provider = CreateWorkerProvider(analyzer, repository, store);
        var worker = new DungeonPowerCalibrationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DungeonPowerCalibrationOptions { Enabled = false }),
            NullLogger<DungeonPowerCalibrationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await WaitForCalibrationAsync(store);

        Assert.True(store.TryGet(persisted.Identity.DungeonId, out var loaded));
        Assert.Equal(220, loaded.RecommendedPartyPower);
        Assert.Equal(0, analyzer.AnalysisCount);
        Assert.Empty(repository.Upserts);
    }

    [Fact]
    public async Task Startup_calculates_and_persists_a_missing_recommendation_when_enabled()
    {
        var persisted = CreatePersisted("calculated_dungeon", 320);
        var analyzer = new FixedPowerAnalyzer(persisted.Identity, persisted.Recommendation);
        var repository = new FixedRecommendationRepository([]);
        var store = new DungeonPowerRecommendationStore();
        await using var provider = CreateWorkerProvider(analyzer, repository, store);
        var worker = new DungeonPowerCalibrationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DungeonPowerCalibrationOptions { Enabled = true }),
            NullLogger<DungeonPowerCalibrationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await WaitForCalibrationAsync(store);

        Assert.Equal(1, analyzer.AnalysisCount);
        Assert.True(store.TryGet(persisted.Identity.DungeonId, out var loaded));
        Assert.Equal(320, loaded.RecommendedPartyPower);
        var saved = Assert.Single(repository.Upserts);
        Assert.Equal(persisted.Identity, saved.Identity);
        Assert.Equal(320, saved.Recommendation.RecommendedPartyPower);
    }

    [Fact]
    public async Task Startup_recalculates_and_replaces_a_stale_algorithm_recommendation()
    {
        var current = CreatePersisted("stale_dungeon", 410);
        var staleIdentity = current.Identity with
        {
            AlgorithmVersion = current.Identity.AlgorithmVersion - 1,
            CombatRulesVersion = current.Identity.CombatRulesVersion - 1
        };
        var stale = new PersistedDungeonPowerRecommendation(
            staleIdentity,
            CreateRecommendation(staleIdentity, 180),
            current.UpdatedAtUtc.AddDays(-1));
        var analyzer = new FixedPowerAnalyzer(current.Identity, current.Recommendation);
        var repository = new FixedRecommendationRepository([stale]);
        var store = new DungeonPowerRecommendationStore();
        await using var provider = CreateWorkerProvider(analyzer, repository, store);
        var worker = new DungeonPowerCalibrationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DungeonPowerCalibrationOptions { Enabled = true }),
            NullLogger<DungeonPowerCalibrationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await WaitForCalibrationAsync(store);

        Assert.Equal(1, analyzer.AnalysisCount);
        Assert.True(store.TryGet(current.Identity.DungeonId, out var loaded));
        Assert.Equal(410, loaded.RecommendedPartyPower);
        var saved = Assert.Single(repository.Upserts);
        Assert.Equal(PowerRatingAlgorithm.Version, saved.Identity.AlgorithmVersion);
        Assert.Equal(PowerRatingAlgorithm.CombatRulesVersion, saved.Identity.CombatRulesVersion);
        Assert.Equal(current.Identity, saved.Identity);
    }

    private static DungeonPowerAnalyzer CreateDisabledAnalyzer(
        DungeonDefinition dungeon,
        IDungeonPowerRecommendationStore store) => new(
        new FixedDungeonDefinitions(dungeon),
        null!,
        null!,
        null!,
        null!,
        store,
        Options.Create(new DungeonPowerCalibrationOptions { Enabled = false }),
        NullLogger<DungeonPowerAnalyzer>.Instance);

    private static PersistedDungeonPowerRecommendation CreatePersisted(
        string dungeonId,
        int recommendedPower)
    {
        var identity = new DungeonPowerCalibrationIdentity(
            dungeonId,
            1,
            "content-hash",
            PowerRatingAlgorithm.Version,
            PowerRatingAlgorithm.CombatRulesVersion,
            PowerRatingAlgorithm.BenchmarkDefinitionVersion,
            PowerRatingAlgorithm.RecommendationSeedSetVersion);
        return new PersistedDungeonPowerRecommendation(
            identity,
            CreateRecommendation(identity, recommendedPower),
            DateTimeOffset.Parse("2026-07-22T12:00:00Z"));
    }

    private static DungeonPowerRecommendation CreateRecommendation(
        DungeonPowerCalibrationIdentity identity,
        int recommendedPower) => new(
        recommendedPower,
        recommendedPower - 20,
        recommendedPower + 20,
        new PowerRequirementProfile(0.6m, 0.4m, 0.5m, 0.5m, 0.3m, 0.2m, 0.7m, 0.4m),
        identity.AlgorithmVersion,
        identity.DungeonContentHash,
        PowerRatingConfidence.Medium,
        PowerAnalysisState.Available,
        24,
        TimeSpan.FromMinutes(2),
        new Dictionary<string, decimal> { ["Balanced"] = 0.75m });

    private static DungeonDefinition CreateDungeon(string id) => new()
    {
        Id = id,
        Name = id,
        SigilItemId = "item.test_sigil",
        Tier = 1,
        Grade = DungeonGrade.GradeI,
        MinRooms = 1,
        MaxRooms = 1,
        Rooms = []
    };

    private static LLDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LLDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LLDbContext(options);
    }

    private static ServiceProvider CreateWorkerProvider(
        FixedPowerAnalyzer analyzer,
        FixedRecommendationRepository repository,
        DungeonPowerRecommendationStore store)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDungeonDefinitions>(
            new FixedDungeonDefinitions(CreateDungeon(analyzer.Identity.DungeonId)));
        services.AddSingleton<IDungeonPowerAnalyzer>(analyzer);
        services.AddSingleton<IDungeonPowerRecommendationRepository>(repository);
        services.AddSingleton<IDungeonPowerRecommendationStore>(store);
        return services.BuildServiceProvider();
    }

    private static async Task WaitForCalibrationAsync(IDungeonPowerRecommendationStore store)
    {
        for (var attempt = 0; attempt < 100 && !store.IsCalibrationComplete; attempt++)
            await Task.Delay(10);

        Assert.True(store.IsCalibrationComplete);
    }

    private sealed class FixedDungeonDefinitions(DungeonDefinition dungeon) : IDungeonDefinitions
    {
        public DungeonDefinition GetByKey(string key) =>
            string.Equals(key, dungeon.Id, StringComparison.OrdinalIgnoreCase)
                ? dungeon
                : throw new KeyNotFoundException(key);

        public IReadOnlyList<DungeonDefinition> GetAll() => [dungeon];
    }

    private sealed class FixedPowerAnalyzer(
        DungeonPowerCalibrationIdentity identity,
        DungeonPowerRecommendation recommendation) : IDungeonPowerAnalyzer
    {
        public DungeonPowerCalibrationIdentity Identity => identity;
        public int AnalysisCount { get; private set; }

        public DungeonPowerCalibrationIdentity GetCalibrationIdentity(string dungeonId) => identity;

        public Task<DungeonPowerRecommendation> AnalyzeDungeonAsync(
            string dungeonId,
            DungeonTier tier,
            CancellationToken cancellationToken)
        {
            AnalysisCount++;
            return Task.FromResult(recommendation);
        }
    }

    private sealed class FixedRecommendationRepository(
        IReadOnlyList<PersistedDungeonPowerRecommendation> recommendations)
        : IDungeonPowerRecommendationRepository
    {
        public List<PersistedDungeonPowerRecommendation> Upserts { get; } = [];

        public Task<IReadOnlyList<PersistedDungeonPowerRecommendation>> GetAllAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(recommendations);

        public Task UpsertAsync(
            PersistedDungeonPowerRecommendation recommendation,
            CancellationToken cancellationToken)
        {
            Upserts.Add(recommendation);
            return Task.CompletedTask;
        }
    }
}
