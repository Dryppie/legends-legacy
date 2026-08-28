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
    TimeProvider timeProvider)
{
    public const int BalanceSchemaVersion = 15;
    public const string SmokeScenarioId = "production-essence-smoke-1v1";

    public BalanceRunReport Run(BalanceRunRequest request)
    {
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
        var metaOptions = (request.EssenceMetaAnalysisOptions ?? new EssenceMetaAnalysisOptions()).Validate();
        var metaSimulatorEvidence = metaSimulator.Run(new AbilityBalanceSimulationRequest(
            BattleCount: metaOptions.SimulatorBattleCount,
            TeamSize: 1,
            EssencesPerParticipant: 1,
            RandomSeed: StableRandom.Seed(
                "balance-essence-meta-simulator-v1",
                simulation.RandomSeed.ToString(CultureInfo.InvariantCulture)),
            TopResults: essences.Length,
            CandidatePoolSize: essences.Length,
            CandidateTeams: null,
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
            scalingValidation);
    }

    private static string CreateRunId(DateTimeOffset createdAtUtc) =>
        $"{createdAtUtc.ToUniversalTime().ToString("yyyyMMddTHHmmssfff'Z'", CultureInfo.InvariantCulture)}-{Guid.NewGuid():N}"[..28];
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
    ScalingValidationOptions? ScalingValidationOptions = null);

public sealed record BalanceRunReport(
    BalanceRunMetadata Metadata,
    BalanceContentSummary Content,
    BalanceSimulationSummary Simulation,
    IReadOnlyList<GearPackageSnapshot> GearPackages,
    IReadOnlyList<EssenceBuildSnapshot> EssenceBuilds,
    PveBenchmarkSuiteSnapshot Benchmarks,
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
    ScalingValidationSnapshot ScalingValidation);

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
