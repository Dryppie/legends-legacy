using System.Text;
using Domain.Models.Combat;
using System.Text.Json.Serialization;
using Domain.Models.Items;

namespace BalanceHarness;

public sealed record TowerPartySearchDefinition(int SchemaVersion, IReadOnlyList<int> SearchSeeds,
    int CandidatesPerArm, int CharacterSamples, int PartyCandidates, int PartySamples, int Finalists,
    int ConfirmationSamples, int CharacterSeed, int PartySeed, int ConfirmationSeed, int CombinationSeed,
    int MaximumBattles, IReadOnlyList<int> ExcludedCombatSeeds,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerSearchBudget? Budget = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<int, IReadOnlyList<IReadOnlyList<string>>>? StartingLoadouts = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<int, IReadOnlyList<string>>? ReferenceBuilds = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TowerWholePartyOptions? WholeParty = null);
public sealed record TowerSearchBudget(int EssenceSlots, int CharacterLevel, int Tier, int Rank, ItemQuality Quality, int PriorityFloor);
public sealed record PartyFloorScore(string Context, int Floor, IReadOnlyList<bool> Clears, int Draws,
    double GuardianHealth, double Survival, IReadOnlyList<string> Trials);
public sealed record PartyMeasurement(string Id, LoadoutFitness Fitness, IReadOnlyList<PartyFloorScore> Cells);
public sealed record CharacterSearchArm(int Slot, LoadoutSearchResult Search, IReadOnlyList<PartyMeasurement> Measurements);
public sealed record CharacterShortlist(int Slot, IReadOnlyList<LoadoutFinalist> Candidates);
public sealed record PartyChoice(string Id, string Source, IReadOnlyDictionary<int, IReadOnlyList<string>> Builds);
public sealed record PartySearchReport(string Status, int PlannedMaximum, int ActualBattles,
    IReadOnlyList<CharacterSearchArm> Characters, IReadOnlyList<CharacterShortlist> Shortlists,
    IReadOnlyList<PartyChoice> Parties, IReadOnlyList<PartyMeasurement> Discovery,
    IReadOnlyList<PartyChoice> Selection, IReadOnlyList<PartyMeasurement> Confirmation);

/// <summary>Explicit selection policy shared by execution and archive reconstruction.</summary>
public static class TowerPartySelection
{
    public static readonly int[] Slots = [1, 2, 3, 4];
    public static IReadOnlyList<int> Targets(TowerPartySearchDefinition d) => d.SchemaVersion == 3 ? TowerWholeParty.Slots : Slots;
    public const string Version = "tower-party-search-v1";
    public static LoadoutFinalist CandidateC => new(
        HarnessJson.Hash(new[] { "essence.nightshade_blossom", "essence.spider_queen_royal_venom", "essence.venomous_spiderling", "essence.cinder_beetle" }),
        "Historical reliability candidate C", ["essence.nightshade_blossom", "essence.spider_queen_royal_venom", "essence.venomous_spiderling", "essence.cinder_beetle"]);

    public static TowerPartySearchDefinition Default => new(1, [8563, 9677], 12, 3, 24, 4, 8, 40,
        202609105, 202609106, 202609107, 10871, 30000,
        TowerLoadoutReliability.Default.ExcludedCombatSeeds!
            .Concat(TowerLoadoutPilot.Schedule(202609103, 6).Values.SelectMany(x => x))
            .Concat(TowerLoadoutPilot.Schedule(202609104, 40).Values.SelectMany(x => x)).Distinct().Order().ToArray());

    public static int Validate(TowerPartySearchDefinition d)
    {
        if (d.SchemaVersion is not (1 or 2 or 3) || d.SearchSeeds is not { Count: > 0 and <= 4 }
            || d.SearchSeeds.Distinct().Count() != d.SearchSeeds.Count || d.CandidatesPerArm is < 4 or > 40
            || d.CharacterSamples is < 1 or > 10 || d.PartySamples is < 1 or > 20
            || d.PartyCandidates < 6 + 2 * d.SearchSeeds.Count || d.PartyCandidates > 100
            || d.Finalists < 2 + 2 * d.SearchSeeds.Count || d.Finalists > 16
            || d.ConfirmationSamples is < 1 or > 100 || d.MaximumBattles is < 1 or > 100000
            || d.ExcludedCombatSeeds is not { Count: > 0 and <= 100000 }
            || d.ExcludedCombatSeeds.Distinct().Count() != d.ExcludedCombatSeeds.Count)
            throw new InvalidDataException("Invalid bounded four-slot party search definition.");
        TowerPartyProgression.ValidateBudget(d);
        TowerWholeParty.Validate(d);
        var schedules = new[] { TowerLoadoutPilot.Schedule(d.CharacterSeed, d.CharacterSamples),
            TowerLoadoutPilot.Schedule(d.PartySeed, d.PartySamples), TowerLoadoutPilot.Schedule(d.ConfirmationSeed, d.ConfirmationSamples) };
        var seeds = schedules.SelectMany(s => s.Values.SelectMany(x => x)).ToArray();
        if (seeds.Distinct().Count() != seeds.Length || seeds.Intersect(d.ExcludedCombatSeeds).Any())
            throw new InvalidDataException("Combat stages overlap each other or declared historical seeds.");
        var cost = 30 * (Targets(d).Count * 2 * d.SearchSeeds.Count * d.CandidatesPerArm * d.CharacterSamples
            + d.PartyCandidates * d.PartySamples + d.Finalists * d.ConfirmationSamples);
        if (cost > d.MaximumBattles) throw new InvalidDataException($"Planned {cost} combats exceed the hard budget.");
        return cost;
    }

