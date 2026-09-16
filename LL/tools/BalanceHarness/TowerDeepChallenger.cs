using Domain.Models.Combat;

namespace BalanceHarness;

public sealed record TowerDeepQuality(string FamilyOutcome, int ViablePrimaries, int RecoveredPrimaries,
    IReadOnlyDictionary<string, RateEstimate> Rates, IReadOnlyDictionary<string, TowerSearchBenchmarkPair> VersusControl,
    IReadOnlyList<string> SupportedImprovements, string Scope);

/// <summary>One independent deep method with separate screening and a complete, bounded confirmation family.</summary>
public static class TowerDeepChallenger
{
    public const string Version = "independent-deep-challenger-v1";
    public const int Candidates = 1536, Attempts = 16384, Restarts = 3, Width = 32,
        DiscoverySamples = 8, ScreenSamples = 64, ConfirmationSamples = 256, Controls = 2, FamilyCapacity = 256,
        DiscoveryFights = 36864, ScreenFights = 6144, MaximumFights = 108544, FreshValues = 331;

    public static TowerBattleInput ControlInput(TowerBattleRunner runner, TowerScenario recipe, int reservedSeed, TowerSettings settings)
    {
        if (recipe.Seeds.Count != 0) throw new InvalidDataException("Control recipes must remain seed-free; declare the existing seed only for preparation.");
        return runner.CreateInput(recipe with { Seeds = [reservedSeed] }, reservedSeed, settings.Threat, settings.CheckpointIntervalTicks);
    }

    public static void Validate(TowerBossDiscoveryDefinition d)
    {
        var cost = TowerBossDiscovery.Validate(d);
        if (d.Mode != TowerBossDiscovery.Independent || d.Generation.PolicyVersion != Version
            || d.Generation.Seeds.Count != Restarts || d.Generation.CandidatesPerArm != Candidates
            || d.Generation.MaximumAttemptsPerArm != Attempts || d.Contexts.Count != 1 || d.References.Count != Controls
            || d.MaximumBattles != MaximumFights || cost.Discovery != DiscoveryFights
            || d.Stages.DiagnosticCandidates != 0 || d.Stages.ReplayReserve != 0
            || d.Stages.Schedules.Values.Any(s => s.Discovery.Count != DiscoverySamples || s.Selection.Count != ScreenSamples
                || s.Confirmation.Count != ConfirmationSamples || s.Diagnostics.Count != 0 || s.Feedback is { Count: > 0 })
            || d.Generation.Seeds.Intersect(d.ExcludedCombatSeeds.Concat(d.Stages.Schedules.Values
                .SelectMany(s => s.Discovery.Concat(s.Selection).Concat(s.Confirmation)))).Any())
            throw new InvalidDataException("Changed deep challenger allocation or disjoint fixed schedules.");
    }

