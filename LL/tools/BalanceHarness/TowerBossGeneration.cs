using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossDiscoveryFitness(double WorstContextWinRate, double GuardianHealth, double Survival, double VictoryDuration);
public sealed record BossDiscoveryMeasurement(string Id, BossDiscoveryFitness Fitness, IReadOnlyList<PartyFloorScore> Cells, BossBehavior Behavior);
public sealed record BossGeneratedProposal(BossDiscoveryProvenance Provenance, PartyChoice? Party, string Intent, string? Interaction, string Result);
public sealed record BossGenerationArm(string Method, int Seed, string StopReason,
    IReadOnlyList<BossGeneratedProposal> Proposals, IReadOnlyList<BossDiscoveryMeasurement> Evaluations);
public sealed record BossGenerationResult(string Version, string Status, IReadOnlyList<BossGenerationArm> Arms,
    IReadOnlyList<PartyChoice> DiscoveryShortlist, string? Error);

/// <summary>Independent search kernel: no benchmark references, actor identities or confirmation outcomes enter this API.</summary>
public static class TowerBossGeneration
{
    public const string Version = "independent-teams-v1";
    public const int BeamSize = 4;
    public const int ExplorationSize = 4;
    public static readonly string[] Operators = ["single", "double", "order", "cross-character", "whole-character", "recombine"];

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
        if (g is null || g.PolicyVersion != Version || g.Objective != TowerBossDiscovery.Objective || g.FreshEvery != 4
            || g.Methods is null || !g.Methods.SequenceEqual(TowerBossDiscovery.Methods) || g.Seeds is not { Count: > 0 and <= 4 }
            || g.Seeds.Distinct().Count() != g.Seeds.Count || g.CandidatesPerArm is < 1 or > 1000
            || g.MaximumAttemptsPerArm < g.CandidatesPerArm || g.MaximumAttemptsPerArm > 10000
            || d.ShortlistCandidates < g.Methods.Count * g.Seeds.Count || d.ShortlistCandidates > 64
            || d.ShortlistCandidates > g.Methods.Count * g.Seeds.Count * g.CandidatesPerArm)
            throw new InvalidDataException("Invalid bounded generation policy or shortlist allocation.");
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
        Action<BossGenerationResult>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        // Isolate caller mutation and keep the policy independent of definition/reference serialization.
        var d = JsonSerializer.Deserialize<BossDiscoveryInputs>(JsonSerializer.Serialize(inputs, HarnessJson.Options), HarnessJson.Options)!;
        ValidateInputs(d);
        if (d.Generation.PolicyVersion != Version) throw new InvalidDataException("Independent execution cannot consume a retained-build policy.");
        var generator = new TowerBossPartyGenerator(d, mechanics);
        var arms = new List<BossGenerationArm>();
        var status = "Incomplete"; string? error = null;
        BossGenerationResult Report(bool select = false) => new(Version, status, arms.ToArray(), select ? Shortlist(d, generator, arms) : [], error);
        try
        {
            foreach (var seed in d.Generation.Seeds)
            foreach (var method in d.Generation.Methods)
            {
                token.ThrowIfCancellationRequested();
                var armId = method + "-" + seed.ToString(CultureInfo.InvariantCulture);
                var random = new Random(StableRandom.Seed(Version, armId));
                var proposals = new List<BossGeneratedProposal>(); var measurements = new List<BossDiscoveryMeasurement>();
                var measured = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
                var initial = Math.Max(1, (d.Generation.CandidatesPerArm + 3) / 4);
                var refinement = 0;
                void Snapshot(string stop)
                {
                    var arm = new BossGenerationArm(method, seed, stop, proposals.ToArray(), measurements.ToArray());
                    if (arms.Count > 0 && arms[^1].Method == method && arms[^1].Seed == seed) arms[^1] = arm; else arms.Add(arm);
                    checkpoint?.Invoke(Report());
                }
                Snapshot("Running");
                for (var attempt = 0; attempt < d.Generation.MaximumAttemptsPerArm && measurements.Count < d.Generation.CandidatesPerArm; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    string operation; BossGeneratedChoice choice; string[] parents = [];
                    if (method == "random" || measurements.Count < initial)
                    {
                        operation = method == "random" ? "fresh-random" : "fresh-constructive";
                        choice = generator.Fresh(random, method != "random");
                    }
                    else
                    {
                        var turn = refinement++;
                        if (turn % d.Generation.FreshEvery == d.Generation.FreshEvery - 1)
                        { operation = "fresh-constructive"; choice = generator.Fresh(random, true); }
                        else
                        {
                            operation = Operators[(turn - turn / d.Generation.FreshEvery) % Operators.Length];
                            var beam = Rank(measurements).Take(BeamSize).Select(m => m.Id).ToArray();
                            var exploration = Explore(generator, measurements, measured, beam, ExplorationSize);
                            var choices = exploration.Length > 0 && random.Next(4) == 0 ? exploration : beam;
                            var parent = measured[choices[random.Next(choices.Length)]];
                            BossGeneratedProposal? other = null;
                            if (operation == "recombine")
                            {
                                var alternatives = beam.Concat(exploration).Distinct().Where(id => id != parent.Party!.Id).ToArray();
                                if (alternatives.Length > 0) other = measured[alternatives[random.Next(alternatives.Length)]];
                            }
                            if (operation == "recombine" && other is null)
                            { operation = "fresh-constructive"; choice = generator.Fresh(random, true); }
                            else
                            {
                                parents = other is null ? [parent.Provenance.Id] : [parent.Provenance.Id, other.Provenance.Id];
                                choice = generator.Mutate(random, operation, parent.Party!, other?.Party);
                            }
                        }
                    }
                    var provenance = new BossDiscoveryProvenance($"{armId}-proposal-{attempt:D5}", seed, method, operation, parents, []);
                    var rejection = choice.Rejection ?? (choice.Party is null ? "no-legal-proposal" : measured.ContainsKey(choice.Party.Id) ? "duplicate" : null);
                    var proposal = new BossGeneratedProposal(provenance, choice.Party, choice.Intent, choice.Interaction, rejection ?? "evaluating");
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
                    proposals[^1] = complete; measured.Add(choice.Party.Id, complete); measurements.Add(row);
                    Snapshot("Running");
                }
                Snapshot(measurements.Count == d.Generation.CandidatesPerArm ? "CandidateBudgetReached" : "ProposalBudgetExhausted");
            }
            status = arms.All(a => a.StopReason == "CandidateBudgetReached") ? "Complete" : "Incomplete";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        if (status is "Cancelled" or "Invalid" && arms.Count > 0)
            arms[^1] = arms[^1] with { StopReason = status, Proposals = arms[^1].Proposals.Select(p => p.Result == "evaluating" ? p with { Result = status } : p).ToArray() };
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
        var rows = arms.SelectMany(a => a.Evaluations).DistinctBy(r => r.Id).ToArray();
        var ids = new List<string>();
        void Add(string id) { if (ids.Count < d.ShortlistCandidates && !ids.Contains(id)) ids.Add(id); }
        if (rows.Length == 0) return [];
        Add(Rank(rows).First().Id);
        foreach (var arm in arms.Where(a => a.Evaluations.Count > 0)) Add(Rank(arm.Evaluations).First().Id);
        foreach (var id in Explore(generator, rows, proposals, ids, Math.Min(ExplorationSize, d.ShortlistCandidates - ids.Count))) Add(id);
        foreach (var row in Rank(rows)) Add(row.Id);
        return ids.Select(id => proposals[id].Party!).ToArray();
    }
}
