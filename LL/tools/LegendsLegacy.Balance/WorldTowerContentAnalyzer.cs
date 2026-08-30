using System.Globalization;
using System.Text.Json;
using Application.Interfaces.Services.LL.WorldTower;
using Common.Randomness;
using Domain.Helpers;
using Domain.Models.Attributes;
using Domain.Models.Combat;
using Domain.Models.Entities;
using Domain.Models.Entities.Creatures;
using Domain.Models.Entities.Creatures.Templates;
using Domain.Models.Entities.Creatures.Templates.Enums;
using Domain.Models.Regions.Areas;
using Domain.Models.Snapshots;
using Domain.Models.WorldTower;
using Services.LL.Combat.Engine;
using Services.LL.Combat.Layers.Resolution;
using Services.LL.Combat.Layers.Orchestration.Models;
using Services.LL.Combat.Layers.Resolution.Models;
using Services.LL.Interfaces;
using Services.LL.Interfaces.Combat.Resolution;
using Services.LL.Interfaces.WorldTower;
using Services.LL.WorldTower;

namespace LegendsLegacy.Balance;

public enum WorldTowerDifficultyClassification
{
    TooHard,
    OnTarget,
    TooEasy
}

public enum WorldTowerTerminalFailure
{
    None,
    PartyDefeated,
    Timeout,
    Other
}

public enum WorldTowerObservedFailureMode
{
    None,
    PrimaryTargetCollapse,
    PartyAttrition,
    BossSustainDominance,
    AddPressure,
    PriorityObjectiveUnmet,
    ControlWindowUnmet,
    CleanseDemandUnmet,
    Other
}

public sealed record WorldTowerRegenerationPointSnapshot(
    int Tick,
    int GuardianHealth,
    int GuardianMaxHealth,
    int CumulativeGuardianRegeneration,
    int ActiveHostileCombatants,
    int ActiveHostileSummons);

public sealed record WorldTowerFailureEvidenceSnapshot(
    string Metric,
    double ObservedValue,
    double? Threshold,
    string Unit,
    string? EntityId = null);

public sealed record WorldTowerFailureDiagnosticSnapshot(
    WorldTowerTerminalFailure TerminalFailure,
    WorldTowerObservedFailureMode PrimaryObservedFailureMode,
    double Confidence,
    IReadOnlyList<WorldTowerObservedFailureMode> ContributingConditions,
    string RuleVersion,
    string? AuthoritativeMechanicCause,
    IReadOnlyList<WorldTowerFailureEvidenceSnapshot> Evidence)
{
    public static WorldTowerFailureDiagnosticSnapshot Success { get; } = new(
        WorldTowerTerminalFailure.None,
        WorldTowerObservedFailureMode.None,
        1,
        [],
        WorldTowerContentAnalyzer.FailureRuleVersion,
        null,
        []);
}

public sealed record WorldTowerAnalysisOptions(
    int SimulationsPerFloor = 10,
    double DesiredClearRate = 0.65,
    double ClearRateTolerance = 0.10,
    int MaxTicks = 6_000)
{
    public WorldTowerAnalysisOptions Validate()
    {
        if (SimulationsPerFloor is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(SimulationsPerFloor), "Simulations per floor must be between 1 and 1,000.");
        if (!double.IsFinite(DesiredClearRate) || DesiredClearRate is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(DesiredClearRate), "Desired clear rate must be between 0 and 1 exclusive.");
        if (!double.IsFinite(ClearRateTolerance) || ClearRateTolerance is < 0 or >= 0.5)
            throw new ArgumentOutOfRangeException(nameof(ClearRateTolerance), "Clear-rate tolerance must be between 0 and 0.5 exclusive.");
        if (MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(MaxTicks), "Maximum combat ticks must be between 1 and 100,000.");
        return this;
    }
}

