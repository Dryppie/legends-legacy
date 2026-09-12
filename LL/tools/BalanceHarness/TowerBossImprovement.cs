using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

/// <summary>Explicit reference-derived search; independent generation never receives these starts.</summary>
public static class TowerBossImprovement
{
    public const string Version = "retained-teams-v1";
    public static readonly string[] Methods = ["retained-local", "retained-joint"];

    public static TowerBossDiscoveryDefinition Prepare(TowerBossDiscoveryDefinition source, IReadOnlyList<string> referenceIds)
    {
        TowerBossDiscovery.Validate(source);
        if (source.Mode != TowerBossDiscovery.Independent || referenceIds is not { Count: > 0 and <= 64 }
            || referenceIds.Distinct().Count() != referenceIds.Count || referenceIds.Except(source.References.Select(r => r.Id)).Any())
            throw new InvalidDataException("Choose distinct existing reference IDs from a fresh independent definition.");
        var starts = referenceIds.Order(StringComparer.Ordinal).Select(id => {
            var reference = source.References.Single(r => r.Id == id);
            return new BossDiscoveryStart("start-" + HarnessJson.Hash(id)[..24], id,
                TowerPartySelection.Choice("supplied", reference.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)));
        }).ToArray();
        var result = source with { Mode = TowerBossDiscovery.Improve, Starts = starts,
            Generation = source.Generation with { PolicyVersion = Version, Methods = Methods } };
        TowerBossDiscovery.Validate(result);
        return result;
    }

    // The common mutation budget contains no reference recipes. Starts travel separately,
    // in the definition and improvement-starts.json, and only enter this search kernel.
    public static BossDiscoveryInputs Inputs(TowerBossDiscoveryDefinition d)
    {
        TowerBossDiscovery.Validate(d);
        if (d.Mode == TowerBossDiscovery.Independent) return TowerBossDiscovery.GenerationInputs(d);
        if (d.Generation.PolicyVersion != Version)
            throw new InvalidDataException("Legacy improve-supplied definitions are not executable. Prepare the retained-teams-v1 policy explicitly.");
        return TowerBossDiscovery.GenerationInputs(d with { Mode = TowerBossDiscovery.Independent, Starts = [],
            Generation = d.Generation with { PolicyVersion = TowerBossGeneration.Version, Methods = TowerBossDiscovery.Methods } })
            with { Generation = d.Generation };
    }

    internal static string Algorithm(TowerBossDiscoveryDefinition d) => TowerBossDiscovery.Version + "/" + d.Generation.PolicyVersion;

    internal static Task<BossGenerationResult> ExecuteAsync(TowerBossDiscoveryDefinition d, BossDiscoveryInputs inputs,
        BossGenerationMechanics mechanics, Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>> evaluate,
        CancellationToken token, Action<BossGenerationResult>? checkpoint = null) => d.Mode == TowerBossDiscovery.Independent
        ? TowerBossGeneration.RunAsync(inputs, mechanics, evaluate, token, checkpoint)
        : RunAsync(d, mechanics, evaluate, token, checkpoint);

    public static async Task<BossGenerationResult> RunAsync(TowerBossDiscoveryDefinition definition, BossGenerationMechanics mechanics,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>> evaluate,
        CancellationToken token = default, Action<BossGenerationResult>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        if (d.Mode != TowerBossDiscovery.Improve || d.Generation.PolicyVersion != Version)
            throw new InvalidDataException("Retained-build execution requires its explicitly versioned improve-supplied definition.");
        var input = Inputs(d);
        var generator = new TowerBossPartyGenerator(input, mechanics);
        var arms = new List<BossGenerationArm>(); var status = "Incomplete"; string? error = null;
        BossGenerationResult Report(bool select = false) => new(Version, status, arms.ToArray(),
            select ? TowerBossGeneration.Shortlist(input, generator, arms) : [], error);
        try
        {
            foreach (var seed in d.Generation.Seeds)
            foreach (var method in d.Generation.Methods)
            {
                token.ThrowIfCancellationRequested();
                var armId = method + "-" + seed.ToString(CultureInfo.InvariantCulture);
                var random = new Random(StableRandom.Seed(Version, armId));
                var proposals = new List<BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
                var measured = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
                var scans = new Dictionary<string, Neighborhood>(StringComparer.Ordinal);
                void Snapshot(string stop)
                {
                    var arm = new BossGenerationArm(method, seed, stop, proposals.ToArray(), rows.ToArray());
                    if (arms.Count > 0 && arms[^1].Method == method && arms[^1].Seed == seed) arms[^1] = arm; else arms.Add(arm);
                    checkpoint?.Invoke(Report());
                }
                Snapshot("Running");
                var turn = 0;
                for (var attempt = 0; attempt < d.Generation.MaximumAttemptsPerArm && rows.Count < d.Generation.CandidatesPerArm; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    string operation; string[] parents = [], references = []; BossGeneratedChoice choice;
                    if (attempt < d.Starts.Count)
                    {
                        var start = d.Starts.OrderBy(s => s.Id, StringComparer.Ordinal).ElementAt(attempt);
                        operation = "supplied"; parents = [start.Id]; references = [start.ReferenceId];
                        choice = new(start.Party, "supplied-baseline", null, null);
                    }
                    else if (turn++ % d.Generation.FreshEvery == d.Generation.FreshEvery - 1)
                    {
                        operation = "fresh-constructive"; choice = generator.Fresh(random, true);
                    }
                    else
                    {
                        var beam = TowerBossGeneration.Rank(rows).Take(TowerBossGeneration.BeamSize).Select(r => measured[r.Id]).ToArray();
                        var anchors = measured.Values.Where(p => p.Provenance.Operator == "supplied").ToArray();
                        var choices = random.Next(4) == 0 ? anchors : beam;
                        var parent = choices[random.Next(choices.Length)];
                        var opIndex = (turn - 1 - (turn - 1) / d.Generation.FreshEvery) % TowerBossGeneration.Operators.Length;
                        operation = method == "retained-local" || opIndex == 0 ? "local" : TowerBossGeneration.Operators[opIndex];
                        BossGeneratedProposal? other = null;
                        if (operation == "recombine")
                        {
                            var alternatives = beam.Concat(anchors).DistinctBy(p => p.Party!.Id).Where(p => p.Party!.Id != parent.Party!.Id).ToArray();
                            if (alternatives.Length > 0) other = alternatives[random.Next(alternatives.Length)];
                            else operation = "local";
                        }
                        parents = other is null ? [parent.Provenance.Id] : [parent.Provenance.Id, other.Provenance.Id];
                        references = parent.Provenance.ReferenceIds.Concat(other?.Provenance.ReferenceIds ?? [])
                            .Distinct().Order(StringComparer.Ordinal).ToArray();
                        if (operation == "local")
                        {
                            if (!scans.TryGetValue(parent.Party!.Id, out var scan))
                                scans.Add(parent.Party.Id, scan = new(input, parent.Party, random));
                            (operation, choice) = scan.Next(generator);
                        }
                        else choice = generator.Mutate(random, operation, parent.Party!, other?.Party);
                    }
                    var provenance = new BossDiscoveryProvenance($"{armId}-proposal-{attempt:D5}", seed, method, operation, parents, references);
                    var rejection = choice.Rejection ?? (choice.Party is null ? "no-legal-proposal" : measured.ContainsKey(choice.Party.Id) ? "duplicate" : null);
                    proposals.Add(new(provenance, choice.Party, choice.Intent, choice.Interaction, rejection ?? "evaluating"));
                    Snapshot("Running");
                    if (rejection is not null) continue;
                    var row = await evaluate(choice.Party!, armId, token);
                    if (row is null || row.Id != choice.Party!.Id || row.Fitness is null
                        || row.Fitness != TowerBossGeneration.Fitness(input, row.Cells, row.Fitness.VictoryDuration) || row.Behavior is null
                        || new[] { row.Behavior.SummonActiveTicks, row.Behavior.HealthDeficit, row.Behavior.DamagePrevented,
                            row.Behavior.Healing, row.Behavior.DeniedTicks }.Any(n => !double.IsFinite(n) || n < 0))
                        throw new InvalidDataException("Incomplete or invalid improvement measurement.");
                    var complete = proposals[^1] with { Result = "evaluated" };
                    proposals[^1] = complete; measured.Add(choice.Party.Id, complete); rows.Add(row);
                    Snapshot("Running");
                }
                Snapshot(rows.Count == d.Generation.CandidatesPerArm ? "CandidateBudgetReached" : "ProposalBudgetExhausted");
            }
            status = arms.All(a => a.StopReason == "CandidateBudgetReached") ? "Complete" : "Incomplete";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        if (status is "Cancelled" or "Invalid" && arms.Count > 0)
            arms[^1] = arms[^1] with { StopReason = status, Proposals = arms[^1].Proposals.Select(p => p.Result == "evaluating" ? p with { Result = status } : p).ToArray() };
        TowerBossDiscovery.ValidateProvenance(d, arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var result = Report(select: true); checkpoint?.Invoke(result); return result;
    }

    // A seeded permutation visits each single substitution and pairwise order swap
    // once per parent without allocating an unbounded neighborhood or retry loop.
    private sealed class Neighborhood
    {
        private readonly BossDiscoveryInputs input;
        private readonly PartyChoice parent;
        private readonly string[] pool;
        private readonly int singles, total, offset, step;
        private int visited;
        public Neighborhood(BossDiscoveryInputs input, PartyChoice parent, Random random)
        {
            this.input = input; this.parent = parent; pool = input.AllowedEssences.Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
            singles = input.RequiredPartySize * input.Budget.EssenceSlots * pool.Length;
            total = singles + input.RequiredPartySize * input.Budget.EssenceSlots * (input.Budget.EssenceSlots - 1) / 2;
            offset = random.Next(total); step = random.Next(1, total);
            while (System.Numerics.BigInteger.GreatestCommonDivisor(step, total) != 1) step = step == total - 1 ? 1 : step + 1;
        }
        public (string Operation, BossGeneratedChoice Choice) Next(TowerBossPartyGenerator generator)
        {
            if (visited >= total) return ("single", new(null, "local-neighborhood", null, "neighborhood-exhausted"));
            var index = (int)((offset + (long)step * visited++) % total);
            var builds = parent.Builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
            var k = input.Budget.EssenceSlots;
            var operation = index < singles ? "single" : "order";
            if (index < singles)
            {
                var position = index / pool.Length; var ids = builds[position / k + 1].ToArray();
                ids[position % k] = pool[index % pool.Length]; builds[position / k + 1] = ids;
            }
            else
            {
                index -= singles; var pairs = k * (k - 1) / 2; var slot = index / pairs + 1; var pair = index % pairs;
                var left = 0; while (pair >= k - left - 1) pair -= k - left++ - 1;
                var ids = builds[slot].ToArray(); var right = left + 1 + pair;
                (ids[left], ids[right]) = (ids[right], ids[left]); builds[slot] = ids;
            }
            var party = TowerPartySelection.Choice("local-neighborhood", builds);
            return (operation, new(party, "local-neighborhood", null, generator.Invalid(party)));
        }
    }
}
