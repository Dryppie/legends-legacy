using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossDiscoveryFitness(double WorstContextWinRate, double GuardianHealth, double Survival, double VictoryDuration);
public sealed record BossDiscoveryMeasurement(string Id, BossDiscoveryFitness Fitness, IReadOnlyList<PartyFloorScore> Cells, BossBehavior Behavior);
public sealed record BossGeneratedProposal(BossDiscoveryProvenance Provenance, PartyChoice? Party, string Intent, string? Interaction, string Result,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BossCoverageReservation>? Reservations = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossLoadoutTrace? Loadouts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossPartyLineage? Lineage = null);
public sealed record BossGenerationArm(string Method, int Seed, string StopReason,
    IReadOnlyList<BossGeneratedProposal> Proposals, IReadOnlyList<BossDiscoveryMeasurement> Evaluations,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BossFeedbackRound>? Feedback = null);
public sealed record BossGenerationResult(string Version, string Status, IReadOnlyList<BossGenerationArm> Arms,
    IReadOnlyList<PartyChoice> DiscoveryShortlist, string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<TowerLateAllocationDecision>? AllocationDecisions = null);

/// <summary>Independent search kernel: no benchmark references, actor identities or confirmation outcomes enter this API.</summary>
public static class TowerBossGeneration
{
    public const string Version = "independent-teams-v1";
    public const string CoordinatedVersion = "independent-coordinated-v2";
    public static readonly string[] CoordinatedMethods = ["constructive-joint", "coordinated-joint"];
    public const string MechanicsVersion = "independent-mechanics-v3";
    public static readonly string[] MechanicsMethods = ["coordinated-joint", "mechanics-joint"];
    public const string CoverageVersion = "independent-coverage-v4";
    public static readonly string[] CoverageMethods = ["mechanics-joint", "coverage-joint"];
    public const string ProviderVersion = "independent-provider-v5";
    public static readonly string[] ProviderMethods = ["coverage-joint", "provider-joint"];
    public const string CollectiveVersion = "independent-collective-v6";
    public static readonly string[] CollectiveMethods = ["provider-joint", "collective-joint"];
    public const string CompletionVersion = "independent-completion-v7";
    public static readonly string[] CompletionMethods = ["collective-joint", "completion-joint"];
    public const string DefenseVersion = "independent-defense-v8";
    public static readonly string[] DefenseMethods = ["collective-joint", "defense-joint"];
    public const string CompatibleDefenseVersion = "independent-compatible-defense-v9";
    public static readonly string[] CompatibleDefenseMethods = ["defense-joint", "compatible-defense-joint"];
    public const string StaggerReservationVersion = "independent-stagger-reservation-v10";
    public static readonly string[] StaggerReservationMethods = ["compatible-defense-joint", "stagger-reservation-joint"];
    public const string LoadoutDiversityVersion = "independent-loadout-diversity-v11";
    public static readonly string[] LoadoutDiversityMethods = ["stagger-reservation-joint", "loadout-diversity-joint"];
    public const string DepthBehaviorVersion = "independent-depth-behavior-v12";
    public static readonly string[] DepthBehaviorMethods = ["coverage-small-joint", "coverage-deep-joint", "behavior-archive-joint"];
    public const string LoadoutCompositionVersion = "independent-loadout-composition-v13";
    public static readonly string[] LoadoutCompositionMethods = ["coverage-deep-joint", "loadout-composition-joint"];
    // Explicit asymmetric policies retain CandidatesPerArm as the unchanged comparator budget.
    public static int CandidateBudget(BossDiscoveryGeneration generation, string method) =>
        generation.PolicyVersion == TowerSearchPortfolio.Version ? TowerSearchPortfolio.ConstructionBudget(method) :
        generation.PolicyVersion is TowerSearchAllocation.Version or TowerLateAllocation.Version && TowerSearchAllocation.IsComponent(method) ? generation.CandidatesPerArm / 2 :
        generation.PolicyVersion == TowerGenerationFeedback.Version && method == TowerGenerationFeedback.Method ? 320 :
        generation.PolicyVersion == DepthBehaviorVersion && method == DepthBehaviorMethods[0]
            ? generation.CandidatesPerArm / 4 : generation.CandidatesPerArm;
    internal static int AttemptBudget(BossDiscoveryGeneration generation, string method) =>
        generation.PolicyVersion == TowerSearchPortfolio.Version ? TowerSearchPortfolio.AttemptBudget(generation, method) :
        generation.PolicyVersion is TowerSearchAllocation.Version or TowerLateAllocation.Version && TowerSearchAllocation.IsComponent(method)
            ? generation.MaximumAttemptsPerArm / 2 : generation.MaximumAttemptsPerArm;
    internal static int CandidateTotal(BossDiscoveryGeneration generation) =>
        checked(generation.Seeds.Count * generation.Methods.Sum(method => CandidateBudget(generation, method)));
    public const int BeamSize = 4;
    public const int ExplorationSize = 4;
    public static readonly string[] Operators = ["single", "double", "order", "cross-character", "whole-character", "recombine"];
    private static readonly string[] CoordinatedOperators = [.. Operators, "broadcast-core"];
    private static readonly string[] MechanicsOperators = [.. CoordinatedOperators, "mechanic-core"];
    private static readonly string[] CoverageOperators = ["coverage-count", "placement", "single", "double", "order", "cross-character",
        "whole-character", "recombine", "mechanic-core", "placement"];
    private static readonly string[] LoadoutOperators = CoverageOperators.SelectMany((op, i) => new[] {
        new[] { "loadout-distribute", "loadout-compose", "loadout-refine", "loadout-placement" }[i % 4], op }).ToArray();
    // A single operator substitution tests provider choice at the parent's existing count and placement.
    private static readonly string[] ProviderOperators = CoverageOperators.Select(op => op == "coverage-count" ? "coverage-provider" : op).ToArray();
    private static readonly string[] CollectiveOperators = ProviderOperators.Select(op => op == "coverage-provider" ? "collective-provider" : op).ToArray();