    public static PartyFloorScore Cell(string context, int floor, IReadOnlyList<TowerBattleReport> reports, IReadOnlyList<string> trials) =>
        new(context, floor, reports.Select(r => r.Succeeded).ToArray(), reports.Count(r => r.Battle.Summary.ContentOutcome == BattleOutcome.Draw),
            reports.Average(r => (double)r.GuardianHealthRemainingPercent), reports.Average(TowerBenchmark.Survival), trials);

    // EntryWins/Wins carry minimum paired win-count gains across contexts in this version, not raw wins.
    // Each context has equal samples. No Wilson interval or personal contribution enters ranking.
    public static LoadoutFitness Fitness(IReadOnlyList<PartyFloorScore> cells, IReadOnlyList<PartyFloorScore> control, int priorityFloor = 1)
    {
        var groups = cells.GroupBy(c => c.Context).ToArray();
        int Delta(PartyFloorScore cell) => cell.Clears.Count(x => x) - control.Single(c => c.Context == cell.Context && c.Floor == cell.Floor).Clears.Count(x => x);
        return new(groups.Min(g => g.Where(c => c.Floor == priorityFloor).Sum(Delta)), groups.Min(g => g.Sum(Delta)),
            cells.Sum(c => c.Clears.Count), cells.Average(c => c.GuardianHealth), cells.Average(c => c.Survival));
    }

    public static IOrderedEnumerable<PartyMeasurement> Rank(IEnumerable<PartyMeasurement> rows) => rows
        .OrderByDescending(r => r.Fitness.EntryWins).ThenByDescending(r => r.Fitness.Wins)
        .ThenBy(r => r.Fitness.GuardianHealth).ThenByDescending(r => r.Fitness.Survival).ThenBy(r => r.Id, StringComparer.Ordinal);

    public static string? Specialist(IEnumerable<PartyMeasurement> candidates, PartyMeasurement control, IEnumerable<string> excluded)
    {
        var skip = excluded.ToHashSet(StringComparer.Ordinal);
        return candidates.Where(c => !skip.Contains(c.Id)).SelectMany(c => c.Cells.Select(cell => new
            { c.Id, cell.Context, cell.Floor, Gain = cell.Clears.Count(x => x) - control.Cells.Single(b => b.Context == cell.Context && b.Floor == cell.Floor).Clears.Count(x => x) }))
            .Where(x => x.Gain > 0).OrderByDescending(x => x.Gain).ThenBy(x => x.Floor)
            .ThenBy(x => x.Context, StringComparer.Ordinal).ThenBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
    }

    public static CharacterShortlist Shortlist(int slot, IReadOnlyList<CharacterSearchArm> arms)
    {
        var evaluations = arms.SelectMany(a => a.Search.Evaluations).DistinctBy(e => e.Id).ToDictionary(e => e.Id);
        var rows = arms.SelectMany(a => a.Measurements).DistinctBy(m => m.Id).ToArray();
        var control = rows.Single(m => m.Id == arms[0].Search.Evaluations[0].Id);
        var chosen = new List<LoadoutFinalist>();
        void Add(string id, string source) => chosen.Add(new(id, source, evaluations[id].Essences));
        Add(control.Id, "Unchanged character control");
        foreach (var row in Rank(rows).Where(r => r.Id != control.Id))
        {
            if (chosen.Skip(1).Any(c => c.Essences.Zip(evaluations[row.Id].Essences).Count(p => p.First != p.Second) < 2)) continue;
            Add(row.Id, "Robust generalist from both ally contexts");
            if (chosen.Count == 3) break;
        }
        var specialist = Specialist(rows, control, chosen.Select(c => c.Id));
        if (specialist is not null) Add(specialist, "Exploratory floor/context specialist; inspect all-floor tradeoffs");
        return new(slot, chosen);
    }

