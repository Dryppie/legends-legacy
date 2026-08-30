using System.Globalization;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Application.Interfaces.Services.LL.Essences;
using Common.Randomness;
using Domain.Models.Essences.Definitions;
using Services.LL.Combat.Engine;

namespace LegendsLegacy.Balance;

public sealed record EliteCertificationRestartSnapshot(
    int Restart,
    int SearchSeed,
    double RawBestScore,
    double BestScore,
    int GenerationsExecuted,
    int GenerationsSinceMaterialImprovement,
    bool PlateauPassed,
    int UniqueCandidatesEvaluated,
    int LocalRefinementPasses,
    int LocalCandidatesEvaluated,
    int OneSwapCandidatesEvaluated = 0,
    int TwoSwapCandidatesEvaluated = 0,
    int RefinementSeedsEvaluated = 1,
    string? RawBestBuildId = null,
    string? BestBuildId = null,
    IReadOnlyList<string>? RawBestEssenceIds = null,
    IReadOnlyList<string>? BestEssenceIds = null,
    int DistanceFromStrongestRestart = 0,
    int ValleyBeamDepthReached = 0,
    int ValleyBeamCandidatesEvaluated = 0,
    bool ValleyBeamBudgetExhausted = false,
    double ValleyBeamBestImprovement = 0,
    int ValleyBeamCandidatesGenerated = 0,
    int ValleyBeamCandidatesRejectedByPrefilter = 0,
    int CoordinatedMutationCandidatesEvaluated = 0,
    int ExplorerContinuationCandidatesEvaluated = 0,
    double BaselineBestScore = 0,
    string? BaselineBestBuildId = null,
    IReadOnlyList<string>? BaselineBestEssenceIds = null,
    int StratifiedPortfolioCandidatesEvaluated = 0,
    int QualityDiversityIslandCandidatesEvaluated = 0,
    int QualityDiversityIslandInitialCandidatesEvaluated = 0,
    int QualityDiversityIslandDescendantsEvaluated = 0,
    int QualityDiversityIslandNichesOccupied = 0,
    int QualityDiversityIslandNicheReplacements = 0,
    double QualityDiversityIslandBestScore = 0,
    string? QualityDiversityIslandBestBuildId = null,
    IReadOnlyList<string>? QualityDiversityIslandBestEssenceIds = null,
    int MechanicArchetypeIslandCandidatesEvaluated = 0,
    int MechanicArchetypeIslandInitialCandidatesEvaluated = 0,
    int MechanicArchetypeIslandDescendantsEvaluated = 0,
    int MechanicArchetypeIslandNichesOccupied = 0,
    int MechanicArchetypeIslandNicheReplacements = 0,
    double MechanicArchetypeIslandBestScore = 0,
    string? MechanicArchetypeIslandBestBuildId = null,
    IReadOnlyList<string>? MechanicArchetypeIslandBestEssenceIds = null,
    bool MechanicArchetypeHighNichePresentInBaseline = false,
    double MechanicArchetypeHighNicheBaselineBestScore = 0,
    int MechanicArchetypeHighNicheIslandCandidatesEvaluated = 0,
    double MechanicArchetypeHighNicheIslandBestScore = 0);

public sealed record EliteCertificationCandidateSnapshot(
    string BuildId,
    double PopulationPercentile,
    double AggregateScore,
    IReadOnlyDictionary<string, double> ComponentScores,
    IReadOnlyList<string> EssenceIds);

public sealed record EliteBridgePathNodeSnapshot(
    string BuildId,
    IReadOnlyList<string> Genome,
    double Score);

public sealed record EliteRestartBridgeAuditSnapshot(
    string ProfileId,
    int SlotCount,
    int SourceRestart,
    string SourceBuildId,
    IReadOnlyList<string> SourceGenome,
    double SourceScore,
    int TargetRestart,
    string TargetBuildId,
    IReadOnlyList<string> TargetGenome,
    double TargetScore,
    int SubstitutionDistance,
    int LegalBridgeNodesEvaluated,
    IReadOnlyList<EliteBridgePathNodeSnapshot> BestMaximinPath,
    double PathMinimumScore,
    double LargestSingleStepRegression,
    double TotalTemporaryRegressionBelowSource,
    double StepRegressionTolerance,
    bool NonRegressingBridgeExists,
    bool ToleranceBoundedBridgeExists);

public sealed record EliteDescriptorAnchorSnapshot(
    string Basin,
    string AnchorId,
    string BuildId,
    IReadOnlyList<string> Genome,
    double AggregateScore,
    IReadOnlyDictionary<string, double> ScenarioScores);

public sealed record EliteDescriptorBasinSnapshot(
    string Basin,
    int CandidateCount,
    double MinimumScore,
    double MedianScore,
    double MaximumScore,
    IReadOnlyDictionary<string, double> MeanScenarioScores);

public sealed record EliteDescriptorFeatureContrastSnapshot(
    string Feature,
    double HighBasinMean,
    double LowBasinMean,
    double NormalizedDifference);

public sealed record EliteDescriptorFamilySnapshot(
    string DescriptorId,
    string DisplayName,
    int FeatureCount,
    int DistinctNeighborhoodSignatures,
    double ExactSignaturePurity,
    double SingletonCandidateRate,
    double NearestAnchorHighAccuracy,
    double NearestAnchorLowAccuracy,
    double NearestAnchorBalancedAccuracy,
    int NearestAnchorAmbiguousCandidates,
    bool HighAnchorCollidesWithLowAnchor,
    int HighAnchorRetainedNicheOccupancy,
    double? HighAnchorRetainedNicheBestScore,
    int? TheoreticalNicheCeiling,
    bool HardNicheCeilingPassed,
    bool SeparabilityPassed,
    bool MapCandidatePassed,
    IReadOnlyList<EliteDescriptorFeatureContrastSnapshot> StrongestFeatureContrasts);

public sealed record EliteDescriptorCollisionAuditSnapshot(
    string ParentDescriptorId,
    string ParentHighNicheSignature,
    string ResidualDescriptorId,
    string DisplayName,
    string CandidateUniverse,
    int FeatureCount,
    int ParentNicheCandidates,
    int CandidateCount,
    int HighBasinCandidates,
    int LowBasinCandidates,
    int AmbiguousQualityCandidatesExcluded,
    double HighScoreFloor,
    double LowScoreCeiling,
    int DistinctResidualSignatures,
    double ExactSignaturePurity,
    double SingletonCandidateRate,
    double LeaveOneOutHighAccuracy,
    double LeaveOneOutLowAccuracy,
    double LeaveOneOutBalancedAccuracy,
    int LeaveOneOutAmbiguousCandidates,
    bool HighAnchorResidualCollidesWithLowCandidate,
    int HighAnchorRetainedResidualNicheOccupancy,
    double? HighAnchorRetainedResidualNicheBestScore,
    int TheoreticalResidualNicheCeiling,
    bool HardNicheCeilingPassed,
    bool SeparabilityPassed,
    bool MapCandidatePassed,
    IReadOnlyList<EliteDescriptorFeatureContrastSnapshot> StrongestFeatureContrasts);

public sealed record EliteDescriptorSeparabilityAuditSnapshot(
    string ProfileId,
    int SlotCount,
    string NeighborhoodDefinition,
    bool AuthoritativeProductionBenchmark,
    bool CertificationEvidenceAffected,
    int UniqueCandidatesEvaluated,
    int HighBasinCandidates,
    int LowBasinCandidates,
    int AmbiguousNeighborhoodCandidatesExcluded,
    int RetainedBaselineCandidates,
    IReadOnlyList<EliteDescriptorAnchorSnapshot> Anchors,
    IReadOnlyList<EliteDescriptorBasinSnapshot> Basins,
    IReadOnlyList<EliteDescriptorFamilySnapshot> DescriptorFamilies,
    IReadOnlyList<string> MapCandidateDescriptorIds,
    IReadOnlyList<string> Warnings,
    EliteDescriptorCollisionAuditSnapshot? CollisionAudit = null);

public sealed record EliteBenchmarkConfidenceBuildSnapshot(
    string BuildId,
    double BaselineScore,
    int BaselineRank,
    double MeanScore,
    double ScoreStandardDeviation,
    double Approximate95ConfidenceHalfWidth,
    int RecommendedSeedCountForTargetMargin,
    double MeanReplicateRank,
    IReadOnlyList<string> EssenceIds,
    int RobustRank = 0,
    int RankChange = 0,
    IReadOnlyDictionary<string, double>? ScenarioMeans = null,
    IReadOnlyDictionary<string, double>? ScenarioStandardDeviations = null,
    IReadOnlyList<double>? PerSeedAggregateScores = null);

public sealed record EliteBenchmarkScenarioVarianceSnapshot(
    string ScenarioId,
    double MeanScore,
    double MeanWithinBuildStandardDeviation,
    double MedianWithinBuildStandardDeviation,
    double MaximumWithinBuildStandardDeviation);

public sealed record EliteBenchmarkReferenceProfileSnapshot(
    string ProfileId,
    int SlotCount,
    int AvailableCandidateCount,
    int CohortSize,
    string LegacyKnownBestBuildId,
    double LegacyKnownBestScore,
    int LegacyKnownBestRobustRank,
    string RobustKnownBestBuildId,
    double RobustKnownBestScore,
    double LegacyToRobustSpearmanCorrelation,
    IReadOnlyList<EliteBenchmarkScenarioVarianceSnapshot> ScenarioVariances,
    IReadOnlyList<EliteBenchmarkConfidenceBuildSnapshot> Builds);

public sealed record EliteBenchmarkPanelSizeSnapshot(
    int SeedCount,
    int TotalCombatExecutions,
    long CumulativeElapsedMilliseconds,
    double SpearmanCorrelationToReference,
    double Top10OverlapWithReference,
    double Top20OverlapWithReference,
    double Top50OverlapWithReference,
    double EliteTop50PairwiseOrderingAgreement,
    double FinalistPairwiseOrderingAgreement,
    int ClearlySeparatedElitePairReversals,
    int ClearlySeparatedElitePairCount,
    double ClearlySeparatedElitePairReversalRate,
    double? MedianApproximate95ConfidenceHalfWidth,
    double? MaximumApproximate95ConfidenceHalfWidth,
    double EstimatedFullSearchRuntimeSeconds,
    bool StatisticalGatesPassed,
    bool FifteenMinuteSearchRuntimePassed,
    int PromotionDepthForReferenceTop10 = 0,
    int PromotionDepthForReferenceTop20 = 0,
    int PromotionDepthForReferenceTop50 = 0,
    double ReferenceTop50RecallAtTop100 = 0);

public sealed record EliteBenchmarkConfidenceComparisonSnapshot(
    string HigherAnchorId,
    string LowerAnchorId,
    double MeanPairedScoreDifference,
    double DifferenceStandardDeviation,
    double Approximate95ConfidenceLowerBound,
    double Approximate95ConfidenceUpperBound,
    double HigherScoreFraction,
    bool OrderingConfident);

public sealed record EliteBenchmarkConfidenceAuditSnapshot(
    string ProfileId,
    string CandidateUniverse,
    int AvailableCandidateCount,
    int CohortSize,
    int SeedCount,
    int ScenarioCount,
    int TotalCombatExecutions,
    bool CommonRandomNumbers,
    double TargetScoreMargin,
    int TopK,
    double BaselineToMeanSpearmanCorrelation,
    double MinimumReplicateToMeanSpearmanCorrelation,
    double MeanReplicateToMeanSpearmanCorrelation,
    double MinimumBaselineTopKOverlap,
    double MeanBaselineTopKOverlap,
    double MedianApproximate95ConfidenceHalfWidth,
    double MaximumApproximate95ConfidenceHalfWidth,
    int MaximumRecommendedSeedCountForTargetMargin,
    bool RankingStabilityPassed,
    bool KnownAnchorOrderingPassed,
    bool ConfiguredSampleAdequate,
    bool CertificationEvidenceAffected,
    IReadOnlyList<EliteBenchmarkConfidenceBuildSnapshot> Builds,
    IReadOnlyList<EliteBenchmarkConfidenceComparisonSnapshot> AnchorComparisons,
    IReadOnlyList<string> Warnings,
    int ReferenceSeedCount = 0,
    int SelectedPracticalSeedCount = 0,
    bool PracticalPanelPassed = false,
    IReadOnlyList<EliteBenchmarkPanelSizeSnapshot>? PanelSizes = null,
    IReadOnlyList<EliteBenchmarkReferenceProfileSnapshot>? ReferenceProfiles = null,
    IReadOnlyList<int>? CombatSeedPanel = null);

public sealed record EliteLocalChallengeSnapshot(
    int FinalistCount,
    int RefinementRounds,
    int OneSwapChallengersEvaluated,
    int TwoSwapChallengersEvaluated,
    bool CompleteForConfiguredDepth,
    double BestAggregateImprovement,
    double BestScenarioImprovement,
    string? ChallengedFinalistBuildId,
    string? ImprovingBuildId);

public sealed record CuratedBuildComparisonSnapshot(
    int CuratedBuildCount,
    double? BestCuratedScore,
    double? AutomatedCeilingScore,
    double? BestCuratedAdvantage,
    bool RequirementSatisfied,
    bool AutomatedCeilingOutperformed);

public sealed record EliteCertificationProfileSnapshot(
    string ProfileId,
    int SlotCount,
    long LegalSearchSpaceSize,
    int UniqueCandidatesEvaluated,
    double P95TargetScore,
    double P99TargetScore,
    double BestScore,
    double BestScoreSpreadAcrossRestarts,
    bool IndependentSearchAgreementPassed,
    bool SearchPlateauPassed,
    bool CrossStrategyAgreementPassed,
    bool ScenarioCoveragePassed,
    EliteCertificationCandidateSnapshot P95Build,
    EliteCertificationCandidateSnapshot P99Build,
    IReadOnlyList<EliteCertificationCandidateSnapshot> Finalists,
    IReadOnlyList<EliteCertificationRestartSnapshot> Restarts,
    EliteLocalChallengeSnapshot LocalChallenge,
    CuratedBuildComparisonSnapshot CuratedComparison,
    EliteCertificationVerdict Verdict,
    IReadOnlyList<string> Warnings);

public sealed record EliteHoldoutSnapshot(
    int SeedCount,
    int SimulationsPerSeed,
    int TrialCount,
    int ClearCount,
    double ClearRate,
    double ConfidenceLowerBound,
    double ConfidenceUpperBound,
    double ConfidenceIntervalWidth,
    double AverageDurationTicks,
    double MedianDurationTicks,
    double AverageFriendlyDeaths,
    double AverageRemainingHealthRatio);

public sealed record EliteCalibrationBuildSnapshot(
    string BuildId,
    IReadOnlyList<string> EssenceIds);

public sealed record EliteCertificationFloorSnapshot(
    int Floor,
    string EncounterName,
    string GenericProfileId,
    int SlotCount,
    int PartyGenomesEvaluated,
    long PartyGenomeSearchSpaceSize,
    bool PartyOptimizationComplete,
    EliteHoldoutSnapshot GenericP75,
    EliteHoldoutSnapshot CertifiedP95,
    EliteHoldoutSnapshot CertifiedP99,
    EliteHoldoutSnapshot SpecializedParty,
    EliteHoldoutSnapshot? BestCuratedParty,
    bool P95ExpectationPassed,
    bool P99ExpectationPassed,
    bool HoldoutPrecisionPassed,
    bool CuratedRequirementSatisfied,
    bool HumanPartyOutperformed,
    EliteCertificationVerdict Verdict,
    IReadOnlyList<string> P95CohortBuildIds,
    IReadOnlyList<string> P99CohortBuildIds,
    IReadOnlyList<string> SpecializedPartyBuildIds,
    IReadOnlyList<string> Warnings)
{
    public IReadOnlyList<EliteCalibrationBuildSnapshot> P95CohortBuilds { get; init; } = [];
}

public sealed record EliteBuildCertificationSnapshot(
    int AlgorithmVersion,
    int Seed,
    string ContentFingerprint,
    string PolicyFingerprint,
    EliteCertificationPolicy Policy,
    EliteCertificationOptions Options,
    bool ProductionContentModified,
    int TotalUniqueCandidatesEvaluated,
    int TotalPartyGenomesEvaluated,
    EliteCertificationVerdict Verdict,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<EliteCertificationProfileSnapshot> Profiles,
    IReadOnlyList<EliteCertificationFloorSnapshot> Floors,
    int TotalBridgeNodesEvaluated = 0,
    IReadOnlyList<EliteRestartBridgeAuditSnapshot>? BridgeAudits = null,
    int TotalDescriptorAuditCandidatesEvaluated = 0,
    EliteDescriptorSeparabilityAuditSnapshot? DescriptorSeparabilityAudit = null,
    int TotalBenchmarkConfidenceCombatExecutions = 0,
    EliteBenchmarkConfidenceAuditSnapshot? BenchmarkConfidenceAudit = null);

