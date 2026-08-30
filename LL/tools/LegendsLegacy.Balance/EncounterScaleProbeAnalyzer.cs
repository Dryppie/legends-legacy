using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace LegendsLegacy.Balance;

public enum EncounterScaleProbeAssessment
{
    AuthoredBaseline = 0,
    WithinDiagnosticTolerance = 1,
    OutsideDiagnosticTolerance = 2,
    Inconclusive = 3,
    Unavailable = 4,
    Disabled = 5
}

public sealed record EncounterScaleProbeOverride(
    int Floor,
    int PlayerCount,
    double HealthMultiplier = 1,
    double OffenseMultiplier = 1,
    double DefenseMultiplier = 1,
    double ResistanceMultiplier = 1,
    double RegenerationMultiplier = 1,
    double GuardianAbilityHealingMultiplier = 1,
    int GuardianAdditionalSummonCopies = 0,
    double GuardianAdditionalSummonPotencyMultiplier = 1,
    double GuardianDistributedDamageMultiplier = 1)
{
    public EncounterScaleProbeOverride Validate()
    {
        if (Floor < 1)
            throw new ArgumentOutOfRangeException(nameof(Floor));
        if (PlayerCount is not (5 or 10 or 15))
            throw new ArgumentOutOfRangeException(nameof(PlayerCount));
        ValidateMultiplier(HealthMultiplier, nameof(HealthMultiplier));
        ValidateMultiplier(OffenseMultiplier, nameof(OffenseMultiplier));
        ValidateMultiplier(DefenseMultiplier, nameof(DefenseMultiplier));
        ValidateMultiplier(ResistanceMultiplier, nameof(ResistanceMultiplier));
        ValidateMultiplier(RegenerationMultiplier, nameof(RegenerationMultiplier));
        ValidateMultiplier(GuardianAbilityHealingMultiplier, nameof(GuardianAbilityHealingMultiplier));
        if (!double.IsFinite(GuardianDistributedDamageMultiplier)
            || GuardianDistributedDamageMultiplier is < 1 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(GuardianDistributedDamageMultiplier),
                "The additive distributed-damage override must be between 1 and 4.");
        }
        if (GuardianAdditionalSummonCopies is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(GuardianAdditionalSummonCopies));
        if (!double.IsFinite(GuardianAdditionalSummonPotencyMultiplier)
            || GuardianAdditionalSummonPotencyMultiplier is < 0.25 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(GuardianAdditionalSummonPotencyMultiplier),
                "Additional-summon potency must be between 0.25 and 1.");
        }
        return this;
    }

    private static void ValidateMultiplier(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0.25 or > 4)
            throw new ArgumentOutOfRangeException(name, "Scale-probe multipliers must be between 0.25 and 4.");
    }
}

public sealed record EncounterScaleProbeOptions
{
    public bool Enabled { get; init; }
    public int PartiesPerSize { get; init; } = 1;
    public int SimulationsPerParty { get; init; } = 1;
    public IReadOnlyList<int> PlayerCounts { get; init; } = [5, 10, 15];
    public IReadOnlyList<EncounterScaleProbeOverride> Overrides { get; init; } = [];
    public double ClearRateTolerance { get; init; } = 0.15;
    public EncounterScaleProbePerformanceBudget PerformanceBudget { get; init; } = new();

    public EncounterScaleProbeOptions Validate()
    {
        if (PartiesPerSize is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(PartiesPerSize));
        if (SimulationsPerParty is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerParty));
        if (PlayerCounts.Count == 0
            || PlayerCounts.Any(count => count is not (5 or 10 or 15))
            || PlayerCounts.Distinct().Count() != PlayerCounts.Count)
        {
            throw new ArgumentException("Scale-probe player counts must be a unique subset of 5, 10, and 15.", nameof(PlayerCounts));
        }
        if (!double.IsFinite(ClearRateTolerance) || ClearRateTolerance is <= 0 or > 0.50)
            throw new ArgumentOutOfRangeException(nameof(ClearRateTolerance));
        foreach (var value in Overrides)
            value.Validate();
        if (Overrides.GroupBy(value => (value.Floor, value.PlayerCount)).Any(group => group.Count() > 1))
            throw new ArgumentException("Only one scale-probe override may be configured per floor and player count.", nameof(Overrides));
        PerformanceBudget.Validate();
        return this;
    }
}

