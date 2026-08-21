using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.Combat.Engine;

public static class StaggerCalibrationReportRenderer
{
    public const int CurrentSchemaVersion = 1;

    public static StaggerCalibrationArtifact CreateArtifact(
        StaggerCalibrationCatalog catalog,
        StaggerCalibrationReport report)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(report);
        var results = report.Results
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => result.CohortId, StringComparer.Ordinal)
            .ThenBy(result => result.ProfileId, StringComparer.Ordinal)
            .ToList();
        return new StaggerCalibrationArtifact(
            CurrentSchemaVersion,
            catalog.Version,
            catalog.EvaluationDurationTicks,
            new StaggerCalibrationSummary(
                catalog.Encounters.Count,
                results.Count,
                results.Sum(result => result.SampleCount),
                report.Exceptions.Count),
            catalog.Cohorts,
            catalog.Profiles,
            catalog.Encounters.Select(encounter => new StaggerCalibrationEncounterSummary(
                encounter.Id,
                encounter.ContentType,
                encounter.Name,
                encounter.Source,
                encounter.Definition.BaseThreshold,
                encounter.Definition.ReferenceParticipantCount,
                encounter.Definition.ParticipantExponent,
                encounter.Definition.BreakDurationTicks,
                encounter.Definition.RecoveryDurationTicks,
                encounter.Definition.DamageTakenBonusPercent,
                encounter.Definition.ThresholdGrowthPercentPerBreak,
                encounter.Definition.MaximumBreaks)).ToList(),
            results,
            report.Exceptions);
    }

    public static string RenderJson(StaggerCalibrationArtifact artifact) =>
        JsonSerializer.Serialize(artifact, CreateJsonOptions()) + Environment.NewLine;

    public static string RenderMarkdown(StaggerCalibrationArtifact artifact)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# LegendsLegacy Stagger Calibration Report");
        builder.AppendLine();
        builder.AppendLine("This deterministic mechanic-isolation report evaluates authored Stagger thresholds. It does not alter live content and intentionally excludes damage, survivability, and boss kill time.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Artifact schema/catalog: {artifact.SchemaVersion}/{artifact.CatalogVersion}");
        builder.AppendLine($"- Evaluation window: {artifact.EvaluationDurationTicks} ticks ({Number(artifact.EvaluationDurationTicks / (double)FastCombatEngine.TicksPerSecond)} seconds)");
        builder.AppendLine($"- Encounters: {artifact.Summary.EncounterCount}");
        builder.AppendLine($"- Result rows: {artifact.Summary.ResultCount}");
        builder.AppendLine($"- Deterministic samples: {artifact.Summary.SampleCount}");
        builder.AppendLine($"- Exceptions: {artifact.Summary.ExceptionCount}");
        builder.AppendLine();
        builder.AppendLine("Only the `reference` party-size cohort is checked against profile target bands. Party-size spread is checked across all cohorts.");

        builder.AppendLine();
        builder.AppendLine("## Control profiles");
        builder.AppendLine();
        builder.AppendLine("| Profile | Contributors | Power | Cadence | Success | Reference target | First break target |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (var profile in artifact.Profiles)
        {
            builder.AppendLine(
                $"| `{Escape(profile.Id)}` | {Percent(profile.ContributorShare)} | {profile.StaggerPower} | " +
                $"{Number(profile.IntervalTicks / (double)FastCombatEngine.TicksPerSecond)}s | {profile.SuccessPercent}% | " +
                $"{Number(profile.MinimumBreaks)}–{Number(profile.MaximumBreaks)} breaks | " +
                $"{TickRange(profile.MinimumFirstBreakTick, profile.MaximumFirstBreakTick)} |");
        }
        builder.AppendLine();
        builder.AppendLine("Profiles use representative authored values: 25-point light control, 35-point balanced control, and 45-point heavy control. `StaggerPower` currently does not scale with Essence ascension, so Essence tier is not a separate mechanic input in this report.");

        builder.AppendLine();
        builder.AppendLine("## Authored encounters");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Content | Base/reference | Exponent | Break/recovery | Damage bonus | Growth | Cap |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
        foreach (var encounter in artifact.Encounters)
        {
            builder.AppendLine(
                $"| `{Escape(encounter.Id)}` | {encounter.ContentType} | {encounter.BaseThreshold}/{encounter.ReferenceParticipantCount} | " +
                $"{Number(encounter.ParticipantExponent)} | {encounter.BreakDurationTicks}/{encounter.RecoveryDurationTicks} ticks | " +
                $"{encounter.DamageTakenBonusPercent}% | {encounter.ThresholdGrowthPercentPerBreak}% | " +
                $"{encounter.MaximumBreaks?.ToString(CultureInfo.InvariantCulture) ?? "—"} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Reference-cohort results");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Profile | Party/control | Threshold | Breaks | First break | Efficiency | Uptime | Cap rate |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var result in artifact.Results.Where(result => result.IsAssessmentCohort))
        {
            builder.AppendLine(
                $"| `{Escape(result.EncounterId)}` | `{Escape(result.ProfileId)}` | {result.ParticipantCount}/{result.ContributorCount} | " +
                $"{result.InitialThreshold} | {Number(result.AverageBreaks)} ({result.MinimumBreaks}–{result.MaximumBreaks}) | " +
                $"{OptionalNumber(result.AverageFirstBreakTick)} | {Percent100(result.AverageContributionEfficiencyPercent)} | " +
                $"{Percent100(result.AverageStaggerUptimePercent)} | {Percent(result.BreakCapRate)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Party-size sensitivity");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Profile | Undersized | Reference | Oversized | Break spread |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|");
        foreach (var group in artifact.Results.GroupBy(
                     result => $"{result.EncounterId}|{result.ProfileId}",
                     StringComparer.OrdinalIgnoreCase))
        {
            var first = group.First();
            var byCohort = group.ToDictionary(result => result.CohortId, StringComparer.OrdinalIgnoreCase);
            builder.AppendLine(
                $"| `{Escape(first.EncounterId)}` | `{Escape(first.ProfileId)}` | " +
                $"{CohortResult(byCohort, "undersized")} | {CohortResult(byCohort, "reference")} | " +
                $"{CohortResult(byCohort, "oversized")} | " +
                $"{Number(group.Max(result => result.AverageBreaks) - group.Min(result => result.AverageBreaks))} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Exceptions");
        builder.AppendLine();
        if (artifact.Exceptions.Count == 0)
        {
            builder.AppendLine("No Stagger calibration metric is outside its target band.");
        }
        else
        {
            builder.AppendLine("| Encounter | Cohort | Profile | Metric | Actual | Expected |");
            builder.AppendLine("|---|---|---|---|---:|---:|");
            foreach (var exception in artifact.Exceptions)
            {
                builder.AppendLine(
                    $"| `{Escape(exception.EncounterId)}` | `{Escape(exception.CohortId)}` | `{Escape(exception.ProfileId)}` | " +
                    $"{Escape(exception.Metric)} | {Number(exception.Actual)} | {Number(exception.Minimum)}–{Number(exception.Maximum)} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("1. Tune `BaseThreshold` when every profile is consistently early or late for one encounter.");
        builder.AppendLine("2. Tune `ParticipantExponent` only when party-size break spread exceeds one break.");
        builder.AppendLine("3. Tune recovery or contribution cadence when efficiency collapses while break counts remain high.");
        builder.AppendLine("4. Validate final candidates in full encounter simulations before changing production content.");
        return builder.ToString();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string CohortResult(
        IReadOnlyDictionary<string, StaggerCalibrationResult> results,
        string cohortId) =>
        results.TryGetValue(cohortId, out var result)
            ? $"{result.ParticipantCount}p / {Number(result.AverageBreaks)}"
            : "—";

    private static string TickRange(int? minimum, int? maximum) =>
        minimum.HasValue || maximum.HasValue
            ? $"{minimum?.ToString(CultureInfo.InvariantCulture) ?? "0"}–{maximum?.ToString(CultureInfo.InvariantCulture) ?? "∞"} ticks"
            : "—";
    private static string OptionalNumber(double? value) => value.HasValue ? Number(value.Value) : "—";
    private static string Number(double value) => double.IsPositiveInfinity(value)
        ? "∞"
        : value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Percent(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
    private static string Percent100(double value) => (value / 100d).ToString("P1", CultureInfo.InvariantCulture);
    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}

public sealed record StaggerCalibrationArtifact(
    int SchemaVersion,
    int CatalogVersion,
    int EvaluationDurationTicks,
    StaggerCalibrationSummary Summary,
    IReadOnlyList<StaggerCalibrationParticipantCohort> Cohorts,
    IReadOnlyList<StaggerCalibrationControlProfile> Profiles,
    IReadOnlyList<StaggerCalibrationEncounterSummary> Encounters,
    IReadOnlyList<StaggerCalibrationResult> Results,
    IReadOnlyList<StaggerCalibrationException> Exceptions);

public sealed record StaggerCalibrationSummary(
    int EncounterCount,
    int ResultCount,
    int SampleCount,
    int ExceptionCount);

public sealed record StaggerCalibrationEncounterSummary(
    string Id,
    StaggerCalibrationContentType ContentType,
    string Name,
    string Source,
    int BaseThreshold,
    int ReferenceParticipantCount,
    double ParticipantExponent,
    int BreakDurationTicks,
    int RecoveryDurationTicks,
    int DamageTakenBonusPercent,
    int ThresholdGrowthPercentPerBreak,
    int? MaximumBreaks);