    internal static bool LegalPolicy(BossDiscoveryGeneration? g) => g?.Methods is not null && (g.PolicyVersion switch {
        Version => g.Methods.SequenceEqual(TowerBossDiscovery.Methods),
        CoordinatedVersion => g.Methods.SequenceEqual(CoordinatedMethods),
        MechanicsVersion => g.Methods.SequenceEqual(MechanicsMethods),
        CoverageVersion => g.Methods.SequenceEqual(CoverageMethods),
        ProviderVersion => g.Methods.SequenceEqual(ProviderMethods),
        CollectiveVersion => g.Methods.SequenceEqual(CollectiveMethods),
        CompletionVersion => g.Methods.SequenceEqual(CompletionMethods),
        DefenseVersion => g.Methods.SequenceEqual(DefenseMethods),
        CompatibleDefenseVersion => g.Methods.SequenceEqual(CompatibleDefenseMethods),
        StaggerReservationVersion => g.Methods.SequenceEqual(StaggerReservationMethods),
        LoadoutDiversityVersion => g.Methods.SequenceEqual(LoadoutDiversityMethods),
        LoadoutCompositionVersion => g.Methods.SequenceEqual(LoadoutCompositionMethods),
        TowerPartyLineages.Version => g.Methods.SequenceEqual(TowerPartyLineages.Methods),
        TowerSearchPortfolio.Version => g.Methods.SequenceEqual(TowerSearchPortfolio.Methods) && g.CandidatesPerArm == TowerSearchPortfolio.Candidates && g.MaximumAttemptsPerArm % 4 == 0,
        TowerLateAllocation.Version => g.Methods.SequenceEqual(TowerSearchAllocation.Methods) && g.CandidatesPerArm == 768 && g.MaximumAttemptsPerArm % 2 == 0,
        TowerSearchAllocation.Version => g.Methods.SequenceEqual(TowerSearchAllocation.Methods) && g.CandidatesPerArm >= 2 && g.CandidatesPerArm % 2 == 0 && g.MaximumAttemptsPerArm % 2 == 0,
        TowerLoadoutRetention.Version => g.Methods.SequenceEqual(TowerLoadoutRetention.Methods),
        TowerGenerationFeedback.Version => g.Methods.SequenceEqual(TowerGenerationFeedback.Methods) && g.CandidatesPerArm == 384,
        DepthBehaviorVersion => g.Methods.SequenceEqual(DepthBehaviorMethods)
            && g.CandidatesPerArm >= 8 && g.CandidatesPerArm % 4 == 0,
        _ => false
    });

