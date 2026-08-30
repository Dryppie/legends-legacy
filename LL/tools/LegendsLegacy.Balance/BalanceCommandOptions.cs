using System.Globalization;

namespace LegendsLegacy.Balance;

public sealed record BalanceCommandOptions(
    int Seed,
    int EssenceBuildsPerProfile,
    int CapabilityProbeSeedCount,
    int PartyFamilySamplesPerFamily,
    int PartyFamilySimulationsPerParty,
    EncounterScaleProbeOptions EncounterScaleProbeOptions,
    RegionOneReliabilityStudyOptions RegionOneReliabilityStudyOptions,
    AutomaticFloorProgressionCalibrationOptions AutomaticFloorProgressionCalibrationOptions,
    EssenceOptimizerOptions OptimizerOptions,
    RepresentativeBuildOptions RepresentativeBuildOptions,
    ProgressionBandOptions ProgressionBandOptions,
    WorldTowerAnalysisOptions WorldTowerAnalysisOptions,
    EssenceMetaAnalysisOptions EssenceMetaAnalysisOptions,
    EncounterCalibrationOptions EncounterCalibrationOptions,
    EncounterSpecificOptimizationOptions EncounterSpecificOptimizationOptions,
    EliteCertificationOptions EliteCertificationOptions,
    ScalingValidationOptions ScalingValidationOptions,
    string? ElitePolicyPath,
    string? ContentRoot,
    string? OutputRoot,
    bool ShowHelp)
{
    public const int DefaultSeed = 1337;

    public const string Usage = """
        LegendsLegacy balance runner

        Usage:
          dotnet run --project LL/tools/LegendsLegacy.Balance -- [--full] [options]

        Options:
          --seed <number>         Deterministic simulation seed (default: 1337).
          --build-count <number>  Random builds per 4/5/6-slot profile (default: 10).
          --capability-seeds <number> Common support/wave probe seeds per build, 1-32 (default: 1).
          --party-family-samples <number> Deterministic roster samples per family, 1-50 (default: 3).
          --party-family-simulations <number> Common-seed encounter trials per retained roster, 1-100 (developer default: 1; release: 25).
          --scale-probes          Run isolated balance-only 5/10/15-player encounter probes (default: disabled).
          --scale-probe-parties <number> Balanced rosters per probed size, 1-20 (default: 1).
          --scale-probe-simulations <number> Production-combat trials per scale-probe roster, 1-100 (default: 1).
          --scale-probe-max-ms-per-trial <number> Optional diagnostic wall-time ceiling per trial.
          --scale-probe-max-allocated-mb-per-trial <number> Optional allocation ceiling per trial in MiB.
          --scale-probe-min-ticks-per-second <number> Optional simulated-tick throughput floor.
          --scale-probe-max-peak-memory-mb <number> Optional process peak-working-set ceiling in MiB.
          --reliability-study                Run the optional Region 1 neutral-reference fault-injection study.
          --reliability-rosters <number>     Exact valid rosters per tested family, 1-15 (default: 3).
          --reliability-simulations <number> Common-seed trials per reliability roster, 5-100 (default: 10).
          --reliability-fault-multiplier <number> One-knob injected multiplier, >1-2 (default: 1.40).
          --floor-progression-calibration         Run the Floor 1/7 constrained continuous-knob pilot (default: disabled).
          --floor-progression-simulations <number> Common-seed trials per search candidate, 1-1000 (default: 10).
          --floor-progression-holdout-simulations <number> Independent holdout trials per candidate, 1-1000 (default: 25).
          --floor-progression-sensitivity-points <number> Ordered sensitivity points, 2-20 (default: 5).
          --floor-progression-refinement-iterations <number> Boundary refinements, 0-20 (default: 4).
          --optimizer-population <number>  Candidates per profile (default: 20).
          --optimizer-generations <number> Generations to evolve (default: 4).
          --optimizer-elites <number>      Elites retained per generation (default: 5).
          --optimizer-mutation <number>    Per-slot mutation rate, 0.01-1.00 (default: 0.25).
          --optimizer-random <number>      Random injection rate, 0.00-0.50 (default: 0.10).
          --optimizer-diversity <number>   Similarity penalty, 0-100 (default: 8).
          --optimizer-retained <number>    Final candidates per profile (default: 10).
          --representative-count <number>  Builds retained per P50/P75/P90 profile (default: 10).
          --progression-curve <value>      linear, ease-in, ease-out, or smooth-step (default).
          --tower-simulations <number>     Seeded party simulations per Floor 1-10 (default: 10).
          --calibration-iterations <number>  Bounded encounter-search iterations (default: 10).
          --assisted-calibration            Run evidence-gated single-parameter sensitivity probes and holdout checks (default: disabled).
          --assisted-calibration-simulations <number> Trials per sensitivity/holdout evaluation; 0 inherits Tower simulations (default: 0).
          --encounter-candidate-simulations <number>  Trials per specialized candidate (default: 3).
          --encounter-retained <number>      Specialized builds retained per floor (default: 5).
          --certification-profile <value>    developer (default) or release.
          --elite-search-only                Skip holdouts and party search; never certifies.
          --elite-restarts <number>          Independent certification restarts.
          --elite-population <number>        Candidates per restart and E4/E5/E6 profile.
          --elite-generations <number>       Search generations per certification restart.
          --elite-max-generations <number>   Hard ceiling for adaptive certification generations.
          --elite-elites <number>            Elites retained per certification generation.
          --elite-crossover <number>         Experimental elite-parent crossover rate, 0.00-1.00 (default: 0).
          --elite-basin-jump <number>        Restart-local coordinated 3/4-gene mutation rate, 0.00-1.00 (default: 0).
          --elite-explorer-archive <number>  Persistent restart-local explorer candidates, 0-100 (default: 0).
          --elite-stratified-portfolio <number>  Isolated deterministic candidates per restart/profile, 0-5000 (default: 0).
          --elite-quality-island <number> Restart-local quality-diversity island budget per profile, 0-5000 (default: 0).
          --elite-mechanic-island <number> Restart-local mechanic-archetype island budget per profile, 0-5000 (default: 0).
          --elite-descriptor-audit          Audit known E5 basin descriptor separability; never affects search or certification.
          --elite-benchmark-confidence-audit  Repeat a stratified E5 cohort on common PvE seeds; diagnostic only.
          --elite-confidence-cohort <number>  E5 builds in the confidence audit (default: 512).
          --elite-confidence-seeds <number>   Common PvE seed replicates (default: 16).
          --elite-confidence-margin <number>  Target 95% score half-width (default: 0.25).
          --elite-valley-beam-width <number> Opt-in restart valley-search beam width; 0 disables.
          --elite-valley-beam-depth <number> Opt-in restart valley-search depth; 0 disables.
          --elite-valley-budget <number>     Candidate budget per restart/profile; 0 disables.
          --elite-valley-prefilter <number> Fully benchmark at most this many valley candidates per depth; 0 disables.
          --elite-bridge-audit              Audit minimum-substitution bridges between differing restart winners.
          --elite-finalists <number>         Pareto-diverse finalists per slot profile.
          --elite-local-swap-depth <1|2>     Local-neighborhood challenge depth.
          --elite-two-swap-limit <number>    Two-swap challengers per finalist; 0 means complete.
          --elite-restart-refinement <number>  Local refinement passes per restart beam seed.
          --elite-restart-seeds <number>     Pareto-diverse refinement seeds per restart.
          --elite-restart-two-swap-limit <number>  Two-swap escape candidates per stalled restart pass; 0 disables.
          --elite-finalist-refinement <number> Neighborhood absorption rounds before final challenge.
          --elite-holdout-seeds <number>     Independent elite holdout seeds.
          --elite-simulations <number>       Elite holdout simulations per seed.
          --elite-party-genomes <number>     Party genomes evaluated per floor.
          --elite-policy <path>              Certification policy JSON override.
          --top-player-builds <path>         Curated top-player fixture JSON override.
          --validation-seeds <number>        Deterministic holdout seeds per floor (default: 8).
          --validation-simulations <number>  Calibrated trials per holdout seed (default: 50).
          --validation-probe-simulations <number>  Trials per sensitivity probe and seed (default: 25).
          --meta-simulator-battles <number>  Complementary 1v1 Essence battles (default: 2000).
          --meta-simulator-rounds-per-matchup <number>  Balanced all-Essence round robin; 0 disables (default: 0).
          --content-root <path>   API.LL directory containing the production Data folder.
          --output <path>         Report root (default: <repository>/balance-output).
          --full                  Run the currently implemented balance pipeline.
          --help, -h              Show this help.
        """;

    public static BalanceCommandOptions Parse(IReadOnlyList<string> args)
    {
        var seed = DefaultSeed;
        var essenceBuildsPerProfile = 10;
        var certificationProfile = ReadCertificationProfile(args);
        var capabilityProbeSeedCount = 1;
        var partyFamilySamplesPerFamily = PartyFamilyCertificationPolicy.V1.MinimumReleasePartiesPerRegularFamily;
        var partyFamilySimulationsPerParty = certificationProfile == EliteCertificationProfile.Release
            ? PartyFamilyCertificationPolicy.V1.MinimumReleaseSimulationsPerParty
            : 1;
        var scaleProbesEnabled = false;
        var scaleProbeParties = 1;
        var scaleProbeSimulations = 1;
        double? scaleProbeMaximumMillisecondsPerTrial = null;
        double? scaleProbeMaximumAllocatedMebibytesPerTrial = null;
        double? scaleProbeMinimumTicksPerSecond = null;
        double? scaleProbeMaximumPeakMemoryMebibytes = null;
        var reliabilityStudyEnabled = false;
        var reliabilityRosters = 3;
        var reliabilitySimulations = 10;
        var reliabilityFaultMultiplier = 1.40;
        var floorProgressionCalibrationEnabled = false;
        var floorProgressionSimulations = 10;
        var floorProgressionHoldoutSimulations = 25;
        var floorProgressionSensitivityPoints = 5;
        var floorProgressionRefinementIterations = 4;
        var optimizerPopulation = 20;
        var optimizerGenerations = 4;
        var optimizerElites = 5;
        var optimizerMutation = 0.25;
        var optimizerRandom = 0.10;
        var optimizerDiversity = 8d;
        var optimizerRetained = 10;
        var representativeCount = 10;
        var progressionCurve = ProgressionCurveKind.SmoothStep;
        var towerSimulations = 10;
        var calibrationIterations = 10;
        var assistedCalibrationEnabled = false;
        var assistedCalibrationSimulations = 0;
        var encounterCandidateSimulations = 3;
        var encounterRetained = 5;
        var eliteDefaults = EliteCertificationOptions.ForProfile(certificationProfile);
        var eliteRestarts = eliteDefaults.RestartCount;
        var eliteSearchOnly = false;
        var elitePopulation = eliteDefaults.PopulationSize;
        var eliteGenerations = eliteDefaults.Generations;
        var eliteMaximumGenerations = eliteDefaults.MaximumGenerations;
        var eliteMaximumGenerationsSpecified = false;
        var eliteElites = eliteDefaults.EliteCount;
        var eliteCrossover = eliteDefaults.CrossoverRate;
        var eliteBasinJump = eliteDefaults.CoordinatedMutationRate;
        var eliteExplorerArchive = eliteDefaults.ExplorerArchiveSize;
        var eliteStratifiedPortfolio = eliteDefaults.StratifiedPortfolioCandidatesPerProfile;
        var eliteQualityIsland = eliteDefaults.QualityDiversityIslandCandidateBudgetPerProfile;
        var eliteDescriptorAudit = eliteDefaults.DescriptorSeparabilityAuditEnabled;
        var eliteMechanicIsland = eliteDefaults.MechanicArchetypeIslandCandidateBudgetPerProfile;
        var eliteBenchmarkConfidenceAudit = eliteDefaults.BenchmarkConfidenceAuditEnabled;
        var eliteConfidenceCohort = eliteDefaults.BenchmarkConfidenceAuditCohortSize;
        var eliteConfidenceSeeds = eliteDefaults.BenchmarkConfidenceAuditSeedCount;
        var eliteConfidenceMargin = eliteDefaults.BenchmarkConfidenceTargetScoreMargin;
        var eliteValleyBeamWidth = eliteDefaults.RestartValleyBeamWidth;
        var eliteValleyBeamDepth = eliteDefaults.RestartValleyBeamDepth;
        var eliteValleyBudget = eliteDefaults.RestartValleyCandidateBudget;
        var eliteValleyPrefilter = eliteDefaults.RestartValleyPrefilterLimitPerDepth;
        var eliteBridgeAudit = eliteDefaults.BridgeAuditEnabled;
        var eliteFinalists = eliteDefaults.FinalistsPerSlotProfile;
        var eliteLocalSwapDepth = eliteDefaults.LocalSwapDepth;
        var eliteTwoSwapLimit = eliteDefaults.TwoSwapChallengerLimitPerFinalist;
        var eliteRestartRefinement = eliteDefaults.RestartLocalRefinementPassLimit;
        var eliteRestartSeeds = eliteDefaults.RestartRefinementSeedCount;
        var eliteRestartTwoSwapLimit = eliteDefaults.RestartTwoSwapChallengerLimitPerPass;
        var eliteFinalistRefinement = eliteDefaults.FinalistRefinementRoundLimit;
        var eliteHoldoutSeeds = eliteDefaults.HoldoutSeeds;
        var eliteSimulations = eliteDefaults.SimulationsPerSeed;
        var elitePartyGenomes = eliteDefaults.PartyGenomeBudgetPerFloor;
        string? elitePolicyPath = null;
        string? topPlayerBuildsPath = null;
        var validationSeeds = 8;
        var validationSimulations = 50;
        var validationProbeSimulations = 25;
        var metaSimulatorBattles = 2_000;
        var metaSimulatorRoundsPerMatchup = 0;
        string? contentRoot = null;
        string? outputRoot = null;
        var showHelp = false;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--full":
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
                case "--seed":
                    var seedValue = ReadValue(args, ref index, argument);
                    if (!int.TryParse(seedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                        throw new BalanceCommandException($"Invalid seed '{seedValue}'. Expected a 32-bit integer.");
                    break;
                case "--content-root":
                    contentRoot = ReadValue(args, ref index, argument);
                    break;
                case "--optimizer-population":
                    optimizerPopulation = ReadInt(args, ref index, argument, 4, 500);
                    break;
                case "--optimizer-generations":
                    optimizerGenerations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--optimizer-elites":
                    optimizerElites = ReadInt(args, ref index, argument, 1, 499);
                    break;
                case "--optimizer-mutation":
                    optimizerMutation = ReadDouble(args, ref index, argument, 0.01, 1);
                    break;
                case "--optimizer-random":
                    optimizerRandom = ReadDouble(args, ref index, argument, 0, 0.5);
                    break;
                case "--optimizer-diversity":
                    optimizerDiversity = ReadDouble(args, ref index, argument, 0, 100);
                    break;
                case "--optimizer-retained":
                    optimizerRetained = ReadInt(args, ref index, argument, 1, 500);
                    break;
                case "--representative-count":
                    representativeCount = ReadInt(args, ref index, argument, 1, 500);
                    break;
                case "--progression-curve":
                    progressionCurve = ParseProgressionCurve(ReadValue(args, ref index, argument));
                    break;
                case "--tower-simulations":
                    towerSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--calibration-iterations":
                    calibrationIterations = ReadInt(args, ref index, argument, 1, 20);
                    break;
                case "--assisted-calibration":
                    assistedCalibrationEnabled = true;
                    break;
                case "--assisted-calibration-simulations":
                    assistedCalibrationSimulations = ReadInt(args, ref index, argument, 0, 1_000);
                    break;
                case "--encounter-candidate-simulations":
                    encounterCandidateSimulations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--encounter-retained":
                    encounterRetained = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--certification-profile":
                    _ = ParseCertificationProfile(ReadValue(args, ref index, argument));
                    break;
                case "--elite-search-only":
                    eliteSearchOnly = true;
                    break;
                case "--elite-restarts":
                    eliteRestarts = ReadInt(args, ref index, argument, 2, 32);
                    break;
                case "--elite-population":
                    elitePopulation = ReadInt(args, ref index, argument, 4, 500);
                    break;
                case "--elite-generations":
                    eliteGenerations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--elite-max-generations":
                    eliteMaximumGenerations = ReadInt(args, ref index, argument, 1, 100);
                    eliteMaximumGenerationsSpecified = true;
                    break;
                case "--elite-elites":
                    eliteElites = ReadInt(args, ref index, argument, 1, 499);
                    break;
                case "--elite-crossover":
                    eliteCrossover = ReadDouble(args, ref index, argument, 0, 1);
                    break;
                case "--elite-basin-jump":
                    eliteBasinJump = ReadDouble(args, ref index, argument, 0, 1);
                    break;
                case "--elite-explorer-archive":
                    eliteExplorerArchive = ReadInt(args, ref index, argument, 0, 100);
                    break;
                case "--elite-stratified-portfolio":
                    eliteStratifiedPortfolio = ReadInt(args, ref index, argument, 0, 5_000);
                    break;
                case "--elite-quality-island":
                    eliteQualityIsland = ReadInt(args, ref index, argument, 0, 5_000);
                    break;
                case "--elite-descriptor-audit":
                    eliteDescriptorAudit = true;
                    break;
                case "--elite-benchmark-confidence-audit":
                    eliteBenchmarkConfidenceAudit = true;
                    break;
                case "--elite-confidence-cohort":
                    eliteConfidenceCohort = ReadInt(args, ref index, argument, 3, 5_000);
                    break;
                case "--elite-confidence-seeds":
                    eliteConfidenceSeeds = ReadInt(args, ref index, argument, 2, 1_000);
                    break;
                case "--elite-confidence-margin":
                    eliteConfidenceMargin = ReadDouble(args, ref index, argument, 0.01, 5);
                    break;
                case "--elite-mechanic-island":
                    eliteMechanicIsland = ReadInt(args, ref index, argument, 0, 5_000);
                    break;
                case "--elite-valley-beam-width":
                    eliteValleyBeamWidth = ReadInt(args, ref index, argument, 0, 100);
                    break;
                case "--elite-valley-beam-depth":
                    eliteValleyBeamDepth = ReadInt(args, ref index, argument, 0, 6);
                    break;
                case "--elite-valley-budget":
                    eliteValleyBudget = ReadInt(args, ref index, argument, 0, 1_000_000);
                    break;
                case "--elite-valley-prefilter":
                    eliteValleyPrefilter = ReadInt(args, ref index, argument, 0, 10_000);
                    break;
                case "--elite-bridge-audit":
                    eliteBridgeAudit = true;
                    break;
                case "--elite-finalists":
                    eliteFinalists = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--elite-local-swap-depth":
                    eliteLocalSwapDepth = ReadInt(args, ref index, argument, 1, 2);
                    break;
                case "--elite-two-swap-limit":
                    eliteTwoSwapLimit = ReadInt(args, ref index, argument, 0, 1_000_000);
                    break;
                case "--elite-restart-refinement":
                    eliteRestartRefinement = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--elite-restart-seeds":
                    eliteRestartSeeds = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--elite-restart-two-swap-limit":
                    eliteRestartTwoSwapLimit = ReadInt(args, ref index, argument, 0, 1_000_000);
                    break;
                case "--elite-finalist-refinement":
                    eliteFinalistRefinement = ReadInt(args, ref index, argument, 0, 20);
                    break;
                case "--elite-holdout-seeds":
                    eliteHoldoutSeeds = ReadInt(args, ref index, argument, 2, 50);
                    break;
                case "--elite-simulations":
                    eliteSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--elite-party-genomes":
                    elitePartyGenomes = ReadInt(args, ref index, argument, 1, 100_000);
                    break;
                case "--elite-policy":
                    elitePolicyPath = ReadValue(args, ref index, argument);
                    break;
                case "--top-player-builds":
                    topPlayerBuildsPath = ReadValue(args, ref index, argument);
                    break;
                case "--validation-seeds":
                    validationSeeds = ReadInt(args, ref index, argument, 2, 50);
                    break;
                case "--validation-simulations":
                    validationSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--validation-probe-simulations":
                    validationProbeSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--meta-simulator-battles":
                    metaSimulatorBattles = ReadInt(args, ref index, argument, 1, 1_000_000);
                    break;
                case "--meta-simulator-rounds-per-matchup":
                    metaSimulatorRoundsPerMatchup = ReadInt(args, ref index, argument, 0, 1_000);
                    if (metaSimulatorRoundsPerMatchup % 2 != 0)
                        throw new BalanceCommandException($"Argument '{argument}' must be even so every matchup receives equal side assignments.");
                    break;
                case "--build-count":
                    var buildCountValue = ReadValue(args, ref index, argument);
                    if (!int.TryParse(
                            buildCountValue,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out essenceBuildsPerProfile)
                        || essenceBuildsPerProfile is < 1 or > 1_000)
                    {
                        throw new BalanceCommandException(
                            $"Invalid build count '{buildCountValue}'. Expected a number from 1 to 1,000.");
                    }
                    break;
                case "--capability-seeds":
                    capabilityProbeSeedCount = ReadInt(args, ref index, argument, 1, 32);
                    break;
                case "--party-family-samples":
                    partyFamilySamplesPerFamily = ReadInt(args, ref index, argument, 1, 50);
                    break;
                case "--party-family-simulations":
                    partyFamilySimulationsPerParty = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--scale-probes":
                    scaleProbesEnabled = true;
                    break;
                case "--scale-probe-parties":
                    scaleProbeParties = ReadInt(args, ref index, argument, 1, 20);
                    break;
                case "--scale-probe-simulations":
                    scaleProbeSimulations = ReadInt(args, ref index, argument, 1, 100);
                    break;
                case "--scale-probe-max-ms-per-trial":
                    scaleProbeMaximumMillisecondsPerTrial = ReadDouble(args, ref index, argument, 0.01, 600_000);
                    break;
                case "--scale-probe-max-allocated-mb-per-trial":
                    scaleProbeMaximumAllocatedMebibytesPerTrial = ReadDouble(args, ref index, argument, 0.01, 2_048);
                    break;
                case "--scale-probe-min-ticks-per-second":
                    scaleProbeMinimumTicksPerSecond = ReadDouble(args, ref index, argument, 0.01, 1_000_000_000);
                    break;
                case "--scale-probe-max-peak-memory-mb":
                    scaleProbeMaximumPeakMemoryMebibytes = ReadDouble(args, ref index, argument, 1, 32_768);
                    break;
                case "--reliability-study":
                    reliabilityStudyEnabled = true;
                    break;
                case "--reliability-rosters":
                    reliabilityRosters = ReadInt(args, ref index, argument, 1, 15);
                    break;
                case "--reliability-simulations":
                    reliabilitySimulations = ReadInt(args, ref index, argument, 5, 100);
                    break;
                case "--reliability-fault-multiplier":
                    reliabilityFaultMultiplier = ReadDouble(args, ref index, argument, 1.0001, 2);
                    break;
                case "--floor-progression-calibration":
                    floorProgressionCalibrationEnabled = true;
                    break;
                case "--floor-progression-simulations":
                    floorProgressionSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--floor-progression-holdout-simulations":
                    floorProgressionHoldoutSimulations = ReadInt(args, ref index, argument, 1, 1_000);
                    break;
                case "--floor-progression-sensitivity-points":
                    floorProgressionSensitivityPoints = ReadInt(args, ref index, argument, 2, 20);
                    break;
                case "--floor-progression-refinement-iterations":
                    floorProgressionRefinementIterations = ReadInt(args, ref index, argument, 0, 20);
                    break;
                case "--output":
                    outputRoot = ReadValue(args, ref index, argument);
                    break;
                default:
                    throw new BalanceCommandException($"Unknown balance-runner argument '{argument}'.");
            }
        }

        var optimizer = new EssenceOptimizerOptions(
            optimizerPopulation,
            optimizerGenerations,
            optimizerElites,
            optimizerMutation,
            optimizerRandom,
            optimizerDiversity,
            optimizerRetained);
        if (!eliteMaximumGenerationsSpecified)
            eliteMaximumGenerations = Math.Max(eliteMaximumGenerations, eliteGenerations);
        try
        {
            optimizer.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new BalanceCommandException(exception.Message);
        }
        var representativeBuilds = new RepresentativeBuildOptions(representativeCount);
        try
        {
            representativeBuilds.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new BalanceCommandException(exception.Message);
        }
        var minimumEvaluatedCandidates = optimizer.PopulationSize
                                         + optimizer.Generations * (optimizer.PopulationSize - optimizer.EliteCount);
        if (representativeBuilds.BuildsPerProfile > minimumEvaluatedCandidates)
        {
            throw new BalanceCommandException(
                $"Representative build count must not exceed the optimizer's minimum evaluated population " +
                $"of {minimumEvaluatedCandidates} candidates per slot profile.");
        }
        if (encounterRetained > minimumEvaluatedCandidates)
        {
            throw new BalanceCommandException(
                $"Encounter retained build count must not exceed the optimizer's minimum evaluated population " +
                $"of {minimumEvaluatedCandidates} candidates per slot profile.");
        }
        var eliteCertification = new EliteCertificationOptions(
            certificationProfile,
            eliteRestarts,
            elitePopulation,
            eliteGenerations,
            eliteElites,
            eliteFinalists,
            eliteLocalSwapDepth,
            eliteTwoSwapLimit,
            eliteHoldoutSeeds,
            eliteSimulations,
            elitePartyGenomes,
            eliteDefaults.MutationRate,
            eliteDefaults.RandomInjectionRate,
            eliteDefaults.DiversityPenalty,
            topPlayerBuildsPath,
            eliteMaximumGenerations,
            eliteRestartRefinement,
            eliteFinalistRefinement,
            eliteRestartTwoSwapLimit,
            eliteRestartSeeds,
            eliteSearchOnly,
            eliteCrossover,
            eliteValleyBeamWidth,
            eliteValleyBeamDepth,
            eliteValleyBudget,
            eliteValleyPrefilter,
            eliteBridgeAudit,
            eliteBasinJump,
            eliteExplorerArchive,
            eliteStratifiedPortfolio,
            eliteQualityIsland,
            eliteDescriptorAudit,
            eliteMechanicIsland,
            eliteBenchmarkConfidenceAudit,
            eliteConfidenceCohort,
            eliteConfidenceSeeds,
            eliteConfidenceMargin);
        try
        {
            eliteCertification.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new BalanceCommandException(exception.Message);
        }

        return new BalanceCommandOptions(
            seed,
            essenceBuildsPerProfile,
            capabilityProbeSeedCount,
            partyFamilySamplesPerFamily,
            partyFamilySimulationsPerParty,
            new EncounterScaleProbeOptions
            {
                Enabled = scaleProbesEnabled,
                PartiesPerSize = scaleProbeParties,
                SimulationsPerParty = scaleProbeSimulations,
                PerformanceBudget = new EncounterScaleProbePerformanceBudget(
                    scaleProbeMaximumMillisecondsPerTrial,
                    scaleProbeMaximumAllocatedMebibytesPerTrial.HasValue
                        ? checked((long)Math.Round(scaleProbeMaximumAllocatedMebibytesPerTrial.Value * 1024 * 1024, MidpointRounding.AwayFromZero))
                        : null,
                    scaleProbeMinimumTicksPerSecond,
                    scaleProbeMaximumPeakMemoryMebibytes.HasValue
                        ? checked((long)Math.Round(scaleProbeMaximumPeakMemoryMebibytes.Value * 1024 * 1024, MidpointRounding.AwayFromZero))
                        : null)
            },
            new RegionOneReliabilityStudyOptions
            {
                Enabled = reliabilityStudyEnabled,
                RostersPerFamily = reliabilityRosters,
                SimulationsPerRoster = reliabilitySimulations,
                FaultMultiplier = reliabilityFaultMultiplier
            },
            new AutomaticFloorProgressionCalibrationOptions(
                floorProgressionCalibrationEnabled,
                floorProgressionSimulations,
                floorProgressionHoldoutSimulations,
                floorProgressionSensitivityPoints,
                floorProgressionRefinementIterations),
            optimizer,
            representativeBuilds,
            new ProgressionBandOptions(progressionCurve),
            new WorldTowerAnalysisOptions(towerSimulations),
            new EssenceMetaAnalysisOptions(
                SimulatorBattleCount: metaSimulatorBattles,
                SimulatorRoundsPerMatchup: metaSimulatorRoundsPerMatchup),
            new EncounterCalibrationOptions(SearchIterations: calibrationIterations)
            {
                AssistedCalibrationEnabled = assistedCalibrationEnabled,
                AssistedProbeSimulations = assistedCalibrationSimulations
            },
            new EncounterSpecificOptimizationOptions(
                CandidateSimulations: encounterCandidateSimulations,
                RetainedBuilds: encounterRetained),
            eliteCertification,
            new ScalingValidationOptions(
                HoldoutSeeds: validationSeeds,
                SimulationsPerSeed: validationSimulations,
                ProbeSimulationsPerSeed: validationProbeSimulations),
            elitePolicyPath,
            contentRoot,
            outputRoot,
            showHelp);
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string argument)
    {
        if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
            throw new BalanceCommandException($"Argument '{argument}' requires a value.");

        return args[index];
    }

    private static int ReadInt(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        int minimum,
        int maximum)
    {
        var value = ReadValue(args, ref index, argument);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            || result < minimum
            || result > maximum)
        {
            throw new BalanceCommandException(
                $"Invalid value '{value}' for '{argument}'. Expected {minimum} through {maximum}.");
        }
        return result;
    }

    private static double ReadDouble(
        IReadOnlyList<string> args,
        ref int index,
        string argument,
        double minimum,
        double maximum)
    {
        var value = ReadValue(args, ref index, argument);
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            || !double.IsFinite(result)
            || result < minimum
            || result > maximum)
        {
            throw new BalanceCommandException(
                $"Invalid value '{value}' for '{argument}'. Expected {minimum} through {maximum}.");
        }
        return result;
    }

    private static ProgressionCurveKind ParseProgressionCurve(string value) =>
        value.ToLowerInvariant() switch
        {
            "linear" => ProgressionCurveKind.Linear,
            "ease-in" => ProgressionCurveKind.EaseIn,
            "ease-out" => ProgressionCurveKind.EaseOut,
            "smooth-step" => ProgressionCurveKind.SmoothStep,
            _ => throw new BalanceCommandException(
                $"Invalid progression curve '{value}'. Expected linear, ease-in, ease-out, or smooth-step.")
        };

    private static EliteCertificationProfile ReadCertificationProfile(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (!args[index].Equals("--certification-profile", StringComparison.Ordinal))
                continue;
            if (++index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
                throw new BalanceCommandException("Argument '--certification-profile' requires a value.");
            return ParseCertificationProfile(args[index]);
        }
        return EliteCertificationProfile.Developer;
    }

    private static EliteCertificationProfile ParseCertificationProfile(string value) =>
        value.ToLowerInvariant() switch
        {
            "developer" => EliteCertificationProfile.Developer,
            "release" => EliteCertificationProfile.Release,
            _ => throw new BalanceCommandException(
                $"Invalid certification profile '{value}'. Expected developer or release.")
        };
}

public sealed class BalanceCommandException(string message) : Exception(message);
