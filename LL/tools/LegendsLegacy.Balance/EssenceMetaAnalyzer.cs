using Application.Interfaces.Services.LL.Essences;

namespace LegendsLegacy.Balance;

public enum EssencePairSynergyClassification
{
    Weak,
    Neutral,
    Strong
}

public enum EssenceMetaWarningKind
{
    MandatoryEssence,
    UnderusedEssence,
    SuspiciousSynergy
}

public sealed record EssenceMetaAnalysisOptions(
    int SimulatorBattleCount = 2_000,
    int MinimumPairAppearances = 3,
    double SynergyDeltaThreshold = 5,
    double MandatoryP95UsageThreshold = 0.80,
    double UnderusedOverallUsageThreshold = 0.02,
    int CommonPartnersPerEssence = 5,
    int MaximumSynergyWarnings = 20)
{
    public EssenceMetaAnalysisOptions Validate()
    {
        if (SimulatorBattleCount is < 1 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(SimulatorBattleCount), "Simulator battle count must be between 1 and 1,000,000.");
        if (MinimumPairAppearances is < 2 or > 10_000)
            throw new ArgumentOutOfRangeException(nameof(MinimumPairAppearances), "Minimum pair appearances must be between 2 and 10,000.");
        if (!double.IsFinite(SynergyDeltaThreshold) || SynergyDeltaThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(SynergyDeltaThreshold), "Synergy delta threshold must be positive.");
        if (!double.IsFinite(MandatoryP95UsageThreshold) || MandatoryP95UsageThreshold is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(MandatoryP95UsageThreshold), "Mandatory P95 usage threshold must be between 0 and 1.");
        if (!double.IsFinite(UnderusedOverallUsageThreshold) || UnderusedOverallUsageThreshold is < 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(UnderusedOverallUsageThreshold), "Underused usage threshold must be between 0 and 1 exclusive.");
        if (CommonPartnersPerEssence is < 1 or > 50)
            throw new ArgumentOutOfRangeException(nameof(CommonPartnersPerEssence), "Common partner count must be between 1 and 50.");
        if (MaximumSynergyWarnings is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(MaximumSynergyWarnings), "Maximum synergy warnings must be between 1 and 1,000.");
        return this;
    }
}

public sealed record EssenceMetaSimulatorEvidenceSnapshot(
    string Mode,
    int BattlesRun,
    int CandidateTeamCount,
    int EquipmentTier,
    string EquipmentRarity,
    string EquipmentProfile,
    int EssenceResultCount);

public sealed record EssenceCommonPartnerSnapshot(
    string EssenceId,
    string DisplayName,
    int CoAppearances,
    double PartnerRate,
    double MeanPairPerformance);

public sealed record EssenceUsageSnapshot(
    string EssenceId,
    string DisplayName,
    string SourceMonsterId,
    int Appearances,
    double OverallUsage,
    double P50Usage,
    double P75Usage,
    double P90Usage,
    double P95Usage,
    double P99Usage,
    double? MeanPerformanceWhenPresent,
    double MeanPerformanceWhenAbsent,
    double? PerformanceDelta,
    int AdminSimulatorBattles,
    double? AdminSimulatorScore,
    double? AdminAdjustedScoreDelta,
    string? AdminClassification,
    IReadOnlyList<EssenceCommonPartnerSnapshot> CommonPartners);

public sealed record EssencePairSynergySnapshot(
    string FirstEssenceId,
    string FirstDisplayName,
    string SecondEssenceId,
    string SecondDisplayName,
    int Appearances,
    double UsageRate,
    double ObservedMeanPerformance,
    double ExpectedMeanPerformance,
    double SynergyDelta,
    EssencePairSynergyClassification Classification);

public sealed record EssenceMetaWarningSnapshot(
    EssenceMetaWarningKind Kind,
    IReadOnlyList<string> EssenceIds,
    double MeasuredValue,
    double Threshold,
    string Message);

public sealed record EssenceMetaAnalysisSnapshot(
    int AlgorithmVersion,
    EssenceMetaAnalysisOptions Options,
    int EvaluatedBuildCount,
    IReadOnlyDictionary<string, int> PercentileCohortSizes,
    EssenceMetaSimulatorEvidenceSnapshot SimulatorEvidence,
    IReadOnlyList<EssenceUsageSnapshot> Essences,
    IReadOnlyList<EssencePairSynergySnapshot> PairSynergies,
    IReadOnlyList<EssenceMetaWarningSnapshot> Warnings);