    public static TowerFeedbackShortlist Freeze(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery)
    {
        Validate(d);
        var g = discovery.Generation ?? throw new InvalidDataException("Missing generation.");
        if (discovery.Status != "Complete" || g.Version != Version || g.Status != "Complete"
            || discovery.ActualBattles != DiscoveryFights || g.Arms.Count != Restarts
            || !g.Arms.Select(a => a.Seed).SequenceEqual(d.Generation.Seeds)
            || g.Arms.Any(a => a.Method != TowerSearchAllocation.Deep || a.StopReason != "CandidateBudgetReached"
                || a.Evaluations.Count != Candidates || a.Proposals.Count > Attempts
                || a.Proposals.Count(p => p.Result == "evaluated") != Candidates
                || a.Evaluations.Select(e => e.Id).Distinct().Count() != Candidates))
            throw new InvalidDataException("Incomplete independent deep search.");
        TowerBossDiscovery.ValidateProvenance(d, g.Arms.SelectMany(a => a.Proposals).Select(p => p.Provenance).ToArray());
        var arms = g.Arms.Select(a => {
            var parties = a.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            var ranked = TowerBossGeneration.Rank(a.Evaluations).Take(Width).Select((e, i) => {
                var p = parties[e.Id]; var scenario = TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, p.Party!, []);
                return new TowerRescreenCandidate(TowerFeedbackBenchmark.Id(scenario), e.Id, p.Provenance.Id, i + 1, scenario);
            }).ToArray();
            return new TowerFeedbackArm(a.Method, a.Seed, ranked);
        }).ToArray();
        return new(1, Version, HarnessJson.Hash(d), HarnessJson.Hash(discovery), arms.Select(a =>
            new TowerSearchArm(a.Method, a.Seed, a.Candidates[0].Id, a.Candidates[1].Id)).ToArray(), arms);
    }

    public static TowerBalanceDefinition Balance(TowerBossDiscoveryDefinition d, IReadOnlyList<TowerSearchSelected> family, bool confirmation)
    {
        Validate(d);
        if (family.Count is < 1 or > FamilyCapacity || family.Select(f => f.Id).Distinct().Count() != family.Count)
            throw new InvalidDataException("Invalid bounded family; no truncation is allowed.");
        var s = d.Stages.Schedules.Values.Single(); var seeds = confirmation ? s.Confirmation : s.Selection;
        var c = d.Contexts.Single();
        var cohort = new TowerBalanceCohort("fixed-cohort", d.Budget, d.RequiredPartySize, c.Id,
            TowerBossDiscovery.EquipmentBudgetHash(c.CharacterTemplates), d.BudgetPurpose);
        var result = new TowerBalanceDefinition(1, confirmation ? "deep-confirmation" : "deep-screen", TowerBalanceEvaluator.IntervalPolicy,
            d.ContentHashes, d.SettingsHash, d.ExecutionHash, [cohort], family.Select(f => new TowerBalanceCellDefinition(f.Id,
                cohort.Id, f.Sources.Any(o => o.Method == "saved-control") ? "reference" : "generated",
                f.Scenario with { Seeds = seeds }, seeds.Count)).ToArray(),
            d.ExcludedCombatSeeds.Concat(d.Generation.Seeds).Concat(s.Discovery)
                .Concat(confirmation ? s.Selection : s.Confirmation).Distinct().Order().ToArray(), family.Count * seeds.Count);
        TowerBalanceEvaluator.Validate(result); return result;
    }

    public static TowerFeedbackComparison Select(TowerBossDiscoveryDefinition d, BossDiscoveryRunReport discovery,
        TowerFeedbackShortlist shortlist, IReadOnlyList<TowerFeedbackEvidence> screens, string controlId)
    {
        TowerPortfolioConfirmation.Equal(Freeze(d, discovery), shortlist, "frozen deep shortlist");
        if (screens.Count != Restarts || !screens.Select(s => (s.Method, s.Seed)).SequenceEqual(shortlist.Arms.Select(a => (a.Method, a.Seed)))
            || d.References.All(r => r.Id != controlId)) throw new InvalidDataException("Incomplete or reordered screens/control.");
        var family = new Dictionary<string, TowerSearchSelected>(StringComparer.Ordinal);
        void Add(TowerScenario scenario, TowerSearchOrigin origin)
        {
            var id = TowerFeedbackBenchmark.Id(scenario);
            if (!family.TryGetValue(id, out var row)) family.Add(id, row = new(id, scenario with { Seeds = [] }, []));
            if (TowerBossDiscovery.RecipeHash(row.Scenario.Party) != TowerBossDiscovery.RecipeHash(scenario.Party))
                throw new InvalidDataException("Recipe identity collision.");
            if (!row.Sources.Contains(origin)) row.Sources.Add(origin);
        }
        foreach (var c in d.References) Add(c.Scenario, new("saved-control", null, null, null, c.Id));
        var nominations = new List<TowerFeedbackNomination>();
        foreach (var (arm, index) in shortlist.Arms.Select((a, i) => (a, i)))
        {
            var evidence = screens[index].Cells;
            var screenDefinition = Balance(d, arm.Candidates.Select(c => new TowerSearchSelected(c.Id, c.Scenario, [])).ToArray(), false);
            TowerFeedbackBenchmark.RequireEvidence(screenDefinition, evidence);
            var byId = evidence.ToDictionary(e => e.CellId);
            var ranking = arm.Candidates.OrderByDescending(c => byId[c.Id].Trials.Count(t => t.Outcome == BattleOutcome.Victory))
                .ThenBy(c => c.OriginalRank).ToArray();
            nominations.Add(new(arm.Method, arm.Seed, ranking[0].Id, ranking[1].Id, HarnessJson.Hash(evidence)));
            foreach (var c in arm.Candidates.Take(2).Concat(ranking.Take(2)).Concat(arm.Candidates.Where(c =>
                byId[c.Id].Trials.Count(t => t.Outcome == BattleOutcome.Victory) * 2 > ScreenSamples)))
                Add(c.Scenario, new(arm.Method + "-screen", arm.Seed, c.OriginalRank, c.PartyId, null));
            var generated = discovery.Generation!.Arms[index];
            var proposals = generated.Proposals.Where(p => p.Result == "evaluated").ToDictionary(p => p.Party!.Id);
            foreach (var (e, rank) in TowerBossGeneration.Rank(generated.Evaluations).Select((e, i) => (e, i + 1)))
                if (e.Fitness.WorstContextWinRate > .5)
                    Add(TowerBossDiscovery.Scenario(d, d.Contexts[0].Id, proposals[e.Id].Party!, []), new(arm.Method, arm.Seed, rank, e.Id, null));
        }
        return new(family.Count > FamilyCapacity ? "CapacityExceeded" : "Ready", shortlist.OriginalArms,
            nominations, family.Values.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray(), controlId,
            d.References.Single(r => r.Id != controlId).Id);
    }

    // Alpha .025 for rates and .025 for the complete family of differences against one prespecified control.
    public static TowerSearchBenchmarkPair Pair(IReadOnlyList<TowerBalanceTrial> left, IReadOnlyList<TowerBalanceTrial> right, int comparisons)
    {
        if (comparisons is < 1 or >= FamilyCapacity || left.Count != ConfirmationSamples || right.Count != ConfirmationSamples
            || !left.Select(t => t.Seed).SequenceEqual(right.Select(t => t.Seed)) || left.Select(t => t.Seed).Distinct().Count() != left.Count
            || left.Concat(right).Any(t => !Enum.IsDefined(t.Outcome))) throw new InvalidDataException("Incomplete paired evidence.");
        var gains = left.Zip(right).Count(p => p.First.Outcome == BattleOutcome.Victory && p.Second.Outcome != BattleOutcome.Victory);
        var losses = left.Zip(right).Count(p => p.Second.Outcome == BattleOutcome.Victory && p.First.Outcome != BattleOutcome.Victory);
        var g = TowerCompleteFamily.Interval(gains, left.Count, 2 * comparisons);
        var l = TowerCompleteFamily.Interval(losses, left.Count, 2 * comparisons);
        return new(left.Count, gains, losses, (gains - losses) / (double)left.Count, g.Lower - l.Upper, g.Upper - l.Lower);
    }

    public static TowerDeepQuality Assess(TowerBossDiscoveryDefinition d, TowerFeedbackComparison selection, IReadOnlyList<TowerBalanceEvidence> evidence)
    {
        if (selection.Status != "Ready") throw new InvalidDataException("No assessment of a truncated or stopped family.");
        TowerFeedbackBenchmark.RequireEvidence(Balance(d, selection.Family, true), evidence);
        var byId = evidence.ToDictionary(e => e.CellId);
        var rates = evidence.ToDictionary(e => e.CellId, e => TowerCompleteFamily.Interval(e.Trials.Count(t => t.Outcome == BattleOutcome.Victory), ConfirmationSamples, evidence.Count));
        var pairs = evidence.Where(e => e.CellId != selection.AnchorId).ToDictionary(e => e.CellId,
            e => Pair(e.Trials, byId[selection.AnchorId].Trials, evidence.Count - 1));
        bool Recovered(string id) => rates[id].Lower >= .1 && (id == selection.AnchorId || pairs[id].Lower >= -.1);
        var primaries = selection.RescreenedArms;
        var outcome = rates.Values.Any(r => r.Rate > .5) || rates.Values.All(r => r.Upper < .1) ? "Fail"
            : rates.Values.All(r => r.Upper <= .5) && rates.Values.Any(r => r.Lower >= .1) ? "Pass" : "Inconclusive";
        return new(outcome, primaries.Count(p => rates[p.Primary].Lower >= .1), primaries.Count(p => Recovered(p.Primary)), rates, pairs,
            selection.Family.Where(f => f.Sources.Any(o => o.Method != "saved-control") && pairs.TryGetValue(f.Id, out var p) && p.Lower > 0).Select(f => f.Id).ToArray(),
            "New deep-search scope only. Recovery requires at least 2/3 frozen primaries viable and within ten points of the fixed strongest control. "
            + "No update to historical portfolio reliability, automatic adoption, full generated-family or global-optimum claim.");
    }
}