    public static void ValidateInputs(BossDiscoveryInputs d)
    {
        if (d?.Generation is { PolicyVersion: TowerBossImprovement.Version } generation)
        {
            if (generation.Methods is null || !generation.Methods.SequenceEqual(TowerBossImprovement.Methods))
                throw new InvalidDataException("Invalid retained-build generation methods.");
            d = d with { Generation = generation with { PolicyVersion = Version, Methods = TowerBossDiscovery.Methods } };
        }
        if (d is null || !TowerBossDiscovery.LegalBudget(d.Budget) || d.Floor != d.Budget.PriorityFloor
            || d.RequiredPartySize is < 1 or > 50 || d.AllowedEssences is not { Count: >= 4 and <= 1000 }
            || d.AllowedEssences.Any(e => e is null || string.IsNullOrWhiteSpace(e.Id) || string.IsNullOrWhiteSpace(e.Family))
            || d.AllowedEssences.Select(e => e.Id).Distinct().Count() != d.AllowedEssences.Count
            || d.DiscoverySeeds is not { Count: > 0 and <= 4 } || d.EquipmentContexts is null
            || !d.DiscoverySeeds.Keys.Order().SequenceEqual(d.EquipmentContexts.Keys.Order())
            || d.DiscoverySeeds.Any(p => !TowerBenchmark.SafeId(p.Key) || p.Value is not { Count: > 0 and <= 100 }
                || p.Value.Distinct().Count() != p.Value.Count)
            || d.DiscoverySeeds.Values.Select(s => s.Count).Distinct().Count() != 1
            || d.DiscoverySeeds.Values.SelectMany(s => s).Distinct().Count() != d.DiscoverySeeds.Values.Sum(s => s.Count)
            || d.EquipmentContexts.Values.Any(c => c is null || c.Count != d.RequiredPartySize
                || c.Any(p => p is null || p.Equipment is null || !double.IsFinite(p.AttributeRollMultiplier) || p.AttributeRollMultiplier <= 0)
                || !c.Select(p => p.PartySlot).SequenceEqual(Enumerable.Range(1, d.RequiredPartySize)))
            || d.ContentHashes is null || !d.ContentHashes.Keys.Order().SequenceEqual(TowerBundle.Files.Order())
            || d.ContentHashes.Values.Any(h => !TowerContractJson.Hash(h)))
            throw new InvalidDataException("Invalid independent generation input boundary.");
        var g = d.Generation;
        if (g is null || g.Methods is null || !LegalPolicy(g) || g.Objective != TowerBossDiscovery.Objective || g.FreshEvery != 4
            || g.Seeds is not { Count: > 0 and <= 4 }
            || g.Seeds.Distinct().Count() != g.Seeds.Count || (g.CandidatesPerArm < 1 || g.CandidatesPerArm > (g.PolicyVersion == TowerSearchPortfolio.Version ? TowerSearchPortfolio.Candidates : 1000))
            || g.MaximumAttemptsPerArm < g.CandidatesPerArm || g.MaximumAttemptsPerArm > (g.PolicyVersion == TowerSearchPortfolio.Version ? TowerSearchPortfolio.Attempts : 10000)
            || d.ShortlistCandidates < g.Methods.Count * g.Seeds.Count || d.ShortlistCandidates > 64
            || d.ShortlistCandidates > CandidateTotal(g))
            throw new InvalidDataException("Invalid bounded generation policy or shortlist allocation.");
        TowerGenerationFeedback.ValidateInputs(d);
        if (d.OwnedCopies is not null && (d.OwnedCopies.Any(p => !d.AllowedEssences.Any(e => e.Id == p.Key) || p.Value is < 0 or > 50)
            || d.AllowedEssences.GroupBy(e => e.Family, StringComparer.OrdinalIgnoreCase)
                .Sum(f => Math.Min(d.RequiredPartySize, f.Sum(e => d.OwnedCopies.GetValueOrDefault(e.Id)))) < d.RequiredPartySize * d.Budget.EssenceSlots))
            throw new InvalidDataException("Owned inventory cannot fill a legal full party.");
    }