public sealed class EssenceMetaAnalyzer(IEssenceDefinitionRepository essenceDefinitions)
{
    public const int AlgorithmVersion = 1;
    private static readonly int[] PercentileThresholds = [50, 75, 90, 95, 99];

    public EssenceMetaAnalysisSnapshot Analyze(
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> evaluatedCandidates,
        AbilityBalanceSimulationReport simulatorEvidence,
        EssenceMetaAnalysisOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(evaluatedCandidates);
        ArgumentNullException.ThrowIfNull(simulatorEvidence);
        if (evaluatedCandidates.Count == 0)
            throw new InvalidOperationException("Essence meta analysis requires evaluated optimizer candidates.");
        var options = (requestedOptions ?? new EssenceMetaAnalysisOptions()).Validate();
        var definitions = essenceDefinitions.GetAll()
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Id)
                                 && !definition.Id.Equals("essence.training", StringComparison.OrdinalIgnoreCase))
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var definitionIds = definitions.Select(definition => definition.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var observations = CreateObservations(evaluatedCandidates, definitionIds);
        var cohortSizes = PercentileThresholds.ToDictionary(
            threshold => $"P{threshold}",
            threshold => observations.Count(observation => observation.Percentile >= threshold),
            StringComparer.Ordinal);
        var globalMean = observations.Average(observation => observation.Score);
        var adminById = simulatorEvidence.EssenceResults.ToDictionary(
            result => result.EssenceId,
            StringComparer.OrdinalIgnoreCase);
        var usage = definitions.ToDictionary(
            definition => definition.Id,
            definition => CreateUsageMeasurement(definition.Id, observations, cohortSizes),
            StringComparer.OrdinalIgnoreCase);
        var pairSynergies = CreatePairSynergies(observations, usage, definitions, globalMean, options);
        var pairsByEssence = pairSynergies
            .SelectMany(pair => new[]
            {
                (EssenceId: pair.FirstEssenceId, PartnerId: pair.SecondEssenceId, Pair: pair),
                (EssenceId: pair.SecondEssenceId, PartnerId: pair.FirstEssenceId, Pair: pair)
            })
            .GroupBy(value => value.EssenceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var essenceSnapshots = definitions.Select(definition =>
        {
            var measurement = usage[definition.Id];
            adminById.TryGetValue(definition.Id, out var admin);
            var partners = pairsByEssence.GetValueOrDefault(definition.Id) ?? [];
            return new EssenceUsageSnapshot(
                definition.Id,
                definition.DisplayName,
                definition.SourceMonsterId,
                measurement.Appearances,
                measurement.OverallUsage,
                measurement.P50Usage,
                measurement.P75Usage,
                measurement.P90Usage,
                measurement.P95Usage,
                measurement.P99Usage,
                measurement.MeanWhenPresent.HasValue ? RoundScore(measurement.MeanWhenPresent.Value) : null,
                RoundScore(measurement.MeanWhenAbsent),
                measurement.PerformanceDelta.HasValue ? RoundScore(measurement.PerformanceDelta.Value) : null,
                admin?.Battles ?? 0,
                admin is null ? null : RoundScore(admin.Score),
                admin is null ? null : RoundScore(admin.AdjustedScoreDelta),
                admin?.Classification,
                partners.OrderByDescending(value => value.Pair.Appearances)
                    .ThenByDescending(value => value.Pair.ObservedMeanPerformance)
                    .ThenBy(value => value.PartnerId, StringComparer.OrdinalIgnoreCase)
                    .Take(options.CommonPartnersPerEssence)
                    .Select(value => new EssenceCommonPartnerSnapshot(
                        value.PartnerId,
                        definitionsById[value.PartnerId].DisplayName,
                        value.Pair.Appearances,
                        RoundRate(value.Pair.Appearances / (double)Math.Max(1, measurement.Appearances)),
                        value.Pair.ObservedMeanPerformance))
                    .ToArray());
        }).ToArray();
        var warnings = CreateWarnings(essenceSnapshots, pairSynergies, options);

        return new EssenceMetaAnalysisSnapshot(
            AlgorithmVersion,
            options,
            observations.Count,
            cohortSizes,
            new EssenceMetaSimulatorEvidenceSnapshot(
                simulatorEvidence.Mode,
                simulatorEvidence.BattlesRun,
                simulatorEvidence.CandidateTeamCount,
                simulatorEvidence.EquipmentTier,
                simulatorEvidence.EquipmentRarity,
                simulatorEvidence.EquipmentProfile,
                simulatorEvidence.EssenceResults.Count),
            essenceSnapshots,
            pairSynergies,
            warnings);
    }

    private static IReadOnlyList<CandidateObservation> CreateObservations(
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> candidates,
        IReadOnlySet<string> definitionIds)
    {
        var observations = new List<CandidateObservation>(candidates.Count);
        foreach (var profile in candidates.GroupBy(candidate => candidate.Build.SlotCount).OrderBy(group => group.Key))
        {
            var ordered = profile.OrderBy(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var candidate = ordered[index];
                var essenceIds = candidate.Build.Essences.Select(essence => essence.EssenceId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var unknown = essenceIds.FirstOrDefault(id => !definitionIds.Contains(id));
                if (unknown is not null)
                    throw new InvalidOperationException($"Optimizer candidate '{candidate.Build.Id}' references unknown Essence '{unknown}'.");
                if (essenceIds.Count != candidate.Build.Essences.Count)
                    throw new InvalidOperationException($"Optimizer candidate '{candidate.Build.Id}' contains duplicate Essences.");
                observations.Add(new CandidateObservation(
                    candidate.Build.Id,
                    candidate.Benchmark.AggregateScore,
                    ordered.Length == 1 ? 100 : index * 100d / (ordered.Length - 1),
                    essenceIds));
            }
        }
        return observations;
    }

    private static UsageMeasurement CreateUsageMeasurement(
        string essenceId,
        IReadOnlyList<CandidateObservation> observations,
        IReadOnlyDictionary<string, int> cohortSizes)
    {
        var present = observations.Where(observation => observation.EssenceIds.Contains(essenceId)).ToArray();
        var absent = observations.Where(observation => !observation.EssenceIds.Contains(essenceId)).ToArray();
        double UsageAt(int percentile)
        {
            var denominator = cohortSizes[$"P{percentile}"];
            return denominator == 0
                ? 0
                : RoundRate(present.Count(observation => observation.Percentile >= percentile) / (double)denominator);
        }
        var presentMean = present.Length == 0 ? (double?)null : present.Average(observation => observation.Score);
        var absentMean = absent.Length == 0 ? 0 : absent.Average(observation => observation.Score);
        return new UsageMeasurement(
            present.Length,
            RoundRate(present.Length / (double)observations.Count),
            UsageAt(50),
            UsageAt(75),
            UsageAt(90),
            UsageAt(95),
            UsageAt(99),
            presentMean,
            absentMean,
            presentMean.HasValue ? presentMean.Value - absentMean : null);
    }

    private static IReadOnlyList<EssencePairSynergySnapshot> CreatePairSynergies(
        IReadOnlyList<CandidateObservation> observations,
        IReadOnlyDictionary<string, UsageMeasurement> usage,
        IReadOnlyList<Domain.Models.Essences.Definitions.EssenceDefinition> definitions,
        double globalMean,
        EssenceMetaAnalysisOptions options)
    {
        var definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var accumulators = new Dictionary<string, PairAccumulator>(StringComparer.OrdinalIgnoreCase);
        foreach (var observation in observations)
        {
            var ids = observation.EssenceIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            for (var first = 0; first < ids.Length; first++)
            {
                for (var second = first + 1; second < ids.Length; second++)
                {
                    var key = $"{ids[first]}\u001f{ids[second]}";
                    if (!accumulators.TryGetValue(key, out var accumulator))
                    {
                        accumulator = new PairAccumulator(ids[first], ids[second]);
                        accumulators.Add(key, accumulator);
                    }
                    accumulator.Appearances++;
                    accumulator.TotalScore += observation.Score;
                }
            }
        }

        return accumulators.Values
            .Where(pair => pair.Appearances >= options.MinimumPairAppearances)
            .Select(pair =>
            {
                var observed = pair.TotalScore / pair.Appearances;
                var firstMean = usage[pair.FirstEssenceId].MeanWhenPresent ?? globalMean;
                var secondMean = usage[pair.SecondEssenceId].MeanWhenPresent ?? globalMean;
                var expected = firstMean + secondMean - globalMean;
                var delta = observed - expected;
                var classification = delta >= options.SynergyDeltaThreshold
                    ? EssencePairSynergyClassification.Strong
                    : delta <= -options.SynergyDeltaThreshold
                        ? EssencePairSynergyClassification.Weak
                        : EssencePairSynergyClassification.Neutral;
                return new EssencePairSynergySnapshot(
                    pair.FirstEssenceId,
                    definitionsById[pair.FirstEssenceId].DisplayName,
                    pair.SecondEssenceId,
                    definitionsById[pair.SecondEssenceId].DisplayName,
                    pair.Appearances,
                    RoundRate(pair.Appearances / (double)observations.Count),
                    RoundScore(observed),
                    RoundScore(expected),
                    RoundScore(delta),
                    classification);
            })
            .OrderByDescending(pair => Math.Abs(pair.SynergyDelta))
            .ThenByDescending(pair => pair.Appearances)
            .ThenBy(pair => pair.FirstEssenceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.SecondEssenceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<EssenceMetaWarningSnapshot> CreateWarnings(
        IReadOnlyList<EssenceUsageSnapshot> essences,
        IReadOnlyList<EssencePairSynergySnapshot> pairs,
        EssenceMetaAnalysisOptions options)
    {
        var warnings = new List<EssenceMetaWarningSnapshot>();
        warnings.AddRange(essences
            .Where(essence => essence.P95Usage >= options.MandatoryP95UsageThreshold)
            .OrderByDescending(essence => essence.P95Usage)
            .ThenBy(essence => essence.EssenceId, StringComparer.OrdinalIgnoreCase)
            .Select(essence => new EssenceMetaWarningSnapshot(
                EssenceMetaWarningKind.MandatoryEssence,
                [essence.EssenceId],
                essence.P95Usage,
                options.MandatoryP95UsageThreshold,
                FormattableString.Invariant(
                    $"{essence.DisplayName} appears in {essence.P95Usage:P0} of P95+ optimizer builds."))));
        warnings.AddRange(essences
            .Where(essence => essence.OverallUsage <= options.UnderusedOverallUsageThreshold)
            .OrderBy(essence => essence.OverallUsage)
            .ThenBy(essence => essence.EssenceId, StringComparer.OrdinalIgnoreCase)
            .Select(essence => new EssenceMetaWarningSnapshot(
                EssenceMetaWarningKind.UnderusedEssence,
                [essence.EssenceId],
                essence.OverallUsage,
                options.UnderusedOverallUsageThreshold,
                FormattableString.Invariant(
                    $"{essence.DisplayName} appears in only {essence.OverallUsage:P1} of evaluated optimizer builds."))));
        warnings.AddRange(pairs
            .Where(pair => pair.Classification != EssencePairSynergyClassification.Neutral)
            .OrderByDescending(pair => Math.Abs(pair.SynergyDelta))
            .ThenBy(pair => pair.FirstEssenceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.SecondEssenceId, StringComparer.OrdinalIgnoreCase)
            .Take(options.MaximumSynergyWarnings)
            .Select(pair => new EssenceMetaWarningSnapshot(
                EssenceMetaWarningKind.SuspiciousSynergy,
                [pair.FirstEssenceId, pair.SecondEssenceId],
                pair.SynergyDelta,
                options.SynergyDeltaThreshold,
                FormattableString.Invariant(
                    $"{pair.FirstDisplayName} + {pair.SecondDisplayName} has a {pair.SynergyDelta:+0.00;-0.00;0.00}-point synergy delta across {pair.Appearances} builds."))));
        return warnings;
    }

    private static double RoundRate(double value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static double RoundScore(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record CandidateObservation(
        string BuildId,
        double Score,
        double Percentile,
        IReadOnlySet<string> EssenceIds);

    private sealed record UsageMeasurement(
        int Appearances,
        double OverallUsage,
        double P50Usage,
        double P75Usage,
        double P90Usage,
        double P95Usage,
        double P99Usage,
        double? MeanWhenPresent,
        double MeanWhenAbsent,
        double? PerformanceDelta);

    private sealed class PairAccumulator(string firstEssenceId, string secondEssenceId)
    {
        public string FirstEssenceId { get; } = firstEssenceId;
        public string SecondEssenceId { get; } = secondEssenceId;
        public int Appearances { get; set; }
        public double TotalScore { get; set; }
    }
}
