using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.LL.Combat.Engine;

public static class EncounterCalibrationReportRenderer
{
    public const int CurrentSchemaVersion = 7;

    public static EncounterCalibrationArtifact CreateArtifact(
        EncounterCalibrationReport report,
        EncounterCalibrationCatalog catalog,
        EncounterCalibrationArtifact? baseline = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(catalog);

        var results = report.Results
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => result.GearEnvelopeId, StringComparer.Ordinal)
            .ThenBy(result => result.BuildFamilyId, StringComparer.Ordinal)
            .ThenBy(result => result.EssenceEnvelopeId, StringComparer.Ordinal)
            .ToList();
        var exceptions = report.Exceptions
            .OrderBy(exception => GetExceptionPriority(exception.Classification, exception.Metric))
            .ThenBy(exception => exception.EncounterId, StringComparer.Ordinal)
            .ThenBy(exception => exception.BuildFamilyId, StringComparer.Ordinal)
            .ThenBy(exception => exception.Metric, StringComparer.Ordinal)
            .ToList();
        var assessed = results.Where(result =>
                result.GearEnvelopeId.Equals(catalog.AssessmentGearEnvelopeId, StringComparison.OrdinalIgnoreCase)
                && result.EssenceEnvelopeId.Equals(catalog.AssessmentEssenceEnvelopeId, StringComparison.OrdinalIgnoreCase)
                && result.IncludedInRoleAssessment)
            .ToList();
        var content = results.GroupBy(result => result.ContentType)
            .OrderBy(group => group.Key)
            .Select(group => new EncounterCalibrationContentSummary(
                group.Key,
                group.Select(result => result.EncounterId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                group.Count(),
                group.Sum(result => result.SampleCount),
                assessed.Count(result => result.ContentType == group.Key),
                exceptions.Count(exception => group.Any(result =>
                    result.EncounterId.Equals(exception.EncounterId, StringComparison.OrdinalIgnoreCase)))))
            .ToList();
        var summary = new EncounterCalibrationArtifactSummary(
            results.Select(result => result.EncounterId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            results.Count,
            results.Sum(result => result.SampleCount),
            assessed.Count,
            exceptions.Count,
            content);
        var supportComparisons = CreateSupportComparisons(results, catalog.SupportAssessment);
        var artifact = new EncounterCalibrationArtifact(
            CurrentSchemaVersion,
            catalog.Version,
            catalog.AssessmentGearEnvelopeId,
            catalog.AssessmentEssenceEnvelopeId,
            summary,
            results,
            exceptions,
            null,
            supportComparisons);

        return baseline is null
            ? artifact
            : artifact with { Comparison = Compare(baseline, artifact) };
    }

    public static string RenderJson(EncounterCalibrationArtifact artifact) =>
        JsonSerializer.Serialize(artifact, CreateJsonOptions(writeIndented: true)) + Environment.NewLine;

    public static EncounterCalibrationArtifact ReadJson(string json) =>
        JsonSerializer.Deserialize<EncounterCalibrationArtifact>(
            json,
            CreateJsonOptions(writeIndented: false))
        ?? throw new InvalidOperationException("Could not deserialize the encounter calibration baseline.");

    public static string RenderMarkdown(EncounterCalibrationArtifact artifact)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# LegendsLegacy Encounter Calibration Report");
        builder.AppendLine();
        builder.AppendLine("This is an offline diagnostic. It does not alter runtime creature scaling.");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Artifact schema: {artifact.SchemaVersion}");
        builder.AppendLine($"- Encounter catalog: {artifact.CatalogVersion}");
        builder.AppendLine($"- Encounters: {artifact.Summary.EncounterCount}");
        builder.AppendLine($"- Aggregated results: {artifact.Summary.ResultCount}");
        builder.AppendLine($"- Seeded combat samples: {artifact.Summary.SeededSampleCount}");
        builder.AppendLine($"- Assessed cohort: `{artifact.AssessmentGearEnvelopeId}` gear + `{artifact.AssessmentEssenceEnvelopeId}` Essences, filtered by role eligibility");
        builder.AppendLine($"- Assessed results: {artifact.Summary.AssessedResultCount}");
        builder.AppendLine($"- Observational role results: {artifact.Results.Count(result => IsExpectedCohort(result, artifact) && !result.IncludedInRoleAssessment)}");
        builder.AppendLine($"- Multiplayer support comparisons: {artifact.SupportComparisons?.Count ?? 0}");
        builder.AppendLine($"- Exceptions: {artifact.Summary.ExceptionCount}");
        builder.AppendLine();
        builder.AppendLine("| Content | Encounters | Results | Seeded samples | Assessed | Exceptions |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (var content in artifact.Summary.Content)
        {
            builder.AppendLine(
                $"| {content.ContentType} | {content.EncounterCount} | {content.ResultCount} | {content.SeededSampleCount} | {content.AssessedResultCount} | {content.ExceptionCount} |");
        }

        AppendEncounterOverview(builder, artifact);
        AppendCompositionExpectations(builder, artifact);
        AppendHighSampleConfidence(builder, artifact);
        AppendExceptions(builder, artifact.Exceptions);
        AppendFailureDiagnostics(builder, artifact);
        AppendObservationalRoleDiagnostics(builder, artifact);
        AppendSupportComparisons(builder, artifact);
        AppendStaggerDiagnostics(builder, artifact);
        AppendComparison(builder, artifact.Comparison);
        AppendReviewGuidance(builder, artifact.Exceptions);
        return builder.ToString();
    }

    private static void AppendHighSampleConfidence(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var highSampleResults = artifact.Results.Where(result => result.SampleCount >= 10)
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => DisplayBuild(result), StringComparer.Ordinal)
            .ThenBy(result => result.GearEnvelopeId, StringComparer.Ordinal)
            .ThenBy(result => result.EssenceEnvelopeId, StringComparer.Ordinal)
            .ToList();
        if (highSampleResults.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## High-sample confidence");
        builder.AppendLine();
        builder.AppendLine("Intervals are two-sided 95% Wilson score intervals for the configured deterministic seed sample.");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Build/composition | Gear | Essences | Samples | Win rate (95% CI) | Timeout rate (95% CI) |");
        builder.AppendLine("|---|---|---|---|---:|---:|---:|");
        foreach (var result in highSampleResults)
        {
            builder.AppendLine(
                $"| `{EscapeCell(result.EncounterId)}` | {EscapeCell(DisplayBuild(result))} | " +
                $"{EscapeCell(result.GearEnvelopeId)} | {EscapeCell(result.EssenceEnvelopeId)} | {result.SampleCount} | " +
                $"{Confidence(result.WinRate, result.WinRateConfidenceLower95, result.WinRateConfidenceUpper95)} | " +
                $"{Confidence(result.TimeoutRate, result.TimeoutRateConfidenceLower95, result.TimeoutRateConfidenceUpper95)} |");
        }
    }

    private static void AppendEncounterOverview(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var assessed = artifact.Results.Where(result =>
                IsExpectedCohort(result, artifact)
                && result.IncludedInRoleAssessment)
            .GroupBy(result => result.EncounterId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.First().ContentType)
            .ThenBy(group => group.Key, StringComparer.Ordinal);
        builder.AppendLine();
        builder.AppendLine("## Expected-cohort encounter overview");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Content | Role | Win-rate range | Avg duration | Survival range | Max timeout | Exceptions |");
        builder.AppendLine("|---|---|---|---:|---:|---:|---:|---:|");
        foreach (var group in assessed)
        {
            var first = group.First();
            builder.AppendLine(
                $"| `{EscapeCell(group.Key)}` | {first.ContentType} | {EscapeCell(first.DifficultyRole)} | " +
                $"{Percent(group.Min(result => result.WinRate))}–{Percent(group.Max(result => result.WinRate))} | " +
                $"{Number(group.Average(result => result.AverageDurationTicks))} | " +
                $"{Percent100(group.Min(result => result.AverageSurvivalResourcePercent))}–{Percent100(group.Max(result => result.AverageSurvivalResourcePercent))} | " +
                $"{Percent(group.Max(result => result.TimeoutRate))} | " +
                $"{artifact.Exceptions.Count(exception => exception.EncounterId.Equals(group.Key, StringComparison.OrdinalIgnoreCase))} |");
        }
    }

    private static void AppendExceptions(
        StringBuilder builder,
        IReadOnlyList<EncounterCalibrationException> exceptions)
    {
        builder.AppendLine();
        builder.AppendLine("## Exceptions");
        builder.AppendLine();
        if (exceptions.Count == 0)
        {
            builder.AppendLine("No expected-cohort results are outside the authored bands.");
            return;
        }

        builder.AppendLine("| Priority | Encounter | Build | Classification | Metric | Actual | Expected |");
        builder.AppendLine("|---:|---|---|---|---|---:|---:|");
        foreach (var exception in exceptions)
        {
            builder.AppendLine(
                $"| {GetExceptionPriority(exception.Classification, exception.Metric)} | " +
                $"`{EscapeCell(exception.EncounterId)}` | {EscapeCell(exception.BuildFamilyId)} | " +
                $"{EscapeCell(exception.Classification)} | {EscapeCell(exception.Metric)} | " +
                $"{FormatMetric(exception.Metric, exception.Actual)} | " +
                $"{FormatRange(exception.Metric, exception.Minimum, exception.Maximum)} |");
        }
    }

    private static void AppendCompositionExpectations(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var results = artifact.Results.Where(result =>
                IsExpectedCohort(result, artifact)
                && !string.IsNullOrWhiteSpace(result.PartyCompositionId))
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => result.PartyCompositionId, StringComparer.Ordinal)
            .ToList();
        if (results.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Authored composition expectations");
        builder.AppendLine();
        builder.AppendLine("Expected compositions use the encounter's complete target band. Alternatives must remain viable, countered and challenge compositions are checked for excessive success, and observational compositions produce telemetry without exceptions.");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Composition | Intent | Win rate | Timeout | Duration | Exceptions |");
        builder.AppendLine("|---|---|---|---:|---:|---:|---:|");
        foreach (var result in results)
        {
            builder.AppendLine(
                $"| `{EscapeCell(result.EncounterId)}` | {EscapeCell(result.PartyCompositionId)} | " +
                $"{result.CompositionExpectation} | {Percent(result.WinRate)} | {Percent(result.TimeoutRate)} | " +
                $"{Number(result.AverageDurationTicks)} | " +
                $"{artifact.Exceptions.Count(exception => exception.EncounterId.Equals(result.EncounterId, StringComparison.OrdinalIgnoreCase) && exception.BuildFamilyId.Equals(result.BuildFamilyId, StringComparison.OrdinalIgnoreCase))} |");
        }
    }

    private static void AppendFailureDiagnostics(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var failures = artifact.Results.Where(result =>
                IsExpectedCohort(result, artifact)
                && result.IncludedInRoleAssessment
                && (result.FriendlyDeathRate > 0 || result.TimeoutRate > 0))
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => DisplayBuild(result), StringComparer.Ordinal)
            .ToList();
        builder.AppendLine();
        builder.AppendLine("## Failure diagnostics");
        builder.AppendLine();
        if (failures.Count == 0)
        {
            builder.AppendLine("No expected-cohort sample recorded a friendly death or timeout.");
            return;
        }

        builder.AppendLine("| Encounter | Build/composition | Death rate | First death | Enemy HP on timeout | Friendly basic / ability | Enemy basic / ability | Healing | Regen overheal | Unused barrier | Stuns/min | Top friendly ability | Top enemy ability |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (var result in failures)
        {
            builder.AppendLine(
                $"| `{EscapeCell(result.EncounterId)}` | {EscapeCell(DisplayBuild(result))} | " +
                $"{Percent(result.FriendlyDeathRate)} | {OptionalNumber(result.AverageFirstFriendlyDeathTick)} | " +
                $"{OptionalPercent100(result.AverageEnemyHealthRemainingOnTimeoutPercent)} | " +
                $"{Number(result.AverageFriendlyBasicAttackDamage)} / {Number(result.AverageFriendlyAbilityDamage)} | " +
                $"{Number(result.AverageEnemyBasicAttackDamage)} / {Number(result.AverageEnemyAbilityDamage)} | " +
                $"{Number(result.AverageHealingDone)} | {Number(result.AverageHealthRegenerationOverhealed)} | " +
                $"{Percent100(result.AverageUnusedBarrierPercent)} | {Number(result.AverageStunsPerMinute)} | " +
                $"{Ability(result.TopFriendlyAbilityName, result.AverageTopFriendlyAbilityDamage)} | " +
                $"{Ability(result.TopEnemyAbilityName, result.AverageTopEnemyAbilityDamage)} |");
        }
    }

    private static void AppendObservationalRoleDiagnostics(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var observations = artifact.Results.Where(result =>
                IsExpectedCohort(result, artifact)
                && !result.IncludedInRoleAssessment)
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => DisplayBuild(result), StringComparer.Ordinal)
            .ToList();
        if (observations.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Observational role diagnostics");
        builder.AppendLine();
        builder.AppendLine("These expected-cohort rows remain simulated, but their specialized role is not judged by the encounter's completion bands and they do not contribute to build-spread exceptions.");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Build | Win rate | Timeout | Duration | Death rate | Survival | Healing | Barrier | Regen overheal | Enemy HP on timeout |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var result in observations)
        {
            builder.AppendLine(
                $"| `{EscapeCell(result.EncounterId)}` | {EscapeCell(DisplayBuild(result))} | " +
                $"{Percent(result.WinRate)} | {Percent(result.TimeoutRate)} | {Number(result.AverageDurationTicks)} | " +
                $"{Percent(result.FriendlyDeathRate)} | {Percent100(result.AverageSurvivalResourcePercent)} | " +
                $"{Number(result.AverageHealingDone)} | {Number(result.AverageBarrierGenerated)} | " +
                $"{Number(result.AverageHealthRegenerationOverhealed)} | " +
                $"{OptionalPercent100(result.AverageEnemyHealthRemainingOnTimeoutPercent)} |");
        }
    }