    public static PartyChoice Choice(string source, IReadOnlyDictionary<int, IReadOnlyList<string>> builds) => new(HarnessJson.Hash(builds), source, builds);

    public static IReadOnlyList<PartyChoice> Combinations(TowerPartySearchDefinition d, IReadOnlyList<CharacterShortlist> lists, IReadOnlyList<CharacterSearchArm> arms)
    {
        if (d.SchemaVersion == 3) return TowerWholeParty.Combinations(d, lists, arms);
        var result = new List<PartyChoice>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        var control = lists.ToDictionary(l => l.Slot, l => l.Candidates[0].Essences);
        void Add(string source, IReadOnlyDictionary<int, IReadOnlyList<string>> builds)
        { var choice = Choice(source, builds); if (seen.Add(choice.Id)) result.Add(choice); }
        Add("control", control);
        var historical = d.ReferenceBuilds ?? new Dictionary<int, IReadOnlyList<string>>(control) { [2] = CandidateC.Essences };
        Add(d.SchemaVersion == 1 ? "historical-C" : "extended-four-slot-reference", historical);
        foreach (var seed in d.SearchSeeds)
            foreach (var method in new[] { "guided", "random" })
                Add($"method-{method}-{seed}", Slots.ToDictionary(slot => slot,
                    slot => LoadoutSearch.Rank(arms.Single(a => a.Slot == slot && a.Search.Method == method && a.Search.Seed == seed).Search.Evaluations).First().Essences));
        foreach (var list in lists)
        {
            var alternative = list.Candidates.Skip(1).FirstOrDefault();
            if (alternative is not null) Add($"single-slot-{list.Slot}", new Dictionary<int, IReadOnlyList<string>>(control) { [list.Slot] = alternative.Essences });
        }
        var all = new List<Dictionary<int, IReadOnlyList<string>>> { new() };
        foreach (var list in lists)
            all = all.SelectMany(prefix => list.Candidates.Select(c => new Dictionary<int, IReadOnlyList<string>>(prefix) { [list.Slot] = c.Essences })).ToList();
        var random = new Random(d.CombinationSeed);
        // Shuffle a small, finite Cartesian product; no retries or unbounded duplicate sampling.
        for (var i = all.Count - 1; i > 0; i--) { var j = random.Next(i + 1); (all[i], all[j]) = (all[j], all[i]); }
        foreach (var builds in all)
        { if (result.Count >= d.PartyCandidates) break; Add("shortlist-combination", builds); }
        return result;
    }

    public static IReadOnlyList<PartyChoice> Select(TowerPartySearchDefinition d, IReadOnlyList<PartyChoice> parties, IReadOnlyList<PartyMeasurement> rows,
        IReadOnlyList<CharacterSearchArm> arms)
    {
        if (d.SchemaVersion == 3) return TowerWholeParty.Select(d, parties, rows, arms);
        // A method winner can deduplicate against historical C or the control. Preserve it by
        // recipe identity, even when the first recorded source label belongs to that earlier proposal.
        var mandatory = d.SearchSeeds.SelectMany(seed => new[] { "guided", "random" }.Select(method =>
            Choice("method", Slots.ToDictionary(slot => slot, slot => LoadoutSearch.Rank(arms.Single(a => a.Slot == slot
                && a.Search.Method == method && a.Search.Seed == seed).Search.Evaluations).First().Essences)).Id)).ToHashSet(StringComparer.Ordinal);
        var selected = parties.Where(p => p.Source == "control" || mandatory.Contains(p.Id)).ToList();
        var control = rows.Single(r => r.Id == parties[0].Id);
        // Reserve one place for a floor/context specialist, when a positive-gain alternative exists.
        foreach (var row in Rank(rows))
        { if (selected.Count >= d.Finalists - 1) break; if (selected.All(p => p.Id != row.Id)) selected.Add(parties.Single(p => p.Id == row.Id)); }
        var specialist = Specialist(rows, control, selected.Select(p => p.Id));
        var final = specialist ?? Rank(rows).FirstOrDefault(r => selected.All(p => p.Id != r.Id))?.Id;
        if (final is not null && selected.Count < d.Finalists) selected.Add(parties.Single(p => p.Id == final) with { Source = specialist is null ? "generalist-finalist" : "floor-specialist-finalist" });
        return selected;
    }

    public static TowerScenario Apply(TowerScenario scenario, IReadOnlyDictionary<int, IReadOnlyList<string>> builds, IReadOnlyList<int> seeds) => scenario with
    { Seeds = seeds, Party = scenario.Party.Select(p => builds.TryGetValue(p.PartySlot, out var ids) ? p with { Build = p.Build with { EssenceIds = ids } } : p).ToArray() };

