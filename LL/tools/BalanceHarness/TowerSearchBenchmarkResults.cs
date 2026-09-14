using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerSearchOrigin(string Method, int? Seed, int? DiscoveryRank, string? PartyId, string? ControlId);
public sealed record TowerSearchSelected(string Id, TowerScenario Scenario, List<TowerSearchOrigin> Sources);
public sealed record TowerSearchArm(string Method, int Seed, string Primary, string Secondary);
public sealed record TowerSearchBenchmarkSelection(IReadOnlyList<TowerSearchArm> Arms, IReadOnlyList<TowerSearchSelected> Family);
public sealed record TowerSearchScreenArm(string Method, int Seed, int Wins, int AnchorWins, bool Qualifies);
public sealed record TowerSearchScreen(bool ContinueValidation, IReadOnlyList<TowerSearchScreenArm> Arms,
    string Scope = "Engineering screen only; primaries remain frozen; no confidence or reliability pass.");
public sealed record TowerSearchBenchmarkPair(int Pairs, int Gains, int Losses, double Difference, double Lower, double Upper);
public sealed record TowerSearchPrimary(string Method, int Seed, string Id, int Wins, RateEstimate Rate,
    TowerSearchBenchmarkPair Comparator, TowerSearchBenchmarkPair Anchor, bool Viable, bool Improved, bool AnchorRecovered, bool Pass);
public sealed record TowerSearchMethod(string Method, int PassingRestarts, string Reliability);
public sealed record TowerSearchQuality(IReadOnlyList<TowerSearchPrimary> Primaries, IReadOnlyList<TowerSearchMethod> Methods,
    IReadOnlyDictionary<string, RateEstimate> Rates, string JointFamilyAssessment,
    string Scope = "Joint alpha .05: .025 for the full rate family, .025 for 12 paired comparisons (24 discordance intervals). Approximate Wilson coverage. No pooling or global optimality claim.");

