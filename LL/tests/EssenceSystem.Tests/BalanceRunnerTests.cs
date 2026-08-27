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

        var first = runner.Run(new BalanceRunRequest(8471, "test-commit"));
        var replay = runner.Run(new BalanceRunRequest(8471, "test-commit"));

        Assert.Equal(first.Metadata.Seed, replay.Metadata.Seed);
        Assert.Equal(first.Content, replay.Content);
        Assert.Equal(first.Simulation, replay.Simulation);
        Assert.Equivalent(first.GearPackages, replay.GearPackages, strict: true);
        Assert.NotEqual(first.Metadata.RunId, replay.Metadata.RunId);
        Assert.True(first.Content.AbilityCount > 0);
        Assert.True(first.Content.EssenceCount > 1);
    }

    [Fact]
    public void Region_one_gear_packages_use_the_configured_floor_anchors()
    {
        var runner = ProductionBalanceComposition.Create(FindApiContentRoot());

        var report = runner.Run(new BalanceRunRequest(8471, "test-commit"));

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
            Assert.True(File.Exists(paths.HistoryJsonPath));
            Assert.True(File.Exists(paths.HistoryMarkdownPath));
            Assert.True(File.Exists(paths.HistoryGearPackagesJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestJsonPath),
                File.ReadAllText(paths.HistoryJsonPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestMarkdownPath),
                File.ReadAllText(paths.HistoryMarkdownPath));
            Assert.Equal(
                File.ReadAllText(paths.LatestGearPackagesJsonPath),
                File.ReadAllText(paths.HistoryGearPackagesJsonPath));

            using var json = JsonDocument.Parse(File.ReadAllText(paths.LatestJsonPath));
            Assert.Equal(1337, json.RootElement.GetProperty("metadata").GetProperty("seed").GetInt32());
            Assert.Contains("Deterministic Smoke Simulation", File.ReadAllText(paths.LatestMarkdownPath));
            Assert.Contains("Region 1 Gear Packages", File.ReadAllText(paths.LatestMarkdownPath));
            using var gearJson = JsonDocument.Parse(File.ReadAllText(paths.LatestGearPackagesJsonPath));
            Assert.Single(gearJson.RootElement.EnumerateArray());
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
                2,
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
            [CreateGearPackage()]);

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
