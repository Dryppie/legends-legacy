using Application.Interfaces.Services.LL.Essences;
using Common.Randomness;
using Services.LL.Combat.Engine;
using System.Globalization;

namespace LegendsLegacy.Balance;

public sealed class ProductionBalanceRunner(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    IAbilityBalanceSimulator simulator,
    IAbilityBalanceSimulator metaSimulator,
    GearPackageFactory gearPackages,
    EssenceBuildGenerator essenceBuilds,
    PveBenchmarkRunner benchmarks,
    BuildCapabilityProfiler capabilityProfiler,
    PartyFamilyBuilder partyFamilyBuilder,
    PartyFamilyEncounterEvaluator partyFamilyEncounterEvaluator,
    EncounterScaleProbeAnalyzer encounterScaleProbeAnalyzer,
    RegionOneReliabilityStudyAnalyzer regionOneReliabilityStudyAnalyzer,
    RegionOneMatchedGenomeProgressionAnalyzer matchedGenomeProgressionAnalyzer,
    CombatRatingAnalyzer combatRatingAnalyzer,
    EssenceBuildOptimizer optimizer,
    RepresentativeBuildLibrary representativeBuilds,
    EssenceMetaAnalyzer essenceMeta,
    PowerAnchorAnalyzer powerAnchors,
    ProgressionBandBuilder progressionBands,
    WorldTowerContentAnalyzer worldTower,
    EncounterCalibrator encounterCalibrator,
    EncounterSpecificOptimizer encounterSpecificOptimizer,
    EliteBuildCertificationAnalyzer eliteBuildCertificationAnalyzer,
    ScalingValidationAnalyzer scalingValidationAnalyzer,
    FloorProgressionPolicyEvaluator floorProgressionPolicyEvaluator,
    AutomaticFloorProgressionCalibrator automaticFloorProgressionCalibrator,
    TimeProvider timeProvider)
{
    public const int BalanceSchemaVersion = 54;
    public const string SmokeScenarioId = "production-essence-smoke-1v1";

    public BalanceRunReport Run(BalanceRunRequest request)
    {
        var floorProgressionPolicy = request.FloorProgressionPolicy?.Validate();
        var catalog = catalogProvider.GetCatalog();
        var essences = essenceDefinitions.GetAll()
            .Where(essence =>
                !string.IsNullOrWhiteSpace(essence.Id)
                && !essence.Id.Equals("essence.training", StringComparison.OrdinalIgnoreCase))
            .OrderBy(essence => essence.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (essences.Length < 2)
            throw new InvalidOperationException("The production catalog must contain at least two usable Essences.");

        var friendlyEssenceId = essences[0].Id;
        var hostileEssenceId = essences[1].Id;
        var simulation = simulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: 1,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: request.Seed,
            TopResults: 2,
            CandidatePoolSize: 2,
            CandidateTeams:
            [
                new AbilityBalanceTeamLoadout(
                    [new AbilityBalanceParticipantLoadout([friendlyEssenceId])]),
                new AbilityBalanceTeamLoadout(
                    [new AbilityBalanceParticipantLoadout([hostileEssenceId])])
            ]));
        var battle = simulation.BattleSummaries.Single();
        var regionOneGearPackages = gearPackages.CreateRegionOneAnchors();
        var generatedEssenceBuilds = essenceBuilds.GenerateInitialProfiles(
            simulation.RandomSeed,
            request.EssenceBuildsPerProfile);
        var benchmarkSuite = benchmarks.Run(generatedEssenceBuilds, simulation.RandomSeed);
        var combatRatingHealth = combatRatingAnalyzer.Analyze(generatedEssenceBuilds, benchmarkSuite);
        var optimization = optimizer.Optimize(
            generatedEssenceBuilds,
            benchmarkSuite,
            simulation.RandomSeed,
            request.OptimizerOptions);
        var representativeBuildLibrary = representativeBuilds.Create(
            optimization.EvaluatedCandidates,
            simulation.RandomSeed,
            optimization.Snapshot.Options.DiversityPenalty,
            request.RepresentativeBuildOptions);
        var capabilityInputs = CreateCapabilityInputs(
            generatedEssenceBuilds,
            benchmarkSuite,
            optimization.EvaluatedCandidates,
            representativeBuildLibrary);
        var buildCapabilities = capabilityProfiler.Profile(
            capabilityInputs.Builds,
            capabilityInputs.Benchmarks,
            simulation.RandomSeed,
            request.BuildCapabilityOptions);
        var metaOptions = (request.EssenceMetaAnalysisOptions ?? new EssenceMetaAnalysisOptions()).Validate();
        var balancedSingletonTeams = metaOptions.SimulatorRoundsPerMatchup > 0
            ? essences.Select(essence => new AbilityBalanceTeamLoadout(
                    [new AbilityBalanceParticipantLoadout([essence.Id])]))
                .ToArray()
            : null;
        var metaSimulatorEvidence = metaSimulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: metaOptions.SimulatorRoundsPerMatchup > 0
                ? metaOptions.SimulatorRoundsPerMatchup
                : metaOptions.SimulatorBattleCount,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: StableRandom.Seed(
                "balance-essence-meta-simulator-v1",
                simulation.RandomSeed.ToString(CultureInfo.InvariantCulture)),
            TopResults: essences.Length,
            CandidatePoolSize: essences.Length,
            CandidateTeams: balancedSingletonTeams,
            EquipmentTier: 1,
            EquipmentRarity: "Rare",
            EquipmentProfile: "Balanced"));
        var essenceMetaAnalysis = essenceMeta.Analyze(
            optimization.EvaluatedCandidates,
            metaSimulatorEvidence,
            metaOptions);
        var powerAnchorMeasurements = powerAnchors.Analyze(
            regionOneGearPackages,
            representativeBuildLibrary);
        var progressionBandTargets = progressionBands.Create(
            powerAnchorMeasurements,
            request.ProgressionBandOptions);
        var worldTowerAnalysis = worldTower.Analyze(
            progressionBandTargets,
            powerAnchorMeasurements,
            representativeBuildLibrary,
            simulation.RandomSeed,
            request.WorldTowerAnalysisOptions);
        var encounterCalibration = encounterCalibrator.Calibrate(
            worldTowerAnalysis,
            representativeBuildLibrary,
            simulation.RandomSeed,
            request.EncounterCalibrationOptions);
        var encounterSpecificOptimization = encounterSpecificOptimizer.Optimize(
            optimization.EvaluatedCandidates,
            representativeBuildLibrary,
            worldTowerAnalysis,
            encounterCalibration,
            simulation.RandomSeed,
            request.EncounterSpecificOptimizationOptions);
        var eliteBuildCertification = eliteBuildCertificationAnalyzer.Certify(
            essenceMetaAnalysis,
            representativeBuildLibrary,
            worldTowerAnalysis,
            encounterCalibration,
            simulation.RandomSeed,
            request.EliteCertificationPolicy,
            request.EliteCertificationOptions);
        var scalingValidation = scalingValidationAnalyzer.Validate(
            worldTowerAnalysis,
            representativeBuildLibrary,
            encounterCalibration,
            simulation.RandomSeed,
            request.ScalingValidationOptions);
        var partyFamilies = partyFamilyBuilder.Build(
            representativeBuildLibrary,
            buildCapabilities,
            worldTowerAnalysis,
            eliteBuildCertification,
            simulation.RandomSeed,
            request.PartyFamilyBuilderOptions);
        var partyFamilyEvaluation = partyFamilyEncounterEvaluator.Evaluate(
            partyFamilies,
            representativeBuildLibrary,
            worldTowerAnalysis,
            eliteBuildCertification,
            simulation.RandomSeed,
            request.PartyFamilyEvaluationOptions,
            request.PartyFamilyCertificationPolicy);
        var floorProgressionPolicyEvaluation = floorProgressionPolicy is null
            ? new FloorProgressionPolicyEvaluationSnapshot(
                FloorProgressionPolicyEvaluator.AlgorithmVersion,
                "disabled",
                0,
                string.Empty,
                ProductionContentModified: false,
                FloorProgressionVerdict.Disabled,
                [],
                ["Floor-to-progression policy evaluation was not configured for this run."])
            : floorProgressionPolicyEvaluator.Evaluate(
                floorProgressionPolicy,
                representativeBuildLibrary,
                worldTowerAnalysis,
                partyFamilyEvaluation,
                eliteBuildCertification);
        var automaticFloorProgressionCalibration = floorProgressionPolicy is null
            ? new AutomaticFloorProgressionCalibrationSnapshot(
                AutomaticFloorProgressionCalibrator.AlgorithmVersion,
                simulation.RandomSeed,
                (request.AutomaticFloorProgressionCalibrationOptions
                    ?? new AutomaticFloorProgressionCalibrationOptions()).Validate(),
                "disabled",
                string.Empty,
                CommonCandidateSeeds: true,
                IndependentHoldoutSeeds: true,
                ProductionContentModified: false,
                AutomaticFloorProgressionCalibrationVerdict.Disabled,
                0,
                0,
                [],
                ["Automatic floor-to-progression calibration requires an authored policy suite."])
            : automaticFloorProgressionCalibrator.Calibrate(
                floorProgressionPolicy,
                floorProgressionPolicyEvaluation,
                representativeBuildLibrary,
                worldTowerAnalysis,
                partyFamilies,
                eliteBuildCertification,
                simulation.RandomSeed,
                request.AutomaticFloorProgressionCalibrationOptions);
        var encounterScaleProbes = encounterScaleProbeAnalyzer.Analyze(
            worldTowerAnalysis,
            representativeBuildLibrary,
            buildCapabilities,
            partyFamilies,
            partyFamilyEvaluation,
            simulation.RandomSeed,
            request.EncounterScaleProbeOptions);
        var reliabilityOptions = request.RegionOneReliabilityStudyOptions ?? new RegionOneReliabilityStudyOptions();
        var matchedGenomeProgression = matchedGenomeProgressionAnalyzer.Analyze(
            generatedEssenceBuilds,
            simulation.RandomSeed,
            reliabilityOptions.Enabled && reliabilityOptions.ProgressionFidelityEnabled);
        var populationProtocol = new RegionOneReliabilityPopulationProtocolSnapshot(
            BalanceSchemaVersion,
            request.EssenceBuildsPerProfile,
            benchmarkSuite.ScoringVersion,
            optimization.Snapshot.AlgorithmVersion,
            optimization.Snapshot.Options,
            representativeBuildLibrary.AlgorithmVersion,
            representativeBuildLibrary.Options,
            buildCapabilities.AlgorithmVersion,
            buildCapabilities.NormalizationVersion,
            buildCapabilities.ContentFingerprint,
            buildCapabilities.ProbeSeedCount,
            partyFamilies.AlgorithmVersion,
            partyFamilies.Options,
            worldTowerAnalysis.AlgorithmVersion,
            worldTowerAnalysis.Options);
        var regionOneReliabilityStudy = regionOneReliabilityStudyAnalyzer.Analyze(
            worldTowerAnalysis,
            representativeBuildLibrary,
            partyFamilies,
            simulation.RandomSeed,
            request.RegionOneReliabilityStudyOptions,
            buildCapabilities,
            catalog.Abilities,
            matchedGenomeProgression,
            populationProtocol);
        var createdAtUtc = timeProvider.GetUtcNow();
        var runId = CreateRunId(createdAtUtc);
        var engineVersion = typeof(FastCombatEngine).Assembly.GetName().Version?.ToString() ?? "unknown";

        return new BalanceRunReport(
            new BalanceRunMetadata(
                runId,
                createdAtUtc,
                simulation.RandomSeed,
                BalanceSchemaVersion,
                AbilityBalanceSimulator.AlgorithmVersion,
                engineVersion,
                request.GitCommitHash),
            new BalanceContentSummary(
                catalog.Abilities.Count,
                catalog.Statuses.Count,
                catalog.Summons.Count,
                essences.Length),
            new BalanceSimulationSummary(
                SmokeScenarioId,
                battle.FriendlyDisplayName,
                friendlyEssenceId,
                battle.HostileDisplayName,
                hostileEssenceId,
                battle.Outcome,
                battle.Duration,
                battle.FriendlyDamageDone,
                battle.FriendlyDamageTaken,
                battle.HostileDamageDone,
                battle.HostileDamageTaken),
            regionOneGearPackages,
            generatedEssenceBuilds,
            benchmarkSuite,
            buildCapabilities,
            partyFamilies,
            partyFamilyEvaluation,
            encounterScaleProbes,
            regionOneReliabilityStudy,
            combatRatingHealth,
            optimization.Snapshot,
            representativeBuildLibrary,
            essenceMetaAnalysis,
            powerAnchorMeasurements,
            progressionBandTargets,
            worldTowerAnalysis,
            encounterCalibration,
            encounterSpecificOptimization,
            eliteBuildCertification,
            scalingValidation,
            floorProgressionPolicyEvaluation,
            automaticFloorProgressionCalibration);
    }

    private static string CreateRunId(DateTimeOffset createdAtUtc) =>
        $"{createdAtUtc.ToUniversalTime().ToString("yyyyMMddTHHmmssfff'Z'", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}"[..28];

    private static CapabilityInputs CreateCapabilityInputs(
        IReadOnlyList<EssenceBuildSnapshot> generatedBuilds,
        PveBenchmarkSuiteSnapshot generatedBenchmarks,
        IReadOnlyList<EssenceOptimizerEvaluatedCandidate> evaluatedCandidates,
        RepresentativeBuildLibrarySnapshot representativeBuilds)
    {
        var requiredIds = generatedBuilds.Select(build => build.Id)
            .Concat(representativeBuilds.Profiles
            .SelectMany(profile => profile.Builds)
                .Select(build => build.SourceBuildId))
            .ToHashSet(StringComparer.Ordinal);
        var buildsById = evaluatedCandidates.Select(candidate => candidate.Build)
            .Concat(generatedBuilds)
            .GroupBy(build => build.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var benchmarksById = evaluatedCandidates.Select(candidate => candidate.Benchmark)
            .Concat(generatedBenchmarks.Builds)
            .GroupBy(benchmark => benchmark.BuildId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var missingBuilds = requiredIds.Where(id => !buildsById.ContainsKey(id)).Order().ToArray();
        var missingBenchmarks = requiredIds.Where(id => !benchmarksById.ContainsKey(id)).Order().ToArray();
        if (missingBuilds.Length > 0 || missingBenchmarks.Length > 0)
        {
            throw new InvalidOperationException(
                $"Capability cohort is incomplete. Missing builds: {string.Join(", ", missingBuilds)}; " +
                $"missing benchmarks: {string.Join(", ", missingBenchmarks)}.");
        }
        var orderedIds = requiredIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        return new CapabilityInputs(
            orderedIds.Select(id => buildsById[id]).ToArray(),
            new PveBenchmarkSuiteSnapshot(
                generatedBenchmarks.ScoringVersion,
                generatedBenchmarks.Scenarios,
                orderedIds.Select(id => benchmarksById[id]).ToArray()));
    }

    private sealed record CapabilityInputs(
        IReadOnlyList<EssenceBuildSnapshot> Builds,
        PveBenchmarkSuiteSnapshot Benchmarks);
}

public sealed record BalanceRunRequest(
    int Seed,
    string? GitCommitHash = null,
    int EssenceBuildsPerProfile = 10,
    EssenceOptimizerOptions? OptimizerOptions = null,
    RepresentativeBuildOptions? RepresentativeBuildOptions = null,
    ProgressionBandOptions? ProgressionBandOptions = null,
    WorldTowerAnalysisOptions? WorldTowerAnalysisOptions = null,
    EssenceMetaAnalysisOptions? EssenceMetaAnalysisOptions = null,
    EncounterCalibrationOptions? EncounterCalibrationOptions = null,
    EncounterSpecificOptimizationOptions? EncounterSpecificOptimizationOptions = null,
    EliteCertificationPolicy? EliteCertificationPolicy = null,
    EliteCertificationOptions? EliteCertificationOptions = null,
    ScalingValidationOptions? ScalingValidationOptions = null,
    BuildCapabilityOptions? BuildCapabilityOptions = null,
    PartyFamilyBuilderOptions? PartyFamilyBuilderOptions = null,
    PartyFamilyEvaluationOptions? PartyFamilyEvaluationOptions = null,
    PartyFamilyCertificationPolicy? PartyFamilyCertificationPolicy = null,
    EncounterScaleProbeOptions? EncounterScaleProbeOptions = null,
    RegionOneReliabilityStudyOptions? RegionOneReliabilityStudyOptions = null,
    FloorProgressionPolicySuite? FloorProgressionPolicy = null,
    AutomaticFloorProgressionCalibrationOptions? AutomaticFloorProgressionCalibrationOptions = null);

public sealed record BalanceRunReport(
    BalanceRunMetadata Metadata,
    BalanceContentSummary Content,
    BalanceSimulationSummary Simulation,
    IReadOnlyList<GearPackageSnapshot> GearPackages,
    IReadOnlyList<EssenceBuildSnapshot> EssenceBuilds,
    PveBenchmarkSuiteSnapshot Benchmarks,
    BuildCapabilitySuiteSnapshot BuildCapabilities,
    PartyFamilySuiteSnapshot PartyFamilies,
    PartyFamilyEvaluationSuiteSnapshot PartyFamilyEvaluation,
    EncounterScaleProbeSuiteSnapshot EncounterScaleProbes,
    RegionOneReliabilityStudySnapshot RegionOneReliabilityStudy,
    CombatRatingHealthSnapshot CombatRatingHealth,
    EssenceOptimizerSnapshot Optimizer,
    RepresentativeBuildLibrarySnapshot RepresentativeBuilds,
    EssenceMetaAnalysisSnapshot EssenceMetaAnalysis,
    PowerAnchorSuiteSnapshot PowerAnchors,
    ProgressionBandSuiteSnapshot ProgressionBands,
    WorldTowerAnalysisSnapshot WorldTowerAnalysis,
    EncounterCalibrationSnapshot EncounterCalibration,
    EncounterSpecificOptimizationSnapshot EncounterSpecificOptimization,
    EliteBuildCertificationSnapshot EliteBuildCertification,
    ScalingValidationSnapshot ScalingValidation,
    FloorProgressionPolicyEvaluationSnapshot FloorProgressionPolicyEvaluation,
    AutomaticFloorProgressionCalibrationSnapshot AutomaticFloorProgressionCalibration);

public sealed record BalanceRunMetadata(
    string RunId,
    DateTimeOffset CreatedAtUtc,
    int Seed,
    int BalanceSchemaVersion,
    int SimulatorAlgorithmVersion,
    string CombatEngineVersion,
    string? GitCommitHash);

public sealed record BalanceContentSummary(
    int AbilityCount,
    int StatusCount,
    int SummonCount,
    int EssenceCount);

public sealed record BalanceSimulationSummary(
    string ScenarioId,
    string FriendlyBuild,
    string FriendlyEssenceId,
    string HostileBuild,
    string HostileEssenceId,
    string Outcome,
    int DurationTicks,
    int FriendlyDamageDone,
    int FriendlyDamageTaken,
    int HostileDamageDone,
    int HostileDamageTaken);
