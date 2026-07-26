using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons;
using Domain.Models.Dungeons.Definitions;

namespace Services.LL.PowerRatings;

public static class DungeonPowerRecommendationDiagnostics
{
    private const decimal MaximumAdjacentDifficultyMultiplier = 4m;

    public static DungeonPowerRecommendationDiagnosticReport Analyze(
        IReadOnlyCollection<DungeonDefinition> dungeons,
        IReadOnlyDictionary<string, DungeonPowerRecommendation> recommendations)
    {
        var missingDungeonIds = dungeons
            .Where(dungeon => !recommendations.ContainsKey(dungeon.Id))
            .Select(dungeon => dungeon.Id)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var issues = new List<DungeonPowerRecommendationIssue>();
        var warnings = new List<DungeonPowerRecommendationIssue>();

        foreach (var dungeon in dungeons)
        {
            if (!recommendations.TryGetValue(dungeon.Id, out var recommendation))
            {
                continue;
            }

            foreach (var message in ValidateRecommendation(recommendation))
            {
                issues.Add(new DungeonPowerRecommendationIssue([dungeon.Id], message));
            }
        }

        foreach (var family in dungeons.GroupBy(
                     dungeon => DungeonDefinitionIdentity.GetFamilyId(dungeon.Id),
                     StringComparer.OrdinalIgnoreCase))
        {
            var ordered = family
                .OrderBy(dungeon => dungeon.Tier)
                .ThenBy(dungeon => dungeon.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (var index = 1; index < ordered.Count; index++)
            {
                var previous = ordered[index - 1];
                var current = ordered[index];
                if (!recommendations.TryGetValue(previous.Id, out var previousRecommendation) ||
                    !recommendations.TryGetValue(current.Id, out var currentRecommendation))
                {
                    continue;
                }

                if (currentRecommendation.RecommendedPartyPower <= previousRecommendation.RecommendedPartyPower)
                {
                    warnings.Add(new DungeonPowerRecommendationIssue(
                        [previous.Id, current.Id],
                        $"Recommended Power must increase with difficulty, but '{current.Id}' is " +
                        $"{currentRecommendation.RecommendedPartyPower} after " +
                        $"'{previous.Id}' at {previousRecommendation.RecommendedPartyPower}."));
                    continue;
                }

                if (currentRecommendation.RecommendedPartyPower >
                    previousRecommendation.RecommendedPartyPower * MaximumAdjacentDifficultyMultiplier)
                {
                    warnings.Add(new DungeonPowerRecommendationIssue(
                        [previous.Id, current.Id],
                        $"Recommended Power for '{current.Id}' is more than " +
                        $"{MaximumAdjacentDifficultyMultiplier:0.#}x the previous difficulty and is an outlier."));
                }
            }
        }

        return new DungeonPowerRecommendationDiagnosticReport(missingDungeonIds, issues, warnings);
    }

    public static IReadOnlyList<string> ValidateRecommendation(
        DungeonPowerRecommendation recommendation)
    {
        var issues = new List<string>();

        if (recommendation.RecommendedPartyPower <= 0)
        {
            issues.Add("Recommended Power must be greater than zero.");
        }

        if (recommendation.LowerRecommendedPower <= 0 ||
            recommendation.LowerRecommendedPower > recommendation.RecommendedPartyPower ||
            recommendation.UpperRecommendedPower < recommendation.RecommendedPartyPower)
        {
            issues.Add("Recommended Power must be inside a positive lower/upper range.");
        }

        if (recommendation.SimulationCount <= 0)
        {
            issues.Add("At least one simulation is required.");
        }

        if (recommendation.EstimatedRunDuration <= TimeSpan.Zero)
        {
            issues.Add("Estimated run duration must be greater than zero.");
        }

        if (recommendation.CanonicalPartyCompletionRates.Values.Any(rate => rate is < 0 or > 1))
        {
            issues.Add("Canonical completion rates must be between zero and one.");
        }

        return issues;
    }
}

public sealed record DungeonPowerRecommendationDiagnosticReport(
    IReadOnlyList<string> MissingDungeonIds,
    IReadOnlyList<DungeonPowerRecommendationIssue> Issues,
    IReadOnlyList<DungeonPowerRecommendationIssue> Warnings)
{
    public bool IsValid => MissingDungeonIds.Count == 0 && Issues.Count == 0;
}

public sealed record DungeonPowerRecommendationIssue(
    IReadOnlyList<string> DungeonIds,
    string Message);
