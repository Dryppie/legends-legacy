using System.Globalization;
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
    int LocalCandidatesEvaluated);

public sealed record EliteCertificationCandidateSnapshot(
    string BuildId,
    double PopulationPercentile,
    double AggregateScore,
    IReadOnlyDictionary<string, double> ComponentScores,
    IReadOnlyList<string> EssenceIds);

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
    IReadOnlyList<string> Warnings);

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
    IReadOnlyList<EliteCertificationFloorSnapshot> Floors);

public sealed class EliteBuildCertificationAnalyzer(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    EssenceBuildGenerator buildGenerator,
    PveBenchmarkRunner benchmarkRunner,
    EssenceBuildOptimizer optimizer,
    IEncounterBuildEvaluator encounterEvaluator)
{
    public const int AlgorithmVersion = 3;

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
        var calibrationByFloor = calibration.Floors.ToDictionary(floor => floor.Floor);
        var representativeById = representativeBuilds.Profiles.ToDictionary(profile => profile.Id, StringComparer.Ordinal);
        var floors = worldTower.Floors.OrderBy(floor => floor.Floor)
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
        var verdict = ResolveOverallVerdict(
            profiles.Select(profile => profile.Verdict).Concat(floors.Select(floor => floor.Verdict)),
            options.Profile);
        var warnings = profiles.SelectMany(profile => profile.Warnings)
            .Concat(floors.SelectMany(floor => floor.Warnings))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

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
            floors);
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
            policy.PlateauImprovementTolerance);
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
            var result = optimizer.Optimize(initial, initialBenchmarks, runSeed, optimizerOptions, searchSeed);
            return new RestartResult(restart, searchSeed, result);
        }).ToArray();
    }

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
            var rawBest = signatures.Select(signature => bySignature[signature])
                .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            var refinement = RefineRestartWinner(
                rawBest,
                bySignature,
                sourceFamilies,
                definitionsById,
                runSeed,
                policy,
                options);
            var generationsSinceImprovement = GenerationsSinceMaterialImprovement(
                profile.Generations,
                policy.PlateauImprovementTolerance);
            return new EliteCertificationRestartSnapshot(
                restart.Restart,
                restart.SearchSeed,
                rawBest.Benchmark.AggregateScore,
                refinement.Best.Benchmark.AggregateScore,
                profile.Generations[^1].Generation,
                generationsSinceImprovement,
                generationsSinceImprovement >= plateauRequirement,
                signatures.Length + refinement.NewCandidatesEvaluated,
                refinement.AcceptedPasses,
                refinement.NewCandidatesEvaluated);
        }).ToArray();
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
        int runSeed,
        EliteCertificationPolicy policy,
        EliteCertificationOptions options)
    {
        var current = initial;
        var acceptedPasses = 0;
        var newCandidatesEvaluated = 0;
        for (var pass = 1; pass <= options.RestartLocalRefinementPassLimit; pass++)
        {
            var genomes = EnumerateOneSwapGenomes(current.Build, sourceFamilies, definitionsById)
                .DistinctBy(Signature)
                .ToArray();
            var missing = genomes.Where(genome => !candidatesBySignature.ContainsKey(Signature(genome)))
                .Select(genome => buildGenerator.MaterializeBuild(
                    CreateCanonicalId(current.Build.SlotCount, Signature(genome)),
                    $"E{current.Build.SlotCount}_ELITE_RESTART_LOCAL",
                    current.Build.SlotCount,
                    runSeed,
                    genome))
                .ToArray();
            if (missing.Length > 0)
            {
                var benchmarks = benchmarkRunner.Run(missing, runSeed).Builds
                    .ToDictionary(build => build.BuildId, StringComparer.Ordinal);
                foreach (var build in missing)
                    candidatesBySignature[Signature(build)] = new CertificationCandidate(build, benchmarks[build.Id]);
                newCandidatesEvaluated += missing.Length;
            }
            var bestNeighbor = genomes.Select(genome => candidatesBySignature[Signature(genome)])
                .OrderByDescending(candidate => candidate.Benchmark.AggregateScore)
                .ThenBy(candidate => candidate.Build.Id, StringComparer.Ordinal)
                .First();
            if (bestNeighbor.Benchmark.AggregateScore - current.Benchmark.AggregateScore
                <= policy.PlateauImprovementTolerance)
            {
                break;
            }
            current = bestNeighbor;
            acceptedPasses++;
        }
        return new RestartRefinementResult(current, acceptedPasses, newCandidatesEvaluated);
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
            warnings);
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

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static double RoundRate(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private sealed record RestartResult(int Restart, int SearchSeed, EssenceOptimizationResult Result);
    private sealed record RestartRefinementResult(
        CertificationCandidate Best,
        int AcceptedPasses,
        int NewCandidatesEvaluated);
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
