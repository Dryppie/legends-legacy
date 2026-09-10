using Common.Randomness;
using System.Globalization;

namespace BalanceHarness;

public sealed record TowerWholePartyOptions(int MaximumPartySlots, IReadOnlyList<PartyChoice> Controls, string Source);
public sealed record TowerWholePartyHistory(int SchemaVersion, IReadOnlyDictionary<int, IReadOnlyList<PartyChoice>> Parties,
    IReadOnlyList<int> ExcludedCombatSeeds, IReadOnlyDictionary<int, string> Sources);

/// <summary>Five-role search with explicit first-cell, repeated-cell and alternating-cell candidates.</summary>
public static class TowerWholeParty
{
    public const string Version = "tower-party-search-v3";
    public const string Fixture = "tower-whole-party-controls.json";
    public static readonly int[] Slots = [1, 2, 3, 4, 5];

    public static void Validate(TowerPartySearchDefinition d)
    {
        if (d.SchemaVersion != 3)
        {
            if (d.WholeParty is not null) throw new InvalidDataException("Whole-party options require schema 3.");
            return;
        }
        var w = d.WholeParty;
        if (w is null || w.MaximumPartySlots is < 5 or > 50 || w.MaximumPartySlots % 5 != 0
            || string.IsNullOrWhiteSpace(w.Source) || w.Controls is not { Count: > 0 and <= 8 }
            || w.Controls.Select(c => c.Id).Distinct().Count() != w.Controls.Count
            || w.Controls[0].Source != "control"
            || w.Controls.Any(c => c.Id != HarnessJson.Hash(c.Builds) || !c.Builds.Keys.Order().SequenceEqual(Slots)
                || c.Builds.Values.Any(ids => ids.Count != d.Budget!.EssenceSlots || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Count))
            || d.PartyCandidates < w.Controls.Count + 6 * d.SearchSeeds.Count + 5
            || d.Finalists < w.Controls.Count + 2 * d.SearchSeeds.Count + 3)
            throw new InvalidDataException("Invalid whole-party controls, deployment coverage or finalist budget.");
    }

