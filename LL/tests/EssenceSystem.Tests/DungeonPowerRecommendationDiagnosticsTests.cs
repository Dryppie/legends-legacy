using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;
using Services.LL.PowerRatings;

namespace EssenceSystem.Tests;

public sealed class DungeonPowerRecommendationDiagnosticsTests
{
    [Fact]
    public void Nine_authored_difficulties_accept_monotonic_recommendations()
    {
        var dungeons = CreateNineDifficulties();
        var recommendations = dungeons.ToDictionary(
            dungeon => dungeon.Id,
            dungeon => CreateRecommendation(
                recommendedPower: (dungeon.Id.StartsWith("goblin", StringComparison.Ordinal) ? 100 :
                    dungeon.Id.StartsWith("forgotten", StringComparison.Ordinal) ? 125 : 150) *
                dungeon.Tier),
            StringComparer.OrdinalIgnoreCase);

        var report = DungeonPowerRecommendationDiagnostics.Analyze(dungeons, recommendations);

        Assert.True(report.IsValid);
        Assert.Empty(report.MissingDungeonIds);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Diagnostics_reject_non_monotonic_and_obvious_outlier_recommendations()
    {
        var dungeons = new[]
        {
            CreateDungeon("goblin_mines", 1),
            CreateDungeon("goblin_mines_ii", 2),
            CreateDungeon("goblin_mines_iii", 3)
        };
        var recommendations = new Dictionary<string, DungeonPowerRecommendation>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["goblin_mines"] = CreateRecommendation(200),
            ["goblin_mines_ii"] = CreateRecommendation(190),
            ["goblin_mines_iii"] = CreateRecommendation(1_000)
        };

        var report = DungeonPowerRecommendationDiagnostics.Analyze(dungeons, recommendations);

        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, issue =>
            issue.Message.Contains("increase with difficulty", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(report.Issues, issue =>
            issue.Message.Contains("outlier", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Diagnostics_reject_invalid_simulation_outputs()
    {
        var recommendation = CreateRecommendation(200) with
        {
            LowerRecommendedPower = 250,
            SimulationCount = 0,
            EstimatedRunDuration = TimeSpan.Zero,
            CanonicalPartyCompletionRates = new Dictionary<string, decimal>
            {
                ["Balanced"] = 1.1m
            }
        };

        var issues = DungeonPowerRecommendationDiagnostics.ValidateRecommendation(recommendation);

        Assert.Equal(4, issues.Count);
    }

    private static DungeonDefinition[] CreateNineDifficulties() =>
    [
        CreateDungeon("goblin_mines", 1),
        CreateDungeon("goblin_mines_ii", 2),
        CreateDungeon("goblin_mines_iii", 3),
        CreateDungeon("forgotten_catacombs", 1),
        CreateDungeon("forgotten_catacombs_ii", 2),
        CreateDungeon("forgotten_catacombs_iii", 3),
        CreateDungeon("hives_abyss", 1),
        CreateDungeon("hives_abyss_ii", 2),
        CreateDungeon("hives_abyss_iii", 3)
    ];

    private static DungeonDefinition CreateDungeon(string id, int tier) => new()
    {
        Id = id,
        Name = id,
        SigilItemId = $"sigil_{DungeonDefinitionIdentity.GetFamilyId(id)}",
        Tier = tier,
        Grade = (DungeonGrade)tier
    };

    private static DungeonPowerRecommendation CreateRecommendation(int recommendedPower) => new(
        recommendedPower,
        Math.Max(1, recommendedPower - 20),
        recommendedPower + 20,
        new PowerRequirementProfile(0.6m, 0.4m, 0.5m, 0.5m, 0.3m, 0.2m, 0.7m, 0.4m),
        PowerRatingAlgorithm.Version,
        "content-hash",
        PowerRatingConfidence.Medium,
        PowerAnalysisState.Available,
        24,
        TimeSpan.FromMinutes(2),
        new Dictionary<string, decimal> { ["Balanced"] = 0.75m });
}