    public static string Markdown(PartySearchReport report, TowerPartySearchDefinition? definition = null)
    {
        var text = new StringBuilder($"# Tower party loadout search\n\n{report.Status}; {report.ActualBattles}/{report.PlannedMaximum} upper-bound combats.\n\n");
        text.AppendLine("Four Essences, level 30, Uncommon Standard tier-1 rank-1 baseline gear, level-1 unascended/unevolved Essences, hypothetical ownership, no styles. Only first-cell Guardian, Restorer and two separately searched Strikers change. Controller and later cells remain fixed. All 15 floors use production RequiredSlots.\n");
        text.AppendLine("Rank by minimum floor-1 win-count gain over each context's control, then minimum all-floor gain, lower guardian health, higher survival and stable ID. JSON fitness EntryWins/Wins encode these minimum gains. Both methods receive identical control and historical starts; random exploration samples uniform legal ordered tuples. Wilson 95% is descriptive uncertainty only. No target, optimality or method-superiority claim.\n");
        text.AppendLine("Character discovery uses two contrasting ally contexts. Joint parties replace slots 1–4 in both contexts: floor 1's resulting teams are identical; later floors differ only in fixed later cells. Paired context results and search restarts are not independent extra samples. Specialists are exploratory, and confirmation never reselects candidates.\n");
        text.AppendLine("## Character alternatives\n\n| Slot | Loadout | Source | Ordered Essences |\n| --- | --- | --- | --- |");
        foreach (var list in report.Shortlists)
            foreach (var c in list.Candidates) text.AppendLine($"| {list.Slot} | {c.Id[..12]} | {c.Source} | {string.Join(" / ", c.Essences)} |");
        text.AppendLine("\n## Frozen parties\n\n| Party | Source | Slot: ordered Essences |\n| --- | --- | --- |");
        foreach (var p in report.Selection) text.AppendLine($"| {p.Id[..12]} | {p.Source} | {string.Join("; ", p.Builds.Select(b => b.Key + ": " + string.Join(" / ", b.Value)))} |");
        text.AppendLine("\n## All-floor confirmation\n\nIntervals are nominal pointwise estimates, without multiplicity correction. Gained/lost count matched seeds against the unchanged control in that same context. All-floor totals describe this encounter mix, not a universal probability.\n\n| Party | Context | Floor | Wins / trials | Draws | Wilson 95% | Control wins | Gained / lost | Paired change 95% |\n| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        if (report.Confirmation.Count == 0) return DescribeBudget(text.ToString(), definition);
        foreach (var row in report.Confirmation)
            foreach (var cell in row.Cells)
            {
                var control = report.Confirmation[0].Cells.Single(c => c.Context == cell.Context && c.Floor == cell.Floor);
                var wins = cell.Clears.Count(x => x);
                var gained = cell.Clears.Zip(control.Clears).Count(p => p.First && !p.Second);
                var lost = cell.Clears.Zip(control.Clears).Count(p => !p.First && p.Second);
                var interval = SuiteScorecard.Wilson(wins, cell.Clears.Count)!;
                var change = PairedStatistics.ClearRate(gained, lost, cell.Clears.Count);
                text.AppendLine(FormattableString.Invariant($"| {row.Id[..12]} | {cell.Context} | {cell.Floor} | {wins}/{cell.Clears.Count} | {cell.Draws} | {100 * interval.Lower:F1}–{100 * interval.Upper:F1}% | {control.Clears.Count(x => x)} | {gained}/{lost} | {change.MeanChange:F1} pp [{change.Lower:F1}, {change.Upper:F1}] |"));
            }
        return DescribeBudget(text.ToString(), definition) + (definition?.SchemaVersion == 3 ? TowerWholeParty.DeploymentMarkdown(definition, report) : "");
    }

    private static string DescribeBudget(string text, TowerPartySearchDefinition? d) => d?.Budget is not { } b ? text : d.SchemaVersion == 3
        ? TowerWholeParty.Describe(text, d) : text
        .Replace("Four Essences, level 30, Uncommon Standard tier-1 rank-1 baseline gear", $"{b.EssenceSlots} Essences, level {b.CharacterLevel}, Uncommon {b.Quality} tier-{b.Tier} rank-{b.Rank} baseline gear (provisional progression budget)")
        .Replace("minimum floor-1 win-count gain", $"minimum floor-{b.PriorityFloor} win-count gain")
        .Replace("identical control and historical starts", "identical controls and legal extensions of the recorded four-slot shortlists")
        + $"\nSearch schema 2; priority floor {b.PriorityFloor}. Budgets remain separate; changing slot count also changes the declared level/gear. This is not an isolated slot-count effect. No numerical balance target is applied.\n";
}