public sealed record EncounterScaleProbePerformanceBudget(
    double? MaximumMillisecondsPerTrial = null,
    long? MaximumAllocatedBytesPerTrial = null,
    double? MinimumSimulatedTicksPerSecond = null,
    long? MaximumProcessPeakWorkingSetBytes = null)
{
    public bool IsConfigured =>
        MaximumMillisecondsPerTrial.HasValue
        || MaximumAllocatedBytesPerTrial.HasValue
        || MinimumSimulatedTicksPerSecond.HasValue
        || MaximumProcessPeakWorkingSetBytes.HasValue;

    public EncounterScaleProbePerformanceBudget Validate()
    {
        if (MaximumMillisecondsPerTrial is <= 0 || MaximumMillisecondsPerTrial is > 600_000)
            throw new ArgumentOutOfRangeException(nameof(MaximumMillisecondsPerTrial));
        if (MaximumAllocatedBytesPerTrial is <= 0 or > 2_147_483_648)
            throw new ArgumentOutOfRangeException(nameof(MaximumAllocatedBytesPerTrial));
        if (MinimumSimulatedTicksPerSecond is <= 0 || MinimumSimulatedTicksPerSecond is > 1_000_000_000)
            throw new ArgumentOutOfRangeException(nameof(MinimumSimulatedTicksPerSecond));
        if (MaximumProcessPeakWorkingSetBytes is <= 0 or > 34_359_738_368)
            throw new ArgumentOutOfRangeException(nameof(MaximumProcessPeakWorkingSetBytes));
        return this;
    }
}

public enum EncounterScaleProbePerformanceAssessment
{
    NotMeasured = 0,
    NotConfigured = 1,
    WithinBudget = 2,
    OutsideBudget = 3
}

public sealed record EncounterScaleProbePerformanceSnapshot(
    bool Measured,
    double WallTimeMilliseconds,
    long AllocatedBytes,
    double AllocatedBytesPerTrial,
    long WorkingSetBeforeBytes,
    long WorkingSetAfterBytes,
    long ProcessPeakWorkingSetBytes,
    long ManagedHeapHighWaterEstimateBytes,
    double TrialsPerSecond,
    double SimulatedTicksPerSecond,
    EncounterScaleProbePerformanceAssessment BudgetAssessment,
    IReadOnlyList<string> BudgetViolations)
{
    public static EncounterScaleProbePerformanceSnapshot NotMeasured { get; } = new(
        false, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        EncounterScaleProbePerformanceAssessment.NotMeasured,
        []);
}

public sealed record EncounterScaleProbePerformanceEnvironmentSnapshot(
    string FrameworkDescription,
    string RuntimeVersion,
    string OperatingSystemDescription,
    string ProcessArchitecture,
    int LogicalProcessorCount,
    bool ServerGarbageCollection,
    long StopwatchFrequency)
{
    public static EncounterScaleProbePerformanceEnvironmentSnapshot Capture() => new(
        RuntimeInformation.FrameworkDescription,
        Environment.Version.ToString(),
        RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        Environment.ProcessorCount,
        GCSettings.IsServerGC,
        Stopwatch.Frequency);
}

public sealed record EncounterScaleProbeCombatRequest(
    int Floor,
    int PlayerCount,
    IReadOnlyList<EssenceBuildSnapshot> Builds,
    int RunSeed,
    int Simulations,
    int MaxTicks,
    EncounterScaleProbeOverride AppliedOverride);

public interface IEncounterScaleProbeCombatEvaluator
{
    IReadOnlyList<WorldTowerTrialSnapshot> EvaluateScaleProbe(EncounterScaleProbeCombatRequest request);
}

public sealed record EncounterScaleProbeVariantSnapshot(
    int PlayerCount,
    bool IsAuthoredPlayerCount,
    string EvidenceSource,
    int PartyCount,
    int TrialCount,
    long TotalSimulatedTicks,
    int ClearCount,
    double ClearRate,
    double ConfidenceLowerBound,
    double ConfidenceUpperBound,
    double ClearRateDeltaFromAuthored,
    double AverageDurationTicks,
    double MedianDurationTicks,
    double HealthFormulaRatio,
    double OffenseFormulaRatio,
    double DurabilityFormulaRatio,
    EncounterScaleProbeOverride AppliedOverride,
    EncounterScaleProbePerformanceSnapshot Performance,
    EncounterScaleProbeAssessment Assessment,
    IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts,
    IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
    IReadOnlyList<string> Warnings);

