namespace BalanceHarness;

public sealed record LoadoutMethodPair(string Cohort, int SearchSeed, string Guided, string Random,
    int GuidedEntryWins, int RandomEntryWins, int GuidedAllWins, int RandomAllWins,
    int GuidedConfirmationEntryWins, int RandomConfirmationEntryWins,
    int GuidedConfirmationAllWins, int RandomConfirmationAllWins);
public sealed record LoadoutTransfer(string Gear, string Candidate, int Floor, int BalancedWins, int AlternativeWins,
    int Trials, int BalancedControlWins, int AlternativeControlWins);
public sealed record LoadoutReliabilityReport(IReadOnlyList<LoadoutMethodPair> Methods, IReadOnlyList<LoadoutTransfer> Transfer);

public static class TowerLoadoutReliability
{
    public static TowerLoadoutPilotDefinition Default => new(2, 2, [TowerLoadoutPilot.Default.Gear[0]],
        [4111, 5227, 6337, 7451], 202609103, 202609104, 12, 6, 40, 45000,
        AllyContexts: ["balanced", "previous-05"],
        FixedCandidates: [
            Candidate("Earlier pilot A; fixed before this experiment", ["essence.glade_panther", "essence.plague_ghoul", "essence.nightshade_blossom", "essence.illusion_fox"]),
            Candidate("Earlier pilot B; fixed before this experiment", ["essence.feral_ghoul", "essence.ravenous_ghoul", "essence.thornback_boar", "essence.green_slime"])],
        ExcludedCombatSeeds: TowerLoadoutPilot.Schedule(202609101, 2).Values.SelectMany(s => s)
            .Concat(TowerLoadoutPilot.Schedule(202609102, 10).Values.SelectMany(s => s)).Concat([1337, 17, -12345]).Distinct().Order().ToArray());

    private static LoadoutFinalist Candidate(string source, string[] ids) => new(HarnessJson.Hash(ids), source, ids);

    public static IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> Contexts(TowerLoadoutPilotDefinition d, string root, string catalogs)
    {
        var result = new Dictionary<string, IReadOnlyList<TowerScenario>>(StringComparer.Ordinal);
        foreach (var gear in d.Gear)
        {
            var original = TowerLoadoutPilot.Scenarios(root, catalogs, gear);
            foreach (var allies in d.AllyContexts ?? ["balanced"])
                result.Add(d.SchemaVersion == 1 ? gear.Id : gear.Id + "--" + allies, original.Select(s =>
                {
                    if (allies == "balanced") return s;
                    var alternative = TowerLoadoutPilot.Apply(s, d.TargetPartySlot, new("previous-05", "Fixed alternative allies", [], true), s.Seeds);
                    // The target is identical across ally contexts, even when it is a Guardian or Striker.
                    return alternative with { Party = alternative.Party.Select(p => p.PartySlot == d.TargetPartySlot
                        ? s.Party.Single(q => q.PartySlot == p.PartySlot) : p).ToArray(),
                        Assumptions = [.. s.Assumptions, "Fixed candidate-05 ally substitutions, excluding the target character. No adaptation during search."] };
                }).ToArray());
        }
        return result;
    }

    public static IReadOnlyList<LoadoutCohortSelection> Select(TowerLoadoutPilotDefinition d,
        IReadOnlyDictionary<string, IReadOnlyList<TowerScenario>> contexts, IReadOnlyList<LoadoutMethodResult> discovery)
    {
        if (d.SchemaVersion == 1) return contexts.Select(c => TowerLoadoutPilot.Select(c.Key,
            c.Value[0].Party.Single(p => p.PartySlot == d.TargetPartySlot).Build.EssenceIds, discovery)).ToArray();
        var selections = new List<LoadoutCohortSelection>();
        foreach (var gear in d.Gear)
        {
            var cohortIds = d.AllyContexts!.Select(a => gear.Id + "--" + a).ToArray();
            var original = contexts[cohortIds[0]][0].Party.Single(p => p.PartySlot == d.TargetPartySlot).Build.EssenceIds;
            var candidates = new List<LoadoutFinalist> { new("control", "Unchanged target in this fixed ally context", original) };
            foreach (var fixedCandidate in d.FixedCandidates ?? [])
                if (!candidates.Any(c => c.Essences.SequenceEqual(fixedCandidate.Essences))) candidates.Add(fixedCandidate);
            foreach (var arm in discovery.Where(a => cohortIds.Contains(a.Cohort)))
            {
                var best = LoadoutSearch.Rank(arm.Search.Evaluations).First();
                if (!candidates.Any(c => c.Essences.SequenceEqual(best.Essences)))
                    candidates.Add(new(best.Id, $"{arm.Cohort}, {arm.Search.Method}, search seed {arm.Search.Seed}", best.Essences));
            }
            // A common frozen union tests every winner in both ally contexts, with no confirmation reselection.
            selections.AddRange(cohortIds.Select(id => new LoadoutCohortSelection(id, candidates.ToArray())));
        }
        return selections;
    }