    public static BossDiscoveryFitness Fitness(BossDiscoveryInputs input, IReadOnlyList<PartyFloorScore> cells, double victoryDuration)
    {
        if (cells is null || cells.Count != input.DiscoverySeeds.Count || cells.Any(c => c is null
                || c.Floor != input.Floor || c.Context is null || !input.DiscoverySeeds.TryGetValue(c.Context, out var seeds)
                || c.Clears is null || c.Clears.Count != seeds.Count || c.Draws < 0 || c.Draws > c.Clears.Count(x => !x)
                || c.Trials is null || c.Trials.Count != seeds.Count || c.Trials.Any(string.IsNullOrWhiteSpace)
                || c.Trials.Distinct().Count() != c.Trials.Count || !double.IsFinite(c.GuardianHealth) || c.GuardianHealth is < 0 or > 100
                || !double.IsFinite(c.Survival) || c.Survival is < 0 or > 100)
            || cells.Select(c => c.Context).Distinct().Count() != cells.Count
            || !double.IsFinite(victoryDuration) || victoryDuration < 0)
            throw new InvalidDataException("Discovery fitness requires the complete equally sampled target/context matrix.");
        return new(cells.Min(c => c.Clears.Count(x => x) / (double)c.Clears.Count), cells.Average(c => c.GuardianHealth),
            cells.Average(c => c.Survival), cells.Any(c => c.Clears.Any(x => x)) ? victoryDuration : double.MaxValue);
    }

    public static IOrderedEnumerable<BossDiscoveryMeasurement> Rank(IEnumerable<BossDiscoveryMeasurement> rows) => rows
        .OrderByDescending(r => r.Fitness.WorstContextWinRate).ThenBy(r => r.Fitness.GuardianHealth)
        .ThenByDescending(r => r.Fitness.Survival).ThenBy(r => r.Fitness.VictoryDuration).ThenBy(r => r.Id, StringComparer.Ordinal);