    private static void AppendStaggerDiagnostics(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var results = artifact.Results.Where(result =>
                result.StaggerEnabled
                && IsExpectedCohort(result, artifact)
                && result.IncludedInRoleAssessment)
            .OrderBy(result => result.ContentType)
            .ThenBy(result => result.EncounterId, StringComparer.Ordinal)
            .ThenBy(result => DisplayBuild(result), StringComparer.Ordinal)
            .ToList();
        if (results.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Stagger diagnostics");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Build/composition | Players | Contribution | Breaks | First break | Uptime | Damage during Stagger | Cap rate |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var result in results)
        {
            builder.AppendLine(
                $"| `{EscapeCell(result.EncounterId)}` | {EscapeCell(DisplayBuild(result))} | {result.PlayerCount} | " +
                $"{Number(result.AverageStaggerContributed)} | {Number(result.AverageStaggerBreaks)} | " +
                $"{OptionalNumber(result.AverageFirstStaggerBreakTick)} | " +
                $"{Percent100(result.AverageStaggerUptimePercent)} | " +
                $"{Percent100(result.AverageDamageDuringStaggerPercent)} | " +
                $"{Percent(result.StaggerBreakCapRate)} |");
        }
    }

    private static void AppendSupportComparisons(
        StringBuilder builder,
        EncounterCalibrationArtifact artifact)
    {
        var comparisons = (artifact.SupportComparisons ?? [])
            .Where(comparison =>
                comparison.GearEnvelopeId.Equals(
                    artifact.AssessmentGearEnvelopeId,
                    StringComparison.OrdinalIgnoreCase)
                && comparison.EssenceEnvelopeId.Equals(
                    artifact.AssessmentEssenceEnvelopeId,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(comparison => comparison.ContentType)
            .ThenBy(comparison => comparison.EncounterId, StringComparer.Ordinal)
            .ToList();
        if (comparisons.Count == 0)
            return;

        builder.AppendLine();
        builder.AppendLine("## Multiplayer support effectiveness");
        builder.AppendLine();
        builder.AppendLine("Each row compares the sustain-heavy composition with the balanced composition on identical gear, Essences, and deterministic seeds. Healing is effective Health restored; regeneration waste records only unused passive regeneration.");
        builder.AppendLine();
        builder.AppendLine("| Encounter | Players | Sustain | Classification | Win Δ | Death Δ | First death | Survival Δ | Duration Δ | Healing Δ | Regen Δ | Barrier consumed Δ | Regen waste Δ | Damage Δ |");
        builder.AppendLine("|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var comparison in comparisons)
        {
            builder.AppendLine(
                $"| `{EscapeCell(comparison.EncounterId)}` | {comparison.PlayerCount} | " +
                $"{comparison.BaselineSustainMembers}→{comparison.SupportSustainMembers} | " +
                $"{EscapeCell(comparison.Classification)} | {SignedPercent(comparison.WinRateDelta)} | " +
                $"{SignedPercent(comparison.FriendlyDeathRateDelta)} | " +
                $"{FirstDeathTransition(comparison.BaselineFirstFriendlyDeathTick, comparison.SupportFirstFriendlyDeathTick)} | " +
                $"{SignedPercent100(comparison.SurvivalResourcePercentDelta)} | " +
                $"{SignedPercent(comparison.DurationRateDelta)} | " +
                $"{SignedNumber(comparison.EffectiveHealingDelta)} | " +
                $"{SignedNumber(comparison.EffectiveRegenerationDelta)} | " +
                $"{SignedNumber(comparison.BarrierConsumedDelta)} | " +
                $"{SignedNumber(comparison.RegenerationWasteDelta)} | " +
                $"{SignedNumber(comparison.FriendlyDamageDelta)} |");
        }
    }

    private static void AppendComparison(
        StringBuilder builder,
        EncounterCalibrationComparison? comparison)
    {
        if (comparison is null)
            return;

        builder.AppendLine();
        builder.AppendLine("## Baseline comparison");
        builder.AppendLine();
        builder.AppendLine($"- Baseline schema/catalog: {comparison.BaselineSchemaVersion}/{comparison.BaselineCatalogVersion}");
        builder.AppendLine($"- Changed result rows: {comparison.ResultChanges.Count}");
        builder.AppendLine($"- Introduced exceptions: {comparison.IntroducedExceptions.Count}");
        builder.AppendLine($"- Resolved exceptions: {comparison.ResolvedExceptions.Count}");
        if (comparison.ResultChanges.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("| Encounter | Gear | Build | Essences | Win Δ | Timeout Δ | Duration Δ | Survival Δ | Breaks Δ | Uptime Δ |");
            builder.AppendLine("|---|---|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (var change in comparison.ResultChanges)
            {
                builder.AppendLine(
                    $"| `{EscapeCell(change.EncounterId)}` | {EscapeCell(change.GearEnvelopeId)} | " +
                    $"{EscapeCell(change.BuildFamilyId)} | {EscapeCell(change.EssenceEnvelopeId)} | " +
                    $"{SignedPercent(change.WinRateDelta)} | {SignedPercent(change.TimeoutRateDelta)} | " +
                    $"{SignedNumber(change.AverageDurationTicksDelta)} | {SignedPercent100(change.SurvivalResourcePercentDelta)} | " +
                    $"{SignedNumber(change.AverageStaggerBreaksDelta)} | {SignedPercent100(change.AverageStaggerUptimePercentDelta)} |");
            }
        }
    }

    private static void AppendReviewGuidance(
        StringBuilder builder,
        IReadOnlyList<EncounterCalibrationException> exceptions)
    {
        builder.AppendLine();
        builder.AppendLine("## Review order");
        builder.AppendLine();
        builder.AppendLine("1. Review build-sensitive encounters before changing shared curves.");
        builder.AppendLine("2. Review win-rate and timeout exceptions for shared content-pressure errors.");
        builder.AppendLine("3. Review duration and survival exceptions for pacing or sustain problems.");
        builder.AppendLine("4. Change an individual creature or Essence kit only when the exception is isolated to that kit.");
        if (exceptions.Count > 0)
        {
            var first = exceptions[0];
            builder.AppendLine();
            builder.AppendLine(
                $"Start with `{first.EncounterId}` / `{first.Metric}` (priority {GetExceptionPriority(first.Classification, first.Metric)})." );
        }
    }

    private static EncounterCalibrationComparison Compare(
        EncounterCalibrationArtifact baseline,
        EncounterCalibrationArtifact current)
    {
        var baselineResults = baseline.Results.ToDictionary(ResultKey, StringComparer.OrdinalIgnoreCase);
        var changes = current.Results
            .Where(result => baselineResults.ContainsKey(ResultKey(result)))
            .Select(result =>
            {
                var previous = baselineResults[ResultKey(result)];
                return new EncounterCalibrationResultChange(
                    result.EncounterId,
                    result.GearEnvelopeId,
                    result.BuildFamilyId,
                    result.EssenceEnvelopeId,
                    result.WinRate - previous.WinRate,
                    result.TimeoutRate - previous.TimeoutRate,
                    result.AverageDurationTicks - previous.AverageDurationTicks,
                    result.AverageSurvivalResourcePercent - previous.AverageSurvivalResourcePercent,
                    result.AverageStaggerBreaks - previous.AverageStaggerBreaks,
                    result.AverageStaggerUptimePercent - previous.AverageStaggerUptimePercent);
            })
            .Where(change => Math.Abs(change.WinRateDelta) > 0.000_000_1
                             || Math.Abs(change.TimeoutRateDelta) > 0.000_000_1
                             || Math.Abs(change.AverageDurationTicksDelta) > 0.000_000_1
                             || Math.Abs(change.SurvivalResourcePercentDelta) > 0.000_000_1
                             || Math.Abs(change.AverageStaggerBreaksDelta) > 0.000_000_1
                             || Math.Abs(change.AverageStaggerUptimePercentDelta) > 0.000_000_1)
            .OrderByDescending(change => Math.Abs(change.WinRateDelta))
            .ThenByDescending(change => Math.Abs(change.AverageDurationTicksDelta))
            .ThenBy(change => change.EncounterId, StringComparer.Ordinal)
            .ToList();
        var baselineExceptions = baseline.Exceptions.ToDictionary(ExceptionKey, StringComparer.OrdinalIgnoreCase);
        var currentExceptions = current.Exceptions.ToDictionary(ExceptionKey, StringComparer.OrdinalIgnoreCase);
        var introduced = currentExceptions.Where(entry => !baselineExceptions.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(exception => exception.EncounterId, StringComparer.Ordinal)
            .ThenBy(exception => exception.Metric, StringComparer.Ordinal)
            .ToList();
        var resolved = baselineExceptions.Where(entry => !currentExceptions.ContainsKey(entry.Key))
            .Select(entry => entry.Value)
            .OrderBy(exception => exception.EncounterId, StringComparer.Ordinal)
            .ThenBy(exception => exception.Metric, StringComparer.Ordinal)
            .ToList();

        return new EncounterCalibrationComparison(
            baseline.SchemaVersion,
            baseline.CatalogVersion,
            changes,
            introduced,
            resolved);
    }

    private static IReadOnlyList<EncounterCalibrationSupportComparison> CreateSupportComparisons(
        IReadOnlyList<EncounterCalibrationResult> results,
        EncounterCalibrationSupportAssessment assessment)
    {
        return results.Where(result =>
                result.PartyCompositionId.Equals(
                    assessment.BaselineCompositionId,
                    StringComparison.OrdinalIgnoreCase)
                || result.PartyCompositionId.Equals(
                    assessment.SupportCompositionId,
                    StringComparison.OrdinalIgnoreCase))
            .GroupBy(result => new
            {
                result.EncounterId,
                result.GearEnvelopeId,
                result.EssenceEnvelopeId
            })
            .Select(group =>
            {
                var baseline = group.SingleOrDefault(result =>
                    result.PartyCompositionId.Equals(
                        assessment.BaselineCompositionId,
                        StringComparison.OrdinalIgnoreCase));
                var support = group.SingleOrDefault(result =>
                    result.PartyCompositionId.Equals(
                        assessment.SupportCompositionId,
                        StringComparison.OrdinalIgnoreCase));
                if (baseline is null || support is null)
                    return null;

                var durationRateDelta = (support.AverageDurationTicks - baseline.AverageDurationTicks)
                                        / Math.Max(1, baseline.AverageDurationTicks);
                var comparison = new EncounterCalibrationSupportComparison(
                    baseline.EncounterId,
                    baseline.ContentType,
                    baseline.GearEnvelopeId,
                    baseline.EssenceEnvelopeId,
                    assessment.BaselineCompositionId,
                    assessment.SupportCompositionId,
                    baseline.PlayerCount,
                    baseline.SustainMemberCount,
                    support.SustainMemberCount,
                    support.WinRate - baseline.WinRate,
                    support.FriendlyDeathRate - baseline.FriendlyDeathRate,
                    baseline.AverageFirstFriendlyDeathTick,
                    support.AverageFirstFriendlyDeathTick,
                    support.AverageSurvivalResourcePercent - baseline.AverageSurvivalResourcePercent,
                    support.AverageDurationTicks - baseline.AverageDurationTicks,
                    durationRateDelta,
                    support.AverageHealingDone - baseline.AverageHealingDone,
                    support.AverageHealthRegenerated - baseline.AverageHealthRegenerated,
                    support.AverageBarrierConsumed - baseline.AverageBarrierConsumed,
                    support.AverageHealthRegenerationOverhealed
                    - baseline.AverageHealthRegenerationOverhealed,
                    support.AverageFriendlyBasicAttackDamage
                    + support.AverageFriendlyAbilityDamage
                    - baseline.AverageFriendlyBasicAttackDamage
                    - baseline.AverageFriendlyAbilityDamage,
                    string.Empty);
                return comparison with
                {
                    Classification = ClassifySupportComparison(comparison, support, assessment)
                };
            })
            .Where(comparison => comparison is not null)
            .Select(comparison => comparison!)
            .OrderBy(comparison => comparison.ContentType)
            .ThenBy(comparison => comparison.EncounterId, StringComparer.Ordinal)
            .ThenBy(comparison => comparison.GearEnvelopeId, StringComparer.Ordinal)
            .ThenBy(comparison => comparison.EssenceEnvelopeId, StringComparer.Ordinal)
            .ToList();
    }

    private static string ClassifySupportComparison(
        EncounterCalibrationSupportComparison comparison,
        EncounterCalibrationResult support,
        EncounterCalibrationSupportAssessment assessment)
    {
        if (comparison.SupportSustainMembers <= comparison.BaselineSustainMembers)
            return "NoAdditionalSupport";
        if (support.WinRate >= 1 && support.FriendlyDeathRate <= 0
            && Math.Abs(comparison.WinRateDelta) < 0.000_000_1)
        {
            return "UnnecessaryForCompletion";
        }
        if (comparison.WinRateDelta < -0.000_000_1)
            return "CompletionRegressed";

        var deathRateReduction = -comparison.FriendlyDeathRateDelta;
        var firstDeathImproved = comparison.BaselineFirstFriendlyDeathTick.HasValue
                                 && (!comparison.SupportFirstFriendlyDeathTick.HasValue
                                     || comparison.SupportFirstFriendlyDeathTick.Value
                                     - comparison.BaselineFirstFriendlyDeathTick.Value
                                     >= assessment.MinimumFirstDeathDelayTicks);
        var meaningful = comparison.WinRateDelta > 0
                         || deathRateReduction >= assessment.MinimumDeathRateReduction
                         || comparison.SurvivalResourcePercentDelta
                         >= assessment.MinimumSurvivalResourceIncreasePercent
                         || firstDeathImproved;

        if (comparison.WinRateDelta > 0)
            return "CompletionImproved";
        if (meaningful && support.WinRate <= 0 && support.FriendlyDeathRate >= 1)
            return "HelpfulButInsufficient";
        if (meaningful
            && comparison.DurationRateDelta > assessment.MaximumDurationIncreaseRate)
        {
            return "EffectiveWithPacingCost";
        }
        if (meaningful)
            return "Effective";
        if (support.WinRate <= 0 && support.FriendlyDeathRate >= 1)
            return "Insufficient";
        return "NoMeaningfulBenefit";
    }

    private static string ResultKey(EncounterCalibrationResult result) =>
        $"{result.EncounterId}|{result.GearEnvelopeId}|{result.BuildFamilyId}|{result.PartyCompositionId}|{result.EssenceEnvelopeId}";

    private static string ExceptionKey(EncounterCalibrationException exception) =>
        $"{exception.EncounterId}|{exception.BuildFamilyId}|{exception.Classification}|{exception.Metric}";

    private static int GetExceptionPriority(string classification, string metric)
    {
        if (classification.Equals("BuildSensitive", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (metric.Equals("WinRate", StringComparison.OrdinalIgnoreCase))
            return 2;
        if (metric.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
            return 3;
        if (metric.Contains("Duration", StringComparison.OrdinalIgnoreCase))
            return 4;
        return 5;
    }

    private static JsonSerializerOptions CreateJsonOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = writeIndented
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string FormatMetric(string metric, double value) =>
        metric.Contains("Rate", StringComparison.OrdinalIgnoreCase)
            || metric.Contains("Spread", StringComparison.OrdinalIgnoreCase)
            ? Percent(value)
            : metric.Contains("Percent", StringComparison.OrdinalIgnoreCase)
                ? Percent100(value)
                : Number(value);

    private static string FormatRange(string metric, double minimum, double maximum) =>
        $"{FormatMetric(metric, minimum)}–{FormatMetric(metric, maximum)}";

    private static string Percent(double value) => value.ToString("P1", CultureInfo.InvariantCulture);
    private static string Percent100(double value) => (value / 100d).ToString("P1", CultureInfo.InvariantCulture);
    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    private static string SignedPercent(double value) => value.ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);
    private static string SignedPercent100(double value) => (value / 100d).ToString("+0.0%;-0.0%;0.0%", CultureInfo.InvariantCulture);
    private static string SignedNumber(double value) => value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
    private static string Confidence(double value, double lower, double upper) =>
        $"{Percent(value)} ({Percent(lower)}–{Percent(upper)})";
    private static string OptionalNumber(double? value) => value.HasValue ? Number(value.Value) : "—";
    private static string OptionalPercent100(double? value) => value.HasValue ? Percent100(value.Value) : "—";
    private static string FirstDeathTransition(double? baseline, double? support) =>
        $"{OptionalNumber(baseline)}→{OptionalNumber(support)}";
    private static string Ability(string id, double damage) => string.IsNullOrWhiteSpace(id)
        ? "—"
        : $"`{EscapeCell(id)}` ({Number(damage)})";
    private static string DisplayBuild(EncounterCalibrationResult result) =>
        string.IsNullOrWhiteSpace(result.PartyCompositionId)
            ? result.BuildFamilyId
            : result.PartyCompositionId;
    private static bool IsExpectedCohort(
        EncounterCalibrationResult result,
        EncounterCalibrationArtifact artifact) =>
        result.GearEnvelopeId.Equals(
            artifact.AssessmentGearEnvelopeId,
            StringComparison.OrdinalIgnoreCase)
        && result.EssenceEnvelopeId.Equals(
            artifact.AssessmentEssenceEnvelopeId,
            StringComparison.OrdinalIgnoreCase);
    private static string EscapeCell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}

public sealed record EncounterCalibrationArtifact(
    int SchemaVersion,
    int CatalogVersion,
    string AssessmentGearEnvelopeId,
    string AssessmentEssenceEnvelopeId,
    EncounterCalibrationArtifactSummary Summary,
    IReadOnlyList<EncounterCalibrationResult> Results,
    IReadOnlyList<EncounterCalibrationException> Exceptions,
    EncounterCalibrationComparison? Comparison,
    IReadOnlyList<EncounterCalibrationSupportComparison>? SupportComparisons = null);

public sealed record EncounterCalibrationSupportComparison(
    string EncounterId,
    EncounterCalibrationContentType ContentType,
    string GearEnvelopeId,
    string EssenceEnvelopeId,
    string BaselineCompositionId,
    string SupportCompositionId,
    int PlayerCount,
    int BaselineSustainMembers,
    int SupportSustainMembers,
    double WinRateDelta,
    double FriendlyDeathRateDelta,
    double? BaselineFirstFriendlyDeathTick,
    double? SupportFirstFriendlyDeathTick,
    double SurvivalResourcePercentDelta,
    double DurationTicksDelta,
    double DurationRateDelta,
    double EffectiveHealingDelta,
    double EffectiveRegenerationDelta,
    double BarrierConsumedDelta,
    double RegenerationWasteDelta,
    double FriendlyDamageDelta,
    string Classification);

public sealed record EncounterCalibrationArtifactSummary(
    int EncounterCount,
    int ResultCount,
    int SeededSampleCount,
    int AssessedResultCount,
    int ExceptionCount,
    IReadOnlyList<EncounterCalibrationContentSummary> Content);

public sealed record EncounterCalibrationContentSummary(
    EncounterCalibrationContentType ContentType,
    int EncounterCount,
    int ResultCount,
    int SeededSampleCount,
    int AssessedResultCount,
    int ExceptionCount);

public sealed record EncounterCalibrationComparison(
    int BaselineSchemaVersion,
    int BaselineCatalogVersion,
    IReadOnlyList<EncounterCalibrationResultChange> ResultChanges,
    IReadOnlyList<EncounterCalibrationException> IntroducedExceptions,
    IReadOnlyList<EncounterCalibrationException> ResolvedExceptions);

public sealed record EncounterCalibrationResultChange(
    string EncounterId,
    string GearEnvelopeId,
    string BuildFamilyId,
    string EssenceEnvelopeId,
    double WinRateDelta,
    double TimeoutRateDelta,
    double AverageDurationTicksDelta,
    double SurvivalResourcePercentDelta,
    double AverageStaggerBreaksDelta = 0,
    double AverageStaggerUptimePercentDelta = 0);
