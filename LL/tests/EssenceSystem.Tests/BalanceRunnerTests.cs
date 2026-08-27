using Domain.Models.Items;
using LegendsLegacy.Balance;
using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class BalanceRunnerTests
{
    [Fact]
    public void Production_smoke_simulation_is_repeatable_for_the_same_seed()
    {
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero));
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot(), timeProvider);

        var first = runner.Run(new BalanceRunRequest(8471, "test-commit", EssenceBuildsPerProfile: 3));
        var replay = runner.Run(new BalanceRunRequest(8471, "test-commit", EssenceBuildsPerProfile: 3));

        Assert.Equal(first.Metadata.Seed, replay.Metadata.Seed);
        Assert.Equal(first.Content, replay.Content);
        Assert.Equal(first.Simulation, replay.Simulation);
        Assert.Equivalent(first.GearPackages, replay.GearPackages, strict: true);
        Assert.Equivalent(first.EssenceBuilds, replay.EssenceBuilds, strict: true);
        Assert.Equivalent(first.Benchmarks, replay.Benchmarks, strict: true);
        Assert.NotEqual(first.Metadata.RunId, replay.Metadata.RunId);
        Assert.True(first.Content.AbilityCount > 0);
        Assert.True(first.Content.EssenceCount > 1);
    }

    [Fact]
    public void Region_one_gear_packages_use_the_configured_floor_anchors()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());

        var report = runner.Run(new BalanceRunRequest(8471, "test-commit", EssenceBuildsPerProfile: 1));

        Assert.Collection(
            report.GearPackages,
            floorOne => AssertGearPackage(
                floorOne,
                "T1_Rare_Exceptional_Balanced",
                "WorldTower.Region1.Floor1",
                Rarity.Rare),
            floorTen => AssertGearPackage(
                floorTen,
                "T1_Epic_Exceptional_Balanced",
                "WorldTower.Region1.Floor10",
                Rarity.Epic));
        Assert.True(
            report.GearPackages[1].CombatRating.RawOverall
            > report.GearPackages[0].CombatRating.RawOverall);
    }

    [Fact]
    public void Random_essence_profiles_are_seeded_unique_and_legal()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());

        var report = runner.Run(new BalanceRunRequest(8471, "test-commit", EssenceBuildsPerProfile: 5));

        Assert.Equal(15, report.EssenceBuilds.Count);
        Assert.Collection(
            report.EssenceBuilds.GroupBy(build => build.ProfileId).OrderBy(group => group.Key),
            group => AssertProfile(group, "E4_RANDOM", 4, 30, "T1_Rare_Exceptional_Balanced"),
            group => AssertProfile(group, "E5_RANDOM", 5, 40, "T1_Rare_Exceptional_Balanced"),
            group => AssertProfile(group, "E6_RANDOM", 6, 50, "T1_Epic_Exceptional_Balanced"));
        Assert.All(report.EssenceBuilds, build =>
        {
            Assert.Equal(build.SlotCount, build.Essences.Count);
            Assert.Equal(
                build.Essences.Count,
                build.Essences.Select(essence => essence.EssenceId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.Equal(
                build.Essences.Count,
                build.Essences.Select(essence => essence.SourceMonsterId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.True(build.Character.UnlockedEssenceSlots >= build.SlotCount);
        });
        Assert.Equal(5, report.Benchmarks.Scenarios.Count);
        Assert.Equal(
            [
                "pve.short-single-target",
                "pve.sustained-single-target",
                "pve.high-incoming-damage",
                "pve.three-targets",
                "pve.attrition"
            ],
            report.Benchmarks.Scenarios.Select(scenario => scenario.Id));
        Assert.Equal(report.EssenceBuilds.Count, report.Benchmarks.Builds.Count);
        Assert.All(report.Benchmarks.Builds, build =>
        {
            Assert.Equal(5, build.Components.Count);
            Assert.InRange(build.AggregateScore, 0, 100);
            Assert.Equal(
                Math.Round(build.Components.Average(component => component.Score), 2),
                build.AggregateScore);
            Assert.All(build.Components, component =>
            {
                Assert.InRange(component.Score, 0, 100);
                Assert.InRange(component.Metrics.RemainingHealthRatio, 0, 1);
            });
        });
        Assert.All(report.Benchmarks.Builds.GroupBy(build => build.ProfileId), profile =>
        {
            var ranked = profile.OrderBy(build => build.ProfileRank).ToArray();
            Assert.Equal(Enumerable.Range(1, 5), ranked.Select(build => build.ProfileRank));
            Assert.Equal(profile.Max(build => build.AggregateScore), ranked[0].AggregateScore);
        });
        Assert.Contains(
            report.Benchmarks.Builds.GroupBy(build => build.ProfileId),
            profile => profile.Select(build => build.AggregateScore).Distinct().Count() > 1);
    }

    [Fact]
    public void Report_writer_persists_latest_and_immutable_history_outputs()
    {
        var outputRoot = Path.Combine(
            Path.GetTempPath(),
            "legends-legacy-balance-tests",
            Guid.NewGuid().ToString("N"));
        try
        {
            var report = CreateReport();

            var paths = new BalanceReportWriter().Write(report, outputRoot);

            Assert.True(File.Exists(paths.LatestJsonPath));
            Assert.True(File.Exists(paths.LatestMarkdownPath));
            Assert.True(File.Exists(paths.LatestGearPackagesJsonPath));
            Assert.True(File.Exists(paths.LatestEssenceBuildsJsonPath));
            Assert.True(File.Exists(paths.LatestBenchmarksJsonPath));
            Assert.True(File.Exists(paths.HistoryJsonPath));
            Assert.True(File.Exists(paths.HistoryMarkdownPath));
            Assert.True(File.Exists(paths.HistoryGearPackagesJsonPath));
            Assert.True(File.Exists(paths.HistoryEssenceBuildsJsonPath));
            Assert.True(File.Exists(paths.HistoryBenchmarksJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestJsonPath),
                File.ReadAllText(paths.HistoryJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestMarkdownPath),
                File.ReadAllText(paths.HistoryMarkdownPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestGearPackagesJsonPath),
                File.ReadAllText(paths.HistoryGearPackagesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestEssenceBuildsJsonPath),
                File.ReadAllText(paths.HistoryEssenceBuildsJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestBenchmarksJsonPath),
                File.ReadAllText(paths.HistoryBenchmarksJsonPath));

            using var json = JsonDocument.Parse(File.ReadAllText(paths.LatestJsonPath));
            Assert.Equal(1337, json.RootElement.GetProperty("metadata").GetProperty("seed").GetInt32());
            Assert.Contains("Deterministic Smoke Simulation", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Region 1 Gear Packages", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("PvE Benchmark Performance", File.ReadAllText(paths.LatestMarkdownPath));
            using var gearJson = JsonDocument.Parse(File.ReadAllText(paths.LatestGearPackagesJsonPath));
            Assert.Single(gearJson.RootElement.EnumerateArray());
            using var essenceJson = JsonDocument.Parse(File.ReadAllText(paths.LatestEssenceBuildsJsonPath));
            Assert.Single(essenceJson.RootElement.EnumerateArray());
            using var benchmarkJson = JsonDocument.Parse(File.ReadAllText(paths.LatestBenchmarksJsonPath));
            Assert.Single(benchmarkJson.RootElement.GetProperty("builds").EnumerateArray());
            Assert.Throws<InvalidOperationException>(() =>
                new BalanceReportWriter().Write(report, outputRoot));
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Fact]
    public void Command_options_reject_unknown_arguments()
    {
        var exception = Assert.Throws<BalanceCommandException>(() =>
            BalanceCommandOptions.Parse(["--not-a-command"]));

        Assert.Contains("Unknown", exception.Message);
    }

    private static BalanceRunReport CreateReport() =>
        new(
            new BalanceRunMetadata(
                "20260827T120000000Z-12345678",
                new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero),
                1337,
                4,
                2,
                "1.0.0.0",
                "abcdef123456"),
            new BalanceContentSummary(10, 5, 2, 3),
            new BalanceSimulationSummary(
                "production-essence-smoke-1v1",
                "Friendly",
                "essence.friendly",
                "Hostile",
                "essence.hostile",
                "Victory",
                42,
                100,
                50,
                50,
                100),
            [CreateGearPackage()],
            [CreateEssenceBuild()],
            CreateBenchmarks());

    private static PveBenchmarkSuiteSnapshot CreateBenchmarks()
    {
        var scenarios = new[]
        {
            new PveBenchmarkScenarioSnapshot(
                "pve.short-single-target",
                "Short Single Target",
                300,
                1,
                "Burst and opening pressure")
        };
        var metrics = new PveBenchmarkMetricsSnapshot(
            "Draw",
            300,
            500,
            100,
            10,
            5,
            20,
            15,
            150,
            50,
            0,
            true,
            0.9);
        return new PveBenchmarkSuiteSnapshot(
            1,
            scenarios,
            [
                new PveBenchmarkBuildSnapshot(
                    "E4_RANDOM_001",
                    "E4_RANDOM",
                    1,
                    75,
                    [new PveBenchmarkComponentSnapshot(scenarios[0].Id, 1234, 75, metrics)])
            ]);
    }

    private static GearPackageSnapshot CreateGearPackage() =>
        new(
            new GearPackageDefinition(
                "T1_Rare_Exceptional_Balanced",
                "WorldTower.Region1.Floor1",
                1,
                Rarity.Rare,
                ItemQuality.Exceptional,
                GearPackageArchetype.Balanced),
            5,
            16,
            new GearPackageCombatRatingSnapshot(25, 16, 100, 1_000, 400, 400, 300, 300, 100, 0),
            new Dictionary<string, float> { ["Power"] = 50 },
            Array.Empty<GearPackageItemSnapshot>());

    private static EssenceBuildSnapshot CreateEssenceBuild() =>
        new(
            "E4_RANDOM_001",
            "E4_RANDOM",
            4,
            123,
            [
                new EssenceBuildSelection(
                    "essence.test_one",
                    "Test Essence One",
                    "monster.test_one",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_two",
                    "Test Essence Two",
                    "monster.test_two",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_three",
                    "Test Essence Three",
                    "monster.test_three",
                    Rarity.Common),
                new EssenceBuildSelection(
                    "essence.test_four",
                    "Test Essence Four",
                    "monster.test_four",
                    Rarity.Common)
            ],
            new EssenceBuildCharacterSnapshot(
                "T1_Rare_Exceptional_Balanced",
                30,
                4,
                new GearPackageCombatRatingSnapshot(25, 16, 100, 1_000, 400, 400, 300, 300, 100, 0)));

    private static void AssertProfile(
        IGrouping<string, EssenceBuildSnapshot> profile,
        string expectedProfile,
        int expectedSlots,
        int expectedLevel,
        string expectedGearPackage)
    {
        Assert.Equal(expectedProfile, profile.Key);
        Assert.Equal(5, profile.Count());
        Assert.Equal(5, profile.Select(build => string.Join('|', build.Essences.Select(x => x.EssenceId)))
            .Distinct(StringComparer.Ordinal)
            .Count());
        Assert.All(profile, build =>
        {
            Assert.Equal(expectedSlots, build.SlotCount);
            Assert.Equal(expectedLevel, build.Character.CharacterLevel);
            Assert.Equal(expectedGearPackage, build.Character.GearPackageId);
        });
    }

    private static void AssertGearPackage(
        GearPackageSnapshot package,
        string expectedId,
        string expectedAnchor,
        Rarity expectedRarity)
    {
        Assert.Equal(expectedId, package.Definition.Id);
        Assert.Equal(expectedAnchor, package.Definition.ProgressionAnchor);
        Assert.Equal(1, package.Definition.Tier);
        Assert.Equal(expectedRarity, package.Definition.Rarity);
        Assert.Equal(ItemQuality.Exceptional, package.Definition.Quality);
        Assert.Equal(GearPackageArchetype.Balanced, package.Definition.Archetype);
        Assert.Equal(7, package.Equipment.Count);
        Assert.All(package.Equipment, item =>
        {
            Assert.Equal(1, item.Tier);
            Assert.Equal(expectedRarity, item.Rarity);
            Assert.Equal(ItemQuality.Exceptional, item.Quality);
            Assert.NotEmpty(item.Modifiers);
        });
        Assert.NotEmpty(package.ProjectedAttributes);
        Assert.True(package.CombatRating.RawOverall > 0);
        Assert.Equal(package.CombatRating.RawOverall / 10, package.CombatRating.DisplayOverall);
    }

    private static string FindApiContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL")
            })
            {
                if (File.Exists(Path.Combine(candidate, "Data", "combat", "abilities.json")))
                    return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the production API.LL content root.");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