    public static LoadoutReliabilityReport Summarize(TowerLoadoutPilotReport report, TowerLoadoutPilotDefinition d)
    {
        var methods = new List<LoadoutMethodPair>();
        foreach (var group in report.Discovery.GroupBy(a => (a.Cohort, a.Search.Seed)))
        {
            var guided = LoadoutSearch.Rank(group.Single(a => a.Search.Method == "guided").Search.Evaluations).First();
            var random = LoadoutSearch.Rank(group.Single(a => a.Search.Method == "random").Search.Evaluations).First();
            string Id(LoadoutEvaluation e) => report.Selection.Single(s => s.Cohort == group.Key.Cohort).Finalists
                .Single(f => f.Essences.SequenceEqual(e.Essences)).Id;
            var g = report.Confirmation.Where(c => c.Cohort == group.Key.Cohort && c.Candidate == Id(guided)).ToArray();
            var r = report.Confirmation.Where(c => c.Cohort == group.Key.Cohort && c.Candidate == Id(random)).ToArray();
            methods.Add(new(group.Key.Cohort, group.Key.Seed, Id(guided), Id(random), guided.Fitness.EntryWins, random.Fitness.EntryWins,
                guided.Fitness.Wins, random.Fitness.Wins, g.Single(c => c.Floor == 1).Wins, r.Single(c => c.Floor == 1).Wins,
                g.Sum(c => c.Wins), r.Sum(c => c.Wins)));
        }
        var transfer = new List<LoadoutTransfer>();
        if (d.AllyContexts?.Count == 2)
            foreach (var gear in d.Gear)
                foreach (var a in report.Confirmation.Where(c => c.Cohort == gear.Id + "--balanced"))
                {
                    var b = report.Confirmation.Single(c => c.Cohort == gear.Id + "--previous-05" && c.Candidate == a.Candidate && c.Floor == a.Floor);
                    transfer.Add(new(gear.Id, a.Candidate, a.Floor, a.Wins, b.Wins, a.Trials, a.ControlWins, b.ControlWins));
                }
        return new(methods, transfer);
    }

    public static string Markdown(LoadoutReliabilityReport report)
    {
        var text = new System.Text.StringBuilder("\n## Equal-cost method comparison\n\nEvery arm uses the same candidate/sample budget. Search-seed repeats are not independent combat samples. Confirmation retains the discovery winner even when it regresses; no method is declared statistically superior.\n\n| Allies / gear | Search seed | Discovery entry G / R | Discovery all G / R | Confirmation entry G / R | Confirmation all G / R |\n| --- | --- | --- | --- | --- | --- |\n");
        foreach (var p in report.Methods) text.AppendLine($"| {p.Cohort} | {p.SearchSeed} | {p.GuidedEntryWins} / {p.RandomEntryWins} | {p.GuidedAllWins} / {p.RandomAllWins} | {p.GuidedConfirmationEntryWins} / {p.RandomConfirmationEntryWins} | {p.GuidedConfirmationAllWins} / {p.RandomConfirmationAllWins} |");
        text.AppendLine("\n## Transfer between fixed ally contexts\n\nA common finalist union runs every floor in both contexts. Compare each candidate against its own context control; raw cross-context differences also include the changed allies. G/R means guided/random above.\n\n| Gear | Candidate | Floor | Balanced wins / trials | Alternative wins / trials | Balanced / alternative control wins |\n| --- | --- | --- | --- | --- | --- |");
        foreach (var p in report.Transfer) text.AppendLine($"| {p.Gear} | {p.Candidate} | {p.Floor} | {p.BalancedWins}/{p.Trials} | {p.AlternativeWins}/{p.Trials} | {p.BalancedControlWins} / {p.AlternativeControlWins} |");
        return text.ToString();
    }
}
