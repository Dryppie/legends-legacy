using System.Globalization;
using System.Text.Json;
using Common.Randomness;

namespace BalanceHarness;

public sealed record BossSuppliedSearchTrace(IReadOnlyList<string> PopulationIds,
    IReadOnlyList<int> ChangedSlots, int MaximumConstructionChecks);

/// <summary>Explicit supplied knowledge, canonical composition, and bounded complete-party search.</summary>
public static class TowerSuppliedCompositionSearch
{
    public const string Version = "supplied-composition-block-v1";
    public const string ScheduledVersion = "supplied-composition-block-v2";
    public const string StandaloneVersion = "retained-composition-v1";
    public const string IncumbentVersion = "retained-composition-incumbents-v1";
    public static bool IsSupported(string? version) => version is Version or ScheduledVersion or StandaloneVersion or IncumbentVersion;
    public const string Baseline = "retained-composition", Block = "supplied-block";
    public static readonly string[] Methods = [Baseline, Block];
    private static IReadOnlyList<string> MethodsFor(string policyVersion) => policyVersion is StandaloneVersion or IncumbentVersion ? [Baseline] : Methods;
    public const int ConstructionChecks = 32;
    private static readonly string[] BaselineOperators = ["single", "double", "cross-character", "whole-character", "recombine"];
    private static readonly string[] BlockOperators = ["essence-block", "character-block", "donor-block"];