public sealed record WorldTowerTrialSnapshot(
    int Trial,
    int CombatSeed,
    string Outcome,
    int DurationTicks,
    int FriendlyDeaths,
    double RemainingHealthRatio,
    double MeanPlayerDisplayCr,
    int TeamDisplayCr,
    IReadOnlyList<string> BuildIds)
{
    public IReadOnlyList<int> PartyNumbers { get; init; } = [];
    public double GuardianHealthRemainingRatio { get; init; }
    public double HostileDamagePerSecond { get; init; }
    public int GuardianPassiveRegeneration { get; init; }
    public int GuardianAbilityHealing { get; init; }
    public int GuardianTotalSelfSustain { get; init; }
    public double GuardianDamageTakenPerSecond { get; init; }
    public double PrimaryTargetDamageTaken { get; init; }
    public double NonPrimaryFriendlyDamageTakenPerSecond { get; init; }
    public double FriendlyDamageTakenConcentration { get; init; }
    public double PartySustainPerSecond { get; init; }
    public int GuardianInjectedDistributedDamage { get; init; }
    public double GuardianInjectedDistributedDamagePerSecond { get; init; }
    public int GuardianInjectedDistributedDamageHitCount { get; init; }
    public int GuardianInjectedDistributedDamageWaveCount { get; init; }
    public int GuardianInjectedDistributedDamagePeakTargetsPerWave { get; init; }
    public int GuardianCalibratedDistributedDamage { get; init; }
    public double GuardianCalibratedDistributedDamagePerSecond { get; init; }
    public int GuardianCalibratedDistributedDamageHitCount { get; init; }
    public int GuardianCalibratedDistributedDamageWaveCount { get; init; }
    public int GuardianCalibratedDistributedDamagePeakTargetsPerWave { get; init; }
    public int? FirstFriendlyDeathTick { get; init; }
    public int PeakActiveHostileCombatants { get; init; }
    public int PeakActiveHostileSummons { get; init; }
    public int FinalActiveHostileCombatants { get; init; }
    public int FinalActiveHostileSummons { get; init; }
    public int? FirstAdditionalHostileTick { get; init; }
    public int? FirstAdditionalHostileClearTick { get; init; }
    public int? FirstAdditionalHostileClearDurationTicks =>
        FirstAdditionalHostileTick.HasValue && FirstAdditionalHostileClearTick.HasValue
            ? Math.Max(0, FirstAdditionalHostileClearTick.Value - FirstAdditionalHostileTick.Value)
            : null;
    public int TotalHostileSummons { get; init; }
    public int AdditionalHostileWindowCount { get; init; }
    public int ClearedAdditionalHostileWindowCount { get; init; }
    public int HostileSummonActiveTicks { get; init; }
    public double HostileSummonUptimeRatio => DurationTicks <= 0
        ? 0
        : Math.Clamp(HostileSummonActiveTicks / (double)DurationTicks, 0, 1);
    public int HostileSummonWaveCount { get; init; }
    public int HostileSummonWaveIntervalCount { get; init; }
    public int HostileSummonWaveIntervalTotalTicks { get; init; }
    public double? AverageHostileSummonWaveIntervalTicks => HostileSummonWaveIntervalCount == 0
        ? null
        : HostileSummonWaveIntervalTotalTicks / (double)HostileSummonWaveIntervalCount;
    public int? MinimumHostileSummonWaveIntervalTicks { get; init; }
    public int? MaximumHostileSummonWaveIntervalTicks { get; init; }
    public int CleansedEffects { get; init; }
    public int DispelledEffects { get; init; }
    public int HostileActionDeniedTicks { get; init; }
    public int FriendlyActionDeniedTicks { get; init; }
    public IReadOnlyList<WorldTowerRegenerationPointSnapshot> GuardianRegenerationTimeline { get; init; } = [];
    public WorldTowerFailureDiagnosticSnapshot FailureDiagnostic { get; init; } =
        WorldTowerFailureDiagnosticSnapshot.Success;
}

public sealed record WorldTowerFloorAnalysisSnapshot(
    int Floor,
    string EncounterName,
    string GuardianName,
    string GuardianAbilityProfileId,
    int RequiredSlots,
    double TargetBenchmarkPower,
    string RepresentativeProfileId,
    double RepresentativeProfilePower,
    int RepresentativeBuildCount,
    int AuthoredRecommendedCr,
    double AuthoredHealthMultiplier,
    double AuthoredDamageMultiplier,
    double RecommendedDisplayCr,
    double? ObservedClearingDisplayCr,
    double DesiredClearRate,
    double ObservedClearRate,
    double AverageDurationTicks,
    double MedianDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio,
    WorldTowerDifficultyClassification Classification,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<WorldTowerTrialSnapshot> Trials)
{
    public double P10DurationTicks { get; init; }
    public double P90DurationTicks { get; init; }
    public double AverageHostileDamagePerSecond { get; init; }
    public double AveragePrimaryTargetDamageTaken { get; init; }
    public double AveragePartySustainPerSecond { get; init; }
    public IReadOnlyDictionary<WorldTowerTerminalFailure, int> TerminalFailureCounts { get; init; } =
        new Dictionary<WorldTowerTerminalFailure, int>();
    public IReadOnlyDictionary<WorldTowerObservedFailureMode, int> PrimaryObservedFailureModeCounts { get; init; } =
        new Dictionary<WorldTowerObservedFailureMode, int>();
}

public sealed record WorldTowerAnalysisSnapshot(
    int AlgorithmVersion,
    WorldTowerAnalysisOptions Options,
    IReadOnlyList<WorldTowerFloorAnalysisSnapshot> Floors);