public sealed class EliteBuildCertificationAnalyzer(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    EssenceBuildGenerator buildGenerator,
    PveBenchmarkRunner benchmarkRunner,
    EssenceBuildOptimizer optimizer,
    IEncounterBuildEvaluator encounterEvaluator)
{
    public const int AlgorithmVersion = 21;

    private static readonly DescriptorAuditAnchor[] DescriptorAuditAnchors =
    [
        new(
            "high",
            "known-e5-high-86.21",
            [
                "essence.bark_golem",
                "essence.giant_bat",
                "essence.plague_ghoul",
                "essence.poisonous_rat",
                "essence.spider_queen_royal_venom"
            ]),
        new(
            "low",
            "known-e5-low-85.27",
            [
                "essence.bark_golem",
                "essence.elder_treant_thornstorm",
                "essence.illusion_fox",
                "essence.venomous_spiderling",
                "essence.wind_harpy"
            ]),
        new(
            "low",
            "known-e5-low-84.91",
            [
                "essence.bark_golem",
                "essence.elder_treant_thornstorm",
                "essence.giant_worm",
                "essence.plague_ghoul",
                "essence.venomous_spiderling"
            ])
    ];

    private static readonly string[] MechanicResidualFeatureNames =
    [
        "intensity:outgoing-result",
        "intensity:health-recovery",
        "intensity:status-lifecycle",
        "intensity:condition-dependency"
    ];

    public EliteBuildCertificationSnapshot Certify(
        EssenceMetaAnalysisSnapshot essenceMeta,
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        EncounterCalibrationSnapshot calibration,
        int runSeed,
        EliteCertificationPolicy? requestedPolicy = null,
        EliteCertificationOptions? requestedOptions = null)
    {
        ArgumentNullException.ThrowIfNull(essenceMeta);
        ArgumentNullException.ThrowIfNull(representativeBuilds);
        ArgumentNullException.ThrowIfNull(worldTower);
        ArgumentNullException.ThrowIfNull(calibration);
        var policy = (requestedPolicy ?? EliteCertificationPolicy.V1).Validate();
        var options = (requestedOptions ?? EliteCertificationOptions.ForProfile(EliteCertificationProfile.Developer)).Validate();
        var sourceFamilies = buildGenerator.GetSourceFamilies();
        var definitions = sourceFamilies.SelectMany(family => family)
            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var definitionsById = definitions.ToDictionary(definition => definition.Id, StringComparer.OrdinalIgnoreCase);
        var contentFingerprint = CreateContentFingerprint(
            AbilityBalanceContentFingerprint.Create(catalogProvider, essenceDefinitions),
            worldTower,
            calibration);
        var fixtures = TopPlayerFixtureDocument.Load(options.TopPlayerBuildsPath, contentFingerprint);
        var curatedCandidates = MaterializeCuratedBuilds(fixtures, definitionsById, runSeed);
        var curatedBenchmarks = curatedCandidates.Count == 0
            ? new Dictionary<string, PveBenchmarkBuildSnapshot>(StringComparer.Ordinal)
            : benchmarkRunner.Run(curatedCandidates, runSeed).Builds.ToDictionary(build => build.BuildId, StringComparer.Ordinal);

        var restartResults = RunIndependentSearches(sourceFamilies, runSeed, policy, options);
        var canonicalCandidates = CanonicalizeAndEvaluate(restartResults, runSeed);
        var profileStates = EssenceBuildGenerator.InitialSlotCounts.Select(slotCount => BuildProfileState(
                slotCount,
                canonicalCandidates.Where(candidate => candidate.Build.SlotCount == slotCount).ToArray(),
                restartResults,
                curatedCandidates,
                curatedBenchmarks,
                sourceFamilies,
                definitionsById,
                essenceMeta,
                runSeed,
                policy,
                options))
            .ToArray();
        var profiles = profileStates.Select(state => state.Snapshot).ToArray();
        var floors = options.SearchOnly
            ? []
            : CertifyFloors(
                representativeBuilds,
                worldTower,
                calibration,
                profileStates,
                fixtures,
                curatedCandidates,
                runSeed,
                policy,
                options);
        var verdictInputs = profiles.Select(profile => profile.Verdict)
            .Concat(floors.Select(floor => floor.Verdict));
        if (options.SearchOnly)
            verdictInputs = verdictInputs.Append(EliteCertificationVerdict.PartyOptimizationRequired);
        var verdict = ResolveOverallVerdict(
            verdictInputs,
            options.Profile);
        var warnings = profiles.SelectMany(profile => profile.Warnings)
            .Concat(floors.SelectMany(floor => floor.Warnings))
            .Concat(options.SearchOnly
                ? ["Search-only mode skipped elite encounter holdouts and party optimization; this run cannot certify."]
                : [])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var bridgeAudits = options.BridgeAuditEnabled
            ? RunBridgeAudits(profiles, definitionsById, runSeed, policy)
            : [];
        var descriptorAudit = options.DescriptorSeparabilityAuditEnabled
            ? RunDescriptorSeparabilityAudit(
                canonicalCandidates,
                profileStates.Single(state => state.SlotCount == 5).Candidates,
                restartResults,
                sourceFamilies,
                definitionsById,
                runSeed)
            : null;
        var benchmarkConfidenceAudit = options.BenchmarkConfidenceAuditEnabled
            ? RunBenchmarkConfidenceAudit(
                profileStates,
                runSeed,
                options)
            : null;

        return new EliteBuildCertificationSnapshot(
            AlgorithmVersion,
            runSeed,
            contentFingerprint,
            policy.CreateFingerprint(),
            policy,
            options,
            false,
            profiles.Sum(profile => profile.UniqueCandidatesEvaluated),
            floors.Sum(floor => floor.PartyGenomesEvaluated),
            verdict,
            warnings,
            profiles,
            floors,
            bridgeAudits.Sum(audit => audit.LegalBridgeNodesEvaluated),
            bridgeAudits,
            descriptorAudit?.UniqueCandidatesEvaluated ?? 0,
            descriptorAudit,
            benchmarkConfidenceAudit?.TotalCombatExecutions ?? 0,
            benchmarkConfidenceAudit);
    }

    private EliteBenchmarkConfidenceAuditSnapshot RunBenchmarkConfidenceAudit(
        IReadOnlyList<ProfileState> profiles,
        int runSeed,
        EliteCertificationOptions options)
    {
        var primaryProfile = profiles.Single(profile => profile.SlotCount == 5);
        var cohorts = profiles.ToDictionary(
            profile => profile.SlotCount,
            profile => PrepareConfidenceCohort(
                profile,
                runSeed,
                profile.SlotCount == 5 ? options.BenchmarkConfidenceAuditCohortSize : 50,
                profile.SlotCount == 5),
            EqualityComparer<int>.Default);
        var allCandidates = cohorts.Values.SelectMany(values => values)
            .OrderBy(candidate => candidate.Build.SlotCount)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        var replicateSeeds = Enumerable.Range(1, options.BenchmarkConfidenceAuditSeedCount)
            .Select(index => StableRandom.Seed(
                "balance-elite-benchmark-confidence-v1",
                runSeed.ToString(CultureInfo.InvariantCulture),
                index.ToString(CultureInfo.InvariantCulture)))
            .ToArray();
        var suites = new List<PveBenchmarkSuiteSnapshot>(replicateSeeds.Length);
        var cumulativeElapsedMilliseconds = new Dictionary<int, long>();
        var stopwatch = Stopwatch.StartNew();
        foreach (var replicateSeed in replicateSeeds)
        {
            suites.Add(benchmarkRunner.RunCommonSeedReplicates(
                allCandidates.Select(candidate => candidate.Build).ToArray(),
                [replicateSeed]).Single());
            cumulativeElapsedMilliseconds[suites.Count] = stopwatch.ElapsedMilliseconds;
        }
        stopwatch.Stop();

        var replicateScores = suites.Select(suite => suite.Builds.ToDictionary(
                build => build.BuildId,
                build => build.AggregateScore,
                StringComparer.Ordinal))
            .ToArray();
        var replicateComponents = suites.Select(suite => suite.Builds.ToDictionary(
                build => build.BuildId,
                build => (IReadOnlyDictionary<string, double>)build.Components.ToDictionary(
                    component => component.ScenarioId,
                    component => component.Score,
                    StringComparer.Ordinal),
                StringComparer.Ordinal))
            .ToArray();
        var scenarioCount = suites[0].Scenarios.Count;
        var referenceProfiles = profiles.OrderBy(profile => profile.SlotCount)
            .Select(profile => CreateReferenceProfileSnapshot(
                profile,
                cohorts[profile.SlotCount],
                replicateScores,
                replicateComponents,
                suites[0].Scenarios,
                options.BenchmarkConfidenceTargetScoreMargin))
            .ToArray();
        var primaryReference = referenceProfiles.Single(profile => profile.SlotCount == 5);
        var primaryCohort = cohorts[5];
        var primaryIds = primaryCohort.Select(candidate => candidate.Build.Id).ToHashSet(StringComparer.Ordinal);
        var primaryReplicateScores = replicateScores.Select(scores =>
                (IReadOnlyDictionary<string, double>)scores
                    .Where(pair => primaryIds.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal))
            .ToArray();
        var baselineScores = primaryCohort.ToDictionary(
            candidate => candidate.Build.Id,
            candidate => candidate.Benchmark.AggregateScore,
            StringComparer.Ordinal);
        var referenceScores = primaryCohort.ToDictionary(
            candidate => candidate.Build.Id,
            candidate => primaryReplicateScores.Average(scores => scores[candidate.Build.Id]),
            StringComparer.Ordinal);
        var baselineRanks = RankDescending(baselineScores);
        var referenceRanks = RankDescending(referenceScores);
        var replicateRanks = primaryReplicateScores.Select(RankDescending).ToArray();
        var topK = Math.Min(20, primaryCohort.Count);
        var baselineTop = baselineRanks.Where(pair => pair.Value <= topK).Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
        var topOverlaps = replicateRanks.Select(ranks =>
                ranks.Count(pair => pair.Value <= topK && baselineTop.Contains(pair.Key)) / (double)topK)
            .ToArray();
        var replicateCorrelations = replicateRanks.Select(ranks => Spearman(ranks, referenceRanks)).ToArray();
        var panelSizes = CreatePanelSizeSnapshots(
            primaryProfile,
            primaryCohort,
            primaryReplicateScores,
            referenceScores,
            cumulativeElapsedMilliseconds,
            allCandidates.Length,
            profiles.Sum(profile => profile.Candidates.Count),
            scenarioCount);
        var selectedPanelIndex = Enumerable.Range(0, Math.Max(0, panelSizes.Count - 1))
            .FirstOrDefault(index => panelSizes[index].StatisticalGatesPassed
                                     && panelSizes[index + 1].StatisticalGatesPassed,
                -1);
        var selectedPanel = selectedPanelIndex >= 0 ? panelSizes[selectedPanelIndex] : null;

        var anchorCandidates = primaryCohort.ToDictionary(candidate => Signature(candidate.Build), StringComparer.Ordinal);
        var anchorBuildIds = DescriptorAuditAnchors.ToDictionary(
            anchor => anchor.AnchorId,
            anchor => anchorCandidates[Signature(anchor.Genome)].Build.Id,
            StringComparer.Ordinal);
        var highAnchor = DescriptorAuditAnchors.Single(anchor => anchor.Basin == "high");
        var comparisons = DescriptorAuditAnchors.Where(anchor => anchor.Basin == "low")
            .Select(lowAnchor => CreateConfidenceComparison(
                highAnchor.AnchorId,
                anchorBuildIds[highAnchor.AnchorId],
                lowAnchor.AnchorId,
                anchorBuildIds[lowAnchor.AnchorId],
                primaryReplicateScores))
            .ToArray();
        var baselineToMean = Spearman(baselineRanks, referenceRanks);
        var rankingPassed = baselineToMean >= 0.95
                            && replicateCorrelations.Min() >= 0.90
                            && topOverlaps.Min() >= 0.70
                            && topOverlaps.Average() >= 0.80;
        var orderingPassed = comparisons.All(comparison => comparison.OrderingConfident);
        var configuredSampleAdequate = primaryReference.Builds.All(row =>
                                                 row.Approximate95ConfidenceHalfWidth
                                                 <= options.BenchmarkConfidenceTargetScoreMargin)
                                             && rankingPassed
                                             && orderingPassed;
        var warnings = new List<string>();
        if (!rankingPassed)
            warnings.Add("The legacy single-seed PvE objective failed the predeclared rank-stability thresholds against the common-seed reference panel.");
        if (!orderingPassed)
            warnings.Add("The legacy E5 high anchor was not confidently superior to every legacy low anchor under the common-seed reference panel.");
        if (primaryReference.Builds.Any(row => row.Approximate95ConfidenceHalfWidth > options.BenchmarkConfidenceTargetScoreMargin))
            warnings.Add("The reference seed count does not achieve the requested score margin for every audited E5 build.");
        if (selectedPanel is null)
            warnings.Add("No submaximal seed panel and its next larger panel both passed the predeclared elite-ranking gates; do not promote a robust search objective.");
        else if (!selectedPanel.FifteenMinuteSearchRuntimePassed)
            warnings.Add($"The smallest statistically stable panel ({selectedPanel.SeedCount} seeds) projects beyond the 15-minute complete-search target; progressive evaluation or caching is required before promotion.");

        return new EliteBenchmarkConfidenceAuditSnapshot(
            "E5_ELITE",
            "Primary deterministic score-stratified E5 cohort plus the top 50 legacy candidates from E4 and E6; known E5 anchors, restart winners, and certification finalists are forced into their profile cohorts.",
            primaryProfile.Candidates.Count,
            primaryCohort.Count,
            replicateSeeds.Length,
            scenarioCount,
            allCandidates.Length * replicateSeeds.Length * scenarioCount,
            true,
            options.BenchmarkConfidenceTargetScoreMargin,
            topK,
            RoundRate(baselineToMean),
            RoundRate(replicateCorrelations.Min()),
            RoundRate(replicateCorrelations.Average()),
            RoundRate(topOverlaps.Min()),
            RoundRate(topOverlaps.Average()),
            Round(primaryReference.Builds.Select(row => row.Approximate95ConfidenceHalfWidth).Order().ElementAt(primaryReference.Builds.Count / 2)),
            Round(primaryReference.Builds.Max(row => row.Approximate95ConfidenceHalfWidth)),
            primaryReference.Builds.Max(row => row.RecommendedSeedCountForTargetMargin),
            rankingPassed,
            orderingPassed,
            configuredSampleAdequate,
            false,
            primaryReference.Builds,
            comparisons,
            warnings,
            replicateSeeds.Length,
            selectedPanel?.SeedCount ?? 0,
            selectedPanel is not null && selectedPanel.FifteenMinuteSearchRuntimePassed,
            panelSizes,
            referenceProfiles,
            replicateSeeds);
    }

    private IReadOnlyList<CertificationCandidate> PrepareConfidenceCohort(
        ProfileState profile,
        int runSeed,
        int requestedSize,
        bool includeKnownE5Anchors)
    {
        var candidatesBySignature = profile.Candidates
            .DistinctBy(candidate => Signature(candidate.Build))
            .ToDictionary(candidate => Signature(candidate.Build), StringComparer.Ordinal);
        if (includeKnownE5Anchors)
        {
            var missingAnchorBuilds = DescriptorAuditAnchors
                .Where(anchor => !candidatesBySignature.ContainsKey(Signature(anchor.Genome)))
                .Select(anchor => buildGenerator.MaterializeBuild(
                    CreateCanonicalId(profile.SlotCount, Signature(anchor.Genome)),
                    profile.Candidates[0].Build.ProfileId,
                    profile.SlotCount,
                    runSeed,
                    anchor.Genome))
                .ToArray();
            if (missingAnchorBuilds.Length > 0)
            {
                var missingBenchmarks = benchmarkRunner.Run(missingAnchorBuilds, runSeed).Builds
                    .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
                foreach (var build in missingAnchorBuilds)
                    candidatesBySignature[Signature(build)] = new CertificationCandidate(build, missingBenchmarks[build.Id]);
            }
        }

        var mandatorySignatures = profile.Finalists.Select(candidate => Signature(candidate.Build))
            .Concat(profile.Snapshot.Restarts.SelectMany(restart => new[]
            {
                restart.BestEssenceIds,
                restart.RawBestEssenceIds,
                restart.BaselineBestEssenceIds
            }).Where(genome => genome is not null).Select(genome => Signature(genome!)))
            .Concat(includeKnownE5Anchors
                ? DescriptorAuditAnchors.Select(anchor => Signature(anchor.Genome))
                : [])
            .ToHashSet(StringComparer.Ordinal);
        var ordered = candidatesBySignature.Values
            .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        var targetSize = Math.Min(ordered.Length, Math.Max(requestedSize, mandatorySignatures.Count));
        var selected = ordered.Where(candidate => mandatorySignatures.Contains(Signature(candidate.Build)))
            .ToDictionary(candidate => Signature(candidate.Build), StringComparer.Ordinal);
        var remainingSlots = targetSize - selected.Count;
        if (includeKnownE5Anchors && remainingSlots > 0)
        {
            for (var index = 0; index < remainingSlots; index++)
            {
                var position = remainingSlots == 1
                    ? ordered.Length / 2
                    : (int)Math.Round(index * (ordered.Length - 1d) / (remainingSlots - 1));
                selected.TryAdd(Signature(ordered[position].Build), ordered[position]);
            }
        }
        foreach (var candidate in ordered)
        {
            if (selected.Count >= targetSize)
                break;
            selected.TryAdd(Signature(candidate.Build), candidate);
        }
        return selected.Values.OrderBy(candidate => candidate.Build.Id, StringComparer.Ordinal).ToArray();
    }

    private static EliteBenchmarkReferenceProfileSnapshot CreateReferenceProfileSnapshot(
        ProfileState profile,
        IReadOnlyList<CertificationCandidate> cohort,
        IReadOnlyList<IReadOnlyDictionary<string, double>> replicateScores,
        IReadOnlyList<IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>> replicateComponents,
        IReadOnlyList<PveBenchmarkScenarioSnapshot> scenarios,
        double targetMargin)
    {
        var baselineScores = cohort.ToDictionary(
            candidate => candidate.Build.Id,
            candidate => candidate.Benchmark.AggregateScore,
            StringComparer.Ordinal);
        var robustScores = cohort.ToDictionary(
            candidate => candidate.Build.Id,
            candidate => replicateScores.Average(scores => scores[candidate.Build.Id]),
            StringComparer.Ordinal);
        var baselineRanks = RankDescending(baselineScores);
        var robustRanks = RankDescending(robustScores);
        var replicateRanks = replicateScores.Select(scores => RankDescending(
                cohort.ToDictionary(candidate => candidate.Build.Id, candidate => scores[candidate.Build.Id], StringComparer.Ordinal)))
            .ToArray();
        var buildRows = cohort.Select(candidate =>
            {
                var buildId = candidate.Build.Id;
                var scores = replicateScores.Select(values => values[buildId]).ToArray();
                var standardDeviation = SampleStandardDeviation(scores);
                var halfWidth = 1.959963984540054 * standardDeviation / Math.Sqrt(scores.Length);
                var recommendedSeeds = Math.Max(2, (int)Math.Ceiling(Math.Pow(
                    1.959963984540054 * standardDeviation / targetMargin,
                    2)));
                var scenarioMeans = scenarios.ToDictionary(
                    scenario => scenario.Id,
                    scenario => Round(replicateComponents.Average(values => values[buildId][scenario.Id])),
                    StringComparer.Ordinal);
                var scenarioStandardDeviations = scenarios.ToDictionary(
                    scenario => scenario.Id,
                    scenario => Round(SampleStandardDeviation(replicateComponents
                        .Select(values => values[buildId][scenario.Id]).ToArray())),
                    StringComparer.Ordinal);
                return new EliteBenchmarkConfidenceBuildSnapshot(
                    buildId,
                    Round(baselineScores[buildId]),
                    baselineRanks[buildId],
                    Round(robustScores[buildId]),
                    Round(standardDeviation),
                    Round(halfWidth),
                    recommendedSeeds,
                    Round(replicateRanks.Average(ranks => ranks[buildId])),
                    candidate.Build.Essences.Select(essence => essence.EssenceId).ToArray(),
                    robustRanks[buildId],
                    baselineRanks[buildId] - robustRanks[buildId],
                    scenarioMeans,
                    scenarioStandardDeviations,
                    scores.Select(Round).ToArray());
            })
            .OrderBy(row => row.RobustRank)
            .ToArray();
        var scenarioVariances = scenarios.Select(scenario =>
            {
                var allScores = cohort.SelectMany(candidate => replicateComponents.Select(values =>
                    values[candidate.Build.Id][scenario.Id])).ToArray();
                var withinBuild = cohort.Select(candidate => SampleStandardDeviation(replicateComponents
                        .Select(values => values[candidate.Build.Id][scenario.Id]).ToArray()))
                    .Order()
                    .ToArray();
                return new EliteBenchmarkScenarioVarianceSnapshot(
                    scenario.Id,
                    Round(allScores.Average()),
                    Round(withinBuild.Average()),
                    Round(withinBuild[withinBuild.Length / 2]),
                    Round(withinBuild.Max()));
            })
            .ToArray();
        var legacyBest = buildRows.Single(row => row.BaselineRank == 1);
        var robustBest = buildRows.Single(row => row.RobustRank == 1);
        return new EliteBenchmarkReferenceProfileSnapshot(
            profile.Snapshot.ProfileId,
            profile.SlotCount,
            profile.Candidates.Count,
            cohort.Count,
            legacyBest.BuildId,
            legacyBest.BaselineScore,
            legacyBest.RobustRank,
            robustBest.BuildId,
            robustBest.MeanScore,
            RoundRate(Spearman(baselineRanks, robustRanks)),
            scenarioVariances,
            buildRows);
    }

    private static IReadOnlyList<EliteBenchmarkPanelSizeSnapshot> CreatePanelSizeSnapshots(
        ProfileState profile,
        IReadOnlyList<CertificationCandidate> cohort,
        IReadOnlyList<IReadOnlyDictionary<string, double>> replicateScores,
        IReadOnlyDictionary<string, double> referenceScores,
        IReadOnlyDictionary<int, long> cumulativeElapsedMilliseconds,
        int totalAuditCandidateCount,
        int totalSearchCandidateCount,
        int scenarioCount)
    {
        var configuredCounts = new[] { 1, 2, 4, 8, 12, 16, 24, 32 }
            .Where(count => count <= replicateScores.Count)
            .Append(replicateScores.Count)
            .Distinct()
            .Order()
            .ToArray();
        var referenceRanks = RankDescending(referenceScores);
        var eliteIds = referenceRanks.Where(pair => pair.Value <= Math.Min(50, cohort.Count))
            .Select(pair => pair.Key)
            .ToArray();
        var finalistIds = profile.Finalists.Select(candidate => candidate.Build.Id)
            .Where(referenceRanks.ContainsKey)
            .ToArray();
        return configuredCounts.Select(seedCount =>
            {
                var panelScores = cohort.ToDictionary(
                    candidate => candidate.Build.Id,
                    candidate => replicateScores.Take(seedCount).Average(scores => scores[candidate.Build.Id]),
                    StringComparer.Ordinal);
                var panelRanks = RankDescending(panelScores);
                var clearlySeparatedPairs = 0;
                var clearlySeparatedReversals = 0;
                for (var first = 0; first < eliteIds.Length; first++)
                for (var second = first + 1; second < eliteIds.Length; second++)
                {
                    var firstId = eliteIds[first];
                    var secondId = eliteIds[second];
                    if (Math.Abs(referenceScores[firstId] - referenceScores[secondId]) < 1)
                        continue;
                    clearlySeparatedPairs++;
                    if (Math.Sign(panelScores[firstId] - panelScores[secondId])
                        != Math.Sign(referenceScores[firstId] - referenceScores[secondId]))
                    {
                        clearlySeparatedReversals++;
                    }
                }
                var halfWidths = seedCount < 2
                    ? []
                    : cohort.Select(candidate =>
                        {
                            var scores = replicateScores.Take(seedCount)
                                .Select(values => values[candidate.Build.Id])
                                .ToArray();
                            return 1.959963984540054 * SampleStandardDeviation(scores) / Math.Sqrt(scores.Length);
                        })
                        .Order()
                        .ToArray();
                var spearman = Spearman(panelRanks, referenceRanks);
                var top10 = TopKOverlap(panelRanks, referenceRanks, 10);
                var top20 = TopKOverlap(panelRanks, referenceRanks, 20);
                var top50 = TopKOverlap(panelRanks, referenceRanks, 50);
                var eliteAgreement = PairwiseOrderingAgreement(panelRanks, referenceRanks, eliteIds);
                var finalistAgreement = PairwiseOrderingAgreement(panelRanks, referenceRanks, finalistIds);
                var reversalRate = clearlySeparatedPairs == 0
                    ? 0
                    : clearlySeparatedReversals / (double)clearlySeparatedPairs;
                var statisticalGatesPassed = spearman >= 0.98
                                             && top10 >= 0.80
                                             && top20 >= 0.85
                                             && top50 >= 0.90
                                             && eliteAgreement >= 0.95
                                             && finalistAgreement >= 0.95
                                             && reversalRate <= 0.02;
                var elapsed = cumulativeElapsedMilliseconds[seedCount];
                var estimatedSearchSeconds = elapsed / 1000d
                                             * totalSearchCandidateCount
                                             / Math.Max(1, totalAuditCandidateCount);
                int PromotionDepth(int referenceTopK) => referenceRanks
                    .Where(pair => pair.Value <= Math.Min(referenceTopK, cohort.Count))
                    .Max(pair => panelRanks[pair.Key]);
                var referenceTop50 = referenceRanks
                    .Where(pair => pair.Value <= Math.Min(50, cohort.Count))
                    .Select(pair => pair.Key)
                    .ToArray();
                return new EliteBenchmarkPanelSizeSnapshot(
                    seedCount,
                    totalAuditCandidateCount * seedCount * scenarioCount,
                    elapsed,
                    RoundRate(spearman),
                    RoundRate(top10),
                    RoundRate(top20),
                    RoundRate(top50),
                    RoundRate(eliteAgreement),
                    RoundRate(finalistAgreement),
                    clearlySeparatedReversals,
                    clearlySeparatedPairs,
                    RoundRate(reversalRate),
                    halfWidths.Length == 0 ? null : Round(halfWidths[halfWidths.Length / 2]),
                    halfWidths.Length == 0 ? null : Round(halfWidths[^1]),
                    Round(estimatedSearchSeconds),
                    statisticalGatesPassed,
                    estimatedSearchSeconds <= 15 * 60,
                    PromotionDepth(10),
                    PromotionDepth(20),
                    PromotionDepth(50),
                    RoundRate(referenceTop50.Count(id => panelRanks[id] <= Math.Min(100, cohort.Count))
                              / (double)referenceTop50.Length));
            })
            .ToArray();
    }

    private static double TopKOverlap(
        IReadOnlyDictionary<string, int> first,
        IReadOnlyDictionary<string, int> second,
        int requestedK)
    {
        var k = Math.Min(requestedK, first.Count);
        var firstTop = first.Where(pair => pair.Value <= k).Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
        return second.Count(pair => pair.Value <= k && firstTop.Contains(pair.Key)) / (double)k;
    }

    private static double PairwiseOrderingAgreement(
        IReadOnlyDictionary<string, int> first,
        IReadOnlyDictionary<string, int> second,
        IReadOnlyList<string> ids)
    {
        if (ids.Count < 2)
            return 1;
        var agreements = 0;
        var comparisons = 0;
        for (var left = 0; left < ids.Count; left++)
        for (var right = left + 1; right < ids.Count; right++)
        {
            comparisons++;
            if (Math.Sign(first[ids[left]] - first[ids[right]])
                == Math.Sign(second[ids[left]] - second[ids[right]]))
            {
                agreements++;
            }
        }
        return comparisons == 0 ? 1 : agreements / (double)comparisons;
    }

    private static EliteBenchmarkConfidenceComparisonSnapshot CreateConfidenceComparison(
        string higherAnchorId,
        string higherBuildId,
        string lowerAnchorId,
        string lowerBuildId,
        IReadOnlyList<IReadOnlyDictionary<string, double>> replicateScores)
    {
        var differences = replicateScores.Select(scores => scores[higherBuildId] - scores[lowerBuildId]).ToArray();
        var mean = differences.Average();
        var standardDeviation = SampleStandardDeviation(differences);
        var halfWidth = 1.959963984540054 * standardDeviation / Math.Sqrt(differences.Length);
        return new EliteBenchmarkConfidenceComparisonSnapshot(
            higherAnchorId,
            lowerAnchorId,
            Round(mean),
            Round(standardDeviation),
            Round(mean - halfWidth),
            Round(mean + halfWidth),
            RoundRate(differences.Count(value => value > 0) / (double)differences.Length),
            mean - halfWidth > 0);
    }

    private static IReadOnlyDictionary<string, int> RankDescending(IReadOnlyDictionary<string, double> scores) =>
        scores.OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select((pair, index) => (pair.Key, Rank: index + 1))
            .ToDictionary(pair => pair.Key, pair => pair.Rank, StringComparer.Ordinal);

    private static double Spearman(
        IReadOnlyDictionary<string, int> first,
        IReadOnlyDictionary<string, int> second)
    {
        var count = first.Count;
        if (count < 2)
            return 1;
        var squaredDifference = first.Sum(pair =>
        {
            var difference = pair.Value - second[pair.Key];
            return difference * difference;
        });
        return 1 - 6d * squaredDifference / (count * (count * count - 1d));
    }

    private static double SampleStandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count < 2)
            return 0;
        var mean = values.Average();
        return Math.Sqrt(values.Sum(value => Math.Pow(value - mean, 2)) / (values.Count - 1));
    }

    private EliteDescriptorSeparabilityAuditSnapshot RunDescriptorSeparabilityAudit(
        IReadOnlyList<CertificationCandidate> canonicalCandidates,
        IReadOnlyList<CertificationCandidate> certificationCandidates,
        IReadOnlyList<RestartResult> restartResults,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed)
    {
        const int slotCount = 5;
        foreach (var essenceId in DescriptorAuditAnchors.SelectMany(anchor => anchor.Genome))
        {
            if (!definitionsById.ContainsKey(essenceId))
                throw new InvalidOperationException($"Descriptor audit anchor Essence '{essenceId}' is absent from production content.");
        }

        var originsBySignature = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var genomesBySignature = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var anchor in DescriptorAuditAnchors)
        foreach (var genome in EnumerateCompleteOneSwapNeighborhood(anchor.Genome, sourceFamilies, definitionsById))
        {
            var signature = Signature(genome);
            genomesBySignature[signature] = genome;
            if (!originsBySignature.TryGetValue(signature, out var origins))
            {
                origins = new HashSet<string>(StringComparer.Ordinal);
                originsBySignature[signature] = origins;
            }
            origins.Add(anchor.Basin);
        }

        var orderedGenomes = genomesBySignature.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
        var builds = orderedGenomes.Select(pair => buildGenerator.MaterializeBuild(
                CreateCanonicalId(slotCount, pair.Key),
                "E5_ELITE_DESCRIPTOR_AUDIT",
                slotCount,
                runSeed,
                pair.Value))
            .ToArray();
        var benchmarks = benchmarkRunner.Run(builds, runSeed).Builds
            .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        var auditedBySignature = builds.ToDictionary(
            Signature,
            build => new CertificationCandidate(build, benchmarks[build.Id]),
            StringComparer.Ordinal);
        var ambiguousSignatures = originsBySignature
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
        var labeledCandidates = originsBySignature
            .Where(pair => pair.Value.Count == 1)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new DescriptorAuditCandidate(auditedBySignature[pair.Key], pair.Value.Single()))
            .ToArray();

        var retainedSignatures = restartResults.SelectMany(restart => restart.Result.Snapshot.Profiles
                .Where(profile => profile.SlotCount == slotCount)
                .SelectMany(profile => profile.RetainedCandidates)
                .Select(candidate => Signature(candidate.EssenceIds)))
            .ToHashSet(StringComparer.Ordinal);
        var retainedCandidates = canonicalCandidates
            .Where(candidate => candidate.Build.SlotCount == slotCount
                                && retainedSignatures.Contains(Signature(candidate.Build)))
            .DistinctBy(candidate => Signature(candidate.Build))
            .OrderBy(candidate => Signature(candidate.Build), StringComparer.Ordinal)
            .ToArray();
        var anchorCandidates = DescriptorAuditAnchors.Select(anchor => new DescriptorAuditAnchorCandidate(
                anchor,
                auditedBySignature[Signature(anchor.Genome)]))
            .ToArray();
        var descriptorDefinitions = CreateDescriptorDefinitions(definitionsById);
        var descriptorFamilies = descriptorDefinitions.Select(descriptor => AnalyzeDescriptorFamily(
                descriptor,
                labeledCandidates,
                anchorCandidates,
                retainedCandidates))
            .ToArray();
        var collisionAudit = AnalyzeMechanicArchetypeCollision(
            certificationCandidates,
            anchorCandidates,
            retainedCandidates,
            definitionsById);
        var warnings = new List<string>();
        if (definitionsById.Values.All(definition => definition.Tags.Count == 0))
        {
            warnings.Add(
                "Production Essence-level tags are empty; mechanic and effect-role descriptors were derived from resolved authored ability specs instead.");
        }
        if (descriptorFamilies.All(descriptor => !descriptor.SeparabilityPassed))
        {
            warnings.Add(
                "No tested descriptor passed the predeclared accuracy, purity, and fragmentation criteria; do not spend a larger quality-diversity budget.");
        }
        var mapCandidateDescriptorIds = descriptorFamilies
            .Where(descriptor => descriptor.MapCandidatePassed)
            .Select(descriptor => descriptor.DescriptorId)
            .ToArray();
        if (mapCandidateDescriptorIds.Length == 0)
        {
            warnings.Add(
                "No tested descriptor passed both separability and a declared hard niche ceiling; do not start a descriptor-driven island search.");
        }
        if (!collisionAudit.MapCandidatePassed)
        {
            warnings.Add(
                "The bounded mechanic-intensity residual did not separate the colliding coarse E5 niche; do not start another mechanic-island search.");
        }

        return new EliteDescriptorSeparabilityAuditSnapshot(
            "E5_ELITE",
            slotCount,
            "Each known anchor plus every unique legal genome at substitution distance one; candidates shared by high and low neighborhoods are excluded from labeled metrics.",
            true,
            false,
            builds.Length,
            labeledCandidates.Count(candidate => candidate.Basin == "high"),
            labeledCandidates.Count(candidate => candidate.Basin == "low"),
            ambiguousSignatures.Count,
            retainedCandidates.Length,
            anchorCandidates.Select(value => new EliteDescriptorAnchorSnapshot(
                    value.Anchor.Basin,
                    value.Anchor.AnchorId,
                    value.Candidate.Build.Id,
                    value.Anchor.Genome,
                    value.Candidate.Benchmark.AggregateScore,
                    value.Candidate.Benchmark.Components.ToDictionary(
                        component => component.ScenarioId,
                        component => component.Score,
                        StringComparer.Ordinal)))
                .ToArray(),
            [
                SummarizeDescriptorBasin("high", labeledCandidates),
                SummarizeDescriptorBasin("low", labeledCandidates)
            ],
            descriptorFamilies,
            mapCandidateDescriptorIds,
            warnings,
            collisionAudit);
    }

    private static IEnumerable<IReadOnlyList<string>> EnumerateCompleteOneSwapNeighborhood(
        IReadOnlyList<string> anchor,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var yielded = new HashSet<string>(StringComparer.Ordinal);
        var orderedAnchor = anchor.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        yielded.Add(Signature(orderedAnchor));
        yield return orderedAnchor;
        for (var replacementIndex = 0; replacementIndex < orderedAnchor.Length; replacementIndex++)
        {
            var retained = orderedAnchor.Where((_, index) => index != replacementIndex).ToArray();
            var occupiedSources = retained.Select(id => definitionsById[id].SourceMonsterId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var family in sourceFamilies
                         .Where(family => !occupiedSources.Contains(family[0].SourceMonsterId))
                         .OrderBy(family => family[0].SourceMonsterId, StringComparer.OrdinalIgnoreCase))
            foreach (var definition in family.OrderBy(value => value.Id, StringComparer.OrdinalIgnoreCase))
            {
                var genome = retained.Append(definition.Id)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (yielded.Add(Signature(genome)))
                    yield return genome;
            }
        }
    }

    private static DescriptorDefinition[] CreateDescriptorDefinitions(
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
    [
        new(
            "source-family",
            "Authored source-family set",
            candidate => ExtractSourceFamilyFeatures(candidate, definitionsById),
            0,
            null),
        new(
            "mechanic",
            "Authored trigger/condition/resource mechanics",
            candidate => ExtractMechanicFeatures(candidate, definitionsById),
            0,
            null),
        new(
            "mechanic-archetype",
            "Generic eight-axis authored mechanic archetype",
            candidate => ExtractMechanicArchetypeFeatures(candidate, definitionsById),
            0,
            256),
        new(
            "effect-role",
            "Authored ability/effect role",
            candidate => ExtractEffectRoleFeatures(candidate, definitionsById),
            0,
            null),
        new(
            "scenario-shape",
            "Authoritative centered PvE scenario vector",
            ExtractScenarioShapeFeatures,
            2.5,
            null)
    ];

    private static IReadOnlyDictionary<string, double> ExtractSourceFamilyFeatures(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
        candidate.Build.Essences
            .Select(essence => definitionsById[essence.EssenceId].SourceMonsterId)
            .GroupBy(source => source, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (double)group.Count(), StringComparer.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, double> ExtractMechanicFeatures(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var features = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var ability in ResolveCandidateAbilities(candidate, definitionsById))
        {
            AddFeature(features, $"ability-kind:{ability.Kind}");
            AddFeature(features, ability.CooldownTicks == 0 ? "cooldown:passive" : "cooldown:active");
            if (ability.IsHardCrowdControl)
                AddFeature(features, "mechanic:hard-crowd-control");
            foreach (var cost in ability.Costs)
                AddFeature(features, $"cost:{cost.Resource}");
            foreach (var trigger in ability.Triggers)
            {
                AddFeature(features, $"trigger:{trigger.Event}");
                foreach (var condition in trigger.Conditions)
                    AddFeature(features, $"condition:{condition.Type}");
            }
            foreach (var effect in ability.Effects)
            foreach (var condition in effect.Conditions)
                AddFeature(features, $"condition:{condition.Type}");
        }
        return features;
    }

    private static IReadOnlyDictionary<string, double> ExtractEffectRoleFeatures(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var features = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var ability in ResolveCandidateAbilities(candidate, definitionsById))
        {
            foreach (var tag in ability.Tags.Concat(ability.DeliveryTags).Concat(ability.EffectTags)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
                AddFeature(features, $"tag:{tag}");
            foreach (var effect in ability.Effects)
            {
                AddFeature(features, $"operation:{effect.Operation}");
                AddFeature(features, $"target:{effect.Target}");
                if (effect.AttackType != Domain.Models.Damages.AttackType.None)
                    AddFeature(features, $"attack:{effect.AttackType}");
                if (effect.DamageType != Domain.Models.Damages.DamageType.None)
                    AddFeature(features, $"damage:{effect.DamageType}");
                foreach (var tag in effect.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
                    AddFeature(features, $"tag:{tag}");
            }
        }
        return features;
    }

    private static IReadOnlyDictionary<string, double> ExtractMechanicArchetypeFeatures(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
        ExtractMechanicArchetypeFeatures(
            candidate.Build.Essences.Select(essence => essence.EssenceId),
            definitionsById);

    private static IReadOnlyDictionary<string, double> ExtractMechanicArchetypeFeatures(
        IEnumerable<string> essenceIds,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var features = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var ability in essenceIds.SelectMany(essenceId =>
                 {
                     var definition = definitionsById[essenceId];
                     return new[] { definition.ActiveAbility, definition.PassiveAbility };
                 }))
        {
            foreach (var trigger in ability.Triggers)
            {
                switch (trigger.Event)
                {
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnAbilityUsed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBasicAttack:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnMeleeAttack:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnRangedAttack:
                        SetFeature(features, "axis:attack-action");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHit:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnKill:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnEnemyDeath:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnDamageDealt:
                        SetFeature(features, "axis:outgoing-result");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnDamaged:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnAttacked:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnMeleeAttacked:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnRangedAttacked:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnDodge:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBarrierApplied:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBarrierAbsorbed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBarrierBroken:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBarrierContributionBroken:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnBarrierExpired:
                        SetFeature(features, "axis:incoming-reaction");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHealthChanged:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHeal:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHealed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnLifestealHeal:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnEnemyHealed:
                        SetFeature(features, "axis:health-recovery");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusApplied:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusExpired:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusRemoved:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusCleansed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusDispelled:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusChanged:
                        SetFeature(features, "axis:status-lifecycle");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnCombatStart:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnInterval:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnDeath:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnSummonChanged:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnSummonGroupResolved:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStaggerBroken:
                        SetFeature(features, "axis:timeline-summon-terminal");
                        break;
                }
                foreach (var condition in trigger.Conditions)
                    AddMechanicConditionArchetype(features, condition.Type);
            }
            foreach (var effect in ability.Effects)
            foreach (var condition in effect.Conditions)
                AddMechanicConditionArchetype(features, condition.Type);
        }
        return features;
    }

    private static void AddMechanicConditionArchetype(
        IDictionary<string, double> features,
        Domain.Models.Combat.Abilities.AbilityConditionType condition)
    {
        switch (condition)
        {
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthAbovePercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthAtOrBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.AnyEnemyHealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NoEnemyHealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NonSummonedEnemyHealthSpreadAtMostPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NonSummonedEnemyHealthSpreadAbovePercent:
                SetFeature(features, "axis:health-recovery");
                break;
            case Domain.Models.Combat.Abilities.AbilityConditionType.HasStatus:
            case Domain.Models.Combat.Abilities.AbilityConditionType.StatusStacksAtLeast:
                SetFeature(features, "axis:status-lifecycle");
                break;
            case Domain.Models.Combat.Abilities.AbilityConditionType.HasCondition:
            case Domain.Models.Combat.Abilities.AbilityConditionType.ConditionStacksAtLeast:
            case Domain.Models.Combat.Abilities.AbilityConditionType.AnyEnemyHasCondition:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NoEnemyHasCondition:
                SetFeature(features, "axis:condition-dependency");
                break;
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventDamageTypeIs:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventAttackTypeIs:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventWasCritical:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventWasDirectHit:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventIdIs:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventSourceIsSelf:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventSourceIsEnemy:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventMagnitudeAtLeast:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventMagnitudeAtMost:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventSourceIsAlly:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventIdIsNot:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventTargetIsAlly:
            case Domain.Models.Combat.Abilities.AbilityConditionType.EventInstigatorIsSelf:
                SetFeature(features, "axis:event-filter");
                break;
        }
    }

    private static IReadOnlyDictionary<string, double> ExtractMechanicResidualFeatures(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
        ExtractMechanicResidualFeatures(
            candidate.Build.Essences.Select(essence => essence.EssenceId),
            definitionsById);

    private static IReadOnlyDictionary<string, double> ExtractMechanicResidualFeatures(
        IEnumerable<string> essenceIds,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var features = MechanicResidualFeatureNames.ToDictionary(
            feature => feature,
            _ => 0d,
            StringComparer.OrdinalIgnoreCase);
        foreach (var ability in essenceIds.SelectMany(essenceId =>
                 {
                     var definition = definitionsById[essenceId];
                     return new[] { definition.ActiveAbility, definition.PassiveAbility };
                 }))
        {
            foreach (var trigger in ability.Triggers)
            {
                switch (trigger.Event)
                {
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHit:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnKill:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnEnemyDeath:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnDamageDealt:
                        AddFeature(features, "intensity:outgoing-result");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHealthChanged:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHeal:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnHealed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnLifestealHeal:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnEnemyHealed:
                        AddFeature(features, "intensity:health-recovery");
                        break;
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusApplied:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusExpired:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusRemoved:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusCleansed:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusDispelled:
                    case Domain.Models.Combat.Abilities.AbilityTriggerEvent.OnStatusChanged:
                        AddFeature(features, "intensity:status-lifecycle");
                        break;
                }
                foreach (var condition in trigger.Conditions)
                    AddMechanicResidualCondition(features, condition.Type);
            }
            foreach (var effect in ability.Effects)
            foreach (var condition in effect.Conditions)
                AddMechanicResidualCondition(features, condition.Type);
        }
        return features.ToDictionary(
            pair => pair.Key,
            pair => Math.Min(2, pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddMechanicResidualCondition(
        IDictionary<string, double> features,
        Domain.Models.Combat.Abilities.AbilityConditionType condition)
    {
        switch (condition)
        {
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthAbovePercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.HealthAtOrBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.AnyEnemyHealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NoEnemyHealthBelowPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NonSummonedEnemyHealthSpreadAtMostPercent:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NonSummonedEnemyHealthSpreadAbovePercent:
                AddFeature(features, "intensity:health-recovery");
                break;
            case Domain.Models.Combat.Abilities.AbilityConditionType.HasStatus:
            case Domain.Models.Combat.Abilities.AbilityConditionType.StatusStacksAtLeast:
                AddFeature(features, "intensity:status-lifecycle");
                break;
            case Domain.Models.Combat.Abilities.AbilityConditionType.HasCondition:
            case Domain.Models.Combat.Abilities.AbilityConditionType.ConditionStacksAtLeast:
            case Domain.Models.Combat.Abilities.AbilityConditionType.AnyEnemyHasCondition:
            case Domain.Models.Combat.Abilities.AbilityConditionType.NoEnemyHasCondition:
                AddFeature(features, "intensity:condition-dependency");
                break;
        }
    }

    private static IReadOnlyDictionary<string, double> ExtractScenarioShapeFeatures(CertificationCandidate candidate) =>
        candidate.Benchmark.Components.ToDictionary(
            component => component.ScenarioId,
            component => component.Score - candidate.Benchmark.AggregateScore,
            StringComparer.Ordinal);

    private static IEnumerable<Domain.Models.Combat.Abilities.AbilitySpec> ResolveCandidateAbilities(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
        candidate.Build.Essences.SelectMany(essence =>
        {
            var definition = definitionsById[essence.EssenceId];
            return new[] { definition.ActiveAbility, definition.PassiveAbility };
        });

    private static void AddFeature(IDictionary<string, double> features, string feature) =>
        features[feature] = (features.TryGetValue(feature, out var current) ? current : 0) + 1;

    private static void SetFeature(IDictionary<string, double> features, string feature) =>
        features[feature] = 1;

    private static EliteDescriptorCollisionAuditSnapshot AnalyzeMechanicArchetypeCollision(
        IReadOnlyList<CertificationCandidate> canonicalCandidates,
        IReadOnlyList<DescriptorAuditAnchorCandidate> anchors,
        IReadOnlyList<CertificationCandidate> retainedCandidates,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        const int theoreticalNicheCeiling = 81;
        var highAnchor = anchors.Single(anchor => anchor.Anchor.Basin == "high");
        var parentHighNicheSignature = CreateDescriptorSignature(
            ExtractMechanicArchetypeFeatures(highAnchor.Candidate, definitionsById),
            0);
        var highScoreFloor = highAnchor.Candidate.Benchmark.AggregateScore - 0.50;
        var lowScoreCeiling = anchors.Where(anchor => anchor.Anchor.Basin == "low")
            .Max(anchor => anchor.Candidate.Benchmark.AggregateScore);
        var parentNicheCandidates = canonicalCandidates
            .Where(candidate => candidate.Build.SlotCount == 5
                                && CreateDescriptorSignature(
                                    ExtractMechanicArchetypeFeatures(candidate, definitionsById),
                                    0) == parentHighNicheSignature)
            .DistinctBy(candidate => Signature(candidate.Build))
            .OrderBy(candidate => Signature(candidate.Build), StringComparer.Ordinal)
            .ToArray();
        var rows = parentNicheCandidates.Select(candidate => new
            {
                Basin = candidate.Benchmark.AggregateScore >= highScoreFloor
                    ? "high"
                    : candidate.Benchmark.AggregateScore <= lowScoreCeiling
                        ? "low"
                        : null,
                Candidate = candidate,
                Features = ExtractMechanicResidualFeatures(candidate, definitionsById)
            })
            .Where(row => row.Basin is not null)
            .Select(row => new
            {
                Basin = row.Basin!,
                row.Candidate,
                row.Features,
                Signature = CreateDescriptorSignature(row.Features, 0)
            })
            .ToArray();
        var groups = rows.GroupBy(row => row.Signature, StringComparer.Ordinal).ToArray();
        var exactPurity = groups.Sum(group => group
                              .GroupBy(row => row.Basin, StringComparer.Ordinal)
                              .Max(labels => labels.Count()))
                          / (double)Math.Max(1, rows.Length);
        var singletonRate = groups.Where(group => group.Count() == 1).Sum(group => group.Count())
                            / (double)Math.Max(1, rows.Length);
        var highCorrect = 0;
        var lowCorrect = 0;
        var ambiguous = 0;
        foreach (var row in rows)
        {
            var group = groups.Single(candidateGroup => candidateGroup.Key == row.Signature);
            var highPeers = group.Count(candidate => candidate.Basin == "high") - (row.Basin == "high" ? 1 : 0);
            var lowPeers = group.Count(candidate => candidate.Basin == "low") - (row.Basin == "low" ? 1 : 0);
            if (highPeers == lowPeers)
            {
                ambiguous++;
                continue;
            }
            var prediction = highPeers > lowPeers ? "high" : "low";
            if (prediction == row.Basin)
            {
                if (row.Basin == "high")
                    highCorrect++;
                else
                    lowCorrect++;
            }
        }

        var highCount = rows.Count(row => row.Basin == "high");
        var lowCount = rows.Count(row => row.Basin == "low");
        var highAccuracy = highCorrect / (double)Math.Max(1, highCount);
        var lowAccuracy = lowCorrect / (double)Math.Max(1, lowCount);
        var balancedAccuracy = (highAccuracy + lowAccuracy) / 2;
        var highAnchorResidualFeatures = ExtractMechanicResidualFeatures(highAnchor.Candidate, definitionsById);
        var highAnchorResidualSignature = CreateDescriptorSignature(highAnchorResidualFeatures, 0);
        var highAnchorCollision = rows.Any(row =>
            row.Basin == "low" && row.Signature == highAnchorResidualSignature);
        var retainedResidualNiche = retainedCandidates
            .Where(candidate => CreateDescriptorSignature(
                                    ExtractMechanicArchetypeFeatures(candidate, definitionsById),
                                    0) == parentHighNicheSignature
                                && CreateDescriptorSignature(
                                    ExtractMechanicResidualFeatures(candidate, definitionsById),
                                    0) == highAnchorResidualSignature)
            .ToArray();
        var contrasts = MechanicResidualFeatureNames.Select(feature =>
            {
                var highValues = rows.Where(row => row.Basin == "high")
                    .Select(row => row.Features.GetValueOrDefault(feature))
                    .ToArray();
                var lowValues = rows.Where(row => row.Basin == "low")
                    .Select(row => row.Features.GetValueOrDefault(feature))
                    .ToArray();
                var allValues = highValues.Concat(lowValues)
                    .Append(highAnchorResidualFeatures.GetValueOrDefault(feature))
                    .ToArray();
                var highMean = highValues.Length == 0 ? 0 : highValues.Average();
                var lowMean = lowValues.Length == 0 ? 0 : lowValues.Average();
                var range = allValues.Max() - allValues.Min();
                return new EliteDescriptorFeatureContrastSnapshot(
                    feature,
                    Round(highMean),
                    Round(lowMean),
                    RoundRate(range <= 0 ? 0 : Math.Abs(highMean - lowMean) / range));
            })
            .OrderByDescending(contrast => contrast.NormalizedDifference)
            .ThenBy(contrast => contrast.Feature, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hardNicheCeilingPassed = groups.Length <= theoreticalNicheCeiling;
        var separabilityPassed = highCount > 0
                                 && lowCount > 0
                                 && !highAnchorCollision
                                 && balancedAccuracy >= 0.80
                                 && exactPurity >= 0.80
                                 && singletonRate <= 0.50;

        return new EliteDescriptorCollisionAuditSnapshot(
            "mechanic-archetype",
            parentHighNicheSignature,
            "mechanic-intensity-residual",
            "Four-axis capped authored-mechanic intensity residual",
            "Every unique independently generated E5 certification candidate after restart and finalist refinement that occupies the known high mechanic-archetype niche; audit-neighborhood candidates are excluded.",
            MechanicResidualFeatureNames.Length,
            parentNicheCandidates.Length,
            rows.Length,
            highCount,
            lowCount,
            parentNicheCandidates.Length - rows.Length,
            Round(highScoreFloor),
            Round(lowScoreCeiling),
            groups.Length,
            RoundRate(exactPurity),
            RoundRate(singletonRate),
            RoundRate(highAccuracy),
            RoundRate(lowAccuracy),
            RoundRate(balancedAccuracy),
            ambiguous,
            highAnchorCollision,
            retainedResidualNiche.Length,
            retainedResidualNiche.Length == 0
                ? null
                : retainedResidualNiche.Max(candidate => candidate.Benchmark.AggregateScore),
            theoreticalNicheCeiling,
            hardNicheCeilingPassed,
            separabilityPassed,
            separabilityPassed && hardNicheCeilingPassed,
            contrasts);
    }

    private static EliteDescriptorFamilySnapshot AnalyzeDescriptorFamily(
        DescriptorDefinition descriptor,
        IReadOnlyList<DescriptorAuditCandidate> candidates,
        IReadOnlyList<DescriptorAuditAnchorCandidate> anchors,
        IReadOnlyList<CertificationCandidate> retainedCandidates)
    {
        var featureRows = candidates.Select(candidate => new DescriptorFeatureRow(
                candidate.Basin,
                descriptor.Extract(candidate.Candidate)))
            .ToArray();
        var anchorRows = anchors.Select(anchor => new DescriptorAnchorFeatureRow(
                anchor.Anchor.Basin,
                descriptor.Extract(anchor.Candidate)))
            .ToArray();
        var allFeatures = featureRows.SelectMany(row => row.Features.Keys)
            .Concat(anchorRows.SelectMany(row => row.Features.Keys))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(feature => feature, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var ranges = allFeatures.ToDictionary(
            feature => feature,
            feature =>
            {
                var values = featureRows.Select(row => row.Features.GetValueOrDefault(feature))
                    .Concat(anchorRows.Select(row => row.Features.GetValueOrDefault(feature)))
                    .DefaultIfEmpty(0)
                    .ToArray();
                return (Minimum: values.Min(), Maximum: values.Max());
            },
            StringComparer.OrdinalIgnoreCase);
        var signatures = featureRows.Select(row => new
            {
                row.Basin,
                Signature = CreateDescriptorSignature(row.Features, descriptor.SignatureBinWidth)
            })
            .ToArray();
        var signatureGroups = signatures.GroupBy(value => value.Signature, StringComparer.Ordinal).ToArray();
        var exactPurity = signatureGroups.Sum(group => group.GroupBy(value => value.Basin, StringComparer.Ordinal)
            .Max(labels => labels.Count())) / (double)Math.Max(1, signatures.Length);
        var singletonRate = signatureGroups.Where(group => group.Count() == 1).Sum(group => group.Count())
                            / (double)Math.Max(1, signatures.Length);
        var highAnchors = anchorRows.Where(anchor => anchor.Basin == "high").ToArray();
        var lowAnchors = anchorRows.Where(anchor => anchor.Basin == "low").ToArray();
        var highCorrect = 0;
        var lowCorrect = 0;
        var ambiguous = 0;
        foreach (var row in featureRows)
        {
            var highDistance = highAnchors.Min(anchor => DescriptorDistance(row.Features, anchor.Features, allFeatures, ranges));
            var lowDistance = lowAnchors.Min(anchor => DescriptorDistance(row.Features, anchor.Features, allFeatures, ranges));
            if (Math.Abs(highDistance - lowDistance) <= 0.000000001)
            {
                ambiguous++;
                continue;
            }
            var predicted = highDistance < lowDistance ? "high" : "low";
            if (predicted == row.Basin)
            {
                if (row.Basin == "high")
                    highCorrect++;
                else
                    lowCorrect++;
            }
        }
        var highCount = featureRows.Count(row => row.Basin == "high");
        var lowCount = featureRows.Count(row => row.Basin == "low");
        var highAccuracy = highCorrect / (double)Math.Max(1, highCount);
        var lowAccuracy = lowCorrect / (double)Math.Max(1, lowCount);
        var balancedAccuracy = (highAccuracy + lowAccuracy) / 2;
        var highAnchorSignature = CreateDescriptorSignature(highAnchors[0].Features, descriptor.SignatureBinWidth);
        var anchorCollision = lowAnchors.Any(anchor =>
            CreateDescriptorSignature(anchor.Features, descriptor.SignatureBinWidth) == highAnchorSignature);
        var retainedNiche = retainedCandidates.Select(candidate => new
            {
                Candidate = candidate,
                Signature = CreateDescriptorSignature(descriptor.Extract(candidate), descriptor.SignatureBinWidth)
            })
            .Where(value => value.Signature == highAnchorSignature)
            .ToArray();
        var contrasts = allFeatures.Select(feature =>
            {
                var highMean = featureRows.Where(row => row.Basin == "high")
                    .Average(row => row.Features.GetValueOrDefault(feature));
                var lowMean = featureRows.Where(row => row.Basin == "low")
                    .Average(row => row.Features.GetValueOrDefault(feature));
                var range = ranges[feature].Maximum - ranges[feature].Minimum;
                return new EliteDescriptorFeatureContrastSnapshot(
                    feature,
                    Round(highMean),
                    Round(lowMean),
                    RoundRate(range <= 0 ? 0 : Math.Abs(highMean - lowMean) / range));
            })
            .OrderByDescending(contrast => contrast.NormalizedDifference)
            .ThenBy(contrast => contrast.Feature, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();
        var separabilityPassed = !anchorCollision
                                 && balancedAccuracy >= 0.80
                                 && exactPurity >= 0.80
                                 && singletonRate <= 0.50;
        var hardNicheCeilingPassed = descriptor.TheoreticalNicheCeiling is null
                                     || signatureGroups.Length <= descriptor.TheoreticalNicheCeiling.Value;
        var mapCandidatePassed = separabilityPassed
                                 && descriptor.TheoreticalNicheCeiling is not null
                                 && hardNicheCeilingPassed;

        return new EliteDescriptorFamilySnapshot(
            descriptor.Id,
            descriptor.DisplayName,
            allFeatures.Length,
            signatureGroups.Length,
            RoundRate(exactPurity),
            RoundRate(singletonRate),
            RoundRate(highAccuracy),
            RoundRate(lowAccuracy),
            RoundRate(balancedAccuracy),
            ambiguous,
            anchorCollision,
            retainedNiche.Length,
            retainedNiche.Length == 0
                ? null
                : retainedNiche.Max(value => value.Candidate.Benchmark.AggregateScore),
            descriptor.TheoreticalNicheCeiling,
            hardNicheCeilingPassed,
            separabilityPassed,
            mapCandidatePassed,
            contrasts);
    }

    private static string CreateDescriptorSignature(
        IReadOnlyDictionary<string, double> features,
        double binWidth) =>
        string.Join(
            "|",
            features.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => binWidth > 0
                    ? $"{pair.Key}={Math.Round(pair.Value / binWidth, MidpointRounding.AwayFromZero):0}"
                    : $"{pair.Key}={pair.Value:0.####}"));

    private static double DescriptorDistance(
        IReadOnlyDictionary<string, double> first,
        IReadOnlyDictionary<string, double> second,
        IReadOnlyList<string> features,
        IReadOnlyDictionary<string, (double Minimum, double Maximum)> ranges)
    {
        if (features.Count == 0)
            return 0;
        return features.Sum(feature =>
        {
            var range = ranges[feature].Maximum - ranges[feature].Minimum;
            return range <= 0
                ? 0
                : Math.Abs(first.GetValueOrDefault(feature) - second.GetValueOrDefault(feature)) / range;
        }) / features.Count;
    }

    private static EliteDescriptorBasinSnapshot SummarizeDescriptorBasin(
        string basin,
        IReadOnlyList<DescriptorAuditCandidate> candidates)
    {
        var selected = candidates.Where(candidate => candidate.Basin == basin)
            .Select(candidate => candidate.Candidate)
            .OrderBy(candidate => candidate.Benchmark.AggregateScore)
            .ToArray();
        var scores = selected.Select(candidate => candidate.Benchmark.AggregateScore).ToArray();
        var middle = scores.Length / 2;
        var median = scores.Length % 2 == 0
            ? (scores[middle - 1] + scores[middle]) / 2
            : scores[middle];
        var scenarios = selected.SelectMany(candidate => candidate.Benchmark.Components)
            .GroupBy(component => component.ScenarioId, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => Round(group.Average(component => component.Score)), StringComparer.Ordinal);
        return new EliteDescriptorBasinSnapshot(
            basin,
            selected.Length,
            scores.Min(),
            Round(median),
            scores.Max(),
            scenarios);
    }

    private EliteRestartBridgeAuditSnapshot[] RunBridgeAudits(
        IReadOnlyList<EliteCertificationProfileSnapshot> profiles,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        EliteCertificationPolicy policy) =>
        profiles.OrderBy(profile => profile.SlotCount)
            .Where(profile => profile.Restarts
                .Select(restart => Signature(restart.BestEssenceIds ?? []))
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(profile => RunBridgeAudit(profile, definitionsById, runSeed, policy))
            .ToArray();

    private EliteRestartBridgeAuditSnapshot RunBridgeAudit(
        EliteCertificationProfileSnapshot profile,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        EliteCertificationPolicy policy)
    {
        var target = profile.Restarts
            .OrderByDescending(restart => restart.BestScore)
            .ThenBy(restart => restart.Restart)
            .ThenBy(restart => restart.BestBuildId, StringComparer.Ordinal)
            .First();
        var targetSignature = Signature(target.BestEssenceIds ?? []);
        var source = profile.Restarts
            .Where(restart => Signature(restart.BestEssenceIds ?? []) != targetSignature)
            .OrderBy(restart => restart.BestScore)
            .ThenByDescending(restart => GenomeDistance(restart.BestEssenceIds ?? [], target.BestEssenceIds ?? []))
            .ThenBy(restart => restart.Restart)
            .ThenBy(restart => restart.BestBuildId, StringComparer.Ordinal)
            .First();
        var sourceGenome = (source.BestEssenceIds ?? []).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        var targetGenome = (target.BestEssenceIds ?? []).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        var distance = GenomeDistance(sourceGenome, targetGenome);
        var genomes = EnumerateMinimumBridgeGenomes(sourceGenome, targetGenome, definitionsById)
            .OrderBy(value => value.Level)
            .ThenBy(value => Signature(value.Genome), StringComparer.Ordinal)
            .ToArray();
        var builds = genomes.Select(value => buildGenerator.MaterializeBuild(
                CreateCanonicalId(profile.SlotCount, Signature(value.Genome)),
                $"E{profile.SlotCount}_ELITE_BRIDGE_AUDIT",
                profile.SlotCount,
                runSeed,
                value.Genome))
            .ToArray();
        var benchmarks = benchmarkRunner.Run(builds, runSeed).Builds
            .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        var nodes = genomes.Select((value, index) => new BridgeNode(
                value.Level,
                Signature(value.Genome),
                builds[index],
                benchmarks[builds[index].Id].AggregateScore))
            .ToArray();
        var nodesByLevel = nodes.GroupBy(node => node.Level)
            .ToDictionary(group => group.Key, group => group.OrderBy(node => node.Signature, StringComparer.Ordinal).ToArray());
        var sourceNode = nodes.Single(node => node.Level == 0 && node.Signature == Signature(sourceGenome));
        var states = new Dictionary<string, BridgePathState>(StringComparer.Ordinal)
        {
            [sourceNode.Signature] = new BridgePathState([sourceNode], sourceNode.Score, 0, 0)
        };
        var nonRegressingReachable = new HashSet<string>(StringComparer.Ordinal) { sourceNode.Signature };
        var toleranceReachable = new HashSet<string>(StringComparer.Ordinal) { sourceNode.Signature };
        for (var level = 1; level <= distance; level++)
        {
            foreach (var node in nodesByLevel.GetValueOrDefault(level, []))
            {
                var predecessors = nodesByLevel.GetValueOrDefault(level - 1, [])
                    .Where(previous => GenomeDistance(previous.Build.Essences.Select(value => value.EssenceId).ToArray(), node.Build.Essences.Select(value => value.EssenceId).ToArray()) == 1)
                    .OrderBy(previous => previous.Signature, StringComparer.Ordinal)
                    .ToArray();
                var path = predecessors.Where(previous => states.ContainsKey(previous.Signature))
                    .Select(previous => ExtendBridgePath(states[previous.Signature], node, sourceNode.Score))
                    .OrderByDescending(state => state.MinimumScore)
                    .ThenBy(state => state.LargestSingleStepRegression)
                    .ThenBy(state => state.TotalTemporaryRegressionBelowSource)
                    .ThenBy(state => BridgePathKey(state.Nodes), StringComparer.Ordinal)
                    .FirstOrDefault();
                if (path is not null)
                    states[node.Signature] = path;
                if (predecessors.Any(previous => nonRegressingReachable.Contains(previous.Signature)
                                                 && node.Score >= previous.Score))
                {
                    nonRegressingReachable.Add(node.Signature);
                }
                if (predecessors.Any(previous => toleranceReachable.Contains(previous.Signature)
                                                 && previous.Score - node.Score <= policy.RestartBestScoreSpreadTolerance))
                {
                    toleranceReachable.Add(node.Signature);
                }
            }
        }
        var targetNode = nodes.Single(node => node.Level == distance && node.Signature == Signature(targetGenome));
        var bestPath = states[targetNode.Signature];
        return new EliteRestartBridgeAuditSnapshot(
            profile.ProfileId,
            profile.SlotCount,
            source.Restart,
            source.BestBuildId ?? sourceNode.Build.Id,
            sourceGenome,
            source.BestScore,
            target.Restart,
            target.BestBuildId ?? targetNode.Build.Id,
            targetGenome,
            target.BestScore,
            distance,
            nodes.Length,
            bestPath.Nodes.Select(node => new EliteBridgePathNodeSnapshot(
                    node.Build.Id,
                    node.Build.Essences.Select(value => value.EssenceId).ToArray(),
                    node.Score))
                .ToArray(),
            Round(bestPath.MinimumScore),
            Round(bestPath.LargestSingleStepRegression),
            Round(bestPath.TotalTemporaryRegressionBelowSource),
            policy.RestartBestScoreSpreadTolerance,
            nonRegressingReachable.Contains(targetNode.Signature),
            toleranceReachable.Contains(targetNode.Signature));
    }

    private static IEnumerable<BridgeGenome> EnumerateMinimumBridgeGenomes(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var common = source.Intersect(target, StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sourceOnly = source.Except(target, StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var targetOnly = target.Except(source, StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        for (var level = 0; level <= sourceOnly.Length; level++)
        {
            foreach (var retainedSource in Combinations(sourceOnly, sourceOnly.Length - level))
            foreach (var introducedTarget in Combinations(targetOnly, level))
            {
                var genome = common.Concat(retainedSource).Concat(introducedTarget)
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (genome.Select(id => definitionsById[id].SourceMonsterId)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() == genome.Length)
                {
                    yield return new BridgeGenome(level, genome);
                }
            }
        }
    }

    private static IEnumerable<IReadOnlyList<string>> Combinations(IReadOnlyList<string> values, int count)
    {
        if (count == 0)
        {
            yield return [];
            yield break;
        }
        for (var index = 0; index <= values.Count - count; index++)
        {
            foreach (var tail in Combinations(values.Skip(index + 1).ToArray(), count - 1))
                yield return new[] { values[index] }.Concat(tail).ToArray();
        }
    }

    private static BridgePathState ExtendBridgePath(BridgePathState path, BridgeNode node, double sourceScore)
    {
        var previous = path.Nodes[^1];
        return new BridgePathState(
            path.Nodes.Append(node).ToArray(),
            Math.Min(path.MinimumScore, node.Score),
            Math.Max(path.LargestSingleStepRegression, Math.Max(0, previous.Score - node.Score)),
            path.TotalTemporaryRegressionBelowSource + Math.Max(0, sourceScore - node.Score));
    }

    private static string BridgePathKey(IEnumerable<BridgeNode> nodes) =>
        string.Join("->", nodes.Select(node => node.Signature));

    private EliteCertificationFloorSnapshot[] CertifyFloors(
        RepresentativeBuildLibrarySnapshot representativeBuilds,
        WorldTowerAnalysisSnapshot worldTower,
        EncounterCalibrationSnapshot calibration,
        IReadOnlyList<ProfileState> profileStates,
        TopPlayerFixtureDocument fixtures,
        IReadOnlyList<EssenceBuildSnapshot> curatedCandidates,
        int runSeed,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        var calibrationByFloor = calibration.Floors.ToDictionary(floor => floor.Floor);
        var representativeById = representativeBuilds.Profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        return worldTower.Floors.OrderBy(floor => floor.Floor)
            .Select(floor => CertifyFloor(
                floor,
                calibrationByFloor.GetValueOrDefault(floor.Floor)
                ?? throw new InvalidOperationException($"Elite certification could not find calibration for Floor {floor.Floor}."),
                representativeById.GetValueOrDefault(floor.RepresentativeProfileId)
                ?? throw new InvalidOperationException($"Elite certification could not find profile '{floor.RepresentativeProfileId}'."),
                profileStates.Single(state => state.SlotCount == representativeById[floor.RepresentativeProfileId].SlotCount),
                fixtures,
                curatedCandidates,
                runSeed,
                worldTower.Options.MaxTicks,
                policy,
                options))
            .ToArray();
    }

    private IReadOnlyList<RestartResult> RunIndependentSearches(
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        int runSeed,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        var plateauRequirement = options.Profile == EliteCertificationProfile.Release
            ? policy.ReleasePlateauGenerations
            : policy.DeveloperPlateauGenerations;
        var optimizerOptions = new EssenceOptimizerOptions(
            options.PopulationSize,
            options.Generations,
            options.EliteCount,
            options.MutationRate,
            options.RandomInjectionRate,
            options.DiversityPenalty,
            Math.Min(options.FinalistsPerSlotProfile, options.PopulationSize),
            options.MaximumGenerations,
            plateauRequirement,
            policy.PlateauImprovementTolerance,
            options.CrossoverRate,
            options.CoordinatedMutationRate,
            options.ExplorerArchiveSize);
        return Enumerable.Range(1, options.RestartCount).Select(restart =>
        {
            var searchSeed = StableRandom.Seed(
                "balance-elite-certification-restart-v1",
                runSeed.ToString(CultureInfo.InvariantCulture),
                restart.ToString(CultureInfo.InvariantCulture));
            var initial = EssenceBuildGenerator.InitialSlotCounts.SelectMany(slotCount =>
                    buildGenerator.GenerateProfile(
                        sourceFamilies,
                        searchSeed,
                        slotCount,
                        options.PopulationSize,
                        $"E{slotCount}_ELITE_R{restart:00}",
                        $"E{slotCount}_ELITE_R{restart:00}_INITIAL"))
                .ToArray();
            var initialBenchmarks = benchmarkRunner.Run(initial, runSeed);
            var baselineResult = optimizer.Optimize(initial, initialBenchmarks, runSeed, optimizerOptions, searchSeed);
            var baselineSignatures = baselineResult.EvaluatedCandidates.Select(candidate => Signature(candidate.Build))
                .ToHashSet(StringComparer.Ordinal);
            var portfolio = GenerateStratifiedPortfolio(
                sourceFamilies,
                baselineSignatures,
                restart,
                searchSeed,
                runSeed,
                options.StratifiedPortfolioCandidatesPerProfile);
            var result = baselineResult with
            {
                EvaluatedCandidates = baselineResult.EvaluatedCandidates.Concat(portfolio).ToArray()
            };
            return new RestartResult(
                restart,
                searchSeed,
                result,
                baselineSignatures,
                options.StratifiedPortfolioCandidatesPerProfile);
        }).ToArray();
    }

    private IReadOnlyList<EssenceOptimizerEvaluatedCandidate> GenerateStratifiedPortfolio(
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlySet<string> baselineSignatures,
        int restart,
        int searchSeed,
        int runSeed,
        int candidatesPerProfile)
    {
        if (candidatesPerProfile == 0)
            return [];
        var orderedFamilies = sourceFamilies
            .OrderBy(family => family[0].SourceMonsterId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var strides = Enumerable.Range(1, orderedFamilies.Length - 1)
            .Where(value => GreatestCommonDivisor(value, orderedFamilies.Length) == 1)
            .ToArray();
        var builds = new List<EssenceBuildSnapshot>(candidatesPerProfile * EssenceBuildGenerator.InitialSlotCounts.Count);
        foreach (var slotCount in EssenceBuildGenerator.InitialSlotCounts)
        {
            var signatures = baselineSignatures.ToHashSet(StringComparer.Ordinal);
            var attempt = 0;
            while (builds.Count(build => build.SlotCount == slotCount) < candidatesPerProfile
                   && attempt++ < candidatesPerProfile * 100)
            {
                var ordinal = attempt - 1;
                var seed = StableRandom.Seed(
                    "balance-elite-stratified-portfolio-v1",
                    searchSeed.ToString(CultureInfo.InvariantCulture),
                    restart.ToString(CultureInfo.InvariantCulture),
                    slotCount.ToString(CultureInfo.InvariantCulture),
                    ordinal.ToString(CultureInfo.InvariantCulture));
                var unsignedSeed = unchecked((uint)seed);
                var start = (int)(unsignedSeed % (uint)orderedFamilies.Length);
                var stride = strides[(int)((unsignedSeed / (uint)orderedFamilies.Length) % (uint)strides.Length)];
                var genes = Enumerable.Range(0, slotCount).Select(geneIndex =>
                    {
                        var family = orderedFamilies[(start + geneIndex * stride) % orderedFamilies.Length]
                            .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                            .ToArray();
                        var variantSeed = StableRandom.Seed(
                            "balance-elite-stratified-variant-v1",
                            seed.ToString(CultureInfo.InvariantCulture),
                            geneIndex.ToString(CultureInfo.InvariantCulture));
                        return family[(int)(unchecked((uint)variantSeed) % (uint)family.Length)].Id;
                    })
                    .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var signature = Signature(genes);
                if (!signatures.Add(signature))
                    continue;
                builds.Add(buildGenerator.MaterializeBuild(
                    $"E{slotCount}_ELITE_R{restart:00}_PORTFOLIO_{builds.Count(build => build.SlotCount == slotCount) + 1:0000}",
                    $"E{slotCount}_ELITE_R{restart:00}_STRATIFIED_PORTFOLIO",
                    slotCount,
                    seed,
                    genes));
            }
            if (builds.Count(build => build.SlotCount == slotCount) != candidatesPerProfile)
            {
                throw new InvalidOperationException(
                    $"Could not generate {candidatesPerProfile} unique stratified E{slotCount} portfolio candidates for restart {restart}.");
            }
        }
        var benchmarks = benchmarkRunner.Run(builds, runSeed).Builds
            .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        return builds.Select(build => new EssenceOptimizerEvaluatedCandidate(
                build,
                benchmarks[build.Id],
                int.MaxValue))
            .ToArray();
    }

    private static int GreatestCommonDivisor(int first, int second)
    {
        while (second != 0)
            (first, second) = (second, first % second);
        return Math.Abs(first);
    }

    private QualityDiversityIslandResult RunQualityDiversityIsland(
        int slotCount,
        int restart,
        int searchSeed,
        IReadOnlyList<CertificationCandidate> baselineSeeds,
        IDictionary<string, CertificationCandidate> sharedCandidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        int candidateBudget) =>
        RunBehaviorIsland(
            slotCount,
            restart,
            searchSeed,
            baselineSeeds,
            sharedCandidatesBySignature,
            sourceFamilies,
            definitionsById,
            runSeed,
            candidateBudget,
            "balance-elite-quality-diversity-island-v1",
            "QUALITY_ISLAND",
            QualityDiversityNiche,
            null);

    private QualityDiversityIslandResult RunMechanicArchetypeIsland(
        int slotCount,
        int restart,
        int searchSeed,
        IReadOnlyList<CertificationCandidate> baselineSeeds,
        IDictionary<string, CertificationCandidate> sharedCandidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        int candidateBudget) =>
        RunBehaviorIsland(
            slotCount,
            restart,
            searchSeed,
            baselineSeeds,
            sharedCandidatesBySignature,
            sourceFamilies,
            definitionsById,
            runSeed,
            candidateBudget,
            "balance-elite-mechanic-archetype-island-v1",
            "MECHANIC_ARCHETYPE_ISLAND",
            candidate => MechanicArchetypeNiche(candidate, definitionsById),
            slotCount == 5
                ? CreateDescriptorSignature(
                    ExtractMechanicArchetypeFeatures(
                        DescriptorAuditAnchors.Single(anchor => anchor.Basin == "high").Genome,
                        definitionsById),
                    0)
                : null);

    private QualityDiversityIslandResult RunBehaviorIsland(
        int slotCount,
        int restart,
        int searchSeed,
        IReadOnlyList<CertificationCandidate> baselineSeeds,
        IDictionary<string, CertificationCandidate> sharedCandidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed,
        int candidateBudget,
        string seedNamespace,
        string profileSuffix,
        Func<CertificationCandidate, string> nicheSelector,
        string? telemetryTargetNiche)
    {
        if (candidateBudget == 0)
            return new QualityDiversityIslandResult(null, 0, 0, 0, 0, 0);

        const int batchSize = 32;
        var islandSeed = StableRandom.Seed(
            seedNamespace,
            searchSeed.ToString(CultureInfo.InvariantCulture),
            restart.ToString(CultureInfo.InvariantCulture),
            slotCount.ToString(CultureInfo.InvariantCulture));
        var random = new Random(islandSeed);
        var archive = new Dictionary<string, CertificationCandidate>(StringComparer.Ordinal);
        foreach (var seed in baselineSeeds.OrderBy(candidate => Signature(candidate.Build), StringComparer.Ordinal))
            UpdateQualityDiversityArchive(archive, seed, nicheSelector);
        var baselineTargetCandidates = telemetryTargetNiche is null
            ? []
            : baselineSeeds.Where(candidate => nicheSelector(candidate) == telemetryTargetNiche).ToArray();

        var seen = baselineSeeds.Select(candidate => Signature(candidate.Build)).ToHashSet(StringComparer.Ordinal);
        var islandCandidates = new List<CertificationCandidate>(candidateBudget);
        var initialCandidates = Math.Min(batchSize, candidateBudget);
        var nicheReplacements = 0;
        while (islandCandidates.Count < candidateBudget)
        {
            var requested = Math.Min(batchSize, candidateBudget - islandCandidates.Count);
            var genomes = new List<IReadOnlyList<string>>(requested);
            var attempts = 0;
            var parents = archive.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Value)
                .ToArray();
            while (genomes.Count < requested && attempts++ < requested * 500)
            {
                var genome = islandCandidates.Count == 0
                    ? CreateQualityDiversityInitialGenome(slotCount, sourceFamilies, random)
                    : CreateQualityDiversityDescendant(
                        parents[random.Next(parents.Length)],
                        sourceFamilies,
                        definitionsById,
                        random);
                if (seen.Add(Signature(genome)))
                    genomes.Add(genome);
            }
            if (genomes.Count != requested)
            {
                throw new InvalidOperationException(
                    $"Could not create {requested} unique {profileSuffix} E{slotCount} candidates for restart {restart}.");
            }

            var builds = genomes.Select((genome, index) => buildGenerator.MaterializeBuild(
                    CreateCanonicalId(slotCount, Signature(genome)),
                    $"E{slotCount}_ELITE_R{restart:00}_{profileSuffix}",
                    slotCount,
                    islandSeed,
                    genome))
                .ToArray();
            var benchmarks = benchmarkRunner.Run(builds, runSeed).Builds
                .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
            foreach (var build in builds)
            {
                var candidate = new CertificationCandidate(build, benchmarks[build.Id]);
                islandCandidates.Add(candidate);
                var signature = Signature(build);
                if (!sharedCandidatesBySignature.ContainsKey(signature))
                    sharedCandidatesBySignature[signature] = candidate;
                var niche = nicheSelector(candidate);
                if (archive.TryGetValue(niche, out var incumbent))
                {
                    if (!IsBetterQualityDiversityCandidate(candidate, incumbent))
                        continue;
                    nicheReplacements++;
                }
                archive[niche] = candidate;
            }
        }

        var best = islandCandidates.OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .First();
        var islandTargetCandidates = telemetryTargetNiche is null
            ? []
            : islandCandidates.Where(candidate => nicheSelector(candidate) == telemetryTargetNiche).ToArray();
        return new QualityDiversityIslandResult(
            best,
            islandCandidates.Count,
            initialCandidates,
            islandCandidates.Count - initialCandidates,
            archive.Count,
            nicheReplacements,
            baselineTargetCandidates.Length > 0,
            baselineTargetCandidates.Length == 0
                ? 0
                : baselineTargetCandidates.Max(candidate => candidate.Benchmark.AggregateScore),
            islandTargetCandidates.Length,
            islandTargetCandidates.Length == 0
                ? 0
                : islandTargetCandidates.Max(candidate => candidate.Benchmark.AggregateScore));
    }

    private static IReadOnlyList<string> CreateQualityDiversityInitialGenome(
        int slotCount,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        Random random)
    {
        var indexes = Enumerable.Range(0, sourceFamilies.Count).ToArray();
        for (var index = 0; index < slotCount; index++)
        {
            var selected = random.Next(index, indexes.Length);
            (indexes[index], indexes[selected]) = (indexes[selected], indexes[index]);
        }
        return indexes.Take(slotCount)
            .Select(index => sourceFamilies[index][random.Next(sourceFamilies[index].Length)].Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> CreateQualityDiversityDescendant(
        CertificationCandidate parent,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        Random random)
    {
        var genes = parent.Build.Essences.Select(essence => essence.EssenceId).ToList();
        var replacementIndex = random.Next(genes.Count);
        var occupiedSources = genes.Select(id => definitionsById[id].SourceMonsterId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        occupiedSources.Remove(definitionsById[genes[replacementIndex]].SourceMonsterId);
        var availableFamilies = sourceFamilies
            .Where(family => !occupiedSources.Contains(family[0].SourceMonsterId))
            .ToArray();
        var family = availableFamilies[random.Next(availableFamilies.Length)];
        genes[replacementIndex] = family[random.Next(family.Length)].Id;
        return genes.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void UpdateQualityDiversityArchive(
        IDictionary<string, CertificationCandidate> archive,
        CertificationCandidate candidate,
        Func<CertificationCandidate, string> nicheSelector)
    {
        var niche = nicheSelector(candidate);
        if (!archive.TryGetValue(niche, out var incumbent)
            || IsBetterQualityDiversityCandidate(candidate, incumbent))
        {
            archive[niche] = candidate;
        }
    }

    private static bool IsBetterQualityDiversityCandidate(
        CertificationCandidate candidate,
        CertificationCandidate incumbent) =>
        candidate.Benchmark.AggregateScore > incumbent.Benchmark.AggregateScore
        || (candidate.Benchmark.AggregateScore.Equals(incumbent.Benchmark.AggregateScore)
            && string.CompareOrdinal(candidate.Build.Id, incumbent.Build.Id) < 0);

    private static string QualityDiversityNiche(CertificationCandidate candidate)
    {
        var strongestScenario = candidate.Benchmark.Components
            .OrderByDescending(component => component.Score)
            .ThenBy(component => component.ScenarioId, StringComparer.Ordinal)
            .First().ScenarioId;
        var weakestScenario = candidate.Benchmark.Components
            .OrderBy(component => component.Score)
            .ThenBy(component => component.ScenarioId, StringComparer.Ordinal)
            .First().ScenarioId;
        return $"{strongestScenario}>{weakestScenario}";
    }

    private static string MechanicArchetypeNiche(
        CertificationCandidate candidate,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById) =>
        CreateDescriptorSignature(ExtractMechanicArchetypeFeatures(candidate, definitionsById), 0);

    private IReadOnlyList<CertificationCandidate> CanonicalizeAndEvaluate(
        IReadOnlyList<RestartResult> restarts,
        int runSeed)
    {
        var unique = restarts.SelectMany(restart => restart.Result.EvaluatedCandidates)
            .GroupBy(candidate => Signature(candidate.Build), StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(candidate => candidate.Build.SlotCount)
            .ThenBy(candidate => Signature(candidate.Build), StringComparer.Ordinal)
            .ToArray();
        var builds = unique.Select(candidate => CanonicalizeBuild(candidate.Build, runSeed)).ToArray();
        var benchmarks = benchmarkRunner.Run(builds, runSeed).Builds.ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        return builds.Select(build => new CertificationCandidate(build, benchmarks[build.Id])).ToArray();
    }

    private ProfileState BuildProfileState(
        int slotCount,
        IReadOnlyList<CertificationCandidate> searchCandidates,
        IReadOnlyList<RestartResult> restartResults,
        IReadOnlyList<EssenceBuildSnapshot> curatedCandidates,
        IReadOnlyDictionary<string, PveBenchmarkBuildSnapshot> curatedBenchmarks,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        EssenceMetaAnalysisSnapshot essenceMeta,
        int runSeed,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        if (searchCandidates.Count == 0)
            throw new InvalidOperationException($"Elite certification search produced no E{slotCount} candidates.");
        var bySignature = searchCandidates.ToDictionary(candidate => Signature(candidate.Build), StringComparer.Ordinal);
        var plateauRequirement = options.Profile == EliteCertificationProfile.Release
            ? policy.ReleasePlateauGenerations
            : policy.DeveloperPlateauGenerations;
        var restartEvidence = restartResults.Select(restart =>
        {
            var profile = restart.Result.Snapshot.Profiles.Single(value => value.SlotCount == slotCount);
            var signatures = restart.Result.EvaluatedCandidates.Where(candidate => candidate.Build.SlotCount == slotCount)
                .Select(candidate => Signature(candidate.Build))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var restartCandidates = signatures.Select(signature => bySignature[signature]).ToArray();
            var baselineCandidates = restartCandidates
                .Where(candidate => restart.BaselineSignatures.Contains(Signature(candidate.Build)))
                .ToArray();
            var rawBest = restartCandidates
                .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            var baselineRefinementSeeds = SelectRestartRefinementSeeds(
                baselineCandidates,
                Math.Min(options.RestartRefinementSeedCount, baselineCandidates.Length),
                options.DiversityPenalty);
            var baselineRefinements = baselineRefinementSeeds.Select(seed => RefineRestartWinner(
                    seed,
                    bySignature,
                    sourceFamilies,
                    definitionsById,
                    essenceMeta,
                    runSeed,
                    policy,
                    options))
                .ToArray();
            var portfolioCandidates = restartCandidates
                .Where(candidate => !restart.BaselineSignatures.Contains(Signature(candidate.Build)))
                .ToArray();
            var portfolioRefinementSeeds = portfolioCandidates.Length == 0
                ? []
                : SelectRestartRefinementSeeds(
                    portfolioCandidates,
                    Math.Min(options.RestartRefinementSeedCount, portfolioCandidates.Length),
                    options.DiversityPenalty);
            var portfolioRefinements = portfolioRefinementSeeds.Select(seed => RefineRestartWinner(
                    seed,
                    bySignature,
                    sourceFamilies,
                    definitionsById,
                    essenceMeta,
                    runSeed,
                    policy,
                    options))
                .ToArray();
            var refinements = baselineRefinements.Concat(portfolioRefinements).ToArray();
            var baselineBest = baselineRefinements
                .OrderByDescending(value => value.Best.Benchmark.AggregateScore)
                .ThenBy(value => value.Best.Build.Id, StringComparer.Ordinal)
                .First()
                .Best;
            var refinement = refinements
                .OrderByDescending(value => value.Best.Benchmark.AggregateScore)
                .ThenBy(value => value.Best.Build.Id, StringComparer.Ordinal)
                .First();
            var oneSwapCandidatesEvaluated = refinements.Sum(value => value.OneSwapCandidatesEvaluated);
            var twoSwapCandidatesEvaluated = refinements.Sum(value => value.TwoSwapCandidatesEvaluated);
            var acceptedPasses = refinement.AcceptedPasses;
            var valley = RunValleyBeam(
                refinement.Best,
                bySignature,
                sourceFamilies,
                definitionsById,
                essenceMeta,
                runSeed,
                options);
            var refinedBest = valley.Best.Benchmark.AggregateScore > refinement.Best.Benchmark.AggregateScore
                ? valley.Best
                : refinement.Best;
            if (valley.Best.Benchmark.AggregateScore - refinement.Best.Benchmark.AggregateScore
                > policy.PlateauImprovementTolerance)
            {
                var polished = RefineRestartWinner(
                    valley.Best,
                    bySignature,
                    sourceFamilies,
                    definitionsById,
                    essenceMeta,
                    runSeed,
                    policy,
                    options);
                if (polished.Best.Benchmark.AggregateScore > refinedBest.Benchmark.AggregateScore)
                    refinedBest = polished.Best;
                acceptedPasses += polished.AcceptedPasses;
                oneSwapCandidatesEvaluated += polished.OneSwapCandidatesEvaluated;
                twoSwapCandidatesEvaluated += polished.TwoSwapCandidatesEvaluated;
            }
            var islandSeeds = baselineCandidates
                .Concat(baselineRefinementSeeds)
                .Concat(baselineRefinements.Select(value => value.Best))
                .DistinctBy(candidate => Signature(candidate.Build))
                .ToArray();
            var qualityIsland = RunQualityDiversityIsland(
                slotCount,
                restart.Restart,
                restart.SearchSeed,
                islandSeeds,
                bySignature,
                sourceFamilies,
                definitionsById,
                runSeed,
                options.QualityDiversityIslandCandidateBudgetPerProfile);
            var mechanicIsland = RunMechanicArchetypeIsland(
                slotCount,
                restart.Restart,
                restart.SearchSeed,
                islandSeeds,
                bySignature,
                sourceFamilies,
                definitionsById,
                runSeed,
                options.MechanicArchetypeIslandCandidateBudgetPerProfile);
            var islandBest = qualityIsland.Best;
            if (mechanicIsland.Best is not null
                && (islandBest is null
                    || mechanicIsland.Best.Benchmark.AggregateScore > islandBest.Benchmark.AggregateScore))
            {
                islandBest = mechanicIsland.Best;
            }
            var islandPolished = false;
            if (islandBest is not null
                && islandBest.Benchmark.AggregateScore > refinedBest.Benchmark.AggregateScore)
            {
                var polished = RefineRestartWinner(
                    islandBest,
                    bySignature,
                    sourceFamilies,
                    definitionsById,
                    essenceMeta,
                    runSeed,
                    policy,
                    options);
                if (polished.Best.Benchmark.AggregateScore > refinedBest.Benchmark.AggregateScore)
                    refinedBest = polished.Best;
                acceptedPasses += polished.AcceptedPasses;
                oneSwapCandidatesEvaluated += polished.OneSwapCandidatesEvaluated;
                twoSwapCandidatesEvaluated += polished.TwoSwapCandidatesEvaluated;
                islandPolished = true;
            }
            var newCandidatesEvaluated = oneSwapCandidatesEvaluated
                                         + twoSwapCandidatesEvaluated
                                         + valley.NewCandidatesEvaluated
                                         + qualityIsland.CandidatesEvaluated
                                         + mechanicIsland.CandidatesEvaluated;
            var generationsSinceImprovement = GenerationsSinceMaterialImprovement(
                profile.Generations,
                policy.PlateauImprovementTolerance);
            return new EliteCertificationRestartSnapshot(
                restart.Restart,
                restart.SearchSeed,
                rawBest.Benchmark.AggregateScore,
                refinedBest.Benchmark.AggregateScore,
                profile.Generations[^1].Generation,
                generationsSinceImprovement,
                generationsSinceImprovement >= plateauRequirement,
                signatures.Length + newCandidatesEvaluated,
                acceptedPasses,
                oneSwapCandidatesEvaluated + twoSwapCandidatesEvaluated,
                oneSwapCandidatesEvaluated,
                twoSwapCandidatesEvaluated,
                baselineRefinementSeeds.Count + portfolioRefinementSeeds.Count + (islandPolished ? 1 : 0),
                rawBest.Build.Id,
                refinedBest.Build.Id,
                rawBest.Build.Essences.Select(value => value.EssenceId).ToArray(),
                refinedBest.Build.Essences.Select(value => value.EssenceId).ToArray(),
                0,
                valley.DepthReached,
                valley.CandidatesEvaluated,
                valley.BudgetExhausted,
                valley.BestImprovement,
                valley.CandidatesGenerated,
                valley.CandidatesRejectedByPrefilter,
                profile.Generations.Sum(generation => generation.CoordinatedMutationBirths),
                profile.Generations.Sum(generation => generation.ExplorerContinuationBirths),
                baselineBest.Benchmark.AggregateScore,
                baselineBest.Build.Id,
                baselineBest.Build.Essences.Select(value => value.EssenceId).ToArray(),
                restart.PortfolioCandidatesEvaluated,
                qualityIsland.CandidatesEvaluated,
                qualityIsland.InitialCandidatesEvaluated,
                qualityIsland.DescendantsEvaluated,
                qualityIsland.NichesOccupied,
                qualityIsland.NicheReplacements,
                qualityIsland.Best?.Benchmark.AggregateScore ?? 0,
                qualityIsland.Best?.Build.Id,
                qualityIsland.Best?.Build.Essences.Select(value => value.EssenceId).ToArray(),
                mechanicIsland.CandidatesEvaluated,
                mechanicIsland.InitialCandidatesEvaluated,
                mechanicIsland.DescendantsEvaluated,
                mechanicIsland.NichesOccupied,
                mechanicIsland.NicheReplacements,
                mechanicIsland.Best?.Benchmark.AggregateScore ?? 0,
                mechanicIsland.Best?.Build.Id,
                mechanicIsland.Best?.Build.Essences.Select(value => value.EssenceId).ToArray(),
                mechanicIsland.TelemetryTargetNichePresentInBaseline,
                mechanicIsland.TelemetryTargetNicheBaselineBestScore,
                mechanicIsland.TelemetryTargetNicheIslandCandidatesEvaluated,
                mechanicIsland.TelemetryTargetNicheIslandBestScore);
        }).ToArray();
        var strongestRestart = restartEvidence
            .OrderByDescending(value => value.BestScore)
            .ThenBy(value => value.Restart)
            .First();
        var strongestGenome = strongestRestart.BestEssenceIds ?? [];
        restartEvidence = restartEvidence.Select(value => value with
            {
                DistanceFromStrongestRestart = GenomeDistance(value.BestEssenceIds ?? [], strongestGenome)
            })
            .ToArray();
        var bestScoreSpread = Round(restartEvidence.Max(value => value.BestScore) - restartEvidence.Min(value => value.BestScore));
        var agreementPassed = bestScoreSpread <= policy.RestartBestScoreSpreadTolerance;
        var plateauPassed = restartEvidence.All(value => value.PlateauPassed);
        IReadOnlyList<CertificationCandidate> finalists = [];
        EliteLocalChallengeSnapshot? challenge = null;
        var cumulativeOneSwapChallenges = 0;
        var cumulativeTwoSwapChallenges = 0;
        for (var refinementRound = 0; refinementRound <= options.FinalistRefinementRoundLimit; refinementRound++)
        {
            var refinementPopulation = bySignature.Values.ToArray();
            var pareto = FindParetoFrontier(refinementPopulation);
            finalists = SelectFinalists(
                pareto,
                refinementPopulation,
                options.FinalistsPerSlotProfile,
                options.DiversityPenalty);
            var roundChallenge = RunLocalChallenge(
                finalists,
                bySignature,
                sourceFamilies,
                definitionsById,
                essenceMeta,
                runSeed,
                options,
                policy);
            cumulativeOneSwapChallenges += roundChallenge.OneSwapChallengersEvaluated;
            cumulativeTwoSwapChallenges += roundChallenge.TwoSwapChallengersEvaluated;
            challenge = roundChallenge with
            {
                RefinementRounds = refinementRound,
                OneSwapChallengersEvaluated = cumulativeOneSwapChallenges,
                TwoSwapChallengersEvaluated = cumulativeTwoSwapChallenges
            };
            var materialImprovement = challenge.BestAggregateImprovement > policy.GenericLocalImprovementTolerance
                                      || challenge.BestScenarioImprovement > policy.GenericLocalImprovementTolerance;
            if (!materialImprovement)
                break;
        }
        if (challenge is null)
            throw new InvalidOperationException($"Elite E{slotCount} finalist refinement did not execute.");
        var scenarioIds = searchCandidates[0].Benchmark.Components.Select(component => component.ScenarioId).ToArray();
        var scenarioCoveragePassed = scenarioIds.All(scenarioId =>
            finalists.Any(finalist => finalist.Benchmark.Components.Any(component => component.ScenarioId == scenarioId)));
        var allCandidates = bySignature.Values.OrderBy(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        var p95Score = Percentile(allCandidates.Select(candidate => candidate.Benchmark.AggregateScore).ToArray(), 0.95);
        var p99Score = Percentile(allCandidates.Select(candidate => candidate.Benchmark.AggregateScore).ToArray(), 0.99);
        var percentileById = allCandidates.Select((candidate, index) => new
            {
                candidate.Build.Id,
                Percentile = allCandidates.Length == 1 ? 100d : index * 100d / (allCandidates.Length - 1)
            })
            .ToDictionary(value => value.Id, value => value.Percentile, StringComparer.Ordinal);
        var p95 = allCandidates.OrderBy(candidate => Math.Abs(candidate.Benchmark.AggregateScore - p95Score))
            .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
            .First();
        var p99 = allCandidates.OrderBy(candidate => Math.Abs(candidate.Benchmark.AggregateScore - p99Score))
            .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
            .First();
        var searchBest = searchCandidates.Max(candidate => candidate.Benchmark.AggregateScore);
        var refinedBest = allCandidates.Max(candidate => candidate.Benchmark.AggregateScore);
        var crossStrategyPassed = refinedBest - searchBest <= policy.CrossStrategyScoreTolerance;
        var curated = curatedCandidates.Where(build => build.SlotCount == slotCount).ToArray();
        var bestCurated = curated.Length == 0 ? (double?)null : curated.Max(build => curatedBenchmarks[build.Id].AggregateScore);
        double? bestAdvantage = bestCurated.HasValue ? Round(bestCurated.Value - refinedBest) : null;
        var curatedRequirement = curated.Length >= policy.MinimumCuratedBuildsPerSlotProfile;
        var humanOutperformed = bestAdvantage > policy.HumanBenchmarkAdvantageTolerance;
        var curatedComparison = new CuratedBuildComparisonSnapshot(
            curated.Length,
            bestCurated,
            refinedBest,
            bestAdvantage,
            curatedRequirement,
            humanOutperformed);
        var warnings = new List<string>();
        if (!agreementPassed)
            warnings.Add($"E{slotCount} independent search spread {bestScoreSpread:F2} exceeds {policy.RestartBestScoreSpreadTolerance:F2}.");
        if (!plateauPassed)
            warnings.Add($"E{slotCount} did not satisfy the configured search plateau.");
        if (!crossStrategyPassed)
            warnings.Add($"E{slotCount} local refinement disagrees materially with restart search.");
        if (challenge.BestAggregateImprovement > policy.GenericLocalImprovementTolerance
            || challenge.BestScenarioImprovement > policy.GenericLocalImprovementTolerance)
        {
            warnings.Add($"E{slotCount} has a material local-neighbor improvement.");
        }
        if (!scenarioCoveragePassed)
            warnings.Add($"E{slotCount} finalists do not cover every benchmark axis.");
        if (!challenge.CompleteForConfiguredDepth)
            warnings.Add($"E{slotCount} did not complete the required local-neighborhood challenge.");
        if (!curatedRequirement)
            warnings.Add($"E{slotCount} has {curated.Length} curated builds; {policy.MinimumCuratedBuildsPerSlotProfile} are required.");
        if (humanOutperformed)
            warnings.Add($"An E{slotCount} curated build exceeds the automated ceiling beyond tolerance.");
        if (options.Profile == EliteCertificationProfile.Developer)
            warnings.Add($"E{slotCount} used the non-certifying developer profile.");
        var verdict = ResolveProfileVerdict(
            options.Profile,
            agreementPassed && plateauPassed && crossStrategyPassed && challenge.CompleteForConfiguredDepth,
            challenge,
            scenarioCoveragePassed,
            curatedRequirement,
            humanOutperformed,
            policy);
        var snapshot = new EliteCertificationProfileSnapshot(
            $"E{slotCount}_ELITE",
            slotCount,
            LegalCombinationCount(sourceFamilies, slotCount),
            allCandidates.Length,
            Round(p95Score),
            Round(p99Score),
            Round(refinedBest),
            bestScoreSpread,
            agreementPassed,
            plateauPassed,
            crossStrategyPassed,
            scenarioCoveragePassed,
            ToSnapshot(p95, percentileById[p95.Build.Id]),
            ToSnapshot(p99, percentileById[p99.Build.Id]),
            finalists.Select(candidate => ToSnapshot(
                    candidate,
                    percentileById.GetValueOrDefault(candidate.Build.Id, 100)))
                .ToArray(),
            restartEvidence,
            challenge,
            curatedComparison,
            verdict,
            warnings);
        return new ProfileState(slotCount, snapshot, allCandidates, finalists);
    }

    private RestartRefinementResult RefineRestartWinner(
        CertificationCandidate initial,
        IDictionary<string, CertificationCandidate> candidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        EssenceMetaAnalysisSnapshot essenceMeta,
        int runSeed,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        var current = initial;
        var acceptedPasses = 0;
        var oneSwapCandidatesEvaluated = 0;
        var twoSwapCandidatesEvaluated = 0;
        for (var pass = 1; pass <= options.RestartLocalRefinementPassLimit; pass++)
        {
            var oneSwapGenomes = EnumerateOneSwapGenomes(current.Build, sourceFamilies, definitionsById)
                .DistinctBy(Signature)
                .ToArray();
            oneSwapCandidatesEvaluated += EvaluateMissingCandidates(
                oneSwapGenomes,
                current.Build.SlotCount,
                "RESTART_ONE_SWAP",
                candidatesBySignature,
                runSeed);
            var bestNeighbor = oneSwapGenomes.Select(genome => candidatesBySignature[Signature(genome)])
                .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            if (bestNeighbor.Benchmark.AggregateScore - current.Benchmark.AggregateScore
                > policy.PlateauImprovementTolerance)
            {
                current = bestNeighbor;
                acceptedPasses++;
                continue;
            }

            if (options.RestartTwoSwapChallengerLimitPerPass == 0)
                break;
            var twoSwapGenomes = EnumerateTwoSwapGenomes(
                    current.Build,
                    sourceFamilies,
                    definitionsById,
                    essenceMeta)
                .Take(options.RestartTwoSwapChallengerLimitPerPass)
                .DistinctBy(Signature)
                .ToArray();
            if (twoSwapGenomes.Length == 0)
                break;
            twoSwapCandidatesEvaluated += EvaluateMissingCandidates(
                twoSwapGenomes,
                current.Build.SlotCount,
                "RESTART_TWO_SWAP",
                candidatesBySignature,
                runSeed);
            var bestTwoSwapNeighbor = twoSwapGenomes.Select(genome => candidatesBySignature[Signature(genome)])
                .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            if (bestTwoSwapNeighbor.Benchmark.AggregateScore - current.Benchmark.AggregateScore
                <= policy.PlateauImprovementTolerance)
            {
                break;
            }
            current = bestTwoSwapNeighbor;
            acceptedPasses++;
        }
        return new RestartRefinementResult(
            current,
            acceptedPasses,
            oneSwapCandidatesEvaluated + twoSwapCandidatesEvaluated,
            oneSwapCandidatesEvaluated,
            twoSwapCandidatesEvaluated);
    }

    private ValleyBeamResult RunValleyBeam(
        CertificationCandidate initial,
        IDictionary<string, CertificationCandidate> candidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        EssenceMetaAnalysisSnapshot essenceMeta,
        int runSeed,
        EliteCertificationOptions options)
    {
        if (options.RestartValleyCandidateBudget == 0)
            return new ValleyBeamResult(initial, 0, 0, 0, false, 0, 0, 0);

        var best = initial;
        IReadOnlyList<CertificationCandidate> beam = [initial];
        var visited = new HashSet<string>(StringComparer.Ordinal) { Signature(initial.Build) };
        var essenceScores = essenceMeta.Essences.ToDictionary(
            value => value.EssenceId,
            value => (value.PerformanceDelta ?? 0) + value.P99Usage * 10 + (value.AdminAdjustedScoreDelta ?? 0),
            StringComparer.OrdinalIgnoreCase);
        var pairScores = essenceMeta.PairSynergies.ToDictionary(
            value => Signature([value.FirstEssenceId, value.SecondEssenceId]),
            value => value.SynergyDelta,
            StringComparer.Ordinal);
        var candidatesEvaluated = 0;
        var newCandidatesEvaluated = 0;
        var candidatesGenerated = 0;
        var candidatesRejectedByPrefilter = 0;
        var depthReached = 0;
        var budgetExhausted = false;

        for (var depth = 1; depth <= options.RestartValleyBeamDepth; depth++)
        {
            var layerGenomes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var parent in beam.OrderByDescending(value => value.Benchmark.AggregateScore)
                         .ThenBy(value => value.Build.Id, StringComparer.Ordinal))
            {
                foreach (var genome in EnumerateOneSwapGenomes(parent.Build, sourceFamilies, definitionsById))
                {
                    var signature = Signature(genome);
                    if (!visited.Add(signature))
                        continue;
                    layerGenomes.Add(signature, genome);
                    candidatesGenerated++;
                }
            }
            if (layerGenomes.Count == 0)
                break;

            var rankedGenomes = layerGenomes.Values
                .Select(genome => new
                {
                    Genome = genome,
                    Signature = Signature(genome),
                    Score = ValleyPrefilterScore(genome, essenceScores, pairScores)
                })
                .OrderByDescending(value => value.Score)
                .ThenBy(value => value.Signature, StringComparer.Ordinal)
                .ToArray();
            var prefilterLimit = options.RestartValleyPrefilterLimitPerDepth == 0
                ? rankedGenomes.Length
                : Math.Min(options.RestartValleyPrefilterLimitPerDepth, rankedGenomes.Length);
            candidatesRejectedByPrefilter += rankedGenomes.Length - prefilterLimit;
            var remainingBudget = options.RestartValleyCandidateBudget - candidatesEvaluated;
            var selectedCount = Math.Min(prefilterLimit, remainingBudget);
            if (selectedCount < prefilterLimit)
                budgetExhausted = true;
            if (selectedCount == 0)
                break;
            var selected = rankedGenomes.Take(selectedCount).ToArray();
            var genomes = selected.Select(value => value.Genome).ToArray();
            candidatesEvaluated += genomes.Length;
            newCandidatesEvaluated += EvaluateMissingCandidates(
                genomes,
                initial.Build.SlotCount,
                $"RESTART_VALLEY_D{depth}",
                candidatesBySignature,
                runSeed);
            var layer = selected.Select(value => candidatesBySignature[value.Signature]).ToArray();
            var layerBest = layer.OrderByDescending(value => value.Benchmark.AggregateScore)
                .ThenBy(value => value.Build.Id, StringComparer.Ordinal)
                .First();
            if (layerBest.Benchmark.AggregateScore > best.Benchmark.AggregateScore)
                best = layerBest;
            beam = SelectRestartRefinementSeeds(
                layer,
                Math.Min(options.RestartValleyBeamWidth, layer.Length),
                options.DiversityPenalty);
            depthReached = depth;
            if (budgetExhausted)
                break;
        }

        return new ValleyBeamResult(
            best,
            depthReached,
            candidatesEvaluated,
            newCandidatesEvaluated,
            budgetExhausted,
            Round(best.Benchmark.AggregateScore - initial.Benchmark.AggregateScore),
            candidatesGenerated,
            candidatesRejectedByPrefilter);
    }

    private int EvaluateMissingCandidates(
        IReadOnlyList<IReadOnlyList<string>> genomes,
        int slotCount,
        string sourceSuffix,
        IDictionary<string, CertificationCandidate> candidatesBySignature,
        int runSeed)
    {
        var missing = genomes.Where(genome => !candidatesBySignature.ContainsKey(Signature(genome)))
            .Select(genome => buildGenerator.MaterializeBuild(
                CreateCanonicalId(slotCount, Signature(genome)),
                $"E{slotCount}_ELITE_{sourceSuffix}",
                slotCount,
                runSeed,
                genome))
            .ToArray();
        if (missing.Length == 0)
            return 0;
        var benchmarks = benchmarkRunner.Run(missing, runSeed).Builds
            .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
        foreach (var build in missing)
            candidatesBySignature[Signature(build)] = new CertificationCandidate(build, benchmarks[build.Id]);
        return missing.Length;
    }

    private EliteLocalChallengeSnapshot RunLocalChallenge(
        IReadOnlyList<CertificationCandidate> finalists,
        IDictionary<string, CertificationCandidate> candidatesBySignature,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        EssenceMetaAnalysisSnapshot essenceMeta,
        int runSeed,
        EliteCertificationOptions options,
        EliteCertificationPolicy policy)
    {
        var links = new List<ChallengeLink>();
        var buildsToEvaluate = new Dictionary<string, EssenceBuildSnapshot>(StringComparer.Ordinal);
        foreach (var finalist in finalists)
        {
            var parentSignature = Signature(finalist.Build);
            foreach (var genome in EnumerateOneSwapGenomes(finalist.Build, sourceFamilies, definitionsById))
                AddChallenge(parentSignature, genome, 1, finalist.Build.SlotCount, runSeed, candidatesBySignature, links, buildsToEvaluate);
            if (options.LocalSwapDepth < 2)
                continue;
            var twoSwap = EnumerateTwoSwapGenomes(finalist.Build, sourceFamilies, definitionsById, essenceMeta);
            if (options.TwoSwapChallengerLimitPerFinalist > 0)
                twoSwap = twoSwap.Take(options.TwoSwapChallengerLimitPerFinalist);
            foreach (var genome in twoSwap)
                AddChallenge(parentSignature, genome, 2, finalist.Build.SlotCount, runSeed, candidatesBySignature, links, buildsToEvaluate);
        }
        if (buildsToEvaluate.Count > 0)
        {
            var benchmarks = benchmarkRunner.Run(buildsToEvaluate.Values.ToArray(), runSeed).Builds
                .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
            foreach (var build in buildsToEvaluate.Values)
                candidatesBySignature[Signature(build)] = new CertificationCandidate(build, benchmarks[build.Id]);
        }
        var finalistsBySignature = finalists.ToDictionary(candidate => Signature(candidate.Build), StringComparer.Ordinal);
        var bestFinalistAggregate = finalists.Max(candidate => candidate.Benchmark.AggregateScore);
        var scenarioLeaders = finalists[0].Benchmark.Components.ToDictionary(
            component => component.ScenarioId,
            component => finalists.Max(candidate => candidate.Benchmark.Components
                .Single(value => value.ScenarioId == component.ScenarioId).Score),
            StringComparer.Ordinal);
        var bestAggregate = 0d;
        var bestScenario = 0d;
        string? bestAggregateParentId = null;
        string? bestAggregateChallengerId = null;
        string? bestScenarioParentId = null;
        string? bestScenarioChallengerId = null;
        foreach (var link in links)
        {
            var parent = finalistsBySignature[link.ParentSignature];
            var challenger = candidatesBySignature[link.ChallengerSignature];
            var parentComponents = parent.Benchmark.Components.ToDictionary(component => component.ScenarioId, StringComparer.Ordinal);
            var challengerComponents = challenger.Benchmark.Components.ToDictionary(component => component.ScenarioId, StringComparer.Ordinal);
            var aggregate = Math.Abs(parent.Benchmark.AggregateScore - bestFinalistAggregate) < 0.0001
                ? challenger.Benchmark.AggregateScore - parent.Benchmark.AggregateScore
                : 0;
            var scenario = parentComponents.Keys
                .Where(scenarioId => Math.Abs(parentComponents[scenarioId].Score - scenarioLeaders[scenarioId]) < 0.0001)
                .Where(scenarioId => EliteCertificationSearchRules.IsScenarioImprovement(
                    parentComponents.ToDictionary(value => value.Key, value => value.Value.Score, StringComparer.Ordinal),
                    challengerComponents.ToDictionary(value => value.Key, value => value.Value.Score, StringComparer.Ordinal),
                    scenarioId,
                    policy.GenericLocalImprovementTolerance))
                .Select(scenarioId => challengerComponents[scenarioId].Score - parentComponents[scenarioId].Score)
                .DefaultIfEmpty(0)
                .Max();
            if (aggregate > bestAggregate)
            {
                bestAggregate = aggregate;
                bestAggregateParentId = parent.Build.Id;
                bestAggregateChallengerId = challenger.Build.Id;
            }
            if (scenario > bestScenario)
            {
                bestScenario = scenario;
                bestScenarioParentId = parent.Build.Id;
                bestScenarioChallengerId = challenger.Build.Id;
            }
        }
        var complete = options.LocalSwapDepth == 1
                       || options.Profile == EliteCertificationProfile.Developer
                       || options.TwoSwapChallengerLimitPerFinalist == 0;
        return new EliteLocalChallengeSnapshot(
            finalists.Count,
            0,
            links.Count(link => link.Depth == 1),
            links.Count(link => link.Depth == 2),
            complete,
            Round(bestAggregate),
            Round(bestScenario),
            bestAggregate > policy.GenericLocalImprovementTolerance ? bestAggregateParentId : bestScenarioParentId,
            bestAggregate > policy.GenericLocalImprovementTolerance ? bestAggregateChallengerId : bestScenarioChallengerId);
    }

    private EliteCertificationFloorSnapshot CertifyFloor(
        WorldTowerFloorAnalysisSnapshot floor,
        EncounterCalibrationFloorSnapshot calibration,
        RepresentativeEssenceProfileSnapshot genericProfile,
        ProfileState profile,
        TopPlayerFixtureDocument fixtures,
        IReadOnlyList<EssenceBuildSnapshot> curatedBuilds,
        int runSeed,
        int maxTicks,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        var p95Builds = SelectCohort(profile.Candidates, profile.Snapshot.P95TargetScore, options.FinalistsPerSlotProfile);
        var p99Builds = SelectCohort(profile.Candidates, profile.Snapshot.P99TargetScore, options.FinalistsPerSlotProfile);
        var genericBuilds = genericProfile.Builds.Select(build => buildGenerator.MaterializeBuild(
                $"ELITE_GENERIC_F{floor.Floor}_{build.Id}",
                genericProfile.Id,
                genericProfile.SlotCount,
                runSeed,
                build.Essences.Select(essence => essence.EssenceId).ToArray()))
            .ToArray();
        var generic = EvaluateHoldout(floor.Floor, genericBuilds, calibration, runSeed, maxTicks, options);
        var p95 = EvaluateHoldout(floor.Floor, p95Builds, calibration, runSeed, maxTicks, options);
        var p99 = EvaluateHoldout(floor.Floor, p99Builds, calibration, runSeed, maxTicks, options);
        var party = OptimizeParty(
            floor,
            calibration,
            profile.Finalists,
            runSeed,
            maxTicks,
            options);
        var specialized = EvaluateHoldout(floor.Floor, party.Builds, calibration, runSeed, maxTicks, options);
        var curatedPartyFixtures = fixtures.Parties.Where(value => value.EncounterFloor == floor.Floor).ToArray();
        EliteHoldoutSnapshot? bestCurated = null;
        foreach (var fixture in curatedPartyFixtures)
        {
            var builds = fixture.BuildIds.Select(buildId => curatedBuilds.SingleOrDefault(build => build.Id == buildId)
                    ?? throw new InvalidOperationException($"Curated party '{fixture.Id}' references missing build '{buildId}'."))
                .ToArray();
            if (builds.Length != floor.RequiredSlots)
            {
                throw new InvalidOperationException(
                    $"Curated party '{fixture.Id}' has {builds.Length} members; Floor {floor.Floor} requires {floor.RequiredSlots}.");
            }
            var evaluation = EvaluateHoldout(floor.Floor, builds, calibration, runSeed, maxTicks, options);
            if (bestCurated is null || CompareHoldout(evaluation, bestCurated) > 0)
                bestCurated = evaluation;
        }
        var p95Expectation = p95.ConfidenceLowerBound >= policy.P95MinimumConfidenceLowerBound;
        var p99Expectation = p99.ConfidenceLowerBound >= policy.P99MinimumConfidenceLowerBound;
        var precisionPassed = new[] { p95, p99, specialized }
            .All(value => value.ConfidenceIntervalWidth <= policy.HoldoutMaximumIntervalWidth);
        var curatedRequirement = curatedPartyFixtures.Length >= policy.MinimumCuratedPartiesPerEncounter;
        var humanOutperformed = bestCurated is not null
                                && (bestCurated.ClearRate - specialized.ClearRate > policy.HumanClearRateAdvantageTolerance
                                    || (bestCurated.ClearRate + policy.HumanClearRateAdvantageTolerance >= specialized.ClearRate
                                        && bestCurated.MedianDurationTicks > 0
                                        && specialized.MedianDurationTicks > 0
                                        && bestCurated.MedianDurationTicks
                                        < specialized.MedianDurationTicks * (1 - policy.HumanKillTimeAdvantageTolerance)));
        var warnings = new List<string>();
        if (!party.Complete)
        {
            warnings.Add(
                $"Floor {floor.Floor} party search evaluated {party.EvaluatedGenomes:N0} of its " +
                $"{party.TargetGenomes:N0}-genome target ({party.SearchSpaceSize:N0} legal unique genomes)." );
        }
        if (!p95Expectation)
            warnings.Add($"Floor {floor.Floor} P95 holdout lower bound is below {policy.P95MinimumConfidenceLowerBound:P0}.");
        if (!p99Expectation)
            warnings.Add($"Floor {floor.Floor} P99 holdout lower bound is below {policy.P99MinimumConfidenceLowerBound:P0}.");
        if (!precisionPassed)
            warnings.Add($"Floor {floor.Floor} elite holdout precision does not satisfy the release policy.");
        AddKillTimeWarning(warnings, floor.Floor, "P95", p95, generic, policy.P95KillTimeRatioWarning, policy.MechanicBypassKillTimeRatio);
        AddKillTimeWarning(warnings, floor.Floor, "P99", p99, generic, policy.P99KillTimeRatioWarning, policy.MechanicBypassKillTimeRatio);
        AddKillTimeWarning(warnings, floor.Floor, "specialized", specialized, generic, policy.SpecializedKillTimeRatioWarning, policy.MechanicBypassKillTimeRatio);
        if (!curatedRequirement)
            warnings.Add($"Floor {floor.Floor} has {curatedPartyFixtures.Length} curated parties; {policy.MinimumCuratedPartiesPerEncounter} are required.");
        if (humanOutperformed)
            warnings.Add($"A curated Floor {floor.Floor} party exceeds the automated party ceiling beyond tolerance.");
        if (options.Profile == EliteCertificationProfile.Developer)
            warnings.Add($"Floor {floor.Floor} used the non-certifying developer profile.");
        var verdict = ResolveFloorVerdict(
            options.Profile,
            party.Complete,
            p95Expectation && p99Expectation && precisionPassed,
            curatedRequirement,
            humanOutperformed);
        return new EliteCertificationFloorSnapshot(
            floor.Floor,
            floor.EncounterName,
            genericProfile.Id,
            genericProfile.SlotCount,
            party.EvaluatedGenomes,
            party.SearchSpaceSize,
            party.Complete,
            generic,
            p95,
            p99,
            specialized,
            bestCurated,
            p95Expectation,
            p99Expectation,
            precisionPassed,
            curatedRequirement,
            humanOutperformed,
            verdict,
            p95Builds.Select(build => build.Id).ToArray(),
            p99Builds.Select(build => build.Id).ToArray(),
            party.Builds.Select(build => build.Id).ToArray(),
            warnings)
        {
            P95CohortBuilds = p95Builds.Select(build =>
                new EliteCalibrationBuildSnapshot(
                    build.Id,
                    build.Essences.Select(essence => essence.EssenceId).ToArray())).ToArray()
        };
    }

    private PartySearchResult OptimizeParty(
        WorldTowerFloorAnalysisSnapshot floor,
        EncounterCalibrationFloorSnapshot calibration,
        IReadOnlyList<CertificationCandidate> finalists,
        int runSeed,
        int maxTicks,
        EliteCertificationOptions options)
    {
        var pool = finalists.OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();
        if (pool.Length == 0)
            throw new InvalidOperationException($"Elite party search has no candidates for Floor {floor.Floor}.");
        var random = new Random(StableRandom.Seed(
            "balance-elite-party-search-v1",
            runSeed.ToString(CultureInfo.InvariantCulture),
            floor.Floor.ToString(CultureInfo.InvariantCulture)));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var evaluated = new List<(EssenceBuildSnapshot[] Builds, EncounterCalibrationEvaluation Evaluation)>();
        var searchSpaceSize = EliteCertificationSearchRules.CountPartyGenomes(pool.Length, floor.RequiredSlots);
        IEnumerable<EssenceBuildSnapshot[]> genomes;
        if (searchSpaceSize <= options.PartyGenomeBudgetPerFloor)
        {
            genomes = EnumeratePartyGenomes(pool, floor.RequiredSlots);
        }
        else
        {
            var sampled = new List<EssenceBuildSnapshot[]>(options.PartyGenomeBudgetPerFloor);
            var attempts = 0;
            while (sampled.Count < options.PartyGenomeBudgetPerFloor
                   && attempts++ < Math.Max(100, options.PartyGenomeBudgetPerFloor * 20))
            {
                var builds = attempts == 1
                    ? Enumerable.Range(0, floor.RequiredSlots).Select(index => pool[index % pool.Length].Build).ToArray()
                    : Enumerable.Range(0, floor.RequiredSlots).Select(_ => pool[random.Next(pool.Length)].Build).ToArray();
                var signature = string.Join('|', builds.Select(build => build.Id).OrderBy(id => id, StringComparer.Ordinal));
                if (seen.Add(signature))
                    sampled.Add(builds);
            }
            genomes = sampled;
        }
        seen.Clear();
        foreach (var builds in genomes)
        {
            var signature = string.Join('|', builds.Select(build => build.Id).OrderBy(id => id, StringComparer.Ordinal));
            if (!seen.Add(signature))
                continue;
            var evaluation = encounterEvaluator.EvaluateBuilds(new EncounterBuildEvaluationRequest(
                floor.Floor,
                builds,
                StableRandom.Seed("balance-elite-party-candidate-v1", runSeed.ToString(), floor.Floor.ToString(), evaluated.Count.ToString()),
                1,
                maxTicks,
                calibration.HealthAdjustmentFactor,
                calibration.DamageAdjustmentFactor));
            evaluated.Add((builds, evaluation));
        }
        var best = evaluated.OrderByDescending(value => PartyScore(value.Evaluation, maxTicks))
            .ThenBy(value => string.Join('|', value.Builds.Select(build => build.Id)), StringComparer.Ordinal)
            .First();
        return new PartySearchResult(
            best.Builds,
            evaluated.Count,
            searchSpaceSize,
            Math.Min(searchSpaceSize, options.PartyGenomeBudgetPerFloor),
            evaluated.Count == Math.Min(searchSpaceSize, options.PartyGenomeBudgetPerFloor));
    }

    private static IEnumerable<EssenceBuildSnapshot[]> EnumeratePartyGenomes(
        IReadOnlyList<CertificationCandidate> pool,
        int requiredSlots)
    {
        var indexes = new int[requiredSlots];
        return Enumerate(0, 0);

        IEnumerable<EssenceBuildSnapshot[]> Enumerate(int position, int minimumIndex)
        {
            if (position == requiredSlots)
            {
                yield return indexes.Select(index => pool[index].Build).ToArray();
                yield break;
            }
            for (var index = minimumIndex; index < pool.Count; index++)
            {
                indexes[position] = index;
                foreach (var genome in Enumerate(position + 1, index))
                    yield return genome;
            }
        }
    }

    private EliteHoldoutSnapshot EvaluateHoldout(
        int floor,
        IReadOnlyList<EssenceBuildSnapshot> builds,
        EncounterCalibrationFloorSnapshot calibration,
        int runSeed,
        int maxTicks,
        EliteCertificationOptions options)
    {
        var evaluations = Enumerable.Range(1, options.HoldoutSeeds).Select(index =>
            encounterEvaluator.EvaluateBuilds(new EncounterBuildEvaluationRequest(
                floor,
                builds,
                StableRandom.Seed(
                    "balance-elite-holdout-v1",
                    runSeed.ToString(CultureInfo.InvariantCulture),
                    floor.ToString(CultureInfo.InvariantCulture),
                    index.ToString(CultureInfo.InvariantCulture)),
                options.SimulationsPerSeed,
                maxTicks,
                calibration.HealthAdjustmentFactor,
                calibration.DamageAdjustmentFactor))).ToArray();
        var trials = evaluations.Sum(value => value.TrialCount);
        var clears = evaluations.Sum(value => (int)Math.Round(
            value.ObservedClearRate * value.TrialCount,
            MidpointRounding.AwayFromZero));
        var rate = clears / (double)trials;
        var (lower, upper) = WilsonInterval(clears, trials);
        return new EliteHoldoutSnapshot(
            options.HoldoutSeeds,
            options.SimulationsPerSeed,
            trials,
            clears,
            RoundRate(rate),
            lower,
            upper,
            RoundRate(upper - lower),
            Round(evaluations.Average(value => value.AverageDurationTicks)),
            Round(evaluations.Average(value => value.MedianDurationTicks > 0
                ? value.MedianDurationTicks
                : value.AverageDurationTicks)),
            Round(evaluations.Average(value => value.AverageFriendlyDeaths)),
            RoundRate(evaluations.Average(value => value.AverageRemainingHealthRatio)));
    }

    private IReadOnlyList<EssenceBuildSnapshot> MaterializeCuratedBuilds(
        TopPlayerFixtureDocument fixtures,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        int runSeed)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        return fixtures.Builds.Select(fixture =>
        {
            if (string.IsNullOrWhiteSpace(fixture.Id) || !ids.Add(fixture.Id))
                throw new InvalidOperationException($"Curated build ID '{fixture.Id}' is missing or duplicated.");
            if (string.IsNullOrWhiteSpace(fixture.SourceCategory)
                || string.IsNullOrWhiteSpace(fixture.ProgressionState)
                || string.IsNullOrWhiteSpace(fixture.IntendedRole)
                || string.IsNullOrWhiteSpace(fixture.ReviewerNote))
            {
                throw new InvalidOperationException($"Curated build '{fixture.Id}' is missing review metadata.");
            }
            if (fixture.EssenceIds.Count != fixture.SlotCount
                || fixture.EssenceIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != fixture.SlotCount)
            {
                throw new InvalidOperationException($"Curated build '{fixture.Id}' has an invalid Essence count.");
            }
            var sourceCount = fixture.EssenceIds.Select(id => definitionsById.GetValueOrDefault(id)
                    ?? throw new InvalidOperationException($"Curated build '{fixture.Id}' references unknown Essence '{id}'."))
                .Select(definition => definition.SourceMonsterId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            if (sourceCount != fixture.SlotCount)
                throw new InvalidOperationException($"Curated build '{fixture.Id}' duplicates an Essence source family.");
            var build = buildGenerator.MaterializeBuild(
                fixture.Id,
                $"E{fixture.SlotCount}_CURATED",
                fixture.SlotCount,
                runSeed,
                fixture.EssenceIds);
            if (!build.Character.GearPackageId.Equals(fixture.GearPackageId, StringComparison.Ordinal)
                || build.Character.CharacterLevel != fixture.CharacterLevel)
            {
                throw new InvalidOperationException($"Curated build '{fixture.Id}' does not match its canonical gear or level.");
            }
            return build;
        }).ToArray();
    }

    private static IEnumerable<IReadOnlyList<string>> EnumerateOneSwapGenomes(
        EssenceBuildSnapshot build,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById)
    {
        var genes = build.Essences.Select(value => value.EssenceId).ToArray();
        for (var index = 0; index < genes.Length; index++)
        {
            var usedSources = genes.Where((_, geneIndex) => geneIndex != index)
                .Select(id => definitionsById[id].SourceMonsterId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var replacement in sourceFamilies.Where(family => !usedSources.Contains(family[0].SourceMonsterId))
                         .SelectMany(family => family)
                         .Where(definition => !definition.Id.Equals(genes[index], StringComparison.OrdinalIgnoreCase))
                         .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase))
            {
                var candidate = genes.ToArray();
                candidate[index] = replacement.Id;
                yield return candidate.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
            }
        }
    }

    private static IEnumerable<IReadOnlyList<string>> EnumerateTwoSwapGenomes(
        EssenceBuildSnapshot build,
        IReadOnlyList<EssenceDefinition[]> sourceFamilies,
        IReadOnlyDictionary<string, EssenceDefinition> definitionsById,
        EssenceMetaAnalysisSnapshot essenceMeta)
    {
        var genes = build.Essences.Select(value => value.EssenceId).ToArray();
        var preferredPairs = essenceMeta.PairSynergies
            .Where(pair => pair.Classification == EssencePairSynergyClassification.Strong)
            .OrderByDescending(pair => pair.SynergyDelta)
            .Select(pair => (pair.FirstEssenceId, pair.SecondEssenceId))
            .ToArray();
        var yielded = new HashSet<string>(StringComparer.Ordinal);
        for (var firstIndex = 0; firstIndex < genes.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < genes.Length; secondIndex++)
            {
                var usedSources = genes.Where((_, index) => index != firstIndex && index != secondIndex)
                    .Select(id => definitionsById[id].SourceMonsterId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var eligible = sourceFamilies.Where(family => !usedSources.Contains(family[0].SourceMonsterId))
                    .SelectMany(family => family)
                    .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var orderedPairs = preferredPairs.Concat(
                    eligible.SelectMany(first => eligible.Where(second =>
                            !first.SourceMonsterId.Equals(second.SourceMonsterId, StringComparison.OrdinalIgnoreCase))
                        .Select(second => (first.Id, second.Id))));
                foreach (var pair in orderedPairs)
                {
                    if (!definitionsById.TryGetValue(pair.Item1, out var first)
                        || !definitionsById.TryGetValue(pair.Item2, out var second)
                        || usedSources.Contains(first.SourceMonsterId)
                        || usedSources.Contains(second.SourceMonsterId)
                        || first.SourceMonsterId.Equals(second.SourceMonsterId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var candidate = genes.ToArray();
                    candidate[firstIndex] = first.Id;
                    candidate[secondIndex] = second.Id;
                    var ordered = candidate.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
                    var signature = Signature(ordered);
                    if (yielded.Add(signature) && signature != Signature(genes))
                        yield return ordered;
                }
            }
        }
    }

    private void AddChallenge(
        string parentSignature,
        IReadOnlyList<string> genome,
        int depth,
        int slotCount,
        int runSeed,
        IDictionary<string, CertificationCandidate> candidatesBySignature,
        ICollection<ChallengeLink> links,
        IDictionary<string, EssenceBuildSnapshot> buildsToEvaluate)
    {
        var signature = Signature(genome);
        links.Add(new ChallengeLink(parentSignature, signature, depth));
        if (candidatesBySignature.ContainsKey(signature) || buildsToEvaluate.ContainsKey(signature))
            return;
        buildsToEvaluate[signature] = buildGenerator.MaterializeBuild(
            CreateCanonicalId(slotCount, signature),
            $"E{slotCount}_ELITE_LOCAL",
            slotCount,
            runSeed,
            genome);
    }

    private EssenceBuildSnapshot CanonicalizeBuild(EssenceBuildSnapshot source, int runSeed)
    {
        var signature = Signature(source);
        return buildGenerator.MaterializeBuild(
            CreateCanonicalId(source.SlotCount, signature),
            $"E{source.SlotCount}_ELITE_SEARCH",
            source.SlotCount,
            runSeed,
            source.Essences.Select(value => value.EssenceId).ToArray());
    }

    private static IReadOnlyList<CertificationCandidate> FindParetoFrontier(
        IReadOnlyList<CertificationCandidate> candidates) =>
        candidates.Where(candidate => !candidates.Any(other =>
                !ReferenceEquals(candidate, other) && Dominates(other, candidate)))
            .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .ToArray();

    private static bool Dominates(CertificationCandidate first, CertificationCandidate second)
    {
        var secondComponents = second.Benchmark.Components.ToDictionary(component => component.ScenarioId, StringComparer.Ordinal);
        var allAtLeast = true;
        var anyGreater = false;
        foreach (var component in first.Benchmark.Components)
        {
            var difference = component.Score - secondComponents[component.ScenarioId].Score;
            allAtLeast &= difference >= 0;
            anyGreater |= difference > 0;
        }
        return allAtLeast && anyGreater;
    }

    private static IReadOnlyList<CertificationCandidate> SelectFinalists(
        IReadOnlyList<CertificationCandidate> pareto,
        IReadOnlyList<CertificationCandidate> all,
        int count,
        double diversityPenalty)
    {
        var selected = new List<CertificationCandidate>();
        AddUnique(selected, all.OrderByDescending(candidate => candidate.Benchmark.AggregateScore).First(), count);
        foreach (var scenarioId in all[0].Benchmark.Components.Select(component => component.ScenarioId))
        {
            var leader = all.OrderByDescending(candidate => candidate.Benchmark.Components.Single(component => component.ScenarioId == scenarioId).Score)
                .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
                .First();
            AddUnique(selected, leader, count);
        }
        var remaining = pareto.Concat(all).DistinctBy(candidate => Signature(candidate.Build)).ToList();
        while (selected.Count < count && remaining.Count > 0)
        {
            var choice = remaining.OrderByDescending(candidate =>
                    candidate.Benchmark.AggregateScore
                    - (selected.Count == 0 ? 0 : selected.Max(existing => Similarity(existing.Build, candidate.Build))) * diversityPenalty)
                .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            AddUnique(selected, choice, count);
            remaining.Remove(choice);
        }
        return selected;
    }

    private static IReadOnlyList<CertificationCandidate> SelectRestartRefinementSeeds(
        IReadOnlyList<CertificationCandidate> candidates,
        int count,
        double diversityPenalty)
    {
        const int aggregatePoolSize = 50;
        const int scenarioPoolSize = 20;
        var seedPool = candidates.OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
            .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
            .Take(aggregatePoolSize)
            .Concat(candidates[0].Benchmark.Components.SelectMany(component => candidates
                .OrderByDescending(candidate => candidate.Benchmark.Components
                    .Single(value => value.ScenarioId == component.ScenarioId).Score)
                .ThenByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .Take(scenarioPoolSize)))
            .DistinctBy(candidate => Signature(candidate.Build))
            .ToArray();
        return SelectFinalists(FindParetoFrontier(seedPool), candidates, count, diversityPenalty);
    }

    private static void AddUnique(ICollection<CertificationCandidate> selected, CertificationCandidate candidate, int count)
    {
        if (selected.Count < count && selected.All(existing => Signature(existing.Build) != Signature(candidate.Build)))
            selected.Add(candidate);
    }

    private static IReadOnlyList<EssenceBuildSnapshot> SelectCohort(
        IReadOnlyList<CertificationCandidate> candidates,
        double minimumScore,
        int maximum)
    {
        var scores = candidates.Select(candidate => candidate.Benchmark.AggregateScore).ToArray();
        return EliteCertificationSearchRules.SelectPercentileCohortIndexes(scores, minimumScore, maximum)
            .Select(index => candidates[index].Build)
            .ToArray();
    }

    private static EliteCertificationCandidateSnapshot ToSnapshot(CertificationCandidate candidate, double percentile) =>
        new(
            candidate.Build.Id,
            Round(percentile),
            candidate.Benchmark.AggregateScore,
            candidate.Benchmark.Components.ToDictionary(component => component.ScenarioId, component => component.Score, StringComparer.Ordinal),
            candidate.Build.Essences.Select(essence => essence.EssenceId).ToArray());

    private static int GenerationsSinceMaterialImprovement(
        IReadOnlyList<EssenceOptimizerGenerationSnapshot> generations,
        double tolerance)
    {
        var lastImprovement = 0;
        var best = generations[0].BestScore;
        foreach (var generation in generations.Skip(1))
        {
            if (generation.BestScore - best > tolerance)
                lastImprovement = generation.Generation;
            best = Math.Max(best, generation.BestScore);
        }
        return generations[^1].Generation - lastImprovement;
    }

    private static long LegalCombinationCount(IReadOnlyList<EssenceDefinition[]> families, int slots)
    {
        var counts = new long[slots + 1];
        counts[0] = 1;
        foreach (var family in families)
        {
            for (var selected = slots; selected >= 1; selected--)
                counts[selected] = checked(counts[selected] + counts[selected - 1] * family.Length);
        }
        return counts[slots];
    }

    private static string CreateContentFingerprint(
        string combatContentFingerprint,
        WorldTowerAnalysisSnapshot worldTower,
        EncounterCalibrationSnapshot calibration)
    {
        var builder = new StringBuilder("elite-content-v1|").Append(combatContentFingerprint);
        foreach (var floor in worldTower.Floors.OrderBy(value => value.Floor))
            builder.Append('|').Append(floor.Floor).Append(':').Append(floor.GuardianAbilityProfileId);
        foreach (var floor in calibration.Floors.OrderBy(value => value.Floor))
            builder.Append('|').Append(floor.Floor).Append(':').Append(floor.HealthAdjustmentFactor.ToString("R", CultureInfo.InvariantCulture))
                .Append(':').Append(floor.DamageAdjustmentFactor.ToString("R", CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static string CreateCanonicalId(int slotCount, string signature)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature))).ToLowerInvariant();
        return $"E{slotCount}_CERT_{hash[..16]}";
    }

    private static EliteCertificationVerdict ResolveProfileVerdict(
        EliteCertificationProfile profile,
        bool searchStable,
        EliteLocalChallengeSnapshot challenge,
        bool scenarioCoverage,
        bool curatedRequirement,
        bool humanOutperformed,
        EliteCertificationPolicy policy)
    {
        if (humanOutperformed)
            return EliteCertificationVerdict.HumanBuildOutperformed;
        if (challenge.BestAggregateImprovement > policy.GenericLocalImprovementTolerance
            || challenge.BestScenarioImprovement > policy.GenericLocalImprovementTolerance)
            return EliteCertificationVerdict.LocalImprovementFound;
        if (!searchStable)
            return EliteCertificationVerdict.SearchUnstable;
        if (!scenarioCoverage)
            return EliteCertificationVerdict.ScenarioCoverageFailure;
        if (!curatedRequirement)
            return EliteCertificationVerdict.InsufficientPlayerEvidence;
        return profile == EliteCertificationProfile.Release
            ? EliteCertificationVerdict.CertifiedElite
            : EliteCertificationVerdict.DeveloperProfileOnly;
    }

    private static EliteCertificationVerdict ResolveFloorVerdict(
        EliteCertificationProfile profile,
        bool partyComplete,
        bool expectationsPassed,
        bool curatedRequirement,
        bool humanOutperformed)
    {
        if (humanOutperformed)
            return EliteCertificationVerdict.HumanBuildOutperformed;
        if (!partyComplete)
            return EliteCertificationVerdict.PartyOptimizationRequired;
        if (!expectationsPassed)
            return EliteCertificationVerdict.ScenarioCoverageFailure;
        if (!curatedRequirement)
            return EliteCertificationVerdict.InsufficientPlayerEvidence;
        return profile == EliteCertificationProfile.Release
            ? EliteCertificationVerdict.CertifiedElite
            : EliteCertificationVerdict.DeveloperProfileOnly;
    }

    private static EliteCertificationVerdict ResolveOverallVerdict(
        IEnumerable<EliteCertificationVerdict> verdicts,
        EliteCertificationProfile profile)
    {
        var values = verdicts.ToArray();
        var priority = new[]
        {
            EliteCertificationVerdict.HumanBuildOutperformed,
            EliteCertificationVerdict.LocalImprovementFound,
            EliteCertificationVerdict.SearchUnstable,
            EliteCertificationVerdict.ScenarioCoverageFailure,
            EliteCertificationVerdict.PartyOptimizationRequired,
            EliteCertificationVerdict.InsufficientPlayerEvidence,
            EliteCertificationVerdict.DeveloperProfileOnly
        };
        return priority.FirstOrDefault(values.Contains,
            profile == EliteCertificationProfile.Release
                ? EliteCertificationVerdict.CertifiedElite
                : EliteCertificationVerdict.DeveloperProfileOnly);
    }

    private static void AddKillTimeWarning(
        ICollection<string> warnings,
        int floor,
        string population,
        EliteHoldoutSnapshot elite,
        EliteHoldoutSnapshot generic,
        double warningRatio,
        double blockingRatio)
    {
        if (generic.MedianDurationTicks <= 0 || elite.MedianDurationTicks <= 0)
            return;
        var ratio = elite.MedianDurationTicks / generic.MedianDurationTicks;
        if (ratio < blockingRatio)
            warnings.Add($"Floor {floor} {population} average kill time is below the mechanic-bypass boundary ({ratio:P0} of P75)." );
        else if (ratio < warningRatio)
            warnings.Add($"Floor {floor} {population} average kill time indicates possible trivialization ({ratio:P0} of P75)." );
    }

    private static int CompareHoldout(EliteHoldoutSnapshot first, EliteHoldoutSnapshot second)
    {
        var clear = first.ClearRate.CompareTo(second.ClearRate);
        return clear != 0 ? clear : second.MedianDurationTicks.CompareTo(first.MedianDurationTicks);
    }

    private static double PartyScore(EncounterCalibrationEvaluation evaluation, int maxTicks) =>
        evaluation.ObservedClearRate * 100
        + evaluation.AverageRemainingHealthRatio * 10
        - evaluation.AverageFriendlyDeaths * 2
        - evaluation.AverageDurationTicks / maxTicks * 5;

    private static (double Lower, double Upper) WilsonInterval(int successes, int trials)
    {
        const double z = 1.959963984540054;
        var p = successes / (double)trials;
        var denominator = 1 + z * z / trials;
        var center = (p + z * z / (2 * trials)) / denominator;
        var halfWidth = z / denominator * Math.Sqrt(p * (1 - p) / trials + z * z / (4d * trials * trials));
        return (RoundRate(Math.Max(0, center - halfWidth)), RoundRate(Math.Min(1, center + halfWidth)));
    }

    private static double Similarity(EssenceBuildSnapshot first, EssenceBuildSnapshot second)
    {
        var ids = first.Essences.Select(value => value.EssenceId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return second.Essences.Count(value => ids.Contains(value.EssenceId)) / (double)Math.Max(first.SlotCount, second.SlotCount);
    }

    private static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        var position = (sorted.Count - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper ? sorted[lower] : sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    private static string Signature(EssenceBuildSnapshot build) =>
        Signature(build.Essences.Select(value => value.EssenceId));

    private static string Signature(IEnumerable<string> essenceIds) =>
        string.Join('|', essenceIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));

    private static double ValleyPrefilterScore(
        IReadOnlyList<string> genome,
        IReadOnlyDictionary<string, double> essenceScores,
        IReadOnlyDictionary<string, double> pairScores)
    {
        var score = genome.Sum(id => essenceScores.GetValueOrDefault(id));
        for (var first = 0; first < genome.Count; first++)
        {
            for (var second = first + 1; second < genome.Count; second++)
                score += pairScores.GetValueOrDefault(Signature([genome[first], genome[second]]));
        }
        return score;
    }

    private static int GenomeDistance(IReadOnlyList<string> first, IReadOnlyList<string> second) =>
        first.Except(second, StringComparer.OrdinalIgnoreCase).Count();

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static double RoundRate(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record RestartResult(
        int Restart,
        int SearchSeed,
        EssenceOptimizationResult Result,
        IReadOnlySet<string> BaselineSignatures,
        int PortfolioCandidatesEvaluated);
    private sealed record RestartRefinementResult(
        CertificationCandidate Best,
        int AcceptedPasses,
        int NewCandidatesEvaluated,
        int OneSwapCandidatesEvaluated,
        int TwoSwapCandidatesEvaluated);
    private sealed record DescriptorAuditAnchor(
        string Basin,
        string AnchorId,
        IReadOnlyList<string> Genome);
    private sealed record DescriptorAuditAnchorCandidate(
        DescriptorAuditAnchor Anchor,
        CertificationCandidate Candidate);
    private sealed record DescriptorAuditCandidate(
        CertificationCandidate Candidate,
        string Basin);
    private sealed record DescriptorDefinition(
        string Id,
        string DisplayName,
        Func<CertificationCandidate, IReadOnlyDictionary<string, double>> Extract,
        double SignatureBinWidth,
        int? TheoreticalNicheCeiling);
    private sealed record DescriptorFeatureRow(
        string Basin,
        IReadOnlyDictionary<string, double> Features);
    private sealed record DescriptorAnchorFeatureRow(
        string Basin,
        IReadOnlyDictionary<string, double> Features);
    private sealed record ValleyBeamResult(
        CertificationCandidate Best,
        int DepthReached,
        int CandidatesEvaluated,
        int NewCandidatesEvaluated,
        bool BudgetExhausted,
        double BestImprovement,
        int CandidatesGenerated,
        int CandidatesRejectedByPrefilter);
    private sealed record QualityDiversityIslandResult(
        CertificationCandidate? Best,
        int CandidatesEvaluated,
        int InitialCandidatesEvaluated,
        int DescendantsEvaluated,
        int NichesOccupied,
        int NicheReplacements,
        bool TelemetryTargetNichePresentInBaseline = false,
        double TelemetryTargetNicheBaselineBestScore = 0,
        int TelemetryTargetNicheIslandCandidatesEvaluated = 0,
        double TelemetryTargetNicheIslandBestScore = 0);
    private sealed record BridgeGenome(int Level, IReadOnlyList<string> Genome);
    private sealed record BridgeNode(
        int Level,
        string Signature,
        EssenceBuildSnapshot Build,
        double Score);
    private sealed record BridgePathState(
        IReadOnlyList<BridgeNode> Nodes,
        double MinimumScore,
        double LargestSingleStepRegression,
        double TotalTemporaryRegressionBelowSource);
    private sealed record CertificationCandidate(EssenceBuildSnapshot Build, PveBenchmarkBuildSnapshot Benchmark);
    private sealed record ChallengeLink(string ParentSignature, string ChallengerSignature, int Depth);
    private sealed record PartySearchResult(
        IReadOnlyList<EssenceBuildSnapshot> Builds,
        int EvaluatedGenomes,
        long SearchSpaceSize,
        long TargetGenomes,
        bool Complete);
    private sealed record ProfileState(
        int SlotCount,
        EliteCertificationProfileSnapshot Snapshot,
        IReadOnlyList<CertificationCandidate> Candidates,
        IReadOnlyList<CertificationCandidate> Finalists);
}