public sealed record EncounterScaleProbeFloorSnapshot(
    int Floor,
    string EncounterName,
    int AuthoredPlayerCount,
    IReadOnlyList<EncounterScaleProbeVariantSnapshot> Variants);

public sealed record EncounterScaleProbeSuiteSnapshot(
    int AlgorithmVersion,
    int Seed,
    EncounterScaleProbeOptions Options,
    bool ProductionContentModified,
    bool ReleaseEligible,
    int TotalCombatTrials,
    long TotalSimulatedTicks,
    double TotalMeasuredWallTimeMilliseconds,
    long TotalAllocatedBytes,
    long ProcessPeakWorkingSetBytes,
    long ManagedHeapHighWaterEstimateBytes,
    double SimulatedTicksPerSecond,
    EncounterScaleProbePerformanceAssessment PerformanceBudgetAssessment,
    EncounterScaleProbePerformanceEnvironmentSnapshot PerformanceEnvironment,
    IReadOnlyList<EncounterScaleProbeFloorSnapshot> Floors,
    IReadOnlyList<string> Warnings);

public sealed class EncounterScaleProbeAnalyzer(
    PartyFamilyBuilder partyBuilder,
    IEncounterScaleProbeCombatEvaluator combatEvaluator)
{
    public const int AlgorithmVersion = 5;

    public EncounterScaleProbeSuiteSnapshot Analyze(
        WorldTowerAnalysisSnapshot worldTower,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        BuildCapabilitySuiteSnapshot capabilities,
        PartyFamilySuiteSnapshot partyFamilies,
        PartyFamilyEvaluationSuiteSnapshot partyFamilyEvaluation,
        int runSeed,
        EncounterScaleProbeOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(partyFamilies);
        ArgumentNullException.ThrowIfNull(partyFamilyEvaluation);
        var options = (requestedOptions ?? new EncounterScaleProbeOptions()).Validate();
        if (!options.Enabled)
        {
            return new EncounterScaleProbeSuiteSnapshot(
                AlgorithmVersion,
                runSeed,
                options,
                false,
                false,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                EncounterScaleProbePerformanceAssessment.NotMeasured,
                EncounterScaleProbePerformanceEnvironmentSnapshot.Capture(),
                [],
                ["Encounter scale probes are disabled for this run."]);
        }

        var representativeById = representativeBuilds.Profiles.SelectMany(profile => profile.Builds
                .Select(build => new RepresentativeLookup(profile.Id, build)))
            .GroupBy(value => value.Build.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var familyByFloor = partyFamilies.Floors.ToDictionary(floor => floor.Floor);
        var evaluationByFloor = partyFamilyEvaluation.Floors.ToDictionary(floor => floor.Floor);
        var suiteWarnings = new List<string>
        {
            "Scale probes are balance-only diagnostics and cannot certify or create production encounter variants."
        };
        if (!options.PerformanceBudget.IsConfigured)
        {
            suiteWarnings.Add(
                "Scale-probe performance thresholds are not configured; host-dependent metrics are reported without a budget verdict.");
        }
        var floors = worldTower.Floors.OrderBy(floor => floor.Floor).Select(floor =>
        {
            var familyFloor = familyByFloor.GetValueOrDefault(floor.Floor)
                              ?? throw new InvalidOperationException($"Party families have no Floor {floor.Floor}.");
            var evaluationFloor = evaluationByFloor.GetValueOrDefault(floor.Floor);
            return AnalyzeFloor(
                floor,
                familyFloor,
                evaluationFloor,
                representativeBuilds,
                capabilities,
                representativeById,
                runSeed,
                worldTower.Options.MaxTicks,
                options,
                suiteWarnings);
        }).ToArray();
        var measured = floors.SelectMany(floor => floor.Variants)
            .Where(variant => variant.Performance.Measured)
            .Select(variant => variant.Performance)
            .ToArray();
        foreach (var floor in floors)
        foreach (var variant in floor.Variants)
        foreach (var violation in variant.Performance.BudgetViolations)
            suiteWarnings.Add($"Floor {floor.Floor} {variant.PlayerCount}P: {violation}");
        var totalWallTime = measured.Sum(value => value.WallTimeMilliseconds);
        var totalTicks = floors.Sum(floor => floor.Variants.Where(variant => variant.EvidenceSource != "reused-authored-party-family")
            .Sum(variant => variant.TotalSimulatedTicks));
        return new EncounterScaleProbeSuiteSnapshot(
            AlgorithmVersion,
            runSeed,
            options,
            false,
            false,
            floors.Sum(floor => floor.Variants.Sum(variant => variant.EvidenceSource == "reused-authored-party-family" ? 0 : variant.TrialCount)),
            totalTicks,
            Round(totalWallTime),
            measured.Sum(value => value.AllocatedBytes),
            measured.Length == 0 ? 0 : measured.Max(value => value.ProcessPeakWorkingSetBytes),
            measured.Length == 0 ? 0 : measured.Max(value => value.ManagedHeapHighWaterEstimateBytes),
            totalWallTime <= 0 ? 0 : Round(totalTicks / (totalWallTime / 1000d)),
            AggregatePerformanceAssessment(measured),
            EncounterScaleProbePerformanceEnvironmentSnapshot.Capture(),
            floors,
            suiteWarnings);
    }

    private EncounterScaleProbeFloorSnapshot AnalyzeFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        PartyFamilyFloorSnapshot partyFamilyFloor,
        PartyFamilyFloorEvaluationSnapshot? evaluationFloor,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        BuildCapabilitySuiteSnapshot capabilities,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById,
        int runSeed,
        int maxTicks,
        EncounterScaleProbeOptions options,
        ICollection<string> suiteWarnings)
    {
        var raw = new List<RawVariant>();
        foreach (var playerCount in options.PlayerCounts.Order())
        {
            var appliedOverride = options.Overrides.SingleOrDefault(value =>
                                      value.Floor == floor.Floor && value.PlayerCount == playerCount)
                                  ?? new EncounterScaleProbeOverride(floor.Floor, playerCount);
            if (CanReuseAuthored(floor, playerCount, partyFamilyFloor, evaluationFloor, options))
            {
                var intended = evaluationFloor!.ProgressionCohorts.Single(value =>
                    value.Cohort == PartyProgressionCohortKind.IntendedP75);
                raw.Add(new RawVariant(
                    playerCount,
                    true,
                    "reused-authored-party-family",
                    intended.PartyCount,
                    intended.TrialCount,
                    0,
                    intended.ClearCount,
                    intended.ObservedClearRate,
                    intended.ConfidenceLowerBound,
                    intended.ConfidenceUpperBound,
                    intended.AverageDurationTicks,
                    intended.MedianDurationTicks,
                    intended.TerminalFailureCounts,
                    intended.PrimaryObservedFailureModeCounts,
                    appliedOverride,
                    EncounterScaleProbePerformanceSnapshot.NotMeasured,
                    []));
                continue;
            }

            var parties = playerCount == floor.RequiredSlots
                ? partyFamilyFloor.Families.Single(value => value.Family == PartyFamilyKind.IntendedBalanced)
                    .Parties.Take(options.PartiesPerSize).ToArray()
                : partyBuilder.BuildBalancedScaleProbeParties(
                    floor,
                    playerCount,
                    representativeBuilds,
                    capabilities,
                    runSeed,
                    options.PartiesPerSize);
            if (parties.Count < options.PartiesPerSize)
            {
                suiteWarnings.Add(
                    $"Floor {floor.Floor} {playerCount}P produced {parties.Count}/{options.PartiesPerSize} scale-probe rosters.");
            }
            if (parties.Count == 0)
            {
                raw.Add(RawVariant.Unavailable(playerCount, playerCount == floor.RequiredSlots, appliedOverride));
                continue;
            }
            var invalidParties = parties.Count(party => !party.ConstraintsSatisfied);
            if (invalidParties > 0)
            {
                suiteWarnings.Add(
                    $"Floor {floor.Floor} {playerCount}P retained {invalidParties} roster(s) with unsatisfied balanced-family constraints.");
            }
            var measuredTrials = MeasureTrials(
                () => parties.SelectMany(party => combatEvaluator.EvaluateScaleProbe(
                        new EncounterScaleProbeCombatRequest(
                            floor.Floor,
                            playerCount,
                            MaterializeBuilds(party, representativeById),
                            runSeed,
                            options.SimulationsPerParty,
                            maxTicks,
                            appliedOverride)))
                    .ToArray(),
                options.PerformanceBudget);
            raw.Add(SummarizeRaw(
                playerCount,
                playerCount == floor.RequiredSlots,
                "production-world-tower-scale-probe",
                parties.Count,
                measuredTrials.Trials,
                appliedOverride,
                measuredTrials.Performance));
        }

        var authored = raw.SingleOrDefault(value => value.IsAuthoredPlayerCount);
        var variants = raw.Select(value => FinalizeVariant(
                value,
                authored,
                floor.RequiredSlots,
                options.ClearRateTolerance))
            .ToArray();
        if (authored is null)
            suiteWarnings.Add($"Floor {floor.Floor} scale probes omitted authored {floor.RequiredSlots}P baseline; deltas are unavailable.");
        return new EncounterScaleProbeFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            floor.RequiredSlots,
            variants);
    }

    private static bool CanReuseAuthored(
        WorldTowerFloorAnalysisSnapshot floor,
        int playerCount,
        PartyFamilyFloorSnapshot partyFamilyFloor,
        PartyFamilyFloorEvaluationSnapshot? evaluationFloor,
        EncounterScaleProbeOptions options) =>
        playerCount == floor.RequiredSlots
        && evaluationFloor is not null
        && partyFamilyFloor.Families.Single(value => value.Family == PartyFamilyKind.IntendedBalanced).Parties.Count
        == options.PartiesPerSize
        && evaluationFloor.ProgressionCohorts.Single(value => value.Cohort == PartyProgressionCohortKind.IntendedP75)
            .Parties.All(party => party.TrialCount == options.SimulationsPerParty)
        && options.Overrides.All(value => value.Floor != floor.Floor || value.PlayerCount != playerCount);

    private static IReadOnlyList<EssenceBuildSnapshot> MaterializeBuilds(
        PartyFamilyPartySnapshot party,
        IReadOnlyDictionary<string, RepresentativeLookup> representativeById) =>
        party.Members.Select(member =>
        {
            if (!representativeById.TryGetValue(member.BuildId, out var representative))
                throw new InvalidOperationException($"Scale-probe party references unknown build '{member.BuildId}'.");
            return new EssenceBuildSnapshot(
                representative.Build.Id,
                representative.ProfileId,
                representative.Build.Essences.Count,
                0,
                representative.Build.Essences,
                representative.Build.Character);
        }).ToArray();

    private static RawVariant SummarizeRaw(
        int playerCount,
        bool isAuthored,
        string source,
        int partyCount,
        IReadOnlyList<WorldTowerTrialSnapshot> trials,
        EncounterScaleProbeOverride appliedOverride,
        EncounterScaleProbePerformanceSnapshot performance)
    {
        var clears = trials.Count(trial => trial.Outcome.Equals("Victory", StringComparison.Ordinal));
        var confidence = Wilson(clears, trials.Count);
        var durations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();
        return new RawVariant(
            playerCount,
            isAuthored,
            source,
            partyCount,
            trials.Count,
            trials.Sum(trial => (long)trial.DurationTicks),
            clears,
            Round(clears / (double)trials.Count),
            Round(confidence.Lower),
            Round(confidence.Upper),
            Round(trials.Average(trial => trial.DurationTicks)),
            Round(Percentile(durations, 0.50)),
            Count(trials.Select(trial => trial.FailureDiagnostic.TerminalFailure)),
            Count(trials.Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)),
            appliedOverride,
            performance,
            []);
    }

    private static EncounterScaleProbeVariantSnapshot FinalizeVariant(
        RawVariant variant,
        RawVariant? authored,
        int authoredPlayerCount,
        double tolerance)
    {
        var delta = authored is null ? 0 : Round(variant.ClearRate - authored.ClearRate);
        EncounterScaleProbeAssessment assessment;
        var warnings = variant.Warnings.ToList();
        warnings.AddRange(variant.Performance.BudgetViolations);
        if (variant.TrialCount == 0)
        {
            assessment = EncounterScaleProbeAssessment.Unavailable;
        }
        else if (variant.IsAuthoredPlayerCount)
        {
            assessment = EncounterScaleProbeAssessment.AuthoredBaseline;
        }
        else if (authored is null || authored.TrialCount == 0)
        {
            assessment = EncounterScaleProbeAssessment.Inconclusive;
            warnings.Add("No authored-size baseline was included for comparison.");
        }
        else if (Math.Abs(delta) <= tolerance)
        {
            assessment = EncounterScaleProbeAssessment.WithinDiagnosticTolerance;
        }
        else
        {
            var minimumDelta = variant.ConfidenceLowerBound - authored.ConfidenceUpperBound;
            var maximumDelta = variant.ConfidenceUpperBound - authored.ConfidenceLowerBound;
            var confidenceOutside = minimumDelta > tolerance || maximumDelta < -tolerance;
            assessment = confidenceOutside
                ? EncounterScaleProbeAssessment.OutsideDiagnosticTolerance
                : EncounterScaleProbeAssessment.Inconclusive;
            warnings.Add(confidenceOutside
                ? "Clear-rate confidence is outside the diagnostic authored-size tolerance."
                : "Point clear rate is outside tolerance, but confidence intervals remain inconclusive.");
        }
        var healthRatio = Math.Pow(variant.PlayerCount / (double)authoredPlayerCount, 0.85);
        var offenseRatio = (1 + 0.05 * (variant.PlayerCount - 1))
                           / (1 + 0.05 * (authoredPlayerCount - 1));
        var durabilityRatio = Math.Pow(variant.PlayerCount / (double)authoredPlayerCount, 0.25);
        return new EncounterScaleProbeVariantSnapshot(
            variant.PlayerCount,
            variant.IsAuthoredPlayerCount,
            variant.EvidenceSource,
            variant.PartyCount,
            variant.TrialCount,
            variant.TotalSimulatedTicks,
            variant.ClearCount,
            variant.ClearRate,
            variant.ConfidenceLowerBound,
            variant.ConfidenceUpperBound,
            delta,
            variant.AverageDurationTicks,
            variant.MedianDurationTicks,
            Round(healthRatio),
            Round(offenseRatio),
            Round(durabilityRatio),
            variant.AppliedOverride,
            variant.Performance,
            assessment,
            variant.TerminalFailureCounts,
            variant.PrimaryObservedFailureModeCounts,
            warnings);
    }

    private static (double Lower, double Upper) Wilson(int successes, int trials)
    {
        if (trials <= 0)
            return (0, 0);
        const double z = 1.959963984540054;
        var proportion = successes / (double)trials;
        var denominator = 1 + z * z / trials;
        var center = (proportion + z * z / (2 * trials)) / denominator;
        var margin = z * Math.Sqrt(
            proportion * (1 - proportion) / trials + z * z / (4d * trials * trials)) / denominator;
        return (Math.Max(0, center - margin), Math.Min(1, center + margin));
    }

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        var position = (ordered.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return ordered[lower];
        return ordered[lower] + (ordered[upper] - ordered[lower]) * (position - lower);
    }

    private static IReadOnlyDictionary<T, int> Count<T>(IEnumerable<T> values)
        where T : struct, Enum =>
        values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count());

    private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static MeasuredTrials MeasureTrials(
        Func<WorldTowerTrialSnapshot[]> action,
        EncounterScaleProbePerformanceBudget budget)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var workingSetBefore = process.WorkingSet64;
        var managedBefore = GC.GetTotalMemory(forceFullCollection: false);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        var trials = action();
        stopwatch.Stop();
        var allocated = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
        var managedAfter = GC.GetTotalMemory(forceFullCollection: false);
        process.Refresh();
        var workingSetAfter = process.WorkingSet64;
        var elapsedMilliseconds = Math.Max(stopwatch.Elapsed.TotalMilliseconds, 0.0001);
        var simulatedTicks = trials.Sum(trial => (long)trial.DurationTicks);
        var violations = EvaluatePerformanceBudget(
            budget,
            elapsedMilliseconds,
            allocated,
            trials.Length,
            simulatedTicks,
            process.PeakWorkingSet64);
        var assessment = !budget.IsConfigured
            ? EncounterScaleProbePerformanceAssessment.NotConfigured
            : violations.Count == 0
                ? EncounterScaleProbePerformanceAssessment.WithinBudget
                : EncounterScaleProbePerformanceAssessment.OutsideBudget;
        return new MeasuredTrials(
            trials,
            new EncounterScaleProbePerformanceSnapshot(
                true,
                Round(elapsedMilliseconds),
                allocated,
                Round(allocated / (double)Math.Max(1, trials.Length)),
                workingSetBefore,
                workingSetAfter,
                process.PeakWorkingSet64,
                Math.Max(managedBefore, managedAfter),
                Round(trials.Length / (elapsedMilliseconds / 1000d)),
                Round(simulatedTicks / (elapsedMilliseconds / 1000d)),
                assessment,
                violations));
    }

    private static IReadOnlyList<string> EvaluatePerformanceBudget(
        EncounterScaleProbePerformanceBudget budget,
        double elapsedMilliseconds,
        long allocatedBytes,
        int trials,
        long simulatedTicks,
        long processPeakWorkingSetBytes)
    {
        var violations = new List<string>();
        var divisor = Math.Max(1, trials);
        var millisecondsPerTrial = elapsedMilliseconds / divisor;
        var allocatedBytesPerTrial = allocatedBytes / (double)divisor;
        var ticksPerSecond = simulatedTicks / Math.Max(elapsedMilliseconds / 1000d, 0.0000001);
        if (budget.MaximumMillisecondsPerTrial is { } maximumMilliseconds && millisecondsPerTrial > maximumMilliseconds)
            violations.Add($"Performance budget exceeded: {millisecondsPerTrial:F2} ms/trial > {maximumMilliseconds:F2} ms/trial.");
        if (budget.MaximumAllocatedBytesPerTrial is { } maximumAllocated && allocatedBytesPerTrial > maximumAllocated)
            violations.Add($"Performance budget exceeded: {allocatedBytesPerTrial:F0} allocated bytes/trial > {maximumAllocated}.");
        if (budget.MinimumSimulatedTicksPerSecond is { } minimumThroughput && ticksPerSecond < minimumThroughput)
            violations.Add($"Performance budget exceeded: {ticksPerSecond:F2} simulated ticks/s < {minimumThroughput:F2}.");
        if (budget.MaximumProcessPeakWorkingSetBytes is { } maximumPeak && processPeakWorkingSetBytes > maximumPeak)
            violations.Add($"Performance budget exceeded: process peak working set {processPeakWorkingSetBytes} bytes > {maximumPeak}.");
        return violations;
    }

    private static EncounterScaleProbePerformanceAssessment AggregatePerformanceAssessment(
        IReadOnlyList<EncounterScaleProbePerformanceSnapshot> measured)
    {
        if (measured.Count == 0)
            return EncounterScaleProbePerformanceAssessment.NotMeasured;
        if (measured.Any(value => value.BudgetAssessment == EncounterScaleProbePerformanceAssessment.OutsideBudget))
            return EncounterScaleProbePerformanceAssessment.OutsideBudget;
        return measured.Any(value => value.BudgetAssessment == EncounterScaleProbePerformanceAssessment.WithinBudget)
            ? EncounterScaleProbePerformanceAssessment.WithinBudget
            : EncounterScaleProbePerformanceAssessment.NotConfigured;
    }

    private sealed record RepresentativeLookup(string ProfileId, RepresentativeEssenceBuildSnapshot Build);

    private sealed record RawVariant(
        int PlayerCount,
        bool IsAuthoredPlayerCount,
        string EvidenceSource,
        int PartyCount,
        int TrialCount,
        long TotalSimulatedTicks,
        int ClearCount,
        double ClearRate,
        double ConfidenceLowerBound,
        double ConfidenceUpperBound,
        double AverageDurationTicks,
        double MedianDurationTicks,
        IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts,
        IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts,
        EncounterScaleProbeOverride AppliedOverride,
        EncounterScaleProbePerformanceSnapshot Performance,
        IReadOnlyList<string> Warnings)
    {
        internal static RawVariant Unavailable(
            int playerCount,
            bool isAuthored,
            EncounterScaleProbeOverride appliedOverride) =>
            new(
                playerCount,
                isAuthored,
                "unavailable",
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                new Dictionary<WorldTowerTerminalFailure, int>(),
                new Dictionary<WorldTowerObservedFailureMode, int>(),
                appliedOverride,
                EncounterScaleProbePerformanceSnapshot.NotMeasured,
                ["No scale-probe roster is available."]);
    }

    private sealed record MeasuredTrials(
        IReadOnlyList<WorldTowerTrialSnapshot> Trials,
        EncounterScaleProbePerformanceSnapshot Performance);
}
