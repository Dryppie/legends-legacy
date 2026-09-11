using System.Text.Json.Serialization;

namespace BalanceHarness;

public sealed record BossFitness(double PrimaryGain, double GuardianHealth, double Survival, double VictoryDuration);

/// <summary>
/// Means over target encounters: hostile summon-active ticks (presence, not attributed kills),
/// sampled initial-party health deficit ratio, accounted friendly damage prevention, reported
/// friendly healing, and hostile action-denied ticks. Healing does not establish deaths prevented;
/// prevention and denial do not establish which Essence caused them. These are diagnostics only.
/// </summary>
public sealed record BossRecovery(double FriendlyRegeneration, double GuardianHealing, double GuardianRegeneration);
public sealed record BossBehavior(double SummonActiveTicks, double HealthDeficit, double DamagePrevented,
    double Healing, double DeniedTicks,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BossRecovery? Recovery = null);
public sealed record BossMeasurement(string Id, BossFitness Fitness, IReadOnlyList<PartyFloorScore> Cells, BossBehavior Behavior);
public sealed record BossStrategy(string Intent, string Id);
public sealed record BossProposal(int Attempt, string Origin, string? Parent, PartyChoice Party, string Result);
public sealed record BossSearchResult(string Method, int Seed, string StopReason, IReadOnlyList<PartyChoice> Parties,
    IReadOnlyList<BossMeasurement> Evaluations, IReadOnlyList<BossProposal> Proposals);

/// <summary>Separate boss objective and bounded complete-party proposals; does not alter the historical generalist policy.</summary>
public static class TowerBossOptimization
{
    public const string Version = "tower-boss-objective-v1";
    public static readonly string[] Methods = ["random", "legacy", "joint", "graph"];

