using Application.Interfaces.Services.LL.Regions;

namespace Services.LL.Regions;

public sealed class RegionAreaBalanceAnalyzer : IRegionAreaBalanceAnalyzer
{
    private const int MinimumAcceptedWinRateBasisPoints = 8_000;
    private const int MaximumAcceptedWinRateBasisPoints = 9_000;
    private const int MinimumProfileWinRateBasisPoints = 7_500;
    private const int MaximumEncountersPerProfile = 250;

    private readonly IAreaCombatSimulator _simulator;
    private readonly IRegionCreatureScalingProvider _scaling;

    public RegionAreaBalanceAnalyzer(
        IAreaCombatSimulator simulator,
        IRegionCreatureScalingProvider scaling)
    {
        _simulator = simulator;
        _scaling = scaling;
    }

    public async Task<RegionAreaBalanceReport> AnalyzeAsync(
        RegionAreaBalanceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var options = await _simulator.GetOptionsAsync(cancellationToken);
        var areas = options.Areas
            .Where(x => x.RegionKey.Equals(request.RegionKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.GlobalStep)
            .ToArray();
        if (areas.Length == 0)
            throw new KeyNotFoundException($"Region combat balance '{request.RegionKey}' was not found.");

        var encounters = Math.Clamp(request.EncountersPerProfile, 1, MaximumEncountersPerProfile);
        var seed = request.RandomSeed == 0 ? 91_007 : request.RandomSeed;
        var results = new List<RegionAreaBalanceResult>(areas.Length);

        foreach (var area in areas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var profileResults = new List<RegionAreaProfileBalanceResult>(options.Profiles.Count);
            var reports = new List<AreaSimulationReport>(options.Profiles.Count);
            foreach (var profile in options.Profiles)
            {
                var report = await _simulator.RunAsync(
                    new AreaSimulationRequest(
                        area.Id,
                        encounters,
                        unchecked(seed + area.GlobalStep * 104_729),
                        profile,
                        area.DefaultBuildId),
                    cancellationToken);
                reports.Add(report);
                profileResults.Add(new RegionAreaProfileBalanceResult(
                    profile,
                    report.WinRate,
                    report.AverageCombatTicks,
                    report.P95DamageTaken));
            }

            var averageWinRate = Math.Round(reports.Average(x => x.WinRate), 2);
            var lowestWinRate = reports.Min(x => x.WinRate);
            var averageWinRateBasisPoints = (int)Math.Round(averageWinRate * 100);
            var lowestWinRateBasisPoints = (int)Math.Round(lowestWinRate * 100);
            var status = lowestWinRateBasisPoints < MinimumProfileWinRateBasisPoints
                ? "Profile viability failed"
                : averageWinRateBasisPoints < MinimumAcceptedWinRateBasisPoints
                    ? "Too hard"
                    : averageWinRateBasisPoints > MaximumAcceptedWinRateBasisPoints
                        ? "Too easy"
                        : "In tolerance";
            var areaWinFraction = (decimal)averageWinRate / 100m;
            var reference = reports[0];
            results.Add(new RegionAreaBalanceResult(
                area.Id,
                area.Name,
                area.GlobalStep,
                area.LevelRequirement,
                area.DefaultBuildId,
                status,
                averageWinRate,
                lowestWinRate,
                decimal.Round(reference.TargetExperiencePerHour * areaWinFraction, 2),
                decimal.Round(reference.TargetCindersPerHour * areaWinFraction, 2),
                reference.Scaling,
                profileResults));
        }

        var catalog = _scaling.GetCatalog();
        var region = catalog.Regions.Single(x =>
            x.RegionKey.Equals(request.RegionKey, StringComparison.OrdinalIgnoreCase));
        var profileDefinition = catalog.Profiles.Single(x =>
            x.Id.Equals(region.ProfileId, StringComparison.OrdinalIgnoreCase));
        var warnings = ValidateSmoothness(results, profileDefinition.MaximumStepIncrease);
        warnings.AddRange(results
            .Where(x => x.Status != "In tolerance")
            .Select(x => $"{x.AreaName}: {x.Status} ({x.AverageWinRate:N2}% average, {x.LowestProfileWinRate:N2}% lowest profile)."));

        return new RegionAreaBalanceReport(
            region.RegionKey,
            catalog.Version,
            profileDefinition.TargetWinRateBasisPoints,
            encounters,
            warnings.All(x => !x.Contains("scaling", StringComparison.OrdinalIgnoreCase)),
            results.All(x => x.Status == "In tolerance"),
            warnings,
            results);
    }

    private static List<string> ValidateSmoothness(
        IReadOnlyList<RegionAreaBalanceResult> areas,
        double maximumStepIncrease)
    {
        var warnings = new List<string>();
        for (var index = 1; index < areas.Count; index++)
        {
            var previous = areas[index - 1];
            var current = areas[index];
            ValidateMetric("health", previous.Scaling.HealthMultiplier, current.Scaling.HealthMultiplier);
            ValidateMetric("offense", previous.Scaling.OffenseMultiplier, current.Scaling.OffenseMultiplier);
            ValidateMetric("defense", previous.Scaling.DefenseMultiplier, current.Scaling.DefenseMultiplier);

            void ValidateMetric(string name, double before, double after)
            {
                if (after < before)
                {
                    warnings.Add($"{current.AreaName} {name} scaling decreases from the previous step.");
                    return;
                }

                var increase = before <= 0 ? 0 : after / before - 1d;
                if (increase > maximumStepIncrease)
                    warnings.Add($"{current.AreaName} {name} scaling increases by {increase:P1}, above {maximumStepIncrease:P1}.");
            }
        }

        return warnings;
    }
}