    public static async Task<BossGenerationResult> RunAsync(BossDiscoveryInputs inputs, BossGenerationMechanics mechanics,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>> evaluate, CancellationToken token = default,
        Action<BossGenerationResult>? checkpoint = null,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>>? evaluateFeedback = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        // Isolate caller mutation and keep the policy independent of definition/reference serialization.
        var d = JsonSerializer.Deserialize<BossDiscoveryInputs>(JsonSerializer.Serialize(inputs, HarnessJson.Options), HarnessJson.Options)!;
        ValidateInputs(d);
        if ((d.Generation.PolicyVersion == TowerGenerationFeedback.Version) != (evaluateFeedback is not null))
            throw new InvalidDataException("Feedback execution needs its explicit separately sampled evaluator; historical policies cannot consume it.");
        if (!LegalPolicy(d.Generation)) throw new InvalidDataException("Independent execution cannot consume a retained-build policy.");
        var baselineGenerator = new TowerBossPartyGenerator(d, mechanics);
        var arms = new List<BossGenerationArm>();
        var portfolio = d.Generation.PolicyVersion == TowerSearchPortfolio.Version;
        var late = portfolio || d.Generation.PolicyVersion == TowerLateAllocation.Version;
        var decisions = new List<TowerLateAllocationDecision>(); var activeArm = -1;
        var status = "Incomplete"; string? error = null;
        BossGenerationResult Report(bool select = false) => new(d.Generation.PolicyVersion, status, arms.ToArray(), select ? Shortlist(d, baselineGenerator, arms) : [], error, late ? decisions.ToArray() : null);
        Func<int, bool, Task> CreateArm(string method, int seed)
        {
            token.ThrowIfCancellationRequested();
            var armId = method + "-" + seed.ToString(CultureInfo.InvariantCulture);
            var coverageBaseline = method is "coverage-small-joint" or "coverage-deep-joint";
            var feedbackArm = method == TowerGenerationFeedback.Method;
            var retentionArm = method == TowerLoadoutRetention.Method;
            var lineageArm = method == TowerPartyLineages.Method;
            var loadoutComposition = method == "loadout-composition-joint" || feedbackArm || retentionArm || lineageArm || TowerSearchAllocation.IsComponent(method) || method == TowerSearchPortfolio.DeepComponent;
            var behaviorArchive = method == "behavior-archive-joint";
            var streamId = portfolio ? TowerSearchPortfolio.StreamId(method, seed) : d.Generation.PolicyVersion is TowerSearchAllocation.Version or TowerLateAllocation.Version ? TowerSearchAllocation.StreamId(method, seed) : coverageBaseline || loadoutComposition ? "coverage-joint-" + seed.ToString(CultureInfo.InvariantCulture) : armId;
            var random = new Random(StableRandom.Seed(Version, streamId));
            var candidateBudget = CandidateBudget(d.Generation, method);
            // Keep legacy arm streams and operators unchanged, including the paired v2 baseline.
            var coordinated = method == "coordinated-joint";
            var mechanical = method == "mechanics-joint";
            var provider = method == "provider-joint";
            var completion = method == "completion-joint";
            var loadoutDiversity = method == "loadout-diversity-joint";
            var staggerReservation = method == "stagger-reservation-joint" || loadoutDiversity;
            var compatibleDefense = method == "compatible-defense-joint" || staggerReservation;
            var defense = method == "defense-joint" || compatibleDefense;
            var generator = defense ? new TowerBossPartyGenerator(d, mechanics, attributeDefense: true, compatibleDefense: compatibleDefense, staggerReservation: staggerReservation) : baselineGenerator;
            var collective = method == "collective-joint" || completion || defense;
            var coverage = method == "coverage-joint" || coverageBaseline || behaviorArchive || loadoutComposition || provider || collective;
            var operators = loadoutComposition ? LoadoutOperators : collective ? CollectiveOperators : provider ? ProviderOperators : coverage ? CoverageOperators : mechanical ? MechanicsOperators : coordinated ? CoordinatedOperators : Operators;
            var freshOperation = loadoutDiversity ? "fresh-loadout-diversity" : staggerReservation ? "fresh-stagger-reservation" : compatibleDefense ? "fresh-compatible-defense" : defense ? "fresh-defense" : completion ? "fresh-completion" : coverage ? "fresh-coverage" : mechanical ? "fresh-mechanics" : coordinated ? "fresh-coordinated" : "fresh-constructive";
            BossGeneratedChoice Fresh() => completion ? generator.FreshCompletion(random) : coverage ? generator.FreshCoverage(random) : mechanical ? generator.FreshMechanics(random) : coordinated ? generator.FreshCoordinated(random) : generator.Fresh(random, true);
            var proposals = new List<BossGeneratedProposal>(); var measurements = new List<BossDiscoveryMeasurement>();
            var measured = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
            var byProposalId = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
            var initial = Math.Max(1, (candidateBudget + 3) / 4);
            if (feedbackArm) initial = 96; // Preserve v13's complete initial construction before the first reassessment.
            var feedbackRounds = new List<BossFeedbackRound>();
            IEnumerable<BossDiscoveryMeasurement> Ranking() => feedbackArm
                ? TowerGenerationFeedback.Effective(d, measurements, feedbackRounds) : measurements;
            if (late && TowerSearchAllocation.IsComponent(method)) initial = TowerLateAllocation.Initial;
            var refinement = 0; var attempt = 0; var armIndex = arms.Count;
            void Snapshot(string stop)
            {
                var arm = new BossGenerationArm(method, seed, stop, proposals.ToArray(), measurements.ToArray(),
                    feedbackArm ? feedbackRounds.ToArray() : null);
                if (armIndex < arms.Count) arms[armIndex] = arm; else arms.Add(arm);
                checkpoint?.Invoke(Report());
            }
            Snapshot("Running");
            return async (target, final) =>
            {
                activeArm = armIndex;
                for (; attempt < AttemptBudget(d.Generation, method) && measurements.Count < target; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    string operation; BossGeneratedChoice choice; string[] parents = []; BossLoadoutTrace? loadouts = null;
                    if (method == "random" || measurements.Count < initial)
                    {
                        operation = method == "random" ? "fresh-random" : freshOperation;
                        choice = method == "random" ? generator.Fresh(random, false) : Fresh();
                    }
                    else
                    {
                        var turn = refinement++;
                        if (turn % d.Generation.FreshEvery == d.Generation.FreshEvery - 1)
                        { operation = freshOperation; choice = Fresh(); }
                        else
                        {
                            operation = operators[(turn - turn / d.Generation.FreshEvery) % operators.Length];
                            var ranking = Ranking().ToArray();
                            var beam = lineageArm ? TowerPartyLineages.Select(ranking, measured)
                                : behaviorArchive ? TowerBehaviorArchive.Select(measurements)
                                : loadoutDiversity ? TowerLoadoutDiversity.Select(measurements, measured)
                                : Rank(ranking).Take(BeamSize).Select(m => m.Id).ToArray();
                            // The new archive itself supplies alternative behaviors. No additional
                            // exploration parents may exceed its 32-entry bound.
                            var exploration = behaviorArchive ? [] : Explore(generator, ranking, measured, beam, ExplorationSize);
                            var choices = exploration.Length > 0 && random.Next(4) == 0 ? exploration : beam;
                            var parent = measured[choices[random.Next(choices.Length)]];
                            BossGeneratedProposal? other = null;
                            if (operation == "recombine")
                            {
                                var alternatives = beam.Concat(exploration).Distinct().Where(id => id != parent.Party!.Id).ToArray();
                                if (alternatives.Length > 0) other = measured[alternatives[random.Next(alternatives.Length)]];
                            }
                            if (loadoutComposition && operation.StartsWith("loadout-", StringComparison.Ordinal))
                            {
                                var coordinatedProposal = generator.CoordinateLoadouts(random, operation, parent,
                                    retentionArm ? TowerLoadoutRetention.Library(ranking, measured) : TowerLoadoutComposition.Library(ranking, measured));
                                choice = coordinatedProposal.Choice; parents = coordinatedProposal.Parents; loadouts = coordinatedProposal.Trace;
                            }
                            else if (operation == "recombine" && other is null)
                            { operation = freshOperation; choice = Fresh(); }
                            else
                            {
                                parents = other is null ? [parent.Provenance.Id] : [parent.Provenance.Id, other.Provenance.Id];
                                choice = operation == "broadcast-core" ? generator.BroadcastCore(random, parent.Party!)
                                    : operation == "mechanic-core" ? generator.ReplaceMechanicCore(random, parent.Party!)
                                    : operation == "coverage-count" ? generator.ChangeCoverage(random, parent.Party!)
                                    : operation == "coverage-provider" ? generator.ChangeCoverageProvider(random, parent.Party!)
                                    : operation == "collective-provider" ? generator.ChangeCollectiveCoverageProvider(random, parent.Party!)
                                    : operation == "placement" ? generator.ChangePlacement(random, parent.Party!)
                                    : generator.Mutate(random, operation, parent.Party!, other?.Party);
                            }
                        }
                    }
                    var provenance = new BossDiscoveryProvenance($"{armId}-proposal-{attempt:D5}", seed, method, operation, parents, []);
                    var rejection = choice.Rejection ?? (choice.Party is null ? "no-legal-proposal" : measured.ContainsKey(choice.Party.Id) ? "duplicate" : null);
                    var proposal = new BossGeneratedProposal(provenance, choice.Party, choice.Intent, choice.Interaction, rejection ?? "evaluating", choice.Reservations, loadouts);
                    proposals.Add(proposal);
                    if (rejection is not null) { Snapshot("Running"); continue; }
                    Snapshot("Running");
                    var row = await evaluate(choice.Party!, armId, token);
                    if (row is null || row.Id != choice.Party!.Id || row.Fitness is null
                        || row.Fitness != Fitness(d, row.Cells, row.Fitness.VictoryDuration) || row.Behavior is null
                        || new[] { row.Behavior.SummonActiveTicks, row.Behavior.HealthDeficit, row.Behavior.DamagePrevented,
                            row.Behavior.Healing, row.Behavior.DeniedTicks }.Any(n => !double.IsFinite(n) || n < 0))
                        throw new InvalidDataException("Invalid discovery measurement; no partial matrix can rank.");
                    var complete = proposal with { Result = "evaluated" };
                    if (lineageArm)
                    {
                        complete = complete with { Lineage = TowerPartyLineages.Describe(complete, byProposalId) };
                        byProposalId.Add(complete.Provenance.Id, complete);
                    }
                    proposals[^1] = complete; measured.Add(choice.Party.Id, complete); measurements.Add(row);
                    if (feedbackArm && TowerGenerationFeedback.Checkpoints.Contains(measurements.Count))
                    {
                        var selected = TowerGenerationFeedback.Choose(d, measurements, feedbackRounds);
                        var fresh = new List<BossDiscoveryMeasurement>();
                        foreach (var id in selected)
                        {
                            var extra = await evaluateFeedback!(measured[id].Party!, armId, token);
                            TowerGenerationFeedback.ValidateMeasurement(d, extra, id);
                            fresh.Add(extra);
                        }
                        feedbackRounds.Add(new(measurements.Count, fresh.ToArray()));
                    }
                    Snapshot("Running");
                }
                Snapshot(measurements.Count == target ? final ? "CandidateBudgetReached" : "AllocationPrefixComplete" : "ProposalBudgetExhausted");
            };
        }
        try
        {
            foreach (var seed in d.Generation.Seeds)
            {
                if (!late)
                {
                    foreach (var method in d.Generation.Methods)
                        await CreateArm(method, seed)(CandidateBudget(d.Generation, method), true);
                    continue;
                }
                await CreateArm(TowerSearchAllocation.Deep, seed)(CandidateBudget(d.Generation, TowerSearchAllocation.Deep), true);
                if (arms[^1].StopReason != "CandidateBudgetReached") break;
                if (portfolio) await CreateArm(TowerSearchPortfolio.DeepComponent, seed)(768, true);
                if (arms[^1].StopReason != "CandidateBudgetReached") break;
                var a = CreateArm(TowerSearchAllocation.IsolatedA, seed);
                await a(TowerLateAllocation.Prefix, false);
                if (arms[^1].StopReason != "AllocationPrefixComplete") break;
                var b = CreateArm(TowerSearchAllocation.IsolatedB, seed);
                await b(TowerLateAllocation.Prefix, false);
                if (arms[^1].StopReason != "AllocationPrefixComplete") break;
                var decision = TowerLateAllocation.Decide(seed, arms[^2], arms[^1]);
                decisions.Add(decision);
                var chooseA = decision.SelectedMethod == TowerSearchAllocation.IsolatedA;
                await (chooseA ? b : a)(TowerLateAllocation.Prefix, true);
                await (chooseA ? a : b)(TowerLateAllocation.Continued, true);
                if (arms[activeArm].StopReason != "CandidateBudgetReached") break;
            }
            status = arms.All(a => a.StopReason == "CandidateBudgetReached") ? "Complete" : "Incomplete";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        if (status is "Cancelled" or "Invalid" && activeArm >= 0)
            arms[activeArm] = arms[activeArm] with { StopReason = status, Proposals = arms[activeArm].Proposals.Select(p => p.Result == "evaluating" ? p with { Result = status } : p).ToArray() };
        var result = Report(select: true); checkpoint?.Invoke(result); return result;
    }

    private static string[] Explore(TowerBossPartyGenerator generator, IEnumerable<BossDiscoveryMeasurement> measurements,
        IReadOnlyDictionary<string, BossGeneratedProposal> proposals, IEnumerable<string> retained, int count)
    {
        var chosen = retained.Distinct().ToHashSet(StringComparer.Ordinal);
        var patterns = chosen.Select(id => generator.CapabilityPattern(proposals[id].Party!)).ToHashSet(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var row in Rank(measurements))
        {
            if (result.Count >= count) break;
            if (!chosen.Contains(row.Id) && patterns.Add(generator.CapabilityPattern(proposals[row.Id].Party!)))
            { chosen.Add(row.Id); result.Add(row.Id); }
        }
        // With no new capability pattern, retain distinct ordered recipes as exploration, not claimed strategies.
        foreach (var row in Rank(measurements))
        {
            if (result.Count >= count) break;
            if (!chosen.Contains(row.Id) && chosen.All(id => proposals[id].Party!.Builds.SelectMany(p => p.Value)
                    .Zip(proposals[row.Id].Party!.Builds.SelectMany(p => p.Value)).Count(p => p.First != p.Second) >= 2))
            { chosen.Add(row.Id); result.Add(row.Id); }
        }
        return result.ToArray();
    }

    internal static IReadOnlyList<PartyChoice> Shortlist(BossDiscoveryInputs d, TowerBossPartyGenerator generator, IReadOnlyList<BossGenerationArm> arms)
    {
        var proposals = arms.SelectMany(a => a.Proposals).Where(p => p.Result == "evaluated").DistinctBy(p => p.Party!.Id)
            .ToDictionary(p => p.Party!.Id, StringComparer.Ordinal);
        var rows = arms.SelectMany(a => TowerGenerationFeedback.Effective(d, a.Evaluations, a.Feedback)).DistinctBy(r => r.Id).ToArray();
        var ids = new List<string>();
        void Add(string id) { if (ids.Count < d.ShortlistCandidates && !ids.Contains(id)) ids.Add(id); }
        if (rows.Length == 0) return [];
        Add(Rank(rows).First().Id);
        foreach (var arm in arms.Where(a => a.Evaluations.Count > 0)) Add(Rank(TowerGenerationFeedback.Effective(d, arm.Evaluations, arm.Feedback)).First().Id);
        foreach (var id in Explore(generator, rows, proposals, ids, Math.Min(ExplorationSize, d.ShortlistCandidates - ids.Count))) Add(id);
        foreach (var row in Rank(rows)) Add(row.Id);
        return ids.Select(id => proposals[id].Party!).ToArray();
    }
}
