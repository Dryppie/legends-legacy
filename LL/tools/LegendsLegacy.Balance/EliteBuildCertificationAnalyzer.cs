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
    int StratifiedPortfolioCandidatesEvaluated = 0);

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
    IReadOnlyList<EliteCertificationFloorSnapshot> Floors,
    int TotalBridgeNodesEvaluated = 0,
    IReadOnlyList<EliteRestartBridgeAuditSnapshot>? BridgeAudits = null);

public sealed class EliteBuildCertificationAnalyzer(
    IAbilityCatalogProvider catalogProvider,
    IEssenceDefinitionRepository essenceDefinitions,
    EssenceBuildGenerator buildGenerator,
    PveBenchmarkRunner benchmarkRunner,
    EssenceBuildOptimizer optimizer,
    IEncounterBuildEvaluator encounterEvaluator)
{
    public const int AlgorithmVersion = 14;

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
            bridgeAudits);
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
            var newCandidatesEvaluated = oneSwapCandidatesEvaluated
                                         + twoSwapCandidatesEvaluated
                                         + valley.NewCandidatesEvaluated;
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
                baselineRefinementSeeds.Count + portfolioRefinementSeeds.Count,
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
                restart.PortfolioCandidatesEvaluated);
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
    private sealed record ValleyBeamResult(
        CertificationCandidate Best,
        int DepthReached,
        int CandidatesEvaluated,
        int NewCandidatesEvaluated,
        bool BudgetExhausted,
        double BestImprovement,
        int CandidatesGenerated,
        int CandidatesRejectedByPrefilter);
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