/// <summary>
/// Runs the authored World Tower Region 1 encounters against deterministic P75
/// representatives. The adapter materializes the balance tool's detached
/// canonical builds, then uses production combat preparation, guardian scaling,
/// authored abilities, and the production combat engine.
/// </summary>
public sealed class WorldTowerContentAnalyzer(
    IWorldTowerDefinitionProvider towerDefinitions,
    IReadOnlyDictionary<Guid, Creature> creatures,
    ICombatSetupService combatSetup,
    ICombatEngineExecutor combatEngine,
    GearPackageFactory gearPackages) : IEncounterCalibrationEvaluator, IEncounterBuildEvaluator, IPartyFamilyCombatEvaluator,
    IEncounterScaleProbeCombatEvaluator
{
    public const int AlgorithmVersion = 8;
    public const string FailureRuleVersion = "world-tower-failure-observation-v3";
    public const string RegionOneBandId = "WorldTower.Region1";
    private readonly WorldTowerEncounterExecutor _encounterExecutor = new(combatSetup, combatEngine);

    public WorldTowerAnalysisSnapshot Analyze(
        ProgressionBandSuiteSnapshot progressionBands,
        PowerAnchorSuiteSnapshot powerAnchors,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        int runSeed,
        WorldTowerAnalysisOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(progressionBands);
        ArgumentNullException.ThrowIfNull(powerAnchors);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        var options = (requestedOptions ?? new WorldTowerAnalysisOptions()).Validate();
        var band = progressionBands.Bands.SingleOrDefault(candidate =>
                       candidate.Definition.Id.Equals(RegionOneBandId, StringComparison.Ordinal))
                   ?? throw new InvalidOperationException($"Progression band '{RegionOneBandId}' was not found.");
        var definitions = towerDefinitions.GetFloors()
            .Where(floor => floor.FloorNumber >= band.Definition.StartFloor
                            && floor.FloorNumber <= band.Definition.EndFloor)
            .OrderBy(floor => floor.FloorNumber)
            .ToArray();
        if (definitions.Length != band.Floors.Count
            || !definitions.Select(floor => floor.FloorNumber).SequenceEqual(band.Floors.Select(floor => floor.Floor)))
        {
            throw new InvalidOperationException(
                $"World Tower content does not define every floor in progression band '{RegionOneBandId}'.");
        }

        var p75Profiles = representativeBuilds.Profiles
            .Where(profile => profile.TargetPercentile == 75)
            .OrderBy(profile => profile.SlotCount)
            .ToArray();
        if (p75Profiles.Length == 0)
            throw new InvalidOperationException("World Tower analysis requires at least one P75 representative profile.");
        var startCr = ResolveAnchorCr(powerAnchors, band.Definition.StartAnchorId);
        var endCr = ResolveAnchorCr(powerAnchors, band.Definition.EndAnchorId);

        var floors = definitions.Select(definition => AnalyzeFloor(
                definition,
                band.Floors.Single(floor => floor.Floor == definition.FloorNumber),
                SelectProfile(p75Profiles, band.Floors.Single(floor => floor.Floor == definition.FloorNumber).TargetBenchmarkPower),
                startCr,
                endCr,
                runSeed,
                options))
            .ToArray();
        return new WorldTowerAnalysisSnapshot(AlgorithmVersion, options, floors);
    }

    private WorldTowerFloorAnalysisSnapshot AnalyzeFloor(
        TowerFloorDefinition definition,
        ProgressionFloorTargetSnapshot target,
        RepresentativeEssenceProfileSnapshot profile,
        double startCr,
        double endCr,
        int runSeed,
        WorldTowerAnalysisOptions options)
    {
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        if (profile.Builds.Count == 0)
            throw new InvalidOperationException($"Representative profile '{profile.Id}' contains no builds.");

        var preparedBuilds = profile.Builds.Select(PrepareBuild).ToArray();
        var resolvedGuardianProfile = Domain.Models.Essences.CreatureEssenceSource.GetMonsterDefinitionId(guardianSource);
        if (!resolvedGuardianProfile.Equals(definition.GuardianAbilityProfileId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"World Tower floor {definition.FloorNumber} expects guardian ability profile " +
                $"'{definition.GuardianAbilityProfileId}', but creature '{guardianSource.Name}' resolves to " +
                $"'{resolvedGuardianProfile}'.");
        }

        var trials = Enumerable.Range(1, options.SimulationsPerFloor)
            .Select(trial => RunTrial(
                definition,
                preparedBuilds,
                guardianSource,
                runSeed,
                trial,
                options.MaxTicks))
            .ToArray();
        var victories = trials.Where(trial => trial.Outcome == BattleOutcome.Victory.ToString()).ToArray();
        var clearRate = victories.Length / (double)trials.Length;
        var recommendedCr = startCr + (endCr - startCr) * target.CurveWeight;
        var observedClearingCr = victories.Length == 0
            ? (double?)null
            : Median(victories.Select(trial => trial.MeanPlayerDisplayCr).OrderBy(value => value).ToArray());
        var classification = clearRate < options.DesiredClearRate - options.ClearRateTolerance
            ? WorldTowerDifficultyClassification.TooHard
            : clearRate > options.DesiredClearRate + options.ClearRateTolerance
                ? WorldTowerDifficultyClassification.TooEasy
                : WorldTowerDifficultyClassification.OnTarget;
        var warnings = CreateWarnings(
            definition,
            profile,
            trials,
            victories.Length,
            recommendedCr,
            classification);
        var orderedDurations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();

        return new WorldTowerFloorAnalysisSnapshot(
            definition.FloorNumber,
            definition.Name,
            definition.GuardianName,
            definition.GuardianAbilityProfileId,
            definition.RequiredSlots,
            target.TargetBenchmarkPower,
            profile.Id,
            profile.MeanSelectedScore,
            profile.Builds.Count,
            definition.RecommendedPowerRating,
            Round(definition.GuardianScaling.Health, 3),
            Round(definition.GuardianScaling.Offense, 3),
            Round(recommendedCr, 2),
            observedClearingCr.HasValue ? Round(observedClearingCr.Value, 2) : null,
            Round(options.DesiredClearRate, 4),
            Round(clearRate, 4),
            Round(trials.Average(trial => trial.DurationTicks), 2),
            Round(Median(trials.Select(trial => (double)trial.DurationTicks).OrderBy(value => value).ToArray()), 2),
            Round(trials.Average(trial => trial.FriendlyDeaths), 2),
            Round(trials.Average(trial => trial.RemainingHealthRatio), 4),
            classification,
            warnings,
            trials)
        {
            P10DurationTicks = Round(Percentile(orderedDurations, 0.10), 2),
            P90DurationTicks = Round(Percentile(orderedDurations, 0.90), 2),
            AverageHostileDamagePerSecond = Round(trials.Average(trial => trial.HostileDamagePerSecond), 2),
            AveragePrimaryTargetDamageTaken = Round(trials.Average(trial => trial.PrimaryTargetDamageTaken), 2),
            AveragePartySustainPerSecond = Round(trials.Average(trial => trial.PartySustainPerSecond), 2),
            TerminalFailureCounts = CreateCountMap(trials.Select(trial => trial.FailureDiagnostic.TerminalFailure)),
            PrimaryObservedFailureModeCounts = CreateCountMap(
                trials.Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode))
        };
    }

    public EncounterCalibrationEvaluation Evaluate(EncounterCalibrationEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RepresentativeBuilds);
        if (!double.IsFinite(request.HealthAdjustmentFactor) || request.HealthAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.HealthAdjustmentFactor));
        if (!double.IsFinite(request.DamageAdjustmentFactor) || request.DamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DamageAdjustmentFactor));
        if (!double.IsFinite(request.DefenseAdjustmentFactor) || request.DefenseAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DefenseAdjustmentFactor));
        if (!double.IsFinite(request.ResistanceAdjustmentFactor) || request.ResistanceAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.ResistanceAdjustmentFactor));
        if (!double.IsFinite(request.RegenerationAdjustmentFactor) || request.RegenerationAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.RegenerationAdjustmentFactor));
        if (!double.IsFinite(request.AbilityHealingAdjustmentFactor) || request.AbilityHealingAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.AbilityHealingAdjustmentFactor));
        if (!double.IsFinite(request.SummonHealthPowerAdjustmentFactor) || request.SummonHealthPowerAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.SummonHealthPowerAdjustmentFactor));
        if (!double.IsFinite(request.DistributedDamageAdjustmentFactor) || request.DistributedDamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DistributedDamageAdjustmentFactor));
        if (request.Simulations is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));

        var definition = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                         ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        var profile = request.RepresentativeBuilds.Profiles.SingleOrDefault(candidate =>
                          candidate.Id.Equals(request.RepresentativeProfileId, StringComparison.Ordinal))
                      ?? throw new InvalidOperationException(
                          $"Representative profile '{request.RepresentativeProfileId}' was not found.");
        if (profile.Builds.Count == 0)
            throw new InvalidOperationException($"Representative profile '{profile.Id}' contains no builds.");
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        var calibratedDefinition = WithCalibration(
            definition,
            request.HealthAdjustmentFactor,
            request.DamageAdjustmentFactor,
            request.DefenseAdjustmentFactor,
            request.ResistanceAdjustmentFactor,
            request.RegenerationAdjustmentFactor);
        var preparedBuilds = profile.Builds.Select(PrepareBuild).ToArray();
        var trials = Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                calibratedDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks,
                request.AbilityHealingAdjustmentFactor,
                authoredSummonHealthPowerMultiplier: request.SummonHealthPowerAdjustmentFactor,
                authoredDistributedDamageMultiplier: request.DistributedDamageAdjustmentFactor))
            .ToArray();
        return CreateCalibrationEvaluation(trials);
    }

    public EncounterCalibrationEvaluation EvaluateBuilds(EncounterBuildEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Builds);
        if (request.Builds.Count == 0)
            throw new InvalidOperationException("Encounter build evaluation requires at least one build.");
        if (!double.IsFinite(request.HealthAdjustmentFactor) || request.HealthAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.HealthAdjustmentFactor));
        if (!double.IsFinite(request.DamageAdjustmentFactor) || request.DamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DamageAdjustmentFactor));
        if (!double.IsFinite(request.AbilityHealingAdjustmentFactor) || request.AbilityHealingAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.AbilityHealingAdjustmentFactor));
        if (!double.IsFinite(request.SummonHealthPowerAdjustmentFactor) || request.SummonHealthPowerAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.SummonHealthPowerAdjustmentFactor));
        if (!double.IsFinite(request.DistributedDamageAdjustmentFactor) || request.DistributedDamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.DistributedDamageAdjustmentFactor));
        if (request.Simulations is < 1 or > 1_000)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));

        var definition = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                         ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        var calibratedDefinition = WithCalibration(
            definition,
            request.HealthAdjustmentFactor,
            request.DamageAdjustmentFactor);
        var preparedBuilds = request.Builds.Select(PrepareBuild).ToArray();
        var trials = Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                calibratedDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks,
                request.AbilityHealingAdjustmentFactor,
                authoredSummonHealthPowerMultiplier: request.SummonHealthPowerAdjustmentFactor,
                authoredDistributedDamageMultiplier: request.DistributedDamageAdjustmentFactor))
            .ToArray();
        return CreateCalibrationEvaluation(trials);
    }

    public IReadOnlyList<WorldTowerTrialSnapshot> EvaluateParty(PartyFamilyCombatEvaluationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Builds);
        if (request.Simulations is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));
        if (!double.IsFinite(request.HealthAdjustmentFactor) || request.HealthAdjustmentFactor <= 0
            || !double.IsFinite(request.DamageAdjustmentFactor) || request.DamageAdjustmentFactor <= 0
            || !double.IsFinite(request.AbilityHealingAdjustmentFactor) || request.AbilityHealingAdjustmentFactor <= 0
            || !double.IsFinite(request.SummonHealthPowerAdjustmentFactor) || request.SummonHealthPowerAdjustmentFactor <= 0
            || !double.IsFinite(request.DistributedDamageAdjustmentFactor) || request.DistributedDamageAdjustmentFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Party-family calibration factors must be positive and finite.");
        var definition = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                         ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        if (request.Builds.Count != definition.RequiredSlots)
        {
            throw new InvalidOperationException(
                $"World Tower floor {request.Floor} requires {definition.RequiredSlots} exact party builds, but received {request.Builds.Count}.");
        }
        if (!creatures.TryGetValue(definition.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{definition.GuardianCreatureId}' was not found in creatures.json.");
        var calibratedDefinition = WithCalibration(
            definition,
            request.HealthAdjustmentFactor,
            request.DamageAdjustmentFactor);
        var preparedBuilds = request.Builds.Select(PrepareBuild).ToArray();
        return Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                calibratedDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks,
                request.AbilityHealingAdjustmentFactor,
                authoredSummonHealthPowerMultiplier: request.SummonHealthPowerAdjustmentFactor,
                authoredDistributedDamageMultiplier: request.DistributedDamageAdjustmentFactor))
            .ToArray();
    }

    private static EncounterCalibrationEvaluation CreateCalibrationEvaluation(
        IReadOnlyList<WorldTowerTrialSnapshot> trials)
    {
        var orderedDurations = trials.Select(trial => (double)trial.DurationTicks).Order().ToArray();
        var orderedDeaths = trials.Select(trial => (double)trial.FriendlyDeaths).Order().ToArray();
        var orderedHealth = trials.Select(trial => trial.RemainingHealthRatio).Order().ToArray();
        return new EncounterCalibrationEvaluation(
            trials.Count,
            Round(trials.Count(trial => trial.Outcome == BattleOutcome.Victory.ToString()) / (double)trials.Count, 4),
            Round(trials.Average(trial => trial.DurationTicks), 2),
            Round(trials.Average(trial => trial.FriendlyDeaths), 2),
            Round(trials.Average(trial => trial.RemainingHealthRatio), 4),
            Round(Median(orderedDurations), 2))
        {
            MedianFriendlyDeaths = Round(Median(orderedDeaths), 2),
            MedianRemainingHealthRatio = Round(Median(orderedHealth), 4),
            AverageCalibratedDistributedDamagePerSecond = Round(
                trials.Average(trial => trial.GuardianCalibratedDistributedDamagePerSecond), 2),
            AverageCalibratedDistributedDamagePeakTargetsPerWave = Round(
                trials.Average(trial => trial.GuardianCalibratedDistributedDamagePeakTargetsPerWave), 2),
            PrimaryObservedFailureModeCounts = trials
                .Select(trial => trial.FailureDiagnostic.PrimaryObservedFailureMode)
                .Where(mode => mode != WorldTowerObservedFailureMode.None)
                .GroupBy(mode => mode)
                .ToDictionary(group => group.Key, group => group.Count())
        };
    }

    public IReadOnlyList<WorldTowerTrialSnapshot> EvaluateScaleProbe(EncounterScaleProbeCombatRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Builds);
        request.AppliedOverride.Validate();
        if (request.PlayerCount is not (5 or 10 or 15))
            throw new ArgumentOutOfRangeException(nameof(request.PlayerCount));
        if (request.Builds.Count != request.PlayerCount)
            throw new InvalidOperationException(
                $"Scale probe requires {request.PlayerCount} exact builds, but received {request.Builds.Count}.");
        if (request.Simulations is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(request.Simulations));
        if (request.MaxTicks is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException(nameof(request.MaxTicks));
        if (request.AppliedOverride.Floor != request.Floor
            || request.AppliedOverride.PlayerCount != request.PlayerCount)
        {
            throw new InvalidOperationException("Scale-probe override does not match the requested floor and player count.");
        }

        var authored = towerDefinitions.GetFloors().SingleOrDefault(floor => floor.FloorNumber == request.Floor)
                       ?? throw new InvalidOperationException($"World Tower floor {request.Floor} was not found.");
        if (!creatures.TryGetValue(authored.GuardianCreatureId, out var guardianSource))
            throw new InvalidOperationException($"World Tower guardian '{authored.GuardianCreatureId}' was not found in creatures.json.");
        var probeDefinition = WithScaleProbe(authored, request.PlayerCount, request.AppliedOverride);
        var preparedBuilds = request.Builds.Select(PrepareBuild).ToArray();
        return Enumerable.Range(1, request.Simulations)
            .Select(trial => RunTrial(
                probeDefinition,
                preparedBuilds,
                guardianSource,
                request.RunSeed,
                trial,
                request.MaxTicks,
                request.AppliedOverride.GuardianAbilityHealingMultiplier,
                request.AppliedOverride.GuardianAdditionalSummonCopies,
                request.AppliedOverride.GuardianAdditionalSummonPotencyMultiplier,
                request.AppliedOverride.GuardianDistributedDamageMultiplier))
            .ToArray();
    }

    private WorldTowerPreparedBuild PrepareBuild(RepresentativeEssenceBuildSnapshot representative)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(representative.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            representative.Essences.Select(essence => essence.EssenceId).ToArray());
        return new WorldTowerPreparedBuild(
            representative.Id,
            representative.Character.CombatRating.DisplayOverall,
            canonical);
    }

    private WorldTowerPreparedBuild PrepareBuild(EssenceBuildSnapshot build)
    {
        var gearDefinition = GearPackageFactory.RegionOneDefinitions.Single(definition =>
            definition.Id.Equals(build.Character.GearPackageId, StringComparison.Ordinal));
        var canonical = gearPackages.CreateCanonicalBuild(
            gearDefinition,
            build.Essences.Select(essence => essence.EssenceId).ToArray());
        return new WorldTowerPreparedBuild(
            build.Id,
            build.Character.CombatRating.DisplayOverall,
            canonical);
    }

    private WorldTowerTrialSnapshot RunTrial(
        TowerFloorDefinition definition,
        IReadOnlyList<WorldTowerPreparedBuild> builds,
        Creature guardianSource,
        int runSeed,
        int trial,
        int maxTicks,
        double guardianAbilityHealingMultiplier = 1,
        int guardianAdditionalSummonCopies = 0,
        double guardianAdditionalSummonPotencyMultiplier = 1,
        double guardianDistributedDamageMultiplier = 1,
        double authoredSummonHealthPowerMultiplier = 1,
        double authoredDistributedDamageMultiplier = 1)
    {
        var selectionSeed = StableRandom.Seed(
            "balance-world-tower-party-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            definition.FloorNumber.ToString(CultureInfo.InvariantCulture),
            trial.ToString(CultureInfo.InvariantCulture));
        var random = new Random(selectionSeed);
        var selected = new List<WorldTowerPreparedBuild>(definition.RequiredSlots);
        while (selected.Count < definition.RequiredSlots)
        {
            var cycle = builds.OrderBy(_ => random.Next()).ToArray();
            selected.AddRange(cycle.Take(definition.RequiredSlots - selected.Count));
        }

        return _encounterExecutor.Execute(
            definition,
            selected,
            guardianSource,
            runSeed,
            trial,
            maxTicks,
            guardianAbilityHealingMultiplier,
            guardianAdditionalSummonCopies,
            guardianAdditionalSummonPotencyMultiplier,
            guardianDistributedDamageMultiplier,
            authoredSummonHealthPowerMultiplier,
            authoredDistributedDamageMultiplier);
    }

    private static RepresentativeEssenceProfileSnapshot SelectProfile(
        IReadOnlyList<RepresentativeEssenceProfileSnapshot> profiles,
        double targetPower) =>
        profiles.OrderBy(profile => Math.Abs(profile.MeanSelectedScore - targetPower))
            .ThenBy(profile => profile.SlotCount)
            .ThenBy(profile => profile.Id, StringComparer.Ordinal)
            .First();

    internal static WorldTowerFailureDiagnosticSnapshot AnalyzeFailure(
        CombatResult result,
        int maxTicks,
        IReadOnlyList<EntityStats> friendlyStats,
        IReadOnlyList<EntityStats> hostileStats,
        string guardianEntityId)
    {
        if (result.Outcome == BattleOutcome.Victory)
            return WorldTowerFailureDiagnosticSnapshot.Success;

        var terminalFailure = result.Outcome switch
        {
            BattleOutcome.Defeat => WorldTowerTerminalFailure.PartyDefeated,
            BattleOutcome.Draw => WorldTowerTerminalFailure.Timeout,
            _ when result.Duration >= maxTicks => WorldTowerTerminalFailure.Timeout,
            _ => WorldTowerTerminalFailure.Other
        };
        var evidence = new List<WorldTowerFailureEvidenceSnapshot>
        {
            new("duration_ticks", result.Duration, maxTicks, "ticks")
        };
        var contributing = new List<WorldTowerObservedFailureMode>();
        var finalDeaths = friendlyStats.Count(stats => stats.Health <= 0 || stats.Deaths > stats.Revivals);
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "friendly_final_deaths",
            finalDeaths,
            friendlyStats.Count,
            "combatants"));

        var primaryTarget = friendlyStats
            .OrderByDescending(stats => stats.AttentionSharePercent)
            .ThenByDescending(stats => stats.TargetedAttacks)
            .FirstOrDefault();
        var focusThreshold = Math.Max(30, 150d / Math.Max(1, friendlyStats.Count));
        var focusedMemberCollapsed = primaryTarget is not null
                                     && (primaryTarget.Health <= 0 || primaryTarget.Deaths > primaryTarget.Revivals)
                                     && primaryTarget.AttentionSharePercent >= focusThreshold;
        if (primaryTarget is not null)
        {
            evidence.Add(new WorldTowerFailureEvidenceSnapshot(
                "highest_attention_share",
                primaryTarget.AttentionSharePercent,
                focusThreshold,
                "percent",
                primaryTarget.EntityId));
        }

        var guardianStats = hostileStats.FirstOrDefault(stats =>
            stats.EntityId.Equals(guardianEntityId, StringComparison.OrdinalIgnoreCase));
        var guardianTargetDamage = friendlyStats.Sum(stats => stats.TargetInteractions
            .Where(target => target.TargetId.Equals(guardianEntityId, StringComparison.OrdinalIgnoreCase))
            .Sum(target => (long)target.DamageDone));
        var friendlyDamage = guardianTargetDamage > 0
            ? guardianTargetDamage
            : friendlyStats.Sum(stats => (long)stats.DamageDone);
        var guardianSelfSustainRatio = guardianStats is null
            ? 0
            : (guardianStats.HealthRegenerated + guardianStats.HealingDone)
              / (double)Math.Max(1, friendlyDamage);
        if (guardianStats is not null)
        {
            evidence.Add(new WorldTowerFailureEvidenceSnapshot(
                "guardian_self_sustain_to_friendly_damage",
                Round(guardianSelfSustainRatio, 4),
                0.20,
                "ratio",
                guardianStats.EntityId));
        }

        var firstFriendlyDeathTick = friendlyStats
            .Where(stats => stats.FirstDeathTick.HasValue)
            .Select(stats => stats.FirstDeathTick)
            .Min();
        if (firstFriendlyDeathTick.HasValue)
        {
            evidence.Add(new WorldTowerFailureEvidenceSnapshot(
                "first_friendly_death_tick",
                firstFriendlyDeathTick.Value,
                null,
                "ticks"));
        }
        var peakAdditionalHostiles = Math.Max(0, result.CompactTelemetry.PeakActiveHostileCombatants - 1);
        var finalAdditionalHostiles = Math.Max(0, result.CompactTelemetry.FinalActiveHostileCombatants - 1);
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "peak_additional_hostiles",
            peakAdditionalHostiles,
            null,
            "combatants"));
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "final_additional_hostiles",
            finalAdditionalHostiles,
            0,
            "combatants"));
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "friendly_cleanse_count",
            friendlyStats.Sum(stats => stats.StatusEffectsCleansed),
            null,
            "effects"));
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "friendly_dispel_count",
            friendlyStats.Sum(stats => stats.StatusEffectsDispelled),
            null,
            "effects"));
        evidence.Add(new WorldTowerFailureEvidenceSnapshot(
            "friendly_action_denied_ticks",
            friendlyStats.Sum(stats => stats.ActionDeniedTicks),
            null,
            "ticks"));

        WorldTowerObservedFailureMode primaryMode;
        double confidence;
        if (terminalFailure == WorldTowerTerminalFailure.PartyDefeated && focusedMemberCollapsed)
        {
            primaryMode = WorldTowerObservedFailureMode.PrimaryTargetCollapse;
            confidence = 0.80;
            if (finalDeaths > 1)
                contributing.Add(WorldTowerObservedFailureMode.PartyAttrition);
        }
        else if (terminalFailure == WorldTowerTerminalFailure.PartyDefeated)
        {
            primaryMode = WorldTowerObservedFailureMode.PartyAttrition;
            confidence = 0.75;
            if (focusedMemberCollapsed)
                contributing.Add(WorldTowerObservedFailureMode.PrimaryTargetCollapse);
        }
        else if (terminalFailure == WorldTowerTerminalFailure.Timeout && finalAdditionalHostiles > 0)
        {
            primaryMode = WorldTowerObservedFailureMode.AddPressure;
            confidence = 0.70;
        }
        else if (terminalFailure == WorldTowerTerminalFailure.Timeout && guardianSelfSustainRatio >= 0.20)
        {
            primaryMode = WorldTowerObservedFailureMode.BossSustainDominance;
            confidence = 0.65;
        }
        else
        {
            primaryMode = WorldTowerObservedFailureMode.Other;
            confidence = 0.50;
        }

        if (primaryMode != WorldTowerObservedFailureMode.BossSustainDominance
            && guardianSelfSustainRatio >= 0.20)
        {
            contributing.Add(WorldTowerObservedFailureMode.BossSustainDominance);
        }
        if (primaryMode != WorldTowerObservedFailureMode.AddPressure
            && peakAdditionalHostiles > 0
            && finalAdditionalHostiles > 0)
        {
            contributing.Add(WorldTowerObservedFailureMode.AddPressure);
        }

        return new WorldTowerFailureDiagnosticSnapshot(
            terminalFailure,
            primaryMode,
            confidence,
            contributing.Distinct().ToArray(),
            FailureRuleVersion,
            null,
            evidence);
    }

    private static double ResolveAnchorCr(PowerAnchorSuiteSnapshot anchors, string anchorId) =>
        anchors.Anchors.SingleOrDefault(anchor =>
                anchor.Definition.Id.Equals(anchorId, StringComparison.Ordinal))
            ?.CombatRating.MedianDisplayCr
        ?? throw new InvalidOperationException(
            $"Power anchor '{anchorId}' was not found for World Tower CR derivation.");

    private static IReadOnlyList<string> CreateWarnings(
        TowerFloorDefinition definition,
        RepresentativeEssenceProfileSnapshot profile,
        IReadOnlyList<WorldTowerTrialSnapshot> trials,
        int victoryCount,
        double recommendedCr,
        WorldTowerDifficultyClassification classification)
    {
        var warnings = new List<string>();
        if (classification == WorldTowerDifficultyClassification.TooHard)
            warnings.Add("Observed clear rate is below the configured target window.");
        else if (classification == WorldTowerDifficultyClassification.TooEasy)
            warnings.Add("Observed clear rate is above the configured target window.");
        if (victoryCount == 0)
            warnings.Add("No simulated party cleared; the observed clearing-CR threshold is not bounded.");
        else if (victoryCount == trials.Count)
            warnings.Add("Every simulated party cleared; the minimum viable CR threshold is not bounded.");
        var authoredDifference = Math.Abs(recommendedCr - definition.RecommendedPowerRating);
        if (authoredDifference >= Math.Max(10, definition.RecommendedPowerRating * 0.10))
        {
            warnings.Add(FormattableString.Invariant(
                $"Derived CR {Round(recommendedCr, 2):F2} differs materially from authored CR {definition.RecommendedPowerRating}."));
        }
        if (profile.MeanPairwiseSimilarity >= 0.75)
            warnings.Add("Selected representative builds have high Essence similarity; composition coverage is narrow.");
        return warnings;
    }

    private static TowerFloorDefinition WithCalibration(
        TowerFloorDefinition source,
        double healthAdjustment,
        double damageAdjustment,
        double defenseAdjustment = 1,
        double resistanceAdjustment = 1,
        double regenerationAdjustment = 1) => new()
    {
        FloorNumber = source.FloorNumber,
        Name = source.Name,
        Type = source.Type,
        GuardianCreatureId = source.GuardianCreatureId,
        GuardianName = source.GuardianName,
        GuardianAbilityProfileId = source.GuardianAbilityProfileId,
        RequiredSlots = source.RequiredSlots,
        RecommendedPowerRating = source.RecommendedPowerRating,
        ProgressionPosition = source.ProgressionPosition,
        GuardianScaling = new TowerGuardianScalingDefinition
        {
            Health = checked((float)(source.GuardianScaling.Health * healthAdjustment)),
            Offense = checked((float)(source.GuardianScaling.Offense * damageAdjustment)),
            Defense = checked((float)(source.GuardianScaling.Defense * defenseAdjustment)),
            Resistance = checked((float)(source.GuardianScaling.Resistance * resistanceAdjustment)),
            Penetration = source.GuardianScaling.Penetration,
            Regeneration = checked((float)(source.GuardianScaling.Regeneration * regenerationAdjustment))
        },
        Stagger = source.Stagger,
        EchoEnabledAfterClear = source.EchoEnabledAfterClear,
        TowerTokens = source.TowerTokens,
        Unlocks = source.Unlocks
    };

    private static TowerFloorDefinition WithScaleProbe(
        TowerFloorDefinition source,
        int playerCount,
        EncounterScaleProbeOverride appliedOverride) => new()
    {
        FloorNumber = source.FloorNumber,
        Name = source.Name,
        Type = source.Type,
        GuardianCreatureId = source.GuardianCreatureId,
        GuardianName = source.GuardianName,
        GuardianAbilityProfileId = source.GuardianAbilityProfileId,
        RequiredSlots = playerCount,
        RecommendedPowerRating = source.RecommendedPowerRating,
        ProgressionPosition = source.ProgressionPosition,
        GuardianScaling = new TowerGuardianScalingDefinition
        {
            Health = checked((float)(source.GuardianScaling.Health * appliedOverride.HealthMultiplier)),
            Offense = checked((float)(source.GuardianScaling.Offense * appliedOverride.OffenseMultiplier)),
            Defense = checked((float)(source.GuardianScaling.Defense * appliedOverride.DefenseMultiplier)),
            Resistance = checked((float)(source.GuardianScaling.Resistance * appliedOverride.ResistanceMultiplier)),
            Penetration = source.GuardianScaling.Penetration,
            Regeneration = checked((float)(source.GuardianScaling.Regeneration * appliedOverride.RegenerationMultiplier))
        },
        Stagger = source.Stagger,
        EchoEnabledAfterClear = source.EchoEnabledAfterClear,
        TowerTokens = source.TowerTokens,
        Unlocks = source.Unlocks
    };

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            throw new InvalidOperationException("Median requires at least one value.");
        var middle = values.Count / 2;
        return values.Count % 2 == 1
            ? values[middle]
            : (values[middle - 1] + values[middle]) / 2d;
    }

    private static double Percentile(IReadOnlyList<double> orderedValues, double percentile)
    {
        if (orderedValues.Count == 0)
            throw new InvalidOperationException("Percentile requires at least one value.");
        var position = percentile * (orderedValues.Count - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return orderedValues[lower];
        return orderedValues[lower] + (orderedValues[upper] - orderedValues[lower]) * (position - lower);
    }

    private static IReadOnlyDictionary<T, int> CreateCountMap<T>(IEnumerable<T> values)
        where T : struct, Enum =>
        values.GroupBy(value => value)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());

    internal static double RoundMetric(double value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    private static double Round(double value, int digits) => RoundMetric(value, digits);
}