    // PrimaryGain is the minimum across declared contexts of weighted target-floor paired rate gain.
    // Non-target floors are transfer evidence and must not influence even the progress tie-breaks.
    public static BossFitness Fitness(IReadOnlyList<PartyFloorScore> cells, IReadOnlyList<PartyFloorScore> control,
        IReadOnlyDictionary<int, double> targetWeights, double victoryDuration = double.MaxValue)
    {
        if (targetWeights.Count == 0 || targetWeights.Any(p => p.Key is < 1 or > 15 || !double.IsFinite(p.Value) || p.Value <= 0)
            || !double.IsFinite(targetWeights.Values.Sum()) || !double.IsFinite(victoryDuration) || victoryDuration < 0)
            throw new InvalidDataException("Boss objective requires positive finite target-floor weights and a finite victory duration.");
        var targets = cells.Where(c => targetWeights.ContainsKey(c.Floor)).ToArray();
        var references = control.Where(c => targetWeights.ContainsKey(c.Floor)).ToArray();
        var contexts = references.Select(c => c.Context).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (contexts.Length == 0 || targets.Length != contexts.Length * targetWeights.Count || references.Length != targets.Length
            || !targets.Select(c => c.Context).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).SequenceEqual(contexts)
            || targets.Select(c => (c.Context, c.Floor)).Distinct().Count() != targets.Length
            || references.Select(c => (c.Context, c.Floor)).Distinct().Count() != references.Length
            || targets.Concat(references).Any(c => string.IsNullOrWhiteSpace(c.Context) || c.Clears.Count == 0
                || c.Draws < 0 || c.Draws > c.Clears.Count - c.Clears.Count(x => x)
                || !double.IsFinite(c.GuardianHealth) || !double.IsFinite(c.Survival)))
            throw new InvalidDataException("Boss objective requires a complete unique target-floor matrix in every reference context.");
        var gains = new List<double>(); var health = 0d; var survival = 0d;
        var weightTotal = targetWeights.Values.Sum();
        foreach (var context in contexts)
        {
            var gain = 0d;
            foreach (var (floor, weight) in targetWeights.OrderBy(p => p.Key))
            {
                var cell = targets.SingleOrDefault(c => c.Context == context && c.Floor == floor);
                var reference = references.SingleOrDefault(c => c.Context == context && c.Floor == floor);
                if (cell is null || reference is null || cell.Clears.Count != reference.Clears.Count)
                    throw new InvalidDataException("Boss objective requires matched sample counts for every target-floor/context pair.");
                gain += weight / weightTotal * cell.Clears.Zip(reference.Clears).Sum(p => (p.First ? 1d : 0d) - (p.Second ? 1d : 0d)) / cell.Clears.Count;
                health += weight / weightTotal * cell.GuardianHealth / contexts.Length;
                survival += weight / weightTotal * cell.Survival / contexts.Length;
            }
            gains.Add(gain);
        }
        return new(gains.Min(), health, survival, targets.Any(c => c.Clears.Any(x => x)) ? victoryDuration : double.MaxValue);
    }

    public static IOrderedEnumerable<BossMeasurement> Rank(IEnumerable<BossMeasurement> rows) => rows
        .OrderByDescending(r => r.Fitness.PrimaryGain).ThenBy(r => r.Fitness.GuardianHealth)
        .ThenByDescending(r => r.Fitness.Survival).ThenBy(r => r.Fitness.VictoryDuration).ThenBy(r => r.Id, StringComparer.Ordinal);

    /// <summary>
    /// Up to five distinct measured behavior representatives, always retaining the primary winner.
    /// Niche measurements only choose among equal primary gains. Labels describe observations,
    /// not causal claims or confirmed strategies. An all-loss archive remains unsuccessful.
    /// </summary>
    public static IReadOnlyList<BossStrategy> Archive(IEnumerable<BossMeasurement> measurements)
    {
        var rows = Rank(measurements).DistinctBy(r => r.Id).ToArray();
        if (rows.Length == 0) return [];
        var eligible = rows.Where(r => r.Fitness.PrimaryGain == rows[0].Fitness.PrimaryGain).ToArray();
        var result = new List<BossStrategy> { new("focused-progress", rows[0].Id) };
        var behaviors = new HashSet<BossBehavior> { rows[0].Behavior };
        void Add(string intent, Func<BossBehavior, double> measure, bool lower)
        {
            if (eligible.Select(r => measure(r.Behavior)).Distinct().Count() < 2) return;
            var sorted = lower ? eligible.OrderBy(r => measure(r.Behavior)) : eligible.OrderByDescending(r => measure(r.Behavior));
            // Equal metric values follow the explicit victory/progress ranking.
            var bestValue = measure(sorted.First().Behavior);
            var best = Rank(eligible.Where(r => measure(r.Behavior) == bestValue)).First();
            if (result.All(r => r.Id != best.Id) && behaviors.Add(best.Behavior)) result.Add(new(intent, best.Id));
        }
        Add("lower-hostile-summon-presence", b => b.SummonActiveTicks, true);
        Add("greater-measured-prevention", b => b.DamagePrevented, false);
        Add("lower-party-health-deficit", b => b.HealthDeficit, true);
        Add("greater-measured-action-denial", b => b.DeniedTicks, false);
        return result;
    }

    public static async Task<BossSearchResult> RunAsync(string method, int seed, int candidates, int attempts,
        IReadOnlyList<PartyChoice> starts, IReadOnlyDictionary<string, string> families, IReadOnlyList<int> mutableSlots,
        IReadOnlyList<(string Enabler, string Consumer)> graphPairs,
        Func<PartyChoice, CancellationToken, Task<BossMeasurement>> evaluate,
        CancellationToken token = default, Action<BossProposal>? onProposal = null,
        IReadOnlySet<(string Enabler, string Consumer)>? sameOwnerPairs = null,
        string? anchorId = null, IReadOnlyList<PartyChoice>? mechanismStarts = null)
    {
        if (!Methods.Contains(method, StringComparer.Ordinal) || candidates is < 1 or > 100 || attempts < candidates || attempts > 10000
            || starts.Count == 0 || starts.Count > candidates || mutableSlots.Count == 0
            || mutableSlots.Distinct().Count() != mutableSlots.Count || starts.Select(p => p.Id).Distinct().Count() != starts.Count
            || families.Count == 0 || families.Any(p => string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value)))
            throw new InvalidDataException("Invalid bounded complete-party boss search.");
        var baseline = starts[0].Builds;
        var slots = mutableSlots.Order().ToArray();
        var fixedSlots = baseline.Keys.Except(slots).ToArray();
        if (baseline.Count == 0 || baseline.Keys.Any(s => s < 1) || slots.Any(s => !baseline.ContainsKey(s))
            || baseline.Values.Any(ids => ids.Count is < 1 or > 10 || ids.Count > families.Count)
            || graphPairs.Any(p => !families.ContainsKey(p.Enabler) || !families.ContainsKey(p.Consumer)))
            throw new InvalidDataException("Invalid mutable party scope, allowed pool or graph references.");
        string? Invalid(IReadOnlyDictionary<int, IReadOnlyList<string>> builds)
        {
            if (!builds.Keys.Order().SequenceEqual(baseline.Keys.Order()) || builds.Any(p => p.Value.Count != baseline[p.Key].Count
                    || p.Value.Any(id => !families.ContainsKey(id)))) return "invalid-pool-or-count";
            if (builds.Any(p => p.Value.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != p.Value.Count)) return "duplicate-family";
            if (fixedSlots.Any(s => !builds[s].SequenceEqual(baseline[s]))) return "fixed-character-changed";
            return null;
        }
        if (starts.Any(p => Invalid(p.Builds) is not null || p.Id != HarnessJson.Hash(p.Builds)))
            throw new InvalidDataException("Shared starts must have valid recipe hashes, legal families, equal per-character budgets and identical fixed characters.");
        if (anchorId is not null && !starts.Any(p => p.Id == anchorId)
            || mechanismStarts?.Any(p => Invalid(p.Builds) is not null || p.Id != HarnessJson.Hash(p.Builds)) == true)
            throw new InvalidDataException("Refinement anchors and mechanism proposals must be exact legal parties.");
        var pool = families.Keys.Order(StringComparer.Ordinal).ToArray();
        var pairs = graphPairs.Distinct().OrderBy(p => p.Enabler, StringComparer.Ordinal).ThenBy(p => p.Consumer, StringComparer.Ordinal).ToArray();
        var random = new Random(seed);
        var parties = new List<PartyChoice>(); var evaluations = new List<BossMeasurement>(); var proposals = new List<BossProposal>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<int, IReadOnlyList<string>> Copy(IReadOnlyDictionary<int, IReadOnlyList<string>> builds) =>
            builds.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToArray());
        Dictionary<int, IReadOnlyList<string>> Sample()
        {
            var builds = Copy(baseline);
            foreach (var slot in slots)
            {
                // Reject families independently for each character. Conditioning each uniform
                // ordered tuple on legality preserves a uniform legal-party distribution while
                // avoiding exponentially rare simultaneous acceptance in large Tower parties.
                string[]? accepted = null;
                for (var retry = 0; retry < 10000 && accepted is null; retry++)
                {
                    token.ThrowIfCancellationRequested();
                    var choices = pool.ToArray();
                    for (var i = 0; i < builds[slot].Count; i++)
                    { var j = random.Next(i, choices.Length); (choices[i], choices[j]) = (choices[j], choices[i]); }
                    var ids = choices.Take(builds[slot].Count).ToArray();
                    if (ids.Select(id => families[id]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Length) accepted = ids;
                }
                builds[slot] = accepted ?? throw new InvalidDataException("Uniform legal-character sampling exhausted its bounded rejection allowance; comparison is incomplete.");
            }
            return builds;
        }
        string[] Mutate(IReadOnlyList<string> ids, string kind) => ids.Count > 1 ? LoadoutSearch.Mutate(ids, pool, random, kind)
            : [pool[random.Next(pool.Length)]];
        void Record(BossProposal proposal) { proposals.Add(proposal); onProposal?.Invoke(proposal); }
        for (var attempt = 0; attempt < attempts && evaluations.Count < candidates; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string origin; string? parent = null; PartyChoice choice;
            if (attempt < starts.Count)
            { choice = starts[attempt]; origin = "shared-start"; }
            else
            {
                Dictionary<int, IReadOnlyList<string>> builds;
                var fresh = attempt - starts.Count;
                if (anchorId is not null && method == "graph" && mechanismStarts is { Count: > 0 } && fresh < mechanismStarts.Count)
                {
                    parent = anchorId; origin = "boss-mechanic-replacement";
                    builds = Copy(mechanismStarts[fresh].Builds);
                }
                else if (anchorId is not null && method != "random" && fresh % 4 != 3)
                {
                    // Alternate the declared historical anchor and current discovery beam.
                    // Every fourth fresh attempt explores globally; random remains uniform.
                    parent = fresh % 2 == 0 ? anchorId : Rank(evaluations).Take(4).ElementAt(random.Next(Math.Min(4, evaluations.Count))).Id;
                    builds = Copy(parties.Single(p => p.Id == parent).Builds);
                    var first = slots[random.Next(slots.Length)];
                    if (method != "legacy" && fresh % 3 == 1 && slots.Length > 1)
                    {
                        var second = slots[(Array.IndexOf(slots, first) + 1 + random.Next(slots.Length - 1)) % slots.Length];
                        builds[first] = Mutate(builds[first], "single"); builds[second] = Mutate(builds[second], "single");
                        origin = "refinement-cross-character";
                    }
                    else
                    {
                        var kind = method == "legacy" ? (fresh % 3) switch { 0 => "single", 1 => "double", _ => "order" }
                            : fresh % 6 == 5 ? "order" : fresh % 3 == 2 ? "double" : "single";
                        builds[first] = Mutate(builds[first], kind); origin = "refinement-" + kind;
                    }
                }
                else if (method == "random" || anchorId is not null || attempt % 4 == 0)
                { origin = "random-restart"; builds = Sample(); }
                else if (method == "legacy")
                {
                    // Preserve the existing character beam/mutation proposal rules, but use
                    // full-party boss ordering for the comparison's shared objective and cost.
                    var slot = slots[random.Next(slots.Length)];
                    var ranked = Rank(evaluations).ToArray();
                    var beam = LoadoutSearch.Beam(ranked.Select((r, i) => new LoadoutEvaluation(r.Id, "legacy", null,
                        parties.Single(p => p.Id == r.Id).Builds[slot], new(ranked.Length - i, 0, 0, r.Fitness.GuardianHealth, r.Fitness.Survival), [])), 3);
                    var source = beam[random.Next(beam.Count)]; parent = source.Id;
                    builds = Copy(parties.Single(p => p.Id == parent).Builds);
                    var kind = (attempt % 4) switch { 1 => "single", 2 => "double", _ => "order" };
                    origin = "legacy-" + kind; builds[slot] = Mutate(source.Essences, kind);
                }
                else
                {
                    var retained = Archive(evaluations).Select(a => a.Id).Concat(Rank(evaluations).Take(4).Select(r => r.Id)).Distinct().ToArray();
                    parent = retained[random.Next(retained.Length)]; builds = Copy(parties.Single(p => p.Id == parent).Builds);
                    var first = slots[random.Next(slots.Length)];
                    // A same-owner relationship needs two real positions on one character.
                    // A one-slot character can only host a pair through an allowed teammate.
                    var graphChoices = method == "graph" && attempt % 3 == 1
                        ? pairs.Where(p => builds[first].Count > 1 || slots.Length > 1 && !(sameOwnerPairs?.Contains(p) ?? false)).ToArray()
                        : [];
                    if (graphChoices.Length > 0)
                    {
                        var pair = graphChoices[random.Next(graphChoices.Length)];
                        var cross = slots.Length > 1 && !(sameOwnerPairs?.Contains(pair) ?? false)
                            && (builds[first].Count == 1 || random.Next(2) == 0);
                        var second = cross ? slots[(Array.IndexOf(slots, first) + 1 + random.Next(slots.Length - 1)) % slots.Length] : first;
                        var left = builds[first].ToArray(); var i = random.Next(left.Length); left[i] = pair.Enabler; builds[first] = left;
                        var right = builds[second].ToArray();
                        var j = second == first && right.Length > 1 ? (i + 1 + random.Next(right.Length - 1)) % right.Length : random.Next(right.Length);
                        right[j] = pair.Consumer; builds[second] = right;
                        origin = cross ? "graph-pair-cross-character" : "graph-pair-same-character";
                    }
                    else if (attempt % 6 == 2 && slots.Length > 1)
                    {
                        var second = slots[(Array.IndexOf(slots, first) + 1 + random.Next(slots.Length - 1)) % slots.Length];
                        builds[first] = Mutate(builds[first], "single"); builds[second] = Mutate(builds[second], "single");
                        origin = "joint-cross-character";
                    }
                    else
                    {
                        var kind = (attempt % 3) switch { 0 => "order", 1 => "single", _ => "double" };
                        builds[first] = Mutate(builds[first], kind); origin = "joint-" + kind;
                    }
                }
                choice = TowerPartySelection.Choice(origin, builds);
            }
            var rejection = Invalid(choice.Builds);
            if (rejection is null && !seen.Add(choice.Id)) rejection = "duplicate";
            if (rejection is not null) { Record(new(attempt, origin, parent, choice, rejection)); continue; }
            var measurement = await evaluate(choice, token);
            if (measurement.Id != choice.Id || !double.IsFinite(measurement.Fitness.PrimaryGain)
                || !double.IsFinite(measurement.Fitness.GuardianHealth) || !double.IsFinite(measurement.Fitness.Survival)
                || !double.IsFinite(measurement.Fitness.VictoryDuration)
                || new[] { measurement.Behavior.SummonActiveTicks, measurement.Behavior.HealthDeficit,
                    measurement.Behavior.DamagePrevented, measurement.Behavior.Healing, measurement.Behavior.DeniedTicks }.Any(v => !double.IsFinite(v) || v < 0)
                || (measurement.Behavior.Recovery is { } recovery && new[] { recovery.FriendlyRegeneration, recovery.GuardianHealing,
                    recovery.GuardianRegeneration }.Any(v => !double.IsFinite(v) || v < 0)))
                throw new InvalidDataException("Complete-party evaluator returned a mismatched ID, nonfinite fitness or invalid behavior measurements.");
            parties.Add(choice); evaluations.Add(measurement); Record(new(attempt, origin, parent, choice, "evaluated"));
        }
        return new(method, seed, evaluations.Count == candidates ? "candidate-budget" : "proposal-budget", parties, evaluations, proposals);
    }
}