public static partial class TowerSearchBenchmark
{
    internal static TowerSearchBenchmarkSelection Select(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery)
    {
        if (discovery.Status != "Complete" || discovery.Generation?.Status != "Complete"
            || discovery.Generation.Arms.Count != d.Generation.Methods.Count * d.Generation.Seeds.Count) throw new InvalidDataException("Complete declared discovery is required before nomination.");
        var byRecipe = new Dictionary<string, TowerSearchSelected>(StringComparer.Ordinal);
        string Add(TowerScenario scenario, TowerSearchOrigin source)
        {
            var key = TowerBossDiscovery.RecipeHash(scenario.Party);
            if (!byRecipe.TryGetValue(key, out var row))
                byRecipe.Add(key, row = new("team-" + key[..32], scenario with { Seeds = [] }, []));
            row.Sources.Add(source); return row.Id;
        }
        var arms = new List<TowerSearchArm>();
        foreach (var arm in discovery.Generation.Arms)
        {
            if (arm.StopReason != "CandidateBudgetReached"
                || arm.Evaluations.Count != TowerBossGeneration.CandidateBudget(d.Generation, arm.Method))
                throw new InvalidDataException("An exhausted or partial search cannot nominate finalists.");
            var parties = arm.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id, p => p.Party!);
            var ranked = TowerBossGeneration.Rank(arm.Evaluations).Take(2).ToArray();
            var ids = ranked.Select((row, rank) => Add(TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, parties[row.Id], []),
                new(arm.Method, arm.Seed, rank + 1, row.Id, null))).ToArray();
            arms.Add(new(arm.Method, arm.Seed, ids[0], ids[1]));
        }
        foreach (var reference in d.References)
            Add(reference.Scenario, new("saved-control", null, null, null, reference.Id));
        var family = byRecipe.Values.OrderBy(p => p.Id, StringComparer.Ordinal).ToArray();
        if (family.Length > 24 || family.Select(p => p.Id).Distinct().Count() != family.Length)
            throw new InvalidDataException("Selected family exceeds capacity or contains colliding identities.");
        return new(arms, family);
    }

    internal static string Anchor(TowerSearchBenchmarkSelection selection, string anchorId) =>
        selection.Family.Single(p => p.Sources.Any(s => s.ControlId == anchorId)).Id;

    internal static TowerBalanceDefinition FamilyDefinition(TowerBossDiscoveryDefinition d, TowerSearchBenchmarkSelection selection, bool validation)
    {
        var schedule = d.Stages.Schedules.Single().Value;
        var seeds = validation ? schedule.Confirmation : schedule.Selection;
        var excluded = d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(schedule.Discovery)
            .Concat(validation ? schedule.Selection : schedule.Confirmation).Distinct().Order().ToArray();
        var context = d.Contexts[0];
        var cohort = new TowerBalanceCohort("benchmark-cohort", d.Budget, d.RequiredPartySize, context.Id,
            TowerBossDiscovery.EquipmentBudgetHash(context.CharacterTemplates), d.BudgetPurpose);
        var result = new TowerBalanceDefinition(1, (Composition(selection) ? "loadout-composition" : "depth-behavior") + (validation ? "-validation" : "-screen"),
            TowerBalanceEvaluator.IntervalPolicy, d.ContentHashes, d.SettingsHash, d.ExecutionHash, [cohort],
            selection.Family.Select(p => new TowerBalanceCellDefinition(p.Id, cohort.Id,
                p.Sources.Any(s => s.Method != "saved-control") ? "generated" : "reference",
                p.Scenario with { Seeds = seeds }, seeds.Count)).ToArray(), excluded, selection.Family.Count * seeds.Count);
        TowerBalanceEvaluator.Validate(result); return result;
    }

    internal static TowerSearchScreen Screen(TowerSearchBenchmarkSelection selection, string anchorId, TowerBalanceReport report)
    {
        var methods = Methods(selection);
        var cells = report.Cells.ToDictionary(c => c.Id);
        if (!cells.Keys.Order().SequenceEqual(selection.Family.Select(p => p.Id).Order())
            || cells.Values.Any(c => c.Valid != 64 || c.Issues.Count != 0))
            throw new InvalidDataException("Screen requires every frozen cell on all 64 trials.");
        var anchor = cells[Anchor(selection, anchorId)].Wins;
        var arms = selection.Arms.Where(a => a.Method != methods[0]).Select(a => {
            var wins = cells[a.Primary].Wins;
            // Integer arithmetic avoids an accidental boundary change through floating-point rounding.
            return new TowerSearchScreenArm(a.Method, a.Seed, wins, anchor, wins >= 7 && 10 * (wins - anchor) >= -64);
        }).ToArray();
        return new(Composition(selection) || arms.GroupBy(a => a.Method).Any(g => g.Count(a => a.Qualifies) >= 2), arms);
    }

    internal static TowerSearchBenchmarkPair Pair(IReadOnlyList<TowerBalanceTrial> left, IReadOnlyList<TowerBalanceTrial> right, int comparisonCount = 12)
    {
        if (comparisonCount is not (6 or 12) || left.Count != 256 || !left.Select(t => t.Seed).SequenceEqual(right.Select(t => t.Seed))
            || left.Select(t => t.Seed).Distinct().Count() != left.Count
            || left.Concat(right).Any(t => !Enum.IsDefined(t.Outcome)))
            throw new InvalidDataException("Paired reliability requires 256 matching unique validation seeds.");
        var gained = left.Zip(right).Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
        var lost = left.Zip(right).Count(p => p.First.Outcome != BattleOutcome.Victory && p.Second.Outcome == BattleOutcome.Victory);
        // .025 / (comparisonCount * 2 discordant probabilities), expressed as a .05 family multiplier.
        var g = TowerBalanceEvaluator.Wilson(gained, left.Count, 4 * comparisonCount)!;
        var l = TowerBalanceEvaluator.Wilson(lost, left.Count, 4 * comparisonCount)!;
        return new(left.Count, gained, lost, (gained - lost) / (double)left.Count, g.Lower - l.Upper, g.Upper - l.Lower);
    }

    internal static TowerSearchQuality Quality(TowerSearchBenchmarkSelection selection, string anchorId, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        var methods = Methods(selection); var comparisonCount = Composition(selection) ? 6 : 12;
        var cells = evidence.ToDictionary(e => e.CellId);
        if (!cells.Keys.Order().SequenceEqual(selection.Family.Select(p => p.Id).Order())
            || cells.Values.Any(e => e.Status != "Complete" || e.Error is not null || e.Trials.Count != 256))
            throw new InvalidDataException("Quality requires the entire untouched validation family.");
        var rates = cells.ToDictionary(p => p.Key, p => TowerBalanceEvaluator.Wilson(
            p.Value.Trials.Count(t => t.Outcome == BattleOutcome.Victory), 256, 2 * cells.Count)!);
        var anchor = cells[Anchor(selection, anchorId)].Trials;
        var primaries = selection.Arms.Where(a => a.Method != methods[0]).Select(a => {
            var methodIndex = Array.IndexOf(methods, a.Method);
            var comparator = selection.Arms.Single(b => b.Seed == a.Seed && b.Method == methods[methodIndex - 1]);
            var row = cells[a.Primary]; var rate = rates[a.Primary];
            var paired = Pair(row.Trials, cells[comparator.Primary].Trials, comparisonCount); var recovery = Pair(row.Trials, anchor, comparisonCount);
            var viable = rate.Lower >= .10; var improved = paired.Lower > 0; var recovered = recovery.Lower >= -.10;
            return new TowerSearchPrimary(a.Method, a.Seed, a.Primary, row.Trials.Count(t => t.Outcome == BattleOutcome.Victory),
                rate, paired, recovery, viable, improved, recovered, viable && improved && recovered);
        }).ToArray();
        var methodResults = primaries.GroupBy(p => p.Method).Select(g => new TowerSearchMethod(g.Key, g.Count(p => p.Pass),
            g.Count(p => p.Pass) >= 2 ? "Pass" : "Fail")).ToArray();
        var family = rates.Values.Any(r => r.Rate > .5) || rates.Values.All(r => r.Rate < .1) ? "Fail"
            : rates.Values.All(r => r.Upper <= .5) && rates.Values.Any(r => r.Lower >= .1) ? "Pass" : "Inconclusive";
        return new(primaries, methodResults, rates, family,
            $"Joint alpha .05: .025 for the full rate family, .025 for {comparisonCount} paired comparisons ({comparisonCount * 2} discordance intervals). Approximate Wilson coverage. No pooling or global optimality claim.");
    }
}
