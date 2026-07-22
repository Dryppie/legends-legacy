using Application.Interfaces.Services.LL.Dungeons;
using Application.Interfaces.Services.LL.PowerRatings;
using Domain.Models.Dungeons.Definitions;

namespace Services.LL.PowerRatings;

public sealed class PowerAnalysisDiagnostics : IPowerAnalysisDiagnostics
{
    private readonly IDungeonDefinitions _dungeons;
    private readonly IDungeonPowerAnalyzer _analyzer;

    public PowerAnalysisDiagnostics(
        IDungeonDefinitions dungeons,
        IDungeonPowerAnalyzer analyzer)
    {
        _dungeons = dungeons;
        _analyzer = analyzer;
    }

    public async Task<IReadOnlyList<DungeonPowerDiagnostic>> AnalyzeAllDungeonsAsync(
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<DungeonPowerDiagnostic>();
        var previousByFamily = new Dictionary<string, DungeonPowerDiagnostic>(StringComparer.OrdinalIgnoreCase);

        foreach (var dungeon in _dungeons.GetAll()
                     .OrderBy(x => DungeonDefinitionIdentity.GetFamilyId(x.Id))
                     .ThenBy(x => x.Tier))
        {
            var recommendation = await _analyzer.AnalyzeDungeonAsync(
                dungeon.Id,
                dungeon.Tier.ToDungeonTier(),
                cancellationToken);
            var warnings = new List<string>();
            var errors = new List<string>();
            if (recommendation.State == PowerAnalysisState.CalculationFailed)
                errors.Add(recommendation.StatusMessage ?? "Recommendation could not be calculated.");

            var rates = recommendation.CanonicalPartyCompletionRates.Values.ToArray();
            if (rates.Length > 1 && rates.Max() - rates.Min() > 0.35m)
                warnings.Add("Canonical party profiles differ by more than 35 percentage points.");
            if (rates.Length > 0 && (rates.All(x => x < 0.45m) || rates.All(x => x > 0.95m)))
                warnings.Add("Canonical completion rates are outside the expected calibration range.");

            var diagnostic = new DungeonPowerDiagnostic(
                dungeon.Id,
                dungeon.Name,
                dungeon.Tier,
                recommendation,
                warnings,
                errors);
            var family = DungeonDefinitionIdentity.GetFamilyId(dungeon.Id);
            if (previousByFamily.TryGetValue(family, out var previous) &&
                recommendation.RecommendedPartyPower < previous.Recommendation.RecommendedPartyPower)
            {
                warnings.Add($"Recommendation is lower than preceding tier {previous.Tier}.");
            }

            previousByFamily[family] = diagnostic;
            diagnostics.Add(diagnostic);
        }

        return diagnostics;
    }
}