    public static TowerPartySearchDefinition Definition(string root, string catalogs, int slots, int master,
        IReadOnlyList<int>? exclusions = null, bool thorough = false)
    {
        var history = HarnessJson.Read<TowerWholePartyHistory>(Path.Combine(catalogs, Fixture));
        if (history.SchemaVersion != 1 || !history.Parties.ContainsKey(slots) || !history.Sources.ContainsKey(slots))
            throw new InvalidDataException("Missing historical whole-party controls.");
        var old = TowerPartyProgression.Definition(root, catalogs, slots, master);
        var scenarios = TowerPartyProgression.Scenarios(root, catalogs, old.Budget!);
        var baseline = scenarios[0].Party.ToDictionary(p => p.PartySlot, p => p.Build.EssenceIds);
        var controls = history.Parties[slots].Select((p, i) => TowerPartySelection.Choice(i == 0 ? "control" : "retained-" + p.Id[..12],
            Slots.ToDictionary(s => s, s => p.Builds.GetValueOrDefault(s) ?? baseline[s]))).DistinctBy(p => p.Id).ToArray();
        if (controls[0].Id != HarnessJson.Hash(baseline)) throw new InvalidDataException("Historical control differs from the cohort's authored party.");
        var starts = Slots.ToDictionary(s => s, s => (IReadOnlyList<IReadOnlyList<string>>)(s == 5
            ? new IReadOnlyList<string>[] { baseline[5].Reverse().ToArray(), controls[1].Builds[2], controls[1].Builds[3] }
            : controls.Skip(1).Select(p => p.Builds[s]).Concat(old.StartingLoadouts![s]))
            .Where(ids => HarnessJson.Hash(ids) != HarnessJson.Hash(baseline[s])).DistinctBy(ids => HarnessJson.Hash(ids)).Take(4).ToArray());
        int Seed(string label) => StableRandom.Seed(Version, master.ToString(CultureInfo.InvariantCulture), label);
        var d = old with { SchemaVersion = 3, SearchSeeds = thorough ? [Seed("search-a"), Seed("search-b"), Seed("search-c")] : [Seed("search-a"), Seed("search-b")],
            CharacterSamples = 1, PartyCandidates = thorough ? 32 : 24, PartySamples = 1, Finalists = thorough ? 16 : controls.Length + 8,
            ConfirmationSamples = thorough ? 20 : 10, CharacterSeed = Seed("character"), PartySeed = Seed("party"),
            ConfirmationSeed = Seed("confirmation"), CombinationSeed = Seed("combinations"), MaximumBattles = thorough ? 18000 : 10000,
            StartingLoadouts = starts, ReferenceBuilds = controls[1].Builds,
            ExcludedCombatSeeds = history.ExcludedCombatSeeds.Concat(exclusions ?? []).Distinct().Order().ToArray(),
            WholeParty = new(scenarios.Max(s => s.Party.Count), controls, history.Sources[slots]) };
        TowerPartySelection.Validate(d); return d;
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(string root, string catalogs, int target, TowerPartySearchDefinition d)
    {
        var contexts = TowerPartyProgression.Contexts(root, catalogs, target, d.Budget!);
        if (contexts.Values.SelectMany(s => s).Max(s => s.Party.Count) != d.WholeParty!.MaximumPartySlots)
            throw new InvalidDataException("Frozen deployment extent differs from released Tower party sizes.");
        var original = contexts.Values.First();
        return contexts.ToDictionary(c => c.Key, c => (IReadOnlyList<TowerScenario>)c.Value.Select(s => s with {
            Party = s.Party.Select(p => target == 0 && Slots.Contains(p.PartySlot)
                ? original.Single(o => o.FloorNumber == s.FloorNumber).Party.Single(o => o.PartySlot == p.PartySlot) : p).ToArray(),
            Assumptions = [.. s.Assumptions.Where(a => !a.Contains("first-cell slots 1–4", StringComparison.Ordinal)
                && !a.Contains("Alternative candidate-05 allies", StringComparison.Ordinal)),
                "Schema 3: all five first-cell roles are searched; joint candidates explicitly choose first-cell, repeated or alternating group deployments. Identities and equipment stay fixed.",
                "Both authored ally contexts are retained for comparison. Full-party deployments overwrite both contexts identically; paired repeats must not be pooled as independent evidence."]
        }).ToArray());
    }

    public static IReadOnlyDictionary<int, IReadOnlyList<string>> Deploy(IReadOnlyDictionary<int, IReadOnlyList<string>> first,
        IReadOnlyDictionary<int, IReadOnlyList<string>> second, string policy, int maximum)
    {
        if (policy is not ("first-cell" or "repeat" or "alternating") || maximum < 5 || maximum % 5 != 0
            || !first.Keys.Order().SequenceEqual(Slots) || !second.Keys.Order().SequenceEqual(Slots))
            throw new InvalidDataException("Invalid five-role deployment.");
        return Enumerable.Range(1, policy == "first-cell" ? 5 : maximum).ToDictionary(slot => slot,
            slot => (policy == "alternating" && (slot - 1) / 5 % 2 == 1 ? second : first)[(slot - 1) % 5 + 1]);
    }

    private static IReadOnlyList<PartyChoice> Variants(TowerPartySearchDefinition d, IReadOnlyList<CharacterSearchArm> arms)
    {
        var result = new List<PartyChoice>();
        foreach (var seed in d.SearchSeeds)
        {
            var builds = new[] { "guided", "random" }.ToDictionary(method => method,
                method => (IReadOnlyDictionary<int, IReadOnlyList<string>>)Slots.ToDictionary(slot => slot,
                    slot => LoadoutSearch.Rank(arms.Single(a => a.Slot == slot && a.Search.Method == method && a.Search.Seed == seed).Search.Evaluations).First().Essences));
            foreach (var method in builds.Keys)
                foreach (var policy in new[] { "first-cell", "repeat", "alternating" })
                    result.Add(TowerPartySelection.Choice($"method-{method}-{seed}/{policy}",
                        Deploy(builds[method], builds[method == "guided" ? "random" : "guided"], policy, d.WholeParty!.MaximumPartySlots)));
        }
        return result;
    }

    public static IReadOnlyList<PartyChoice> Combinations(TowerPartySearchDefinition d, IReadOnlyList<CharacterShortlist> lists, IReadOnlyList<CharacterSearchArm> arms)
    {
        var choices = new List<PartyChoice>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        void Add(PartyChoice p) { if (seen.Add(p.Id)) choices.Add(p); }
        foreach (var p in d.WholeParty!.Controls) Add(p);
        foreach (var p in Variants(d, arms)) Add(p);
        var control = d.WholeParty.Controls[0].Builds;
        foreach (var list in lists)
        {
            var alternative = list.Candidates.Skip(1).FirstOrDefault();
            if (alternative is not null) Add(TowerPartySelection.Choice($"single-slot-{list.Slot}",
                new Dictionary<int, IReadOnlyList<string>>(control) { [list.Slot] = alternative.Essences }));
        }
        var combinations = new List<Dictionary<int, IReadOnlyList<string>>> { new() };
        foreach (var list in lists)
            combinations = combinations.SelectMany(prefix => list.Candidates.Select(c => new Dictionary<int, IReadOnlyList<string>>(prefix) { [list.Slot] = c.Essences })).ToList();
        var random = new Random(d.CombinationSeed);
        for (var i = combinations.Count - 1; i > 0; i--) { var j = random.Next(i + 1); (combinations[i], combinations[j]) = (combinations[j], combinations[i]); }
        foreach (var builds in combinations)
        {
            if (choices.Count >= d.PartyCandidates) break;
            Add(TowerPartySelection.Choice("shortlist-combination/repeat", Deploy(builds, builds, "repeat", d.WholeParty.MaximumPartySlots)));
        }
        if (choices.Count > d.PartyCandidates) throw new InvalidDataException("Mandatory deployment comparisons exceed budget.");
        return choices;
    }

    public static IReadOnlyList<PartyChoice> Select(TowerPartySearchDefinition d, IReadOnlyList<PartyChoice> parties,
        IReadOnlyList<PartyMeasurement> rows, IReadOnlyList<CharacterSearchArm> arms)
    {
        var selected = d.WholeParty!.Controls.Select(p => parties.Single(c => c.Id == p.Id)).ToList();
        void Add(string id) { if (selected.All(p => p.Id != id)) selected.Add(parties.Single(p => p.Id == id)); }
        var variants = Variants(d, arms);
        foreach (var family in variants.GroupBy(p => p.Source.Split('/')[0]))
            Add(TowerPartySelection.Rank(rows.Where(r => family.Any(p => p.Id == r.Id))).First().Id);
        // Freeze a matched deployment trio for the best discovery family, even if some variants lose.
        var best = TowerPartySelection.Rank(rows.Where(r => variants.Any(p => p.Id == r.Id))).First().Id;
        var name = variants.First(p => p.Id == best).Source.Split('/')[0];
        foreach (var p in variants.Where(p => p.Source.Split('/')[0] == name)) Add(p.Id);
        foreach (var row in TowerPartySelection.Rank(rows))
        { if (selected.Count >= d.Finalists - 1) break; Add(row.Id); }
        var specialist = TowerPartySelection.Specialist(rows, rows.Single(r => r.Id == d.WholeParty.Controls[0].Id), selected.Select(p => p.Id));
        var last = specialist ?? TowerPartySelection.Rank(rows).FirstOrDefault(r => selected.All(p => p.Id != r.Id))?.Id;
        if (last is not null && selected.Count < d.Finalists) Add(last);
        if (selected.Count > d.Finalists) throw new InvalidDataException("Mandatory confirmation comparisons exceed budget.");
        return selected;
    }

    public static object Comparisons(TowerPartySearchDefinition d, IReadOnlyList<CharacterSearchArm> arms,
        IReadOnlyList<PartyMeasurement> discovery, IReadOnlyList<PartyMeasurement> confirmation) => Variants(d, arms)
        .GroupBy(p => p.Source.Split('/')[0]).Select(family => new {
            Family = family.Key, Variants = family.Select(p => new {
                Policy = p.Source.Split('/')[1], p.Id, Discovery = discovery.Single(r => r.Id == p.Id).Fitness,
                Confirmed = confirmation.Any(r => r.Id == p.Id),
                PairedAgainstFirstCell = confirmation.SingleOrDefault(r => r.Id == p.Id)?.Cells.Select(cell => {
                    var baseline = confirmation.SingleOrDefault(r => r.Id == family.First().Id)?.Cells.Single(c => c.Context == cell.Context && c.Floor == cell.Floor);
                    return new { cell.Context, cell.Floor, Available = baseline is not null,
                        Gained = baseline is null ? (int?)null : cell.Clears.Zip(baseline.Clears).Count(p => p.First && !p.Second),
                        Lost = baseline is null ? (int?)null : cell.Clears.Zip(baseline.Clears).Count(p => !p.First && p.Second) };
                }).ToArray()
            }).ToArray()
        }).ToArray();

    public static string DeploymentMarkdown(TowerPartySearchDefinition d, PartySearchReport report)
    {
        var text = new System.Text.StringBuilder("\n## Deployment comparisons\n\nEvery method/seed proposes all three policies. Same recipe IDs can share labels. The best discovery family's complete trio is frozen; other families may have only their best deployment confirmed. Whole-party contexts become identical after replacement and cannot be pooled. Per-floor paired gained/lost counts are saved in deployment-comparisons.json.\n\n| Method / seed | Deployment | Party | Confirmed |\n| --- | --- | --- | --- |\n");
        foreach (var p in Variants(d, report.Characters))
            text.AppendLine($"| {p.Source.Split('/')[0]} | {p.Source.Split('/')[1]} | {p.Id[..12]} | {(report.Selection.Any(c => c.Id == p.Id) ? "Yes" : "No")} |");
        return text.ToString();
    }

    public static string Describe(string text, TowerPartySearchDefinition d)
    {
        var b = d.Budget!;
        return text.Replace("Four Essences, level 30, Uncommon Standard tier-1 rank-1 baseline gear", $"{b.EssenceSlots} Essences, level {b.CharacterLevel}, Uncommon {b.Quality} tier-{b.Tier} rank-{b.Rank} baseline gear")
            .Replace("Only first-cell Guardian, Restorer and two separately searched Strikers change. Controller and later cells remain fixed.", "Guardian, Restorer, both Strikers and Controller are searched. Explicit candidates deploy first-cell-only, repeated five-role groups, or alternating guided/random groups from the same generation seed. Other budgets, identities and composition stay fixed.")
            .Replace("minimum floor-1 win-count gain", $"minimum floor-{b.PriorityFloor} win-count gain")
            .Replace("Joint parties replace slots 1–4 in both contexts: floor 1's resulting teams are identical; later floors differ only in fixed later cells.", "First-cell-only parties retain contrasting later allies. Repeated/alternating whole-party deployments replace both contexts identically, so their repeated outcomes are not independent evidence.")
            + $"\nSearch schema 3. All {d.WholeParty!.Controls.Count} previous finalists remain confirmation controls. Each method/seed retains its best discovery deployment; a matched first-cell/repeat/alternating trio is also frozen before confirmation. Deduplicated recipes retain their first source label. Alternation is a joint proposal, not proof of synergy or a pure method comparison. No slot-count causal claim, optimality claim or automatic Tower target.\n";
    }

    public static async Task RunBatchAsync(string root, string catalogs, string output, CancellationToken token, Action<string>? progress = null)
    {
        if (Path.Exists(output)) throw new IOException("Choose a new whole-party study directory.");
        var exclusions = HarnessJson.Read<TowerWholePartyHistory>(Path.Combine(catalogs, Fixture)).ExcludedCombatSeeds.ToList();
        var definitions = new Dictionary<int, TowerPartySearchDefinition>();
        foreach (var slots in new[] { 6, 4, 5, 7, 8, 9, 10 })
        {
            var d = Definition(root, catalogs, slots, 202609210 + slots, exclusions, slots == 6);
            definitions.Add(slots, d); exclusions.AddRange(TowerPartyProgression.CombatSeeds(d));
        }
        Directory.CreateDirectory(output);
        HarnessJson.WriteNew(Path.Combine(output, "batch-plan.json"), new { MaximumBattles = definitions.Values.Sum(TowerPartySelection.Validate), Definitions = definitions });
        var completed = new List<object>(); var status = "Incomplete";
        try
        {
            foreach (var pair in definitions)
            {
                progress?.Invoke($"Starting {pair.Key}-slot whole-party cohort: {TowerPartySelection.Validate(pair.Value)} maximum combats.");
                var report = await TowerPartySearch.RunAsync(root, catalogs, Path.Combine(output, $"slots-{pair.Key}"), pair.Value, token, progress);
                completed.Add(new { Slots = pair.Key, report.Status, report.ActualBattles, report.Selection });
            }
            status = "Complete";
        }
        catch (OperationCanceledException) { status = "Cancelled"; throw; }
        finally { HarnessJson.WriteNew(Path.Combine(output, "batch-results.json"), new { Status = status, Completed = completed }); }
    }
}
