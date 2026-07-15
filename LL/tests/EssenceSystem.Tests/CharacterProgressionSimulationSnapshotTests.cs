using System.Text.Json;

namespace EssenceSystem.Tests;

public sealed class CharacterProgressionSimulationSnapshotTests
{
    [Fact]
    public void Committed_progression_content_matches_the_balance_snapshot()
    {
        var snapshot = Simulate();

        Assert.Equal(10_800, snapshot.Areas[0].ExperiencePerHourAtFullWinRate, 1);
        Assert.Equal(14_693.3, snapshot.Areas.Single(x => x.Id == "region_01_area_06").ExperiencePerHourAtFullWinRate, 1);
        Assert.Equal(21_589.2, snapshot.Areas[^1].ExperiencePerHourAtFullWinRate, 1);

        var finalArea = snapshot.Areas[^1];
        Assert.Equal(15_112.5, finalArea.ExperiencePerHourAt70PercentWinRate, 1);
        Assert.Equal(18_350.9, finalArea.ExperiencePerHourAt85PercentWinRate, 1);
        Assert.Equal(23_748.2, finalArea.ExperiencePerHourWithCommonBonus, 1);
        Assert.Equal(440_420.7, snapshot.MaximumOfflineExperienceAt85PercentWinRate, 1);

        Assert.Equal(2.38, snapshot.CumulativeHoursByMilestone[10], 2);
        Assert.Equal(17.80, snapshot.CumulativeHoursByMilestone[20], 2);
        Assert.Equal(54.67, snapshot.CumulativeHoursByMilestone[30], 2);
        Assert.Equal(117.02, snapshot.CumulativeHoursByMilestone[40], 2);
        Assert.Equal(158.12, snapshot.CumulativeHoursByMilestone[45], 2);
        Assert.Equal(334.13, snapshot.CumulativeHoursByMilestone[60], 2);
        Assert.Equal(625.47, snapshot.CumulativeHoursByMilestone[75], 2);
        Assert.Equal(1_446.32, snapshot.CumulativeHoursByMilestone[100], 2);
        Assert.InRange(snapshot.CumulativeHoursByMilestone[100] / 24d, 60d, 61d);

        Assert.Equal(4_500, snapshot.MinimumProphecyWeekBasisPoints);
        Assert.Equal(6_500, snapshot.MaximumProphecyWeekBasisPoints);
        Assert.Empty(snapshot.Warnings);
    }

    private static ProgressionSimulationSnapshot Simulate()
    {
        var dataRoot = FindDataRoot();
        using var regionJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataRoot, "world", "regions.json")));
        using var curveJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataRoot, "progression", "character-experience.json")));
        using var areaExperienceJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataRoot, "progression", "area-experience.json")));
        using var rewardJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(dataRoot, "prophecies", "rewards.json")));

        var areaExperience = areaExperienceJson.RootElement.GetProperty("areaExperience");
        var baseAreaExperiencePerHour = areaExperience.GetProperty("baseExperiencePerHour").GetDouble();
        var difficultyTierMultiplier = areaExperience.GetProperty("difficultyTierMultiplier").GetDouble();

        var areas = regionJson.RootElement.GetProperty("regions")
            .EnumerateArray()
            .SelectMany(region => region.GetProperty("areas").EnumerateArray())
            .Where(area => area.GetProperty("difficultyTier").GetInt32() > 0)
            .Select(area => ProjectArea(area, baseAreaExperiencePerHour, difficultyTierMultiplier))
            .OrderBy(x => x.LevelRequirement)
            .ToList();

        var curve = curveJson.RootElement.GetProperty("characterLevelCurve");
        var baseExperience = curve.GetProperty("baseExperience").GetInt32();
        var linear = curve.GetProperty("linearExperiencePerLevel").GetInt32();
        var quadratic = curve.GetProperty("quadraticExperiencePerLevelSquared").GetInt32();
        var rounding = curve.GetProperty("roundingIncrement").GetInt32();

        var cumulativeHours = 0d;
        var milestoneHours = new Dictionary<int, double>();
        var milestones = new HashSet<int> { 10, 20, 30, 40, 45, 60, 75, 100 };
        for (var level = 1; level < 100; level++)
        {
            var raw = checked(baseExperience + linear * level + quadratic * level * level);
            var required = ((raw + rounding - 1) / rounding) * rounding;
            var bestAvailableArea = areas
                .Where(x => x.LevelRequirement <= level)
                .MaxBy(x => x.ExperiencePerHourAtFullWinRate)!;
            cumulativeHours += required / bestAvailableArea.ExperiencePerHourAtFullWinRate;

            if (milestones.Contains(level + 1))
            {
                milestoneHours[level + 1] = cumulativeHours;
            }
        }

        var profiles = rewardJson.RootElement.GetProperty("profiles")
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("id").GetString()!,
                x => x.GetProperty("characterExperience").GetProperty("nextLevelBasisPoints").GetInt32());

        var warnings = new List<string>();
        for (var index = 1; index < areas.Count; index++)
        {
            if (areas[index].ExperiencePerHourAtFullWinRate <
                areas[index - 1].ExperiencePerHourAtFullWinRate * difficultyTierMultiplier - 0.001d)
            {
                warnings.Add($"{areas[index].Id} does not follow the difficulty-tier XP curve.");
            }
        }

        return new ProgressionSimulationSnapshot(
            Areas: areas,
            CumulativeHoursByMilestone: milestoneHours,
            MinimumProphecyWeekBasisPoints: 5 * profiles["Daily.Common"] + profiles["Weekly.Uncommon"],
            MaximumProphecyWeekBasisPoints: 5 * profiles["Daily.Rare"] + profiles["Weekly.Epic"],
            MaximumOfflineExperienceAt85PercentWinRate: areas[^1].ExperiencePerHourAt85PercentWinRate * 24,
            Warnings: warnings);
    }

    private static AreaProjection ProjectArea(
        JsonElement area,
        double baseExperiencePerHour,
        double difficultyTierMultiplier)
    {
        var difficultyTier = area.GetProperty("difficultyTier").GetInt32();
        var fullRate = baseExperiencePerHour * Math.Pow(difficultyTierMultiplier, difficultyTier);

        return new AreaProjection(
            area.GetProperty("id").GetString()!,
            area.GetProperty("levelRequirement").GetInt32(),
            fullRate,
            fullRate * 0.70d,
            fullRate * 0.85d,
            fullRate * 1.10d);
    }

    private static string FindDataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidates = new[]
            {
                Path.Combine(directory.FullName, "src", "API", "API.LL", "Data"),
                Path.Combine(directory.FullName, "LL", "src", "API", "API.LL", "Data")
            };
            var match = candidates.FirstOrDefault(candidate =>
                File.Exists(Path.Combine(candidate, "progression", "character-experience.json")));
            if (match is not null)
            {
                return match;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate API.LL progression data.");
    }

    private sealed record ProgressionSimulationSnapshot(
        IReadOnlyList<AreaProjection> Areas,
        IReadOnlyDictionary<int, double> CumulativeHoursByMilestone,
        int MinimumProphecyWeekBasisPoints,
        int MaximumProphecyWeekBasisPoints,
        double MaximumOfflineExperienceAt85PercentWinRate,
        IReadOnlyList<string> Warnings);

    private sealed record AreaProjection(
        string Id,
        int LevelRequirement,
        double ExperiencePerHourAtFullWinRate,
        double ExperiencePerHourAt70PercentWinRate,
        double ExperiencePerHourAt85PercentWinRate,
        double ExperiencePerHourWithCommonBonus);
}