    public static TowerBossDiscoveryDefinition Prepare(TowerBossDiscoveryDefinition source, IReadOnlyList<string> referenceIds,
        string policyVersion = Version)
    {
        if (!IsSupported(policyVersion)) throw new InvalidDataException("Unknown supplied composition policy.");
        TowerBossDiscovery.Validate(source);
        if (source.Mode != TowerBossDiscovery.Independent || referenceIds is not { Count: >= 1 and <= 2 }
            || referenceIds.Distinct(StringComparer.Ordinal).Count() != referenceIds.Count
            || !referenceIds.Order(StringComparer.Ordinal).SequenceEqual(source.References.Select(r => r.Id).Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Declare exactly the one or two supplied references; no implicit archive import or control removal.");
        // A new definition owns the canonical recipes; the source and its evidence remain intact.
        var references = source.References.Select(r => r with { Scenario = r.Scenario with {
            Party = r.Scenario.Party.OrderBy(p => p.PartySlot).Select(p => p with { Build = p.Build with {
                EssenceIds = p.Build.EssenceIds.Order(StringComparer.Ordinal).ToArray() } }).ToArray() },
            Source = r.Source + "; canonical supplied composition: historical fitness is not a current measurement" }).ToArray();
        var starts = references.OrderBy(r => r.Id, StringComparer.Ordinal).Select(r => new BossDiscoveryStart(
            "start-" + HarnessJson.Hash(r.Id)[..24], r.Id, TowerPartySelection.Choice("supplied",
                r.Scenario.Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds)))).ToArray();
        var result = source with { Mode = TowerBossDiscovery.Improve, References = references, Starts = starts,
            Generation = source.Generation with { PolicyVersion = policyVersion, Methods = MethodsFor(policyVersion) },
            Stages = source.Stages with { SelectionPolicyVersion = TowerBossStudyPolicy.ZeroWinVersion } };
        TowerBossDiscovery.Validate(result);
        return result;
    }

    internal static bool ValidGeneration(BossDiscoveryGeneration g) => IsSupported(g.PolicyVersion) && g.Methods is not null
        && g.Methods.SequenceEqual(MethodsFor(g.PolicyVersion)) && g.CandidatesPerArm is >= 8 and <= 64
        && g.MaximumAttemptsPerArm >= g.CandidatesPerArm && g.MaximumAttemptsPerArm <= 256;

    internal static int ParentCount(BossDiscoveryProvenance p) => p.Operator switch {
        "supplied" => 1, "fresh-legal" => 0,
        "essence-block" or "character-block" when p.Method == Block => 1,
        "donor-block" when p.Method == Block => p.ParentIds.Count is 1 or 2 ? p.ParentIds.Count : -1,
        "single" or "double" or "cross-character" or "whole-character" when p.Method == Baseline => 1,
        "recombine" when p.Method == Baseline => 2,
        _ => -1
    };

    internal static int Distance(PartyChoice a, PartyChoice b) => a.Builds.Sum(p =>
        p.Value.Except(b.Builds[p.Key], StringComparer.Ordinal).Count()
        + b.Builds[p.Key].Except(p.Value, StringComparer.Ordinal).Count());

    internal static string[] Retain(IEnumerable<BossDiscoveryMeasurement> rows,
        IReadOnlyDictionary<string, BossGeneratedProposal> measured)
    {
        var ranked = TowerBossGeneration.Rank(rows).ToArray();
        var ids = ranked.Take(4).Select(r => r.Id).ToList();
        while (ids.Count < 8 && ids.Count < ranked.Length)
        {
            // Rank enumeration supplies fitness/ID tie breaks, without fitting distance to outcomes.
            var next = ranked.Where(r => !ids.Contains(r.Id)).OrderByDescending(r => ids.Min(id =>
                Distance(measured[r.Id].Party!, measured[id].Party!))).First();
            ids.Add(next.Id);
        }
        return ids.ToArray();
    }

    // Keep the existing counter coupling visible and testable. In particular, a
    // fixed nine-parent population does not receive every operator per parent.
    // Changing that behavior needs a separately versioned search contract.
    internal static (string ParentId, string Operator, int OperatorOrdinal) PlanBlockMutation(
        IEnumerable<string> population, IEnumerable<string> anchors, int mutation)
    {
        var choices = population.Concat(anchors).Distinct().ToArray();
        return (choices[mutation % choices.Length], BlockOperators[mutation % BlockOperators.Length], mutation / 3);
    }

    // One instance per arm. Visits survive retention changes and advance before
    // proposal construction, including rejected/duplicate outcomes. At most the
    // arm's 64 evaluated parties can become keys.
    internal sealed class BlockSchedule
    {
        private readonly Dictionary<string, int> visits = new(StringComparer.Ordinal);
        internal (string ParentId, string Operator, int OperatorOrdinal) Next(
            IEnumerable<string> population, IEnumerable<string> anchors, int mutation)
        {
            var choices = population.Concat(anchors).Distinct(StringComparer.Ordinal).ToArray();
            var parent = choices[mutation % choices.Length];
            var visit = visits.GetValueOrDefault(parent);
            visits[parent] = visit + 1;
            return (parent, BlockOperators[visit % BlockOperators.Length], visit / BlockOperators.Length);
        }
    }

    public static async Task<BossGenerationResult> RunAsync(TowerBossDiscoveryDefinition definition, BossGenerationMechanics mechanics,
        Func<PartyChoice, string, CancellationToken, Task<BossDiscoveryMeasurement>> evaluate,
        CancellationToken token = default, Action<BossGenerationResult>? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(evaluate);
        var d = JsonSerializer.Deserialize<TowerBossDiscoveryDefinition>(JsonSerializer.Serialize(definition, HarnessJson.Options), HarnessJson.Options)!;
        TowerBossDiscovery.Validate(d);
        if (d.Mode != TowerBossDiscovery.Improve || !IsSupported(d.Generation.PolicyVersion))
            throw new InvalidDataException("Supplied composition search requires its explicit improve-supplied contract.");
        var input = TowerBossImprovement.Inputs(d);
        var generator = new TowerBossPartyGenerator(input, mechanics);
        var arms = new List<BossGenerationArm>(); var status = "Incomplete"; string? error = null;
        PartyChoice[]? incumbentShortlist = null;
        BossGenerationResult Report() => new(d.Generation.PolicyVersion, status, arms.ToArray(),
            status == "Complete" ? incumbentShortlist ?? Shortlist(input, arms) : [], error);
        try
        {
            foreach (var seed in d.Generation.Seeds)
            foreach (var method in d.Generation.Methods)
            {
                var seedText = seed.ToString(CultureInfo.InvariantCulture);
                var armId = method + "-" + seedText;
                // Preserve the v1 stream namespace in v2: only block scheduling changes.
                // Equal fresh streams regardless of mutation outcomes or random draws in either arm.
                var fresh = new Random(StableRandom.Seed(Version, seedText, "fresh"));
                var random = new Random(StableRandom.Seed(Version, seedText, method));
                var proposals = new List<BossGeneratedProposal>(); var rows = new List<BossDiscoveryMeasurement>();
                var measured = new Dictionary<string, BossGeneratedProposal>(StringComparer.Ordinal);
                var scans = new Dictionary<string, SingleNeighborhood>(StringComparer.Ordinal);
                var schedule = new BlockSchedule();
                var starts = d.Starts.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
                var armIndex = arms.Count;
                void Snapshot(string stop)
                {
                    var arm = new BossGenerationArm(method, seed, stop, proposals.ToArray(), rows.ToArray());
                    if (arms.Count == armIndex) arms.Add(arm); else arms[armIndex] = arm;
                    checkpoint?.Invoke(Report());
                }
                Snapshot("Running");
                for (var attempt = 0; attempt < d.Generation.MaximumAttemptsPerArm && rows.Count < d.Generation.CandidatesPerArm; attempt++)
                {
                    token.ThrowIfCancellationRequested();
                    string operation; string[] parents = [], references = [], population = [];
                    BossGeneratedChoice choice; BossGeneratedProposal? parent = null;
                    var initial = starts.Length + 6;
                    var turn = attempt - initial;
                    if (attempt < starts.Length)
                    {
                        var start = starts[attempt]; operation = "supplied";
                        parents = [start.Id]; references = [start.ReferenceId];
                        choice = new(start.Party, operation, null, null);
                    }
                    else if (attempt < initial || turn % 4 == 3 || rows.Count == 0)
                    { operation = "fresh-legal"; choice = Fresh(input, fresh); }
                    else
                    {
                        var ranked = TowerBossGeneration.Rank(rows).Select(r => r.Id).ToArray();
                        var anchors = measured.Values.Where(p => p.Provenance.Operator == "supplied").Select(p => p.Party!.Id).ToArray();
                        population = method == Block ? Retain(rows, measured) : ranked.Take(4).ToArray();
                        var mutation = turn - turn / 4;
                        var operatorOrdinal = mutation / 3;
                        if (method == Block)
                        {
                            var step = d.Generation.PolicyVersion == ScheduledVersion
                                ? schedule.Next(population, anchors, mutation)
                                : PlanBlockMutation(population, anchors, mutation);
                            parent = measured[step.ParentId]; operation = step.Operator; operatorOrdinal = step.OperatorOrdinal;
                        }
                        else
                        {
                            var choices = random.Next(4) == 0 && anchors.Length > 0 ? anchors : population;
                            parent = measured[choices[random.Next(choices.Length)]];
                            operation = BaselineOperators[mutation % BaselineOperators.Length];
                        }
                        BossGeneratedProposal? donor = null;
                        if (operation is "donor-block" or "recombine")
                        {
                            var alternatives = population.Concat(anchors).Distinct().Where(id => id != parent.Party!.Id).ToArray();
                            if (alternatives.Length > 0) donor = measured[alternatives[random.Next(alternatives.Length)]];
                            else if (method == Baseline) operation = "single";
                        }
                        parents = donor is null ? [parent.Provenance.Id] : [parent.Provenance.Id, donor.Provenance.Id];
                        references = parent.Provenance.ReferenceIds.Concat(donor?.Provenance.ReferenceIds ?? []).Distinct().Order(StringComparer.Ordinal).ToArray();
                        if (method == Block) choice = Propose(input, parent.Party!, donor?.Party, operation, operatorOrdinal, random);
                        else if (operation == "single")
                        {
                            if (!scans.TryGetValue(parent.Party!.Id, out var scan)) scans.Add(parent.Party.Id, scan = new(input, parent.Party, random));
                            choice = scan.Next(input);
                        }
                        else choice = generator.Mutate(random, operation, parent.Party!, donor?.Party);
                    }
                    var provenance = new BossDiscoveryProvenance($"{armId}-proposal-{attempt:D5}", seed, method, operation, parents, references);
                    var rejection = choice.Rejection ?? (choice.Party is null ? "no-legal-proposal" : measured.ContainsKey(choice.Party.Id) ? "duplicate" : null);
                    var changed = choice.Party?.Builds.Where(p => parent is null || !p.Value.SequenceEqual(parent.Party!.Builds[p.Key]))
                        .Select(p => p.Key).Order().ToArray() ?? [];
                    proposals.Add(new(provenance, choice.Party, choice.Intent, choice.Interaction, rejection ?? "evaluating",
                        Supplied: new(population, changed, ConstructionChecks)));
                    Snapshot("Running");
                    if (rejection is not null) continue;
                    var row = await evaluate(choice.Party!, armId, token);
                    if (row is null || row.Id != choice.Party!.Id || row.Fitness is null
                        || row.Fitness != TowerBossGeneration.Fitness(input, row.Cells, row.Fitness.VictoryDuration)
                        || row.Behavior is null || new[] { row.Behavior.HealthDeficit, row.Behavior.Healing,
                            row.Behavior.DamagePrevented, row.Behavior.DeniedTicks, row.Behavior.SummonActiveTicks }.Any(v => !double.IsFinite(v) || v < 0))
                        throw new InvalidDataException("Supplied search requires a complete finite discovery measurement.");
                    var completed = proposals[^1] with { Result = "evaluated" };
                    proposals[^1] = completed; measured.Add(row.Id, completed); rows.Add(row); Snapshot("Running");
                }
                Snapshot(rows.Count == d.Generation.CandidatesPerArm ? "CandidateBudgetReached" : "ProposalBudgetExhausted");
            }
            status = arms.All(a => a.StopReason == "CandidateBudgetReached") ? "Complete" : "Incomplete";
            if (status == "Complete" && d.Generation.PolicyVersion == IncumbentVersion)
            {
                incumbentShortlist = IncumbentShortlist(d, arms);
                if (incumbentShortlist.Length != d.Stages.Shortlist) status = "Incomplete";
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { status = "Cancelled"; }
        catch (Exception exception) { status = "Invalid"; error = exception.GetType().Name + ": " + exception.Message; }
        if (status is "Cancelled" or "Invalid" && arms.Count > 0)
            arms[^1] = arms[^1] with { StopReason = status, Proposals = arms[^1].Proposals.Select(p => p.Result == "evaluating" ? p with { Result = status } : p).ToArray() };
        TowerBossDiscovery.ValidateProvenance(d, arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var result = Report(); checkpoint?.Invoke(result); return result;
    }

    internal static PartyChoice[] IncumbentShortlist(TowerBossDiscoveryDefinition d, IReadOnlyList<BossGenerationArm> arms)
    {
        TowerBossDiscovery.Validate(d);
        if (d.Generation.PolicyVersion != IncumbentVersion || arms.Count != 1)
            throw new InvalidDataException("Incumbent nomination requires its single-arm contract.");
        var arm = arms[0];
        var measured = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id, StringComparer.Ordinal);
        var ranked = TowerBossGeneration.Rank(arm.Evaluations).ToArray();
        var rows = ranked.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var inputs = TowerBossImprovement.Inputs(d);
        foreach (var start in d.Starts)
        {
            if (!measured.TryGetValue(start.Party.Id, out var proposal)
                || proposal.Provenance.Operator != "supplied"
                || !proposal.Provenance.ParentIds.SequenceEqual(new[] { start.Id })
                || !proposal.Provenance.ReferenceIds.SequenceEqual(new[] { start.ReferenceId })
                || !rows.TryGetValue(start.Party.Id, out var row)
                || row.Fitness != TowerBossGeneration.Fitness(inputs, row.Cells, row.Fitness.VictoryDuration))
                throw new InvalidDataException("Every incumbent requires its exact evaluated supplied proposal and complete measurement.");
        }
        if (!rows.Keys.Order(StringComparer.Ordinal).SequenceEqual(measured.Keys.Order(StringComparer.Ordinal)))
            throw new InvalidDataException("Nomination requires matching evaluated proposals and measurements.");
        var ids = d.Starts.Select(s => s.Party.Id).ToHashSet(StringComparer.Ordinal);
        var challengers = ranked.Where(r => !ids.Contains(r.Id)).Take(2).ToArray();
        if (challengers.Length != 2) return [];
        ids.UnionWith(challengers.Select(r => r.Id));
        // Membership is protected; existing discovery rank still determines selection ties.
        return ranked.Where(r => ids.Contains(r.Id)).Select(r => measured[r.Id].Party!).ToArray();
    }

    internal static PartyChoice[] Shortlist(BossDiscoveryInputs input, IReadOnlyList<BossGenerationArm> arms)
    {
        var parties = arms.SelectMany(a => a.Proposals).Where(p => p.Result == "evaluated").DistinctBy(p => p.Party!.Id).ToDictionary(p => p.Party!.Id);
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var queues = arms.Select(a => {
            var own = a.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            var top = TowerBossGeneration.Rank(a.Evaluations).Take(2).Select(r => r.Id);
            return new Queue<string>(top.Concat(Retain(a.Evaluations, own).Skip(4).Take(2))
                .Concat(TowerBossGeneration.Rank(a.Evaluations).Select(r => r.Id)).Distinct());
        }).ToArray();
        while (selected.Count < input.ShortlistCandidates && queues.Any(q => q.Count > 0))
            foreach (var queue in queues)
            {
                while (queue.Count > 0 && selected.Contains(queue.Peek())) queue.Dequeue();
                if (queue.Count > 0 && selected.Count < input.ShortlistCandidates) selected.Add(queue.Dequeue());
            }
        // Freeze discovery rank as shortlist order for the optional staged zero-win selector.
        return TowerBossGeneration.Rank(arms.SelectMany(a => a.Evaluations).DistinctBy(r => r.Id)).Where(r => selected.Contains(r.Id))
            .Select(r => parties[r.Id].Party!).ToArray();
    }

    private sealed class SingleNeighborhood
    {
        private readonly PartyChoice parent;
        private readonly string[] pool;
        private readonly int total, offset, step;
        private int visited;
        internal SingleNeighborhood(BossDiscoveryInputs input, PartyChoice parent, Random random)
        {
            this.parent = parent; pool = input.AllowedEssences.Select(e => e.Id).Order(StringComparer.Ordinal).ToArray();
            total = input.RequiredPartySize * input.Budget.EssenceSlots * pool.Length;
            offset = random.Next(total); step = random.Next(1, total);
            while (System.Numerics.BigInteger.GreatestCommonDivisor(step, total) != 1) step = step == total - 1 ? 1 : step + 1;
        }
        internal BossGeneratedChoice Next(BossDiscoveryInputs input)
        {
            if (visited == total) return new(null, "single", null, "neighborhood-exhausted");
            var index = (int)((offset + (long)step * visited++) % total);
            var position = index / pool.Length;
            var builds = parent.Builds.ToDictionary(p => p.Key, p => p.Value.ToArray());
            builds[position / input.Budget.EssenceSlots + 1][position % input.Budget.EssenceSlots] = pool[index % pool.Length];
            return Choice(input, builds, "single");
        }
    }

    internal static BossGeneratedChoice Fresh(BossDiscoveryInputs input, Random random)
    {
        for (var check = 0; check < ConstructionChecks; check++)
        {
            var builds = new Dictionary<int, string[]>();
            for (var slot = 1; slot <= input.RequiredPartySize; slot++)
            {
                var recipe = Sample(input, random, builds, []);
                if (recipe is null) break;
                builds.Add(slot, recipe);
            }
            if (builds.Count == input.RequiredPartySize) return Choice(input, builds, "fresh-legal");
        }
        return new(null, "fresh-legal", null, "construction-exhausted");
    }

    internal static BossGeneratedChoice Propose(BossDiscoveryInputs input, PartyChoice parent, PartyChoice? donor,
        string operation, int ordinal, Random random)
    {
        if (operation is not ("essence-block" or "character-block" or "donor-block"))
            throw new InvalidDataException("Unknown supplied block operator.");
        if (operation == "donor-block" && (donor is null || donor.Id == parent.Id)) return new(null, operation, null, "needs-distinct-donor");
        for (var check = 0; check < ConstructionChecks; check++)
        {
            var builds = parent.Builds.ToDictionary(p => p.Key, p => p.Value.ToArray());
            var slots = builds.Keys.Order().ToArray();
            if (operation == "essence-block")
            {
                var slot = slots[random.Next(slots.Length)]; var old = builds[slot];
                var count = Math.Min(old.Length, 2 + ordinal % 2);
                var positions = Enumerable.Range(0, old.Length).ToArray(); random.Shuffle(positions);
                var kept = old.Where((_, i) => !positions.Take(count).Contains(i)).ToArray();
                builds.Remove(slot);
                var recipe = Sample(input, random, builds, kept);
                if (recipe is null || old.Except(recipe).Count() != count) continue;
                builds[slot] = recipe;
            }
            else if (operation == "character-block")
            {
                // Alternate within/across production five-player subgroup boundaries when available.
                var pairs = (from a in slots from b in slots where a < b select (a, b)).ToArray();
                if (pairs.Length == 0) return new(null, operation, null, "needs-two-characters");
                var preferred = pairs.Where(p => ((p.a - 1) / 5 == (p.b - 1) / 5) == (ordinal % 2 == 0)).ToArray();
                var pair = (preferred.Length == 0 ? pairs : preferred)[random.Next(preferred.Length == 0 ? pairs.Length : preferred.Length)];
                builds.Remove(pair.a); builds.Remove(pair.b);
                var first = Sample(input, random, builds, []);
                if (first is null) continue;
                builds[pair.a] = first;
                var second = Sample(input, random, builds, []);
                if (second is null) continue;
                builds[pair.b] = second;
                if (first.Order(StringComparer.Ordinal).SequenceEqual(parent.Builds[pair.a])
                    || second.Order(StringComparer.Ordinal).SequenceEqual(parent.Builds[pair.b])) continue;
            }
            else
            {
                random.Shuffle(slots);
                var count = Math.Min(slots.Length, new[] { 2, 5, slots.Length }[ordinal % 3]);
                if (count < 2) return new(null, operation, null, "needs-two-characters");
                // At least one changed owner from each parent survives. Full-block copying cannot qualify.
                var block = slots.Take(count).ToArray();
                var split = random.Next(1, count);
                foreach (var slot in block.Take(split)) builds[slot] = donor!.Builds[slot].ToArray();
                if (!block.Take(split).Any(s => !parent.Builds[s].SequenceEqual(donor!.Builds[s]))
                    || !block.Skip(split).Any(s => !parent.Builds[s].SequenceEqual(donor!.Builds[s]))) continue;
            }
            var choice = Choice(input, builds, operation);
            if (choice.Rejection is null && choice.Party!.Id != parent.Id && choice.Party.Id != donor?.Id) return choice;
        }
        return new(null, operation, null, "construction-exhausted");
    }

    private static string[]? Sample(BossDiscoveryInputs input, Random random, Dictionary<int, string[]> others, string[] prefix)
    {
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var selected = prefix.ToList();
        var used = others.Values.SelectMany(x => x).Concat(prefix).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var available = input.AllowedEssences.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray(); random.Shuffle(available);
        foreach (var essence in available)
        {
            if (selected.Count == input.Budget.EssenceSlots) break;
            if (selected.Any(id => StringComparer.OrdinalIgnoreCase.Equals(families[id], essence.Family))
                || input.OwnedCopies is not null && used.GetValueOrDefault(essence.Id) >= input.OwnedCopies.GetValueOrDefault(essence.Id)) continue;
            selected.Add(essence.Id); used[essence.Id] = used.GetValueOrDefault(essence.Id) + 1;
        }
        return selected.Count == input.Budget.EssenceSlots ? selected.Order(StringComparer.Ordinal).ToArray() : null;
    }

    private static BossGeneratedChoice Choice(BossDiscoveryInputs input, Dictionary<int, string[]> builds, string operation)
    {
        var party = TowerPartySelection.Choice("supplied-composition", TowerCompositionSearch.CanonicalBuilds(
            builds.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value)));
        var families = input.AllowedEssences.ToDictionary(e => e.Id, e => e.Family, StringComparer.Ordinal);
        var reason = !party.Builds.Keys.SequenceEqual(Enumerable.Range(1, input.RequiredPartySize)) ? "invalid-party-slots"
            : party.Builds.Values.Any(ids => ids.Count != input.Budget.EssenceSlots || ids.Any(id => !families.ContainsKey(id))) ? "invalid-pool-or-count"
            : party.Builds.Values.Any(ids => ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count) ? "duplicate-family"
            : input.OwnedCopies is not null && party.Builds.Values.SelectMany(x => x).GroupBy(x => x).Any(g => g.Count() > input.OwnedCopies.GetValueOrDefault(g.Key)) ? "owned-copies-exceeded" : null;
        return new(party, operation, null, reason);
    }
}