public static class WorldTowerCreatureCatalog
{
    public static IReadOnlyDictionary<Guid, Creature> Load(string path, JsonSerializerOptions options)
    {
        var document = JsonSerializer.Deserialize<CreatureCatalogDocument>(File.ReadAllText(path), options)
                       ?? throw new InvalidOperationException("Creature catalog is empty or invalid.");
        var creatures = document.Creatures.Select(seed => new Creature
            {
                Id = seed.Id,
                Name = seed.Name,
                ImagePath = seed.ImagePath,
                Archetype = seed.Archetype,
                DamageProfile = seed.DamageProfile,
                DefenseProfile = seed.DefenseProfile,
                RewardTableId = seed.RewardTableId,
                BaseLevel = seed.BaseLevel,
                Level = seed.BaseLevel,
                Tier = seed.Tier,
                StatOverrides = seed.StatOverrides.Select(value => new StatOverride
                {
                    Id = value.Id,
                    AttributeType = value.AttributeType,
                    Multiplier = value.Multiplier,
                    Additive = value.Additive
                }).ToArray(),
                BaseAttributes = EntityBaseAttributeHelper.CreateEntityAttributes(seed.Id)
            })
            .ToArray();
        var duplicate = creatures.GroupBy(creature => creature.Id).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Creature catalog contains duplicate id '{duplicate.Key}'.");
        return creatures.ToDictionary(creature => creature.Id);
    }

    private sealed class CreatureCatalogDocument
    {
        public List<CreatureSeed> Creatures { get; init; } = [];
    }

    private sealed class CreatureSeed
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string ImagePath { get; init; } = string.Empty;
        public CreatureArchetype Archetype { get; init; } = CreatureArchetype.Balanced;
        public DamageProfile DamageProfile { get; init; } = DamageProfile.Hybrid;
        public DefenseProfile DefenseProfile { get; init; } = DefenseProfile.Balanced;
        public string? RewardTableId { get; init; }
        public int BaseLevel { get; init; } = 1;
        public int Tier { get; init; } = 1;
        public List<CreatureStatOverrideSeed> StatOverrides { get; init; } = [];
    }

    private sealed class CreatureStatOverrideSeed
    {
        public Guid Id { get; init; }
        public AttributeType AttributeType { get; init; }
        public float? Multiplier { get; init; }
        public float? Additive { get; init; }
    }
}
